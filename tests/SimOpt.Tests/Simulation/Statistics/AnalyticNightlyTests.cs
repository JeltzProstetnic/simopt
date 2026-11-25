using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using FluentAssertions;
using SimOpt.Mathematics.Stochastics.Distributions;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Enum;
using SimOpt.Simulation.Statistics;
using SimOpt.Simulation.Templates;
using SimOpt.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace SimOpt.Tests.Simulation.Statistics;

/// <summary>
/// SIM-96: the nightly half of the analytic gate, the three checks the SIM-64 battery left out because
/// each costs minutes rather than seconds. All at M/M/1, λ = 0.8, μ = 1 (ρ = 0.8, Wq = 4, W = 5), sized
/// in <c>docs/2026-08-26-analytic-reference.md</c> ("Recommended design for M/M/1 at ρ = 0.8"):
/// <list type="number">
/// <item>the nightly design: 30 replications of 100,000 time units after a 1,000-unit warm-up, asserting
/// the closed form inside the interval and the half-width within κ = 2 % of it;</item>
/// <item>coverage as a meta-test: the per-commit design on 200 independent seed sets, its 95 % interval
/// covering Wq in 0.90 to 0.99 of them (0.94 was measured; the lower bound must not be 0.95, because
/// means of a right-skewed wait over 20 replications are not exactly normal);</item>
/// <item>Welch's moving average over 30 replications of the first 2,400 customers, so that the 1,000-unit
/// warm-up rests on someone having looked at the curve: <c>docs/plans/2026-11-25-sim-96/</c>.</item>
/// </list>
/// Seeds are pinned, so each check is deterministic. Run them with <c>SIMOPT_NIGHTLY=1</c>.
/// </summary>
[Trait("Category", "Nightly")]
public class AnalyticNightlyTests
{
    private const int NightlySeed = 20_261_125;
    private const double Lambda = 0.8, Mu = 1.0, Wq = 4.0, W = 5.0;

    private readonly ITestOutputHelper output;

    public AnalyticNightlyTests(ITestOutputHelper output) => this.output = output;

    private static NegExponentialDistribution Exponential(double rate)
    {
        var d = new NegExponentialDistribution();
        d.Configure(rate);
        return d;
    }

    /// <summary>The battery's M/M/1, with a hook on each completion for its sojourn time.</summary>
    private static Model MM1(Action<double>? sojourn = null)
    {
        var model = new Model("mm1-nightly", 1, 0d);
        int n = 0;
        var source = new SimpleSource(model, SimOpt.Basics.Utilities.StableHash.Of("nightly:src"), Exponential(Lambda),
            () => new SimpleEntity(model, $"E{++n}", $"E{n}"), autoStartDelay: 0d, id: "src", name: "src");
        var buffer = new SimpleBuffer(model, QueueRule.FIFO, id: "buf", name: "buf", maxCapacity: int.MaxValue);
        var server = new SimpleServer(model, SimOpt.Basics.Utilities.StableHash.Of("nightly:srv"), Exponential(Mu),
            id: "srv", name: "srv", createProduct: m => m[0]) { AutoContinue = true };
        var sink = new SimpleSink(model, id: "snk", name: "snk");

        buffer.ConnectTo(source);
        server.ConnectTo(buffer);
        buffer.ItemReceivedEvent.AddHandler((_, _) => { if (server.Idle) server.Start(); });
        sink.ConnectTo(server);
        Instrumentation.ObserveWaitingTime(buffer);

        if (sojourn != null)
        {
            var born = new Dictionary<SimpleEntity, double>();
            source.EntityCreatedEvent.AddHandler((_, e) => born[e] = model.CurrentTime);
            sink.ItemReceived.AddHandler((_, e) => { if (born.Remove(e, out double t)) sojourn(model.CurrentTime - t); });
        }
        return model;
    }

    private static readonly string WaitTime = $"buf.stat.{Instrumentation.WaitTime}";

