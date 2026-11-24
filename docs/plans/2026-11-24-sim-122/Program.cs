// SIM-122 experiment: replication spread of an M/G/1 queue with operating-time breakdowns.
// Same model as a textbook Pollaczek-Khinchine case: arrivals exp(mean 60 min), service exp(mean 45),
// time to failure exp(mean 480 operating minutes), repair exp(mean 30), preempt-resume.
// Measures, per replication, the mean time in system of items arriving after the warm-up,
// items handed on as null (lost) and the queue left at the end.
using System;
using System.Collections.Generic;
using System.Linq;
using SimOpt.Basics.Utilities;
using SimOpt.Mathematics.Stochastics.Distributions;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Enum;
using SimOpt.Simulation.Statistics;
using SimOpt.Simulation.Templates;

static class Program
{
    const double Day = 1440d;

    static NegExponentialDistribution Exponential(double mean)
    {
        var d = new NegExponentialDistribution();
        d.Configure(1d / mean);
        return d;
    }

    static int Main(string[] args)
    {
        int reps = args.Length > 0 ? int.Parse(args[0]) : 20;
        int baseSeed = args.Length > 1 ? int.Parse(args[1]) : 42;
        double days = args.Length > 2 ? double.Parse(args[2]) : 360d;
        double warmDays = args.Length > 3 ? double.Parse(args[3]) : 30d;
        double gap = 60, serve = 45, mtbf = 480, mttr = 30;

        var model = new Model("mg1-breakdowns", baseSeed, 0d);
        int n = 0;
        var source = new SimpleSource(model, StableHash.Of("bench:src"), Exponential(gap),
            () => new SimpleEntity(model, $"E{++n}", $"E{n}"), autoStartDelay: 0d, id: "src", name: "src");
        var buffer = new SimpleBuffer(model, QueueRule.FIFO, id: "buf", name: "buf", maxCapacity: int.MaxValue);
        var server = new SimpleServer(model, StableHash.Of("bench:srv"), Exponential(serve), id: "srv", name: "srv",
            createProduct: m => m[0], timeToFailure: Exponential(mtbf), timeToRecover: Exponential(mttr))
        {
            AutoContinue = true,
            AutoRecover = true,
            AutoRestart = true,
            ContinueProductAfterFailure = true,
        };
        var sink = new SimpleSink(model, id: "snk", name: "snk");
        buffer.ConnectTo(source);
        server.ConnectTo(buffer);
        buffer.ItemReceivedEvent.AddHandler((_, _) => { if (server.Idle) server.Start(); });
        sink.ConnectTo(server);

        var arrived = new Dictionary<SimpleEntity, double>(ReferenceEqualityComparer.Instance);
        var times = new List<double>();
        int lost = 0, finished = 0;
        double warm = warmDays * Day;
        buffer.ItemReceivedEvent.AddHandler((_, item) => arrived[item] = model.CurrentTime);
        server.EntityFinishedEvent.AddHandler((_, product) =>
        {
            finished++;
            if (product is null) { lost++; return; }
            if (arrived.TryGetValue(product, out double t0))
            {
                if (t0 >= warm) times.Add(model.CurrentTime - t0);
                arrived.Remove(product);
            }
        });

        var means = new List<double>();
        Console.WriteLine("rep,seed,measured,mean_time_in_system,lost_null,finished,queue_at_end,error");
        for (int r = 0; r < reps; r++)
        {
            int seed = ReplicationRunner.SeedFor(baseSeed, r);
            arrived.Clear(); times.Clear(); lost = 0; finished = 0;
            string error = "";
            model.Reset(seed);
            try { model.Run(days * Day); }
            catch (Exception e) { error = e.GetType().Name; }
            double mean = times.Count > 0 ? times.Average() : double.NaN;
            if (error == "" && times.Count > 0) means.Add(mean);
            Console.WriteLine($"{r},{seed},{times.Count},{mean:F2},{lost},{finished},{buffer.Count},{error}");
            if (Environment.GetEnvironmentVariable("CIEXP_GC") == "1")
                Console.WriteLine($"# heap_after_rep {r}: {GC.GetTotalMemory(true) / 1048576.0:F1} MB");
        }

        double f = 1d / mtbf;
        double m1 = serve * (1 + f * mttr);
        double m2 = (1 + f * mttr) * (1 + f * mttr) * 2 * serve * serve + 2 * f * mttr * mttr * serve;
        double pk = m2 / gap / (2 * (1 - m1 / gap)) + m1;
        double avg = means.Average();
        double sd = Math.Sqrt(means.Sum(x => (x - avg) * (x - avg)) / (means.Count - 1));
        double? hw = ReplicationRunner.ConfidenceHalfWidth(means, 0.95);
        Console.WriteLine($"# replications_ok={means.Count} mean={avg:F2} sd={sd:F2} ci95_halfwidth={hw:F2} " +
                          $"rel_halfwidth={hw / avg:P1} pollaczek_khinchine={pk:F2} bias={(avg / pk - 1):P1}");
        return 0;
    }
}
