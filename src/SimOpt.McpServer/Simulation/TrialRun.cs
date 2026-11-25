using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using SimOpt.McpServer.Models;
using SimOpt.Simulation.Enum;

namespace SimOpt.McpServer.Simulation
{
    /// <summary>
    /// SIM-66 step 4 (D6): one short replication of a topology that builds, on a copy of its own, to
    /// see whether items get through: how many were made, finished, are still inside and were refused.
    /// </summary>
    /// <remarks>
    /// The run lasts <see cref="Arrivals"/> mean interarrival times of the busiest source, so about
    /// that many items arrive there whatever the model's unit of time, in ten pieces with a wall-clock
    /// budget after each: a model whose events explode stops early and says so instead of hanging the
    /// call. A full queue refuses an item without a trace in the engine (the notification is a no-op
    /// by default); the copy counts those refusals.
    /// </remarks>
    public static class TrialRun
    {
        public const double Arrivals = 500;

        public static readonly TimeSpan DefaultBudget = TimeSpan.FromSeconds(1);

        private const int Pieces = 10;

        /// <summary>Adds a <c>TRIAL</c> info, and warnings for what the trial saw, to <paramref name="report"/>.</summary>
        public static void Check(TopologyDefinition topology, ValidationReport report, TimeSpan? budget = null)
        {
            TimeSpan limit = budget ?? DefaultBudget;
            ActiveModel? copy = ModelRegistry.TryBuild(topology, new ValidationReport());
            if (copy == null || copy.Sources.Count == 0) return;   // the build's own report says why

            double length = Arrivals * topology.Nodes.Where(n => copy.Sources.ContainsKey(n.Id))
                                                     .Min(n => OfferedLoad.MeanTime(n, "arrival", "mean_interval"));
            int made = 0;
            foreach (var source in copy.Sources.Values) source.EntityCreatedEvent.AddHandler((_, _) => made++);
            var refused = new Dictionary<string, int>();
            foreach (var (id, queue) in copy.Buffers)
                queue.NotifyItemNotAccepted = _ => refused[id] = refused.GetValueOrDefault(id) + 1;

            var watch = Stopwatch.StartNew();
            double reached = 0d;
            bool stopped = false;
            try
            {
                for (int piece = 1; piece <= Pieces; piece++)
                {
                    reached = length * piece / Pieces;
                    copy.Model.Run(reached);
                    if (copy.Model.CurrentState == ExecutionState.Stopped) break;   // no events left
                    if (piece < Pieces && watch.Elapsed >= limit) { stopped = true; break; }
                }
            }
            catch (Exception e)
            {
                report.Errors.Add(new ValidationIssue("TRIAL_FAILED",
                    $"The trial run failed at time {Fmt(copy.Model.CurrentTime)}: {e.GetType().Name}: {e.Message}",
                    Suggestion: "This is a fault in the engine or in a combination the builder accepts; report it with the topology."));
                return;
            }
            watch.Stop();

            foreach (var (id, delay) in copy.Delays) if (delay.RefusedCount > 0) refused[id] = delay.RefusedCount;
            foreach (var (id, router) in copy.Routers) if (router.RefusedCount > 0) refused[id] = router.RefusedCount;
            int finished = copy.Sinks.Values.Sum(s => s.Count);
            int inside = copy.Buffers.Values.Sum(b => b.Count) + copy.Servers.Values.Count(s => s.Busy)
                       + copy.Delays.Values.Sum(d => d.Count);

            string limited = stopped
                ? $", stopped at the {limit.TotalSeconds.ToString("G3", CultureInfo.InvariantCulture)} s time limit before {Fmt(length)}"
                : "";
            report.Info.Add(new ValidationIssue("TRIAL",
                $"Trial run of {Fmt(reached)} time units{limited}: {made} items made, {finished} finished, {inside} still inside, " +
                $"{refused.Values.Sum()} refused ({copy.Model.EventCounter} events, {watch.ElapsedMilliseconds} ms).",
                Value: finished));

            if (made > 0 && finished == 0 && copy.Sinks.Count > 0)
                report.Warnings.Add(new ValidationIssue("NOTHING_FINISHED",
                    $"No item reached a sink in the trial run of {Fmt(reached)} time units ({made} made).",
                    Suggestion: "Check that the service and delay times fit the time scale of the arrivals; run_simulation, then " +
                                "get_status, shows where the items wait."));

            foreach (var (id, count) in refused.OrderBy(r => topology.Nodes.FindIndex(n => n.Id == r.Key)))
            {
                bool isQueue = copy.Buffers.TryGetValue(id, out var queue);
                report.Warnings.Add(new ValidationIssue("REFUSED", isQueue
                        ? $"Queue '{id}' refused {count} of the {made} items made in the trial: it was full."
                        : $"'{id}' refused {count} of the {made} items made in the trial: the node after it refused them.",
                    id, isQueue
                        ? $"Raise its capacity (now {queue!.MaxCapacity}), or add servers to the station it feeds."
                        : "Give the node after it room: raise its queue's capacity or add servers behind it.",
                    count));
            }
        }

        private static string Fmt(double value) => value.ToString("G4", CultureInfo.InvariantCulture);
    }
}
