// SIM-129 speed harness. Usage:
//   dotnet run -c Release --project benchmarks/SimOpt.Benchmarks -- [--scale 1] [--reps 20] [--repeat 3] [--case mm1,...] [--each]
// Prints per case the events run, the best wall time over the repeats, nanoseconds and bytes allocated
// per event, and the results fingerprint. A run that changes speed but not results keeps every fingerprint.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace SimOpt.Benchmarks
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            double scale = 1;
            int reps = 20, repeat = 3, baseSeed = 20_260_826;
            bool eachRepeat = false;
            IReadOnlyList<string> cases = BenchmarkCases.Names;
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--scale": scale = double.Parse(args[++i], CultureInfo.InvariantCulture); break;
                    case "--reps": reps = int.Parse(args[++i]); break;
                    case "--repeat": repeat = int.Parse(args[++i]); break;
                    case "--seed": baseSeed = int.Parse(args[++i]); break;
                    case "--case": cases = args[++i].Split(','); break;
                    case "--each": eachRepeat = true; break;
                    default: Console.Error.WriteLine($"unknown argument {args[i]}"); return 2;
                }
            }

            Console.WriteLine($"# SIM-129 harness: scale {scale}, {reps} replications, best of {repeat}, base seed {baseSeed}");
            Console.WriteLine("case,events,best_s,ns_per_event,bytes_per_event,fingerprint");

            // Warm up every case before timing any, for at least 1.5 s, then give the JIT's background
            // tier-up time to finish. The JIT stops counting calls while it is still compiling new
            // methods, so a short warm-up left a case run on its own (md1: 620 instead of 200 ns per
            // event) on unoptimised code.
            var warm = Stopwatch.StartNew();
            while (warm.Elapsed.TotalSeconds < 1.5)
                foreach (string name in cases) BenchmarkCases.Run(name, 0.1, 2, baseSeed);
            System.Threading.Thread.Sleep(300);

            double totalSeconds = 0;
            long totalEvents = 0;
            foreach (string name in cases)
            {

                CaseOutcome outcome = null;
                double best = double.MaxValue;
                long bytes = long.MaxValue;
                for (int r = 0; r < repeat; r++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    long allocated = GC.GetTotalAllocatedBytes(precise: true);
                    var watch = Stopwatch.StartNew();
                    CaseOutcome run = BenchmarkCases.Run(name, scale, reps, baseSeed);
                    watch.Stop();
                    allocated = GC.GetTotalAllocatedBytes(precise: true) - allocated;
                    if (outcome != null && run.Fingerprint != outcome.Fingerprint)
                    {
                        Console.Error.WriteLine($"{name}: fingerprint differs between repeats ({outcome.Fingerprint} vs {run.Fingerprint})");
                        return 1;
                    }
                    outcome = run;
                    if (eachRepeat) Console.WriteLine($"# {name} repeat {r}: {watch.Elapsed.TotalSeconds:F3} s, {GC.CollectionCount(0)} gen0 collections so far");
                    best = Math.Min(best, watch.Elapsed.TotalSeconds);
                    bytes = Math.Min(bytes, allocated);
                }

                totalSeconds += best;
                totalEvents += outcome.Events;
                Console.WriteLine(string.Join(",", name, outcome.Events, best.ToString("F3", CultureInfo.InvariantCulture),
                    (best * 1e9 / outcome.Events).ToString("F0", CultureInfo.InvariantCulture),
                    ((double)bytes / outcome.Events).ToString("F0", CultureInfo.InvariantCulture), outcome.Fingerprint));
            }
            Console.WriteLine($"# total: {totalEvents} events in {totalSeconds:F3} s, {totalSeconds * 1e9 / totalEvents:F0} ns per event");
            return 0;
        }
    }
}
