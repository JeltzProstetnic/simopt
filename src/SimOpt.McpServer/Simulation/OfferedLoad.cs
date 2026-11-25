using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SimOpt.McpServer.Models;

namespace SimOpt.McpServer.Simulation
{
    /// <summary>The load offered to one station, before any run.</summary>
    /// <param name="ArrivalRate">Items reaching the station per unit of time.</param>
    /// <param name="Load">ρ = ArrivalRate × MeanService / Servers.</param>
    /// <param name="EstimatedVia">Shortest-queue routers upstream whose split was estimated as equal; empty when exact.</param>
    /// <param name="FiniteQueue">A queue feeding the station that holds a limited number of items, or null.</param>
    public sealed record StationLoad(string Station, double ArrivalRate, double MeanService, int Servers, double Load,
                                     IReadOnlyList<string> EstimatedVia, string? FiniteQueue);

    /// <summary>
    /// SIM-66 step 3 (D5): the offered load of every station of a topology that builds, from the
    /// traffic equations <c>λ = γ + λP</c>, solved exactly, so a rework loop is no harder than a line.
    /// </summary>
    /// <remarks>
    /// <para>
    /// γ is each source's 1 / mean interarrival time. In P, a probabilistic router splits by its
    /// connection weights (equally without), a shortest-queue router equally, which is an estimate and
    /// is reported as one, a queue passes its items to the stations it feeds in proportion to their
    /// service capacity (servers / mean service), and a source, station or delay passes everything on.
    /// A sink, and any node with nothing leaving it, absorbs.
    /// </para>
    /// <para>
    /// The load is the demand reaching a station if everything upstream keeps up; a station behind a
    /// saturated one is offered less in a run. The mean service time is the distribution's
    /// <c>Mean</c>, shift included (SIM-102). Nodes in a loop with no way out (TopologyLint's
    /// LOOP_WITHOUT_EXIT) carry unbounded flow and are not reported here.
    /// </para>
    /// </remarks>
    public static class OfferedLoad
    {
        /// <summary>A load from which on a station is reported as near saturation.</summary>
        public const double Headroom = 0.9;

        /// <summary>
        /// The load of every station. The topology must build (<see cref="ModelRegistry.TryBuild"/>
        /// without errors): its parameters are read without checking them again.
        /// </summary>
        public static IReadOnlyList<StationLoad> Compute(TopologyDefinition topology)
        {
            var nodes = topology.Nodes.ToDictionary(n => n.Id, n => n);
            var type = nodes.ToDictionary(n => n.Key, n => SchemaCatalog.Canonical(n.Value.Type)!);
            List<string> order = topology.Nodes.Select(n => n.Id).ToList();
            var index = order.Select((id, i) => (id, i)).ToDictionary(p => p.id, p => p.i);
            int n = order.Count;

            var outs = order.ToDictionary(id => id, _ => new List<ConnectionDefinition>());
            var wired = new HashSet<(string, string)>();
            foreach (ConnectionDefinition c in topology.Connections)
                if (wired.Add((c.From, c.To))) outs[c.From].Add(c);

            var servers = order.Where(id => type[id] == "station").ToDictionary(id => id, id => Servers(nodes[id]));
            var service = order.Where(id => type[id] == "station")
                               .ToDictionary(id => id, id => MeanTime(nodes[id], "service", "service_time"));

            // routing matrix, row = from
            var p = new double[n, n];
            foreach (string id in order)
            {
                List<ConnectionDefinition> leaving = outs[id];
                if (leaving.Count == 0 || type[id] == "sink") continue;
                IEnumerable<(string To, double Share)> shares;
                if (type[id] == "queue")
                {
                    List<string> fed = leaving.Select(c => c.To).Where(t => type[t] == "station").ToList();
                    double capacity = fed.Sum(s => servers[s] / service[s]);
                    shares = fed.Select(s => (s, servers[s] / service[s] / capacity));
                }
                else if (type[id] == "router" && leaving.All(c => c.Weight.HasValue))
                {
                    double total = leaving.Sum(c => c.Weight!.Value);
                    shares = leaving.Select(c => (c.To, c.Weight!.Value / total));
                }
                else
                {
                    shares = leaving.Select(c => (c.To, 1d / leaving.Count));
                }
                foreach (var (to, share) in shares) p[index[id], index[to]] += share;
            }

            // a node from which no path leads to one that absorbs carries unbounded flow: leave it out
            var absorbing = order.Where(id => Enumerable.Range(0, n).All(j => p[index[id], j] == 0d)).ToList();
            HashSet<string> leadsOut = Walk(absorbing, order.ToDictionary(id => id,
                id => order.Where(from => p[index[from], index[id]] > 0d).ToList()));
            foreach (string id in order.Where(id => !leadsOut.Contains(id)))
                for (int j = 0; j < n; j++) p[index[id], j] = 0d;

            var gamma = new double[n];
            foreach (string id in order.Where(id => type[id] == "source"))
                gamma[index[id]] = 1d / MeanTime(nodes[id], "arrival", "mean_interval");

            double[] lambda = SolveTraffic(p, gamma);

            // shortest-queue routers whose equal split an estimate downstream rests on
            var downstream = order.ToDictionary(id => id, id => outs[id].Select(c => c.To).ToList());
            var estimatedVia = order.ToDictionary(id => id, _ => new List<string>());
            foreach (string r in order.Where(id => type[id] == "router" && Text(nodes[id], "policy") == "shortest_queue"))
                foreach (string reached in Walk(downstream[r], downstream))
                    estimatedVia[reached].Add(r);

            var loads = new List<StationLoad>();
            foreach (string s in order.Where(id => type[id] == "station" && leadsOut.Contains(id)))
            {
                double rate = lambda[index[s]];
                string? finite = order.FirstOrDefault(q => type[q] == "queue" && outs[q].Any(c => c.To == s)
                                                           && nodes[q].Params.ContainsKey("capacity"));
                loads.Add(new StationLoad(s, rate, service[s], servers[s], rate * service[s] / servers[s],
                    estimatedVia[s], finite));
            }
            return loads;
        }

