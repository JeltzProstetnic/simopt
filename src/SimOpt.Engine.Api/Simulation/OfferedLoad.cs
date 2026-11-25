using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SimOpt.Engine.Api.Models;

namespace SimOpt.Engine.Api.Simulation
{
    /// <summary>The load offered to one station, before any run.</summary>
    /// <param name="ArrivalRate">Items reaching the station per unit of time.</param>
    /// <param name="Load">ρ = ArrivalRate × MeanService / Servers.</param>
    /// <param name="EstimatedVia">Shortest-queue routers upstream whose split was estimated as equal; empty when exact.</param>
    /// <param name="FiniteQueue">A queue feeding the station that holds a limited number of items, or null.</param>
    public sealed record StationLoad(string Station, double ArrivalRate, double MeanService, int Servers, double Load,
                                     IReadOnlyList<string> EstimatedVia, string? FiniteQueue);

    /// <summary>SIM-126 schema, slice 5: the load offered to one resource, before any run.</summary>
    /// <param name="Units">Units busy on average if every item that seizes one gets it.</param>
    /// <param name="LowerBound">A unit is held through a wait (for stock, another resource), counted as nothing.</param>
    /// <param name="Patient">The processes whose seize of it has a patience.</param>
    public sealed record ResourceLoad(string Resource, double Units, int Capacity, double Load, bool LowerBound,
                                      IReadOnlyList<string> Users, IReadOnlyList<string> Patient);

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
        public static IReadOnlyList<StationLoad> Compute(TopologyDefinition topology) => Compute(topology, out _);