    [NightlyFact]
    public void TheNightlyDesign_AgreesWithTheClosedForm_ToTwoPercent()
    {
        MetricSummary wait = new ReplicationRunner(MM1(), runLength: 101_000d, warmupTime: 1_000d).Run(30, NightlySeed)[WaitTime];

        double[] estimates = wait.ReplicateEstimates;
        double reported = ReplicationRunner.ConfidenceHalfWidth(estimates, 0.95)!.Value;
        double gate = ReplicationRunner.ConfidenceHalfWidth(estimates, 0.999)!.Value;
        output.WriteLine($"M/M/1 Wq nightly: analytic {Wq}, simulated {wait.Mean:F5} ± {reported:F5} ({reported / Wq:P2}); " +
                         $"reference predicted ±0.0587, measured ±0.0578");

        Math.Abs(wait.Mean - Wq).Should().BeLessThanOrEqualTo(gate);
        reported.Should().BeLessThanOrEqualTo(0.02 * Wq);
    }

    [NightlyFact]
    public void ThePerCommitInterval_CoversTheTrueWait_InNinetyToNinetyNinePercentOfTwoHundredSeedSets()
    {
        var runner = new ReplicationRunner(MM1(), runLength: 21_000d, warmupTime: 1_000d);
        int covered = 0;
        for (int set = 0; set < 200; set++)
        {
            MetricSummary wait = runner.Run(20, ReplicationRunner.SeedFor(NightlySeed, set))[WaitTime];
            double h = ReplicationRunner.ConfidenceHalfWidth(wait.ReplicateEstimates, 0.95)!.Value;
            if (Math.Abs(wait.Mean - Wq) <= h) covered++;
        }
        double coverage = covered / 200d;
        output.WriteLine($"coverage of the per-commit 95 % interval over 200 seed sets: {coverage:F3} (reference measured 0.940)");

        coverage.Should().BeInRange(0.90, 0.99);
    }

    [NightlyFact]
    public void WelchsMovingAverage_RisesFromAnEmptyStart_AndSettlesWellInsideTheWarmup()
    {
        const int replications = 30, customers = 2_400, window = 100;
        var sums = new double[customers];
        for (int r = 0; r < replications; r++)
        {
            var sojourns = new List<double>(customers);
            Model model = MM1(s => { if (sojourns.Count < customers) sojourns.Add(s); });
            model.Reset(ReplicationRunner.SeedFor(NightlySeed, r));
            model.Run(1.15 * customers / Lambda);
            sojourns.Should().HaveCount(customers, $"replication {r} completed too few customers");
            for (int i = 0; i < customers; i++) sums[i] += sojourns[i];
        }
        double[] mean = sums.Select(s => s / replications).ToArray();
        double[] moving = Welch(mean, window);
        WriteCurve(mean, moving);

        // the warm-up is 1,000 time units, about 800 customers at λ = 0.8
        double early = mean.Take(10).Average();
        double settled = mean.Skip(800).Average();
        output.WriteLine($"Welch: first 10 customers {early:F3}, customers 801..{customers} {settled:F3}, W = {W}");
        // measured 2.40 at these seeds; the bound says "clearly below the steady state", not a pin of that value
        early.Should().BeLessThan(0.6 * W, "an empty, idle start makes the first customers' sojourn about one service time");
        Math.Abs(settled - W).Should().BeLessThan(0.7, "after the warm-up the curve is at the steady-state sojourn");
    }

    /// <summary>Welch's moving average: a centred window of 2w + 1, shrunk near the start to what exists.</summary>
    private static double[] Welch(double[] y, int w)
    {
        int m = y.Length - w;
        var result = new double[m];
        for (int i = 0; i < m; i++)
        {
            int half = Math.Min(i, w);
            double sum = 0;
            for (int s = i - half; s <= i + half; s++) sum += y[s];
            result[i] = sum / (2 * half + 1);
        }
        return result;
    }

    /// <summary>Writes the curve to <c>SIMOPT_WELCH_CSV</c> when set; that is how the plot in docs was made.</summary>
    private static void WriteCurve(double[] mean, double[] moving)
    {
        string? path = Environment.GetEnvironmentVariable("SIMOPT_WELCH_CSV");
        if (string.IsNullOrEmpty(path)) return;
        var lines = new List<string> { "customer,mean_sojourn,welch_moving_average" };
        for (int i = 0; i < moving.Length; i++)
            lines.Add(string.Create(CultureInfo.InvariantCulture, $"{i + 1},{mean[i]:R},{moving[i]:R}"));
        File.WriteAllLines(path, lines);
    }
}