        /// <summary>Adds an <c>OFFERED_LOAD</c> info per station, and a warning where one is (nearly) saturated.</summary>
        public static void Check(TopologyDefinition topology, ValidationReport report)
        {
            foreach (StationLoad load in Compute(topology))
            {
                string estimate = load.EstimatedVia.Count == 0 ? "" :
                    $"; an estimate: {string.Join(", ", load.EstimatedVia.Select(r => $"'{r}'"))} assumed to split equally";
                report.Info.Add(new ValidationIssue("OFFERED_LOAD",
                    $"Station '{load.Station}': offered load {Fmt(load.Load)} ({Fmt(load.ArrivalRate)} arrivals per time unit, " +
                    $"mean service {Fmt(load.MeanService)}, {load.Servers} server{(load.Servers == 1 ? "" : "s")}{estimate}).",
                    load.Station, Value: load.Load));

                int enough = (int)Math.Floor(load.ArrivalRate * load.MeanService / Headroom) + 1;
                string fix = $"Give '{load.Station}' {enough} servers (offered load {Fmt(load.ArrivalRate * load.MeanService / enough)}), " +
                             "or shorten its service time.";
                if (load.Load >= 1d)
                    report.Warnings.Add(new ValidationIssue("SATURATED", load.FiniteQueue == null
                            ? $"Station '{load.Station}' is offered load {Fmt(load.Load)}: more work arrives than it can do, so its queue grows without bound."
                            : $"Station '{load.Station}' is offered load {Fmt(load.Load)}: more work arrives than it can do, so queue " +
                              $"'{load.FiniteQueue}' fills and items are refused there.",
                        load.Station, fix, load.Load));
                else if (load.Load >= Headroom)
                    report.Warnings.Add(new ValidationIssue("NEAR_SATURATION",
                        $"Station '{load.Station}' is offered load {Fmt(load.Load)}: expect long queues, a long warm-up and wide intervals.",
                        load.Station, fix, load.Load));
            }
        }

        /// <summary>Solves (I − Pᵀ) λ = γ by Gaussian elimination with partial pivoting.</summary>
        private static double[] SolveTraffic(double[,] p, double[] gamma)
        {
            int n = gamma.Length;
            var a = new double[n, n + 1];
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++) a[i, j] = (i == j ? 1d : 0d) - p[j, i];
                a[i, n] = gamma[i];
            }
            for (int col = 0; col < n; col++)
            {
                int pivot = col;
                for (int r = col + 1; r < n; r++)
                    if (Math.Abs(a[r, col]) > Math.Abs(a[pivot, col])) pivot = r;
                if (Math.Abs(a[pivot, col]) < 1e-12)
                    throw new InvalidOperationException("The traffic equations are singular; a loop without exit was not left out.");
                if (pivot != col)
                    for (int j = 0; j <= n; j++) (a[col, j], a[pivot, j]) = (a[pivot, j], a[col, j]);
                for (int r = 0; r < n; r++)
                {
                    if (r == col || a[r, col] == 0d) continue;
                    double f = a[r, col] / a[col, col];
                    for (int j = col; j <= n; j++) a[r, j] -= f * a[col, j];
                }
            }
            var lambda = new double[n];
            for (int i = 0; i < n; i++) lambda[i] = a[i, n] / a[i, i];
            return lambda;
        }

        /// <summary>The mean of a time given as a distribution object or as the older exponential mean (default 1).</summary>
        private static double MeanTime(NodeDefinition node, string distName, string meanName)
        {
            if (node.Params.TryGetValue(distName, out ParamValue? given) && given?.Distribution != null)
                return DistributionFactory.Create(given.Distribution, $"node '{node.Id}', parameter '{distName}'").Mean;
            return node.Params.TryGetValue(meanName, out ParamValue? mean) && mean?.Number is double m ? m : 1d;
        }

        private static int Servers(NodeDefinition node) =>
            node.Params.TryGetValue("servers", out ParamValue? c) && c?.Number is double count ? (int)count : 1;

        private static string? Text(NodeDefinition node, string name) =>
            node.Params.TryGetValue(name, out ParamValue? v) ? v?.Text?.Trim().ToLowerInvariant() : null;

        private static HashSet<string> Walk(IEnumerable<string> start, IReadOnlyDictionary<string, List<string>> next)
        {
            var seen = new HashSet<string>(start);
            var open = new Queue<string>(seen);
            while (open.Count > 0)
                foreach (string m in next[open.Dequeue()])
                    if (seen.Add(m)) open.Enqueue(m);
            return seen;
        }

        private static string Fmt(double value) => value.ToString("G4", CultureInfo.InvariantCulture);
    }
}