        /// <param name="rates">Items reaching each node per unit of time; 0 for a node in a loop without exit.</param>
        private static IReadOnlyList<StationLoad> Compute(TopologyDefinition topology, out Dictionary<string, double> rates)
        {
            var nodes = topology.Nodes.ToDictionary(n => n.Id, n => n);
            var type = nodes.ToDictionary(n => n.Key, n => SchemaCatalog.Canonical(n.Value.Type)!);
            List<string> order = topology.Nodes.Select(n => n.Id).ToList();
            var index = order.Select((id, i) => (id, i)).ToDictionary(p => p.id, p => p.i);
            int n = order.Count;

            var outs = order.ToDictionary(id => id, _ => new List<ConnectionDefinition>());
            var wired = new HashSet<(string, string)>();
            // SIM-126: the share of items that give up is not known before a run, so a process's flow is
            // counted through its normal exit; loads behind an outlet are not reached by it
            foreach (ConnectionDefinition c in topology.Connections)
                if (c.Outlet == null && wired.Add((c.From, c.To))) outs[c.From].Add(c);

            var servers = order.Where(id => type[id] == "station").ToDictionary(id => id, id => Servers(nodes[id]));
            // SIM-110: a batch station serves `batch` items per service time, so an item's share is a fraction
            var service = order.Where(id => type[id] == "station")
                               .ToDictionary(id => id, id => MeanTime(nodes[id], "service", "service_time") / Batch(nodes[id]));

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
            rates = order.ToDictionary(id => id, id => leadsOut.Contains(id) ? lambda[index[id]] : 0d);

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

        /// <summary>
        /// SIM-126 schema, slice 5: the load of every resource, the units busy on average over its capacity. A
        /// fed process contributes its arrival rate times the mean time a unit is held, the mean durations of
        /// the delay steps from the seize to its release or the node's end; a process nothing feeds holds a
        /// unit for its share of each cycle (the hold over the sum of its delays). Waits while a unit is held
        /// (for stock, for another resource) count as nothing, so the figure is then a lower bound.
        /// </summary>
        public static IReadOnlyList<ResourceLoad> ComputeResources(TopologyDefinition topology)
        {
            Compute(topology, out Dictionary<string, double> rates);
            var units = new Dictionary<string, double>();
            var lower = new HashSet<string>();
            var users = new Dictionary<string, List<string>>();
            var patient = new Dictionary<string, List<string>>();
            foreach (ProcessView process in Processes(topology))
            {
                double rate = process.Fed ? rates.GetValueOrDefault(process.Id) : (process.Cycle > 0d ? 1d / process.Cycle : 0d);
                var released = new HashSet<int>();
                for (int i = 0; i < process.Count; i++)
                {
                    if (process.Type(i) != "seize" || process.Text(i, "resource") is not string resource) continue;
                    int end = process.Count;
                    for (int j = i + 1; j < process.Count; j++)
                        if (process.Type(j) == "release" && process.Text(j, "resource") == resource && released.Add(j)) { end = j; break; }
                    double hold = 0d;
                    for (int k = i + 1; k < end; k++)
                    {
                        if (process.Type(k) == "delay") hold += process.Mean(k, "duration");
                        else if (process.Type(k) != "release") lower.Add(resource);
                    }
                    units[resource] = units.GetValueOrDefault(resource) + rate * hold;
                    Add(users, resource, process.Id);
                    if (process.Steps[i].Fields.ContainsKey("patience")) Add(patient, resource, process.Id);
                }
            }

            var loads = new List<ResourceLoad>();
            foreach (NodeDefinition node in topology.Nodes.Where(n => SchemaCatalog.Canonical(n.Type) == "resource"))
            {
                int capacity = node.Params.TryGetValue("capacity", out ParamValue? c) && c?.Number is double given ? (int)given : 1;
                double busy = units.GetValueOrDefault(node.Id);
                loads.Add(new ResourceLoad(node.Id, busy, capacity, busy / capacity, lower.Contains(node.Id),
                    users.GetValueOrDefault(node.Id) ?? new List<string>(), patient.GetValueOrDefault(node.Id) ?? new List<string>()));
            }
            return loads;

            static void Add(Dictionary<string, List<string>> into, string key, string value)
            {
                if (!into.TryGetValue(key, out List<string>? list)) into[key] = list = new List<string>();
                if (!list.Contains(value)) list.Add(value);
            }
        }

        /// <summary>A process node's steps as the offered load reads them; the topology has built.</summary>
        private sealed class ProcessView
        {
            public ProcessView(string id, IReadOnlyList<StepDefinition> steps, bool fed)
            {
                Id = id;
                Steps = steps;
                Fed = fed;
                Ids = SchemaCatalog.StepIds(steps);
                Cycle = Enumerable.Range(0, steps.Count).Where(i => Type(i) == "delay").Sum(i => Mean(i, "duration"));
            }

            public string Id { get; }
            public IReadOnlyList<StepDefinition> Steps { get; }
            public IReadOnlyList<string> Ids { get; }
            public bool Fed { get; }
            public int Count => Steps.Count;

            /// <summary>The sum of the mean durations of its delay steps.</summary>
            public double Cycle { get; }

            public string Type(int i) => (Steps[i].Type ?? string.Empty).Trim().ToLowerInvariant();

            public string? Text(int i, string field) =>
                Steps[i].Fields.TryGetValue(field, out ParamValue? v) ? v?.Text?.Trim() : null;

            public double? Number(int i, string field) =>
                Steps[i].Fields.TryGetValue(field, out ParamValue? v) ? v?.Number : null;

            /// <summary>The mean of a field that is a number or a distribution; 0 when it is neither.</summary>
            public double Mean(int i, string field)
            {
                if (!Steps[i].Fields.TryGetValue(field, out ParamValue? v) || v == null) return 0d;
                if (v.Number is double number) return number;
                return v.Distribution == null ? 0d
                    : DistributionFactory.Create(v.Distribution, $"process '{Id}', step '{Ids[i]}', field '{field}'").Mean;
            }
        }

        private static IEnumerable<ProcessView> Processes(TopologyDefinition topology) =>
            topology.Nodes.Where(n => SchemaCatalog.Canonical(n.Type) == "process" && n.Steps != null)
                    .Select(n => new ProcessView(n.Id, n.Steps!, topology.Connections.Any(c => c.To == n.Id)));

        /// <summary>
        /// SIM-126 schema, slice 5 (candidate check, measured on the car wash): a stock whose supplier, a
        /// process nothing feeds that waits until the level is at most its reorder level and later puts into
        /// it, takes longer to deliver than the reorder level lasts at the mean rate the fed processes take
        /// from it. Returns (stock, supplier, wait step id, reorder level, used during one delivery, lead time).
        /// </summary>
        public static IEnumerable<(string Stock, string Supplier, string Step, double Reorder, double Used, double Lead)> StocksRunningOut(
            TopologyDefinition topology)
        {
            Compute(topology, out Dictionary<string, double> rates);
            List<ProcessView> processes = Processes(topology).ToList();
            var taken = new Dictionary<string, double>();
            foreach (ProcessView process in processes.Where(p => p.Fed))
                for (int i = 0; i < process.Count; i++)
                    if (process.Type(i) == "take" && process.Text(i, "stock") is string stock)
                        taken[stock] = taken.GetValueOrDefault(stock) + rates.GetValueOrDefault(process.Id) * process.Mean(i, "amount");

            foreach (ProcessView supplier in processes.Where(p => !p.Fed))
                for (int w = 0; w < supplier.Count; w++)
                {
                    if (supplier.Type(w) != "wait_until" || supplier.Text(w, "stock") is not string stock
                        || supplier.Number(w, "at_most") is not double reorder) continue;
                    int put = Enumerable.Range(w + 1, supplier.Count - w - 1)
                                        .FirstOrDefault(j => supplier.Type(j) == "put" && supplier.Text(j, "stock") == stock, -1);
                    if (put < 0) continue;
                    double lead = Enumerable.Range(w + 1, put - w - 1).Where(k => supplier.Type(k) == "delay")
                                            .Sum(k => supplier.Mean(k, "duration"));
                    double used = taken.GetValueOrDefault(stock) * lead;
                    if (used > reorder) yield return (stock, supplier.Id, supplier.Ids[w], reorder, used, lead);
                }
        }

        /// <summary>Adds an <c>OFFERED_LOAD</c> info per station, and a warning where one is (nearly) saturated.</summary>
        public static void Check(TopologyDefinition topology, ValidationReport report)
        {
            CheckResources(topology, report);
            foreach (var (stock, supplier, step, reorder, used, lead) in StocksRunningOut(topology))
                report.Warnings.Add(new ValidationIssue("STOCK_RUNS_OUT",
                    $"Stock '{stock}': about {Fmt(used)} is used during the {Fmt(lead)} that '{supplier}' takes to deliver, more than " +
                    $"its reorder level {Fmt(reorder)}, so the stock runs empty before most deliveries arrive.",
                    stock, $"Raise {supplier}.steps.{step}.at_most to {Fmt(Math.Ceiling(used))} or more, or shorten the delivery.",
                    Clean(used)));

            foreach (StationLoad load in Compute(topology))
            {
                string estimate = load.EstimatedVia.Count == 0 ? "" :
                    $"; an estimate: {string.Join(", ", load.EstimatedVia.Select(r => $"'{r}'"))} assumed to split equally";
                report.Info.Add(new ValidationIssue("OFFERED_LOAD",
                    $"Station '{load.Station}': offered load {Fmt(load.Load)} ({Fmt(load.ArrivalRate)} arrivals per time unit, " +
                    $"mean service {Fmt(load.MeanService)}, {load.Servers} server{(load.Servers == 1 ? "" : "s")}{estimate}).",
                    load.Station, Value: Clean(load.Load)));

                int enough = (int)Math.Floor(load.ArrivalRate * load.MeanService / Headroom) + 1;
                string fix = $"Give '{load.Station}' {enough} servers (offered load {Fmt(load.ArrivalRate * load.MeanService / enough)}), " +
                             "or shorten its service time.";
                if (load.Load >= 1d)
                    report.Warnings.Add(new ValidationIssue("SATURATED", load.FiniteQueue == null
                            ? $"Station '{load.Station}' is offered load {Fmt(load.Load)}: more work arrives than it can do, so its queue grows without bound."
                            : $"Station '{load.Station}' is offered load {Fmt(load.Load)}: more work arrives than it can do, so queue " +
                              $"'{load.FiniteQueue}' fills and items are refused there.",
                        load.Station, fix, Clean(load.Load)));
                else if (load.Load >= Headroom)
                    report.Warnings.Add(new ValidationIssue("NEAR_SATURATION",
                        $"Station '{load.Station}' is offered load {Fmt(load.Load)}: expect long queues, a long warm-up and wide intervals.",
                        load.Station, fix, Clean(load.Load)));
            }
        }

        /// <summary>SIM-126 schema, slice 5: an <c>OFFERED_LOAD</c> info per resource, and a warning where one is (nearly) saturated.</summary>
        private static void CheckResources(TopologyDefinition topology, ValidationReport report)
        {
            foreach (ResourceLoad load in ComputeResources(topology))
            {
                string from = load.Users.Count == 0 ? "no process seizes it" : $"seized by {string.Join(", ", load.Users.Select(u => $"'{u}'"))}";
                string bound = load.LowerBound ? "; at least: waits while a unit is held count as nothing" : "";
                report.Info.Add(new ValidationIssue("OFFERED_LOAD",
                    $"Resource '{load.Resource}': offered load {Fmt(load.Load)} ({Fmt(load.Units)} of {load.Capacity} " +
                    $"unit{(load.Capacity == 1 ? "" : "s")} busy on average, {from}{bound}).",
                    load.Resource, Value: Clean(load.Load)));

                int enough = (int)Math.Floor(load.Units / Headroom) + 1;
                string fix = $"Give '{load.Resource}' {enough} units (offered load {Fmt(load.Units / enough)}), or shorten the time a unit is held.";
                if (load.Load >= 1d)
                    report.Warnings.Add(new ValidationIssue("SATURATED", load.Patient.Count > 0
                            ? $"Resource '{load.Resource}' is offered load {Fmt(load.Load)}: more work arrives than its units can do, so items " +
                              $"give up at their patience in {string.Join(", ", load.Patient.Select(p => $"'{p}'"))} and a share of them leaves."
                            : $"Resource '{load.Resource}' is offered load {Fmt(load.Load)}: more work arrives than its units can do, so the " +
                              "queue for it grows without bound.",
                        load.Resource, fix, Clean(load.Load)));
                else if (load.Load >= Headroom)
                    report.Warnings.Add(new ValidationIssue("NEAR_SATURATION",
                        $"Resource '{load.Resource}' is offered load {Fmt(load.Load)}: expect long waits for it, a long warm-up and wide intervals.",
                        load.Resource, fix, Clean(load.Load)));
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

        /// <summary>
        /// The mean of a time given as a distribution object (<paramref name="distName"/>) or as the older
        /// exponential mean (<paramref name="meanName"/>, default 1), shift included. The node must have built.
        /// </summary>
        public static double MeanTime(NodeDefinition node, string distName, string meanName)
        {
            if (node.Params.TryGetValue(distName, out ParamValue? given) && given?.Distribution != null)
                return DistributionFactory.Create(given.Distribution, $"node '{node.Id}', parameter '{distName}'").Mean;
            return node.Params.TryGetValue(meanName, out ParamValue? mean) && mean?.Number is double m ? m : 1d;
        }

        private static int Batch(NodeDefinition node) =>
            node.Params.TryGetValue("batch", out ParamValue? b) && b?.Number is double batch && batch >= 1 ? (int)batch : 1;

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

        /// <summary>Twelve significant digits: drops floating-point noise such as 0.4000000000000001.</summary>
        private static double Clean(double value) =>
            double.Parse(value.ToString("G12", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
    }
}
