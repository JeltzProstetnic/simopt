using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using SimOpt.Mathematics.Stochastics.Distributions;
using SimOpt.Mathematics.Stochastics.Interfaces;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Enum;
using SimOpt.Simulation.Statistics;
using SimOpt.Simulation.Templates;
using SimOpt.Statistics.Analysis;
using Xunit;
using Xunit.Abstractions;

namespace SimOpt.Tests.Simulation.Statistics;

/// <summary>
/// Builds and runs the four benchmark systems once, so that every assertion in
/// <see cref="AnalyticBatteryTests"/> reads from the same experiments rather than repeating them.
/// </summary>
/// <remarks>
/// The simulation is the expensive artifact here and the assertions are free. Re-running a 20
/// replication experiment per assertion cost more wall clock than the whole rest of the suite, and
/// it also meant two tests could disagree about "the same" system because they were in fact two
/// different sets of sample paths.
/// </remarks>
public sealed class AnalyticBatteryFixture
{
    /// <summary>
    /// Root of every seed in the battery. Pinned, because a 95% interval evaluated on fresh entropy
    /// fails one build in twenty <i>by construction</i> — that is what 95% means. Fixing it makes
    /// the gate deterministic without changing what the interval claims about the estimator.
    /// </summary>
    public const int BaseSeed = 20_260_826;

    /// <summary>Replications per experiment. ν = 19, t(0.975, 19) = 2.0930240544.</summary>
    public const int Replications = 20;

    public ReplicationResult MM1 { get; }
    public ReplicationResult MD1 { get; }
    public ReplicationResult MG1Uniform { get; }
    public ReplicationResult MMc { get; }
    public ReplicationResult Jackson { get; }

    /// <summary>Identifiers of the M/M/c server pool, in construction order.</summary>
    public IReadOnlyList<string> MMcServerIds { get; }

    /// <summary>
    /// Thinned steady-state sojourn times from an M/M/1 at λ = 0.5, μ = 1.0, whose exact
    /// distribution is Exp(μ − λ) = Exp(0.5).
    /// </summary>
    public double[] SojournTimes { get; }

    /// <summary>Rate of the exponential distribution <see cref="SojournTimes"/> must follow.</summary>
    public const double SojournRate = 0.5;

    public AnalyticBatteryFixture()
    {
        MM1 = RunMM1();
        MD1 = RunMG1(new ConstantDoubleDistribution(1.0, initialize: false), "md1");
        MG1Uniform = RunMG1(Uniform(0.5, 1.5), "mg1u");
        (MMc, MMcServerIds) = RunMMc();
        Jackson = RunJackson();
        SojournTimes = CollectSojournTimes();
    }

    // ── shared construction ──────────────────────────────────────────────────

    private static int StableSeed(string key) => SimOpt.Basics.Utilities.StableHash.Of(key);

    /// <summary>Exponential with the given <b>rate</b>.</summary>
    /// <remarks>
    /// <c>Configure</c> takes a rate and <c>ConfigureMean</c> takes a mean. Passing λ = 0.8 to
    /// <c>ConfigureMean</c> silently produces a rate of 1.25 and validates a completely different
    /// system — which then fails against the closed form and sends the reader hunting in the
    /// engine. This wrapper exists so the choice is made once, here, in a named method.
    /// </remarks>
    private static NegExponentialDistribution Exponential(double rate)
    {
        var d = new NegExponentialDistribution();
        d.Configure(rate);
        return d;
    }

    private static UniformDoubleDistribution Uniform(double min, double max)
    {
        var d = new UniformDoubleDistribution();
        d.Configure(min, max);
        return d;
    }

    private static SimpleSource PoissonArrivals(Model model, double lambda, string tag)
    {
        int n = 0;
        return new SimpleSource(model, StableSeed(tag + ":src"), Exponential(lambda),
            () => new SimpleEntity(model, $"E{++n}", $"E{n}"),
            autoStartDelay: 0d, id: "src", name: "src");
    }

    private static SimpleBuffer UnboundedQueue(Model model, string id)
        => new SimpleBuffer(model, QueueRule.FIFO, id: id, name: id, maxCapacity: int.MaxValue);

    /// <summary>
    /// A server that preserves the entity it processed rather than emitting a fresh one.
    /// </summary>
    /// <remarks>
    /// SIM-90: the default product generator returns <c>new TProduct()</c> with a null Identifier,
    /// and a downstream <c>Buffer.Put</c> keys on exactly that — so without this every multi-stage
    /// topology threw and the Jackson network could not be built at all.
    /// </remarks>
    private static SimpleServer PassThroughServer(Model model, IDistribution<double> service, string id, string tag)
        => new SimpleServer(model, StableSeed($"{tag}:{id}"), service,
               id: id, name: id, createProduct: m => m[0])
           { AutoContinue = true };

    // ── the four systems ─────────────────────────────────────────────────────

    /// <summary>
    /// M/M/1 at λ = 0.8, μ = 1.0 ⇒ ρ = 0.8, Lq = 3.2, Wq = 4 — exact integers, so a reader can
    /// check the target without a calculator.
    /// </summary>
    /// <remarks>
    /// Warm-up 1,000 time units ≈ 11 relaxation times (τ = 89.72 at ρ = 0.8). Post-warm-up 20,000
    /// time units ≈ 16,000 customers, sized from the time-average variance constant σ²∞ = 1976 —
    /// <b>not</b> from the marginal variance of 24, which understates the requirement 82-fold and
    /// is the single most common way a gate like this ends up flaky.
    /// </remarks>
    private static ReplicationResult RunMM1()
    {
        var model = new Model("mm1", 1, 0d);
        SimpleSource source = PoissonArrivals(model, 0.8, "mm1");
        SimpleBuffer buffer = UnboundedQueue(model, "buf");
        SimpleServer server = PassThroughServer(model, Exponential(1.0), "srv", "mm1");
        var sink = new SimpleSink(model, id: "snk", name: "snk");

        buffer.ConnectTo(source);
        server.ConnectTo(buffer);
        buffer.ItemReceivedEvent.AddHandler((_, _) => { if (server.Idle) server.Start(); });
        sink.ConnectTo(server);

        Instrumentation.ObserveQueueLength(buffer);
        Instrumentation.ObserveWaitingTime(buffer);
        Instrumentation.ObserveUtilization(server);

        return new ReplicationRunner(model, runLength: 21_000d, warmupTime: 1_000d)
            .Run(Replications, BaseSeed);
    }

    /// <summary>
    /// M/G/1 at λ = 0.5 with the given service distribution. ρ = 0.5, so τ ≈ 11.7 time units and a
    /// 200-unit warm-up is roughly 17 of them.
    /// </summary>
    private static ReplicationResult RunMG1(IDistribution<double> service, string tag)
    {
        var model = new Model(tag, 1, 0d);
        SimpleSource source = PoissonArrivals(model, 0.5, tag);
        SimpleBuffer buffer = UnboundedQueue(model, "buf");
        SimpleServer server = PassThroughServer(model, service, "srv", tag);
        var sink = new SimpleSink(model, id: "snk", name: "snk");

        buffer.ConnectTo(source);
        server.ConnectTo(buffer);
        buffer.ItemReceivedEvent.AddHandler((_, _) => { if (server.Idle) server.Start(); });
        sink.ConnectTo(server);

        Instrumentation.ObserveQueueLength(buffer);
        Instrumentation.ObserveWaitingTime(buffer);
        Instrumentation.ObserveUtilization(server);

        return new ReplicationRunner(model, runLength: 20_200d, warmupTime: 200d)
            .Run(Replications, BaseSeed);
    }

    /// <summary>M/M/c at λ = 2.4, μ = 1.0, c = 3 ⇒ a = 2.4, ρ = 0.8.</summary>
    private static (ReplicationResult Result, IReadOnlyList<string> ServerIds) RunMMc()
    {
        const int servers = 3;

        var model = new Model("mmc", 1, 0d);
        SimpleSource source = PoissonArrivals(model, 2.4, "mmc");
        SimpleBuffer buffer = UnboundedQueue(model, "buf");
        var sink = new SimpleSink(model, id: "snk", name: "snk");
        buffer.ConnectTo(source);

        var pool = new List<SimpleServer>();
        for (int s = 0; s < servers; s++)
        {
            SimpleServer server = PassThroughServer(model, Exponential(1.0), $"srv{s}", "mmc");
            server.ConnectTo(buffer);
            sink.ConnectTo(server);
            pool.Add(server);
        }

        // One arrival wakes exactly one idle server. Which one is a dispatch policy and cannot
        // affect a queueing statistic — the system is work-conserving either way — but a handler
        // that woke none, or that could only ever reach the first, would silently reduce c and
        // break every number this system is checked against.
        buffer.ItemReceivedEvent.AddHandler((_, _) => pool.FirstOrDefault(s => s.Idle)?.Start());

        Instrumentation.ObserveQueueLength(buffer);
        Instrumentation.ObserveWaitingTime(buffer);
        foreach (SimpleServer s in pool) Instrumentation.ObserveUtilization(s);

        ReplicationResult result = new ReplicationRunner(model, runLength: 21_000d, warmupTime: 1_000d)
            .Run(Replications, BaseSeed);

        return (result, pool.Select(s => s.Identifier).ToList());
    }

    /// <summary>
    /// Two-station tandem Jackson network: λ = 1.0 external, μ₁ = 2.0, μ₂ = 1.5, feed-forward.
    /// </summary>
    private static ReplicationResult RunJackson()
    {
        var model = new Model("jackson", 1, 0d);
        SimpleSource source = PoissonArrivals(model, 1.0, "jk");

        SimpleBuffer buf1 = UnboundedQueue(model, "buf1");
        SimpleServer srv1 = PassThroughServer(model, Exponential(2.0), "srv1", "jk");
        SimpleBuffer buf2 = UnboundedQueue(model, "buf2");
        SimpleServer srv2 = PassThroughServer(model, Exponential(1.5), "srv2", "jk");
        var sink = new SimpleSink(model, id: "snk", name: "snk");

        buf1.ConnectTo(source);
        srv1.ConnectTo(buf1);
        buf1.ItemReceivedEvent.AddHandler((_, _) => { if (srv1.Idle) srv1.Start(); });

        buf2.ConnectTo(srv1);
        srv2.ConnectTo(buf2);
        buf2.ItemReceivedEvent.AddHandler((_, _) => { if (srv2.Idle) srv2.Start(); });

        sink.ConnectTo(srv2);

        Instrumentation.ObserveQueueLength(buf1);
        Instrumentation.ObserveWaitingTime(buf1);
        Instrumentation.ObserveUtilization(srv1);
        Instrumentation.ObserveQueueLength(buf2);
        Instrumentation.ObserveWaitingTime(buf2);
        Instrumentation.ObserveUtilization(srv2);

        // Station 2 is the slower one: ρ₂ = 2/3 gives τ ≈ 19.8 time units, so 500 is ~25 of them.
        return new ReplicationRunner(model, runLength: 20_500d, warmupTime: 500d)
            .Run(Replications, BaseSeed);
    }

    // ── the distributional check ─────────────────────────────────────────────

    /// <summary>Sojourn observations retained. Deliberately not larger — see the remarks.</summary>
    public const int SojournSampleSize = 2_000;

    /// <summary>
    /// One in <see cref="SojournThinning"/> completions is kept, because consecutive sojourn times
    /// are <b>not</b> independent and a goodness-of-fit test assumes they are.
    /// </summary>
    /// <remarks>
    /// This is the trap that makes a naive version of this check worthless. At λ = 0.5 the
    /// integrated autocorrelation time of the sojourn series is roughly 10 customers (σ²∞ ≈ 29
    /// against a marginal variance of 3), so consecutive observations carry about a tenth of the
    /// information a KS test would credit them with — the statistic's null distribution is wrong
    /// and the test rejects a perfectly correct engine. Keeping one in 50 leaves a lag-50
    /// correlation of about e⁻⁵, which is negligible.
    /// <para>
    /// The reference design in <c>docs/2026-08-26-analytic-reference.md</c> §4 proposes this check
    /// at ρ = 0.8. It is run at ρ = 0.5 instead: at ρ = 0.8 the autocorrelation time is about 82
    /// customers, so an equally independent sample would need eight times the run for no gain in
    /// what is being proven. The distributional claim is exact at every stable ρ.
    /// </para>
    /// </remarks>
    public const int SojournThinning = 50;

    /// <summary>
    /// Runs one long M/M/1 and returns thinned steady-state sojourn times.
    /// </summary>
    /// <remarks>
    /// A single long run rather than replications, because this is a claim about a distribution
    /// rather than about a mean: there is no interval to compute, and replicating would only add
    /// starting transients to discard. Timestamps are stamped at the source and read at the sink,
    /// which is possible only because a server now preserves the entity it processed (SIM-90).
    /// </remarks>
    private static double[] CollectSojournTimes()
    {
        const double lambda = 0.5, mu = 1.0, warmup = 200d;

        var model = new Model("mm1-sojourn", 1, 0d);
        SimpleSource source = PoissonArrivals(model, lambda, "sojourn");
        SimpleBuffer buffer = UnboundedQueue(model, "buf");
        SimpleServer server = PassThroughServer(model, Exponential(mu), "srv", "sojourn");
        var sink = new SimpleSink(model, id: "snk", name: "snk");

        buffer.ConnectTo(source);
        server.ConnectTo(buffer);
        buffer.ItemReceivedEvent.AddHandler((_, _) => { if (server.Idle) server.Start(); });
        sink.ConnectTo(server);

        var born = new Dictionary<SimpleEntity, double>();
        var sojourns = new List<double>(SojournSampleSize);
        int completed = 0;

        source.EntityCreatedEvent.AddHandler((_, e) => born[e] = model.CurrentTime);
        sink.ItemReceived.AddHandler((_, e) =>
        {
            if (!born.Remove(e, out double arrived)) return;
            if (model.CurrentTime < warmup) return;
            if (completed++ % SojournThinning != 0) return;
            if (sojourns.Count < SojournSampleSize) sojourns.Add(model.CurrentTime - arrived);
        });

        model.Reset(BaseSeed);

        // Enough time for warm-up plus the SojournSampleSize·SojournThinning completions the
        // thinning consumes, with a margin so the sample cannot come up short.
        model.Run(warmup + 1.15 * SojournSampleSize * SojournThinning / lambda);

        return sojourns.ToArray();
    }
}

/// <summary>
/// SIM-64 — the analytic benchmark battery, serving UN-007.
///
/// <para>
/// <c>QueueingFormulasTests</c> pins the closed forms against values derived on paper. This file is
/// the other half, and it is the half that gates the engine: it runs each of those systems in the
/// simulator and asserts the closed-form answer falls inside the reported confidence interval. A
/// simulator that agrees with theory wherever theory has an answer is making a claim an
/// LLM-written one-off script cannot.
/// </para>
///
/// <para>
/// Every constant — rates, analytic answers, run lengths, warm-up periods — is derived and
/// justified in <c>docs/2026-08-26-analytic-reference.md</c>. Do not re-derive them, and do not
/// change a run length without reading §4 of that document first.
/// </para>
///
/// <para><b>Every quantity checked here is a first moment.</b> An engine that matches all of them
/// can still have a broken service-time variance, tie-break or queue discipline. The
/// M/D/1-against-M/G/1-uniform pair is the battery's only lever on a second moment, which is why
/// it gets a test of its own rather than being left implicit in the two systems' separate results.
/// </para>
/// </summary>
public class AnalyticBatteryTests : IClassFixture<AnalyticBatteryFixture>
{
    private readonly AnalyticBatteryFixture systems;
    private readonly ITestOutputHelper output;

    public AnalyticBatteryTests(AnalyticBatteryFixture systems, ITestOutputHelper output)
    {
        this.systems = systems;
        this.output = output;
    }

    /// <summary>The interval a user is shown, and the one the tightness assertion is made against.</summary>
    private const double ReportedLevel = 0.95;

    /// <summary>
    /// The interval the <b>gate</b> uses, deliberately wider than the reported one.
    /// </summary>
    /// <remarks>
    /// Gating on the reported 95% interval would be a mistake that looks like rigour. This file
    /// makes fourteen containment assertions; at 95% each, a <i>correct</i> engine on a fresh set
    /// of seeds fails at least one of them about half the time (0.95¹⁴ ≈ 0.49). The seeds are
    /// pinned so today's build is deterministic, but the next legitimate change to the random
    /// number stream — SIM-81 replaces it outright — would re-roll all fourteen and turn a coin
    /// flip into a red build with nothing wrong.
    /// <para>
    /// So the reported interval stays at 95% and is asserted to be <i>narrow</i>, while containment
    /// is gated at 99.9%, where the same fourteen assertions pass together 98.6% of the time. Both
    /// halves are computed from the same replication data; nothing is loosened except the threshold
    /// at which the build goes red.
    /// </para>
    /// </remarks>
    private const double GateLevel = 0.999;

    /// <summary>
    /// Asserts that a simulated statistic agrees with its closed form in both directions: the
    /// interval contains the analytic value, and the interval is narrow enough to be a claim.
    /// </summary>
    /// <remarks>
    /// The second assertion is not decoration. Without it, <b>an engine returning garbage with
    /// enormous variance sails through a containment-only check</b>, because an interval wide
    /// enough contains anything.
    /// <para>
    /// Every comparison is also written to the test output. A gate that only says "passed" proves
    /// the engine right to whoever ran it and to nobody else; these numbers are the evidence, and
    /// they belong in the CI log where a sceptical reader can find them.
    /// </para>
    /// </remarks>
    private void AgreesWithTheory(MetricSummary metric, double analytic, double kappa, string what)
    {
        metric.HasData.Should().BeTrue($"{what}: the collector produced no observations at all");
        metric.Replications.Should().Be(AnalyticBatteryFixture.Replications,
            $"{what}: a replication produced no data, so the interval rests on fewer than intended");

        double[] estimates = metric.ReplicateEstimates.Where(v => !double.IsNaN(v)).ToArray();
        double reported = ReplicationRunner.ConfidenceHalfWidth(estimates, ReportedLevel)!.Value;
        double gate = ReplicationRunner.ConfidenceHalfWidth(estimates, GateLevel)!.Value;
        double error = Math.Abs(metric.Mean - analytic);

        output.WriteLine(
            $"{what,-24} analytic {analytic,9:F5}   simulated {metric.Mean,9:F5} ± {reported,7:F5}" +
            $"  ({reported / analytic,6:P2})   error {error,8:F5} = {error / reported,5:F2}·h");

        error.Should().BeLessThanOrEqualTo(gate,
            $"{what}: simulated {metric.Mean:F5} must fall inside the {GateLevel:P1} interval " +
            $"±{gate:F5} around the analytic {analytic:F5} — it is off by {error:F5}");

        reported.Should().BeLessThanOrEqualTo(kappa * analytic,
            $"{what}: the reported half-width {reported:F5} must be within {kappa:P0} of " +
            $"{analytic:F5} — an interval wide enough to contain anything is not evidence of anything");
    }

    private const double Kappa = 0.05;

    private static string Stat(string entity, string statistic) => $"{entity}.stat.{statistic}";

    // ── M/M/1 ────────────────────────────────────────────────────────────────

    [Fact]
    public void MM1_AgreesWithTheClosedForm()
    {
        const double lambda = 0.8, mu = 1.0;

        AgreesWithTheory(systems.MM1[Stat("buf", Instrumentation.WaitTime)],
            QueueingFormulas.MM1_Wq(lambda, mu), Kappa, "M/M/1 Wq");
        AgreesWithTheory(systems.MM1[Stat("buf", Instrumentation.QueueLength)],
            QueueingFormulas.MM1_Lq(lambda, mu), Kappa, "M/M/1 Lq");
        AgreesWithTheory(systems.MM1[Stat("srv", Instrumentation.Utilization)],
            QueueingFormulas.Utilization(lambda, mu), Kappa, "M/M/1 rho");
    }

    [Fact]
    public void MM1_SatisfiesLittlesLaw_InTheSimulationsOwnNumbers()
    {
        // Lq = λ·Wq relates two *measured* quantities, so it holds whether or not either matches
        // theory. That makes it a different test from the one above rather than a weaker copy: it
        // catches a time-weighted collector and an observation collector disagreeing about the same
        // queue, which is a defect in an instrument rather than in the model.
        const double lambda = 0.8;

        double lq = systems.MM1[Stat("buf", Instrumentation.QueueLength)].Mean;
        double wq = systems.MM1[Stat("buf", Instrumentation.WaitTime)].Mean;

        output.WriteLine($"Little's law             lambda*Wq {lambda * wq,9:F5}   against Lq {lq,9:F5}");

        (lambda * wq).Should().BeApproximately(lq, 0.05,
            $"Little's law must hold in the simulation's own output: lambda*Wq = {lambda * wq:F5} " +
            $"against Lq = {lq:F5}");
    }

    // ── M/D/1 and M/G/1: the battery's only lever on a second moment ──────────

    [Fact]
    public void MD1_AgreesWithPollaczekKhinchine()
    {
        // λ = 0.5, service constant at 1.0 ⇒ ρ = 0.5, Var(S) = 0, Wq = 0.5 — exactly half the
        // M/M/1 waiting time at the same utilisation, as (1 + c_s²)/2 = ½ requires.
        AgreesWithTheory(systems.MD1[Stat("buf", Instrumentation.WaitTime)],
            QueueingFormulas.MG1_Wq(0.5, meanService: 1.0, serviceVariance: 0d), Kappa, "M/D/1 Wq");
        AgreesWithTheory(systems.MD1[Stat("buf", Instrumentation.QueueLength)],
            QueueingFormulas.MG1_Lq(0.5, meanService: 1.0, serviceVariance: 0d), Kappa, "M/D/1 Lq");
        AgreesWithTheory(systems.MD1[Stat("srv", Instrumentation.Utilization)],
            0.5, Kappa, "M/D/1 rho");
    }

    [Fact]
    public void MG1WithUniformService_AgreesWithPollaczekKhinchine()
    {
        // Service uniform on [0.5, 1.5]: E[S] = 1.0, Var(S) = 1/12, so Wq = 13/24 = 0.541666…
        AgreesWithTheory(systems.MG1Uniform[Stat("buf", Instrumentation.WaitTime)],
            QueueingFormulas.MG1_Wq(0.5, meanService: 1.0, serviceVariance: 1d / 12d),
            Kappa, "M/G/1 uniform Wq");
        AgreesWithTheory(systems.MG1Uniform[Stat("buf", Instrumentation.QueueLength)],
            QueueingFormulas.MG1_Lq(0.5, meanService: 1.0, serviceVariance: 1d / 12d),
            Kappa, "M/G/1 uniform Lq");
    }

    [Fact]
    public void ServiceVariance_ChangesTheAnswer_AndNotOnlyTheMean()
    {
        // The two systems share λ, ρ, E[S] and W − Wq and differ in exactly one thing: Var(S).
        //
        // **If they come out equal, the service sampler is returning its mean** — and every other
        // test in this file still passes, because every other quantity in the battery is a first
        // moment. This is the whole reason the pair exists.
        double deterministic = systems.MD1[Stat("buf", Instrumentation.WaitTime)].Mean;
        double uniform = systems.MG1Uniform[Stat("buf", Instrumentation.WaitTime)].Mean;

        double analyticGap = QueueingFormulas.MG1_Wq(0.5, 1.0, 1d / 12d)
                           - QueueingFormulas.MG1_Wq(0.5, 1.0, 0d);   // 13/24 − 1/2 = 1/24

        output.WriteLine($"Second moment            gap {uniform - deterministic,9:F5}   analytic {analyticGap,9:F5}" +
                         $"   (Wq {deterministic:F5} deterministic vs {uniform:F5} uniform)");

        // Both service distributions have the same mean, so equal utilisation is a precondition for
        // the comparison meaning anything at all — it establishes that the only difference between
        // the two runs is the variance.
        systems.MD1[Stat("srv", Instrumentation.Utilization)].Mean
            .Should().BeApproximately(systems.MG1Uniform[Stat("srv", Instrumentation.Utilization)].Mean, 0.02,
                "the two systems must be at the same utilisation, or they differ in more than variance");

        (uniform - deterministic).Should().BeApproximately(analyticGap, 0.02,
            $"the extra service variance must add {analyticGap:F5} to the wait: measured " +
            $"{deterministic:F5} deterministic against {uniform:F5} uniform");
    }

    // ── M/M/c ────────────────────────────────────────────────────────────────

    [Fact]
    public void MMc_AgreesWithErlangC()
    {
        const double lambda = 2.4, mu = 1.0;
        const int servers = 3;

        AgreesWithTheory(systems.MMc[Stat("buf", Instrumentation.WaitTime)],
            QueueingFormulas.MMc_Wq(lambda, mu, servers), Kappa, "M/M/3 Wq");
        AgreesWithTheory(systems.MMc[Stat("buf", Instrumentation.QueueLength)],
            QueueingFormulas.MMc_Lq(lambda, mu, servers), Kappa, "M/M/3 Lq");
    }

    [Fact]
    public void MMc_SpreadsTheOfferedLoadAcrossThePool()
    {
        const double offeredLoad = 2.4;   // a = λ/μ

        double[] busy = systems.MMcServerIds
            .Select(id => systems.MMc[Stat(id, Instrumentation.Utilization)].Mean)
            .ToArray();

        output.WriteLine($"M/M/3 utilisation        total {busy.Sum(),9:F5}   analytic {offeredLoad,9:F5}" +
                         $"   per server [{string.Join(", ", busy.Select(b => b.ToString("F4")))}]");

        // Asserting the *total* rather than each server's share is deliberate: how work is split
        // depends on the dispatch policy, whereas the total cannot. Note this is a completeness
        // check, not the correctness one — a pool where a single server did everything would still
        // total 2.4 while being an M/M/1, and it is Wq above that catches that.
        busy.Sum().Should().BeApproximately(offeredLoad, 0.02,
            "the three servers together must carry the offered load");

        foreach ((string id, double b) in systems.MMcServerIds.Zip(busy))
            b.Should().BeGreaterThan(0d, $"{id} never worked, so c is effectively smaller than 3");
    }

    // ── Jackson tandem network ───────────────────────────────────────────────

    [Fact]
    public void JacksonTandem_AgreesWithTheProductForm()
    {
        // λ = 1.0 into station 1, μ₁ = 2.0, μ₂ = 1.5, feed-forward. Jackson's theorem gives each
        // station the marginal distribution of an independent M/M/1 at its own rate.
        //
        // Two caveats, stated because they are the ones misremembered: the product form does NOT
        // make the queue-length processes independent, only their stationary joint distribution
        // factorise; and in a network with feedback the internal flows are not Poisson while the
        // product form holds anyway. This network is feed-forward, so Burke's theorem additionally
        // makes station 2's input genuinely Poisson(1.0) — a special case, not the general lesson
        // for whoever extends this battery.
        //
        // This case was unbuildable before SIM-90 closed: the default product generator emitted an
        // entity with a null Identifier and the downstream Buffer.Put keys on exactly that.
        const double lambda = 1.0, mu1 = 2.0, mu2 = 1.5;

        AgreesWithTheory(systems.Jackson[Stat("buf1", Instrumentation.WaitTime)],
            QueueingFormulas.MM1_Wq(lambda, mu1), Kappa, "Jackson 1 Wq");
        AgreesWithTheory(systems.Jackson[Stat("buf1", Instrumentation.QueueLength)],
            QueueingFormulas.MM1_Lq(lambda, mu1), Kappa, "Jackson 1 Lq");
        AgreesWithTheory(systems.Jackson[Stat("srv1", Instrumentation.Utilization)],
            QueueingFormulas.Utilization(lambda, mu1), Kappa, "Jackson 1 rho");

        AgreesWithTheory(systems.Jackson[Stat("buf2", Instrumentation.WaitTime)],
            QueueingFormulas.MM1_Wq(lambda, mu2), Kappa, "Jackson 2 Wq");
        AgreesWithTheory(systems.Jackson[Stat("buf2", Instrumentation.QueueLength)],
            QueueingFormulas.MM1_Lq(lambda, mu2), Kappa, "Jackson 2 Lq");
        AgreesWithTheory(systems.Jackson[Stat("srv2", Instrumentation.Utilization)],
            QueueingFormulas.Utilization(lambda, mu2), Kappa, "Jackson 2 rho");
    }

    [Fact]
    public void JacksonTandem_ConservesFlow_SoStationTwoSeesTheExternalArrivalRate()
    {
        // The traffic equations give λ₂ = λ₁ = 1.0, and everything the product form says about
        // station 2 depends on that. Station 2's utilisation is the measurement of it: ρ₂ = λ₂/μ₂,
        // so ρ₂·μ₂ recovers the rate arriving at station 2 without instrumenting a counter.
        //
        // A tandem that lost or duplicated entities between the stations would still produce a
        // self-consistent station 1 and a station 2 that quietly answered a different question.
        double rho2 = systems.Jackson[Stat("srv2", Instrumentation.Utilization)].Mean;
        double impliedRate = rho2 * 1.5;

        output.WriteLine($"Jackson flow             station 2 sees {impliedRate,9:F5}   external {1.0,9:F5}");

        impliedRate.Should().BeApproximately(1.0, 0.02,
            $"station 2's throughput must equal the external arrival rate, but its utilisation of " +
            $"{rho2:F5} implies {impliedRate:F5}");
    }

    // ── the distributional check ─────────────────────────────────────────────

    private static Func<double, double> ExponentialCdf(double rate)
        => x => x <= 0d ? 0d : 1d - Math.Exp(-rate * x);

    [Fact]
    public void MM1SojournTime_HasExactlyTheExponentialDistributionTheoryGivesIt()
    {
        // Everything else in this file is a first moment. This is the one assertion about a whole
        // distribution, and it is far stronger than Wq = 4.0: for M/M/1 the time in system is
        // *exactly* Exp(μ − λ), not approximately and not asymptotically.
        //
        // That this closes a real gap was measured, not assumed. Switching every queue in the
        // battery from FIFO to LIFO leaves the engine work-conserving, so **every other test in
        // this file still passes** — Wq, Lq, ρ, Little's law, Pollaczek–Khinchine, Erlang-C, the
        // Jackson product form and even the second-moment pair. This test fails immediately:
        // sqrt(n)·D goes from 0.66 to 2.78 against a critical value of 1.63, and A² from 0.56 to
        // 14.08 against 3.86, while the mean stays at 2.07 against a truth of 2. A queue discipline
        // is invisible to every mean the battery can check and obvious to this one.
        double[] sample = systems.SojournTimes;
        Func<double, double> exact = ExponentialCdf(AnalyticBatteryFixture.SojournRate);

        sample.Should().HaveCount(AnalyticBatteryFixture.SojournSampleSize,
            "a short sample would weaken the test silently rather than failing it");

        double d = GoodnessOfFit.KolmogorovSmirnov(sample, exact);
        double a2 = GoodnessOfFit.AndersonDarling(sample, exact);
        double scaled = Math.Sqrt(sample.Length) * d;

        output.WriteLine($"Sojourn ~ Exp(0.5)       mean {sample.Average(),9:F5}   analytic {1d / AnalyticBatteryFixture.SojournRate,9:F5}" +
                         $"   sqrt(n)*D {scaled,7:F5} (crit {GoodnessOfFit.KolmogorovSmirnovCriticalValue(0.01):F5})" +
                         $"   A2 {a2,7:F5} (crit {GoodnessOfFit.AndersonDarlingCriticalValue(0.01):F5})");

        sample.Average().Should().BeApproximately(1d / AnalyticBatteryFixture.SojournRate, 0.15,
            "the mean is the cheap anchor: a sample that fails here is not worth testing the shape of");

        scaled.Should().BeLessThan(GoodnessOfFit.KolmogorovSmirnovCriticalValue(0.01),
            $"simulated sojourn times must be indistinguishable from Exp(0.5): sqrt(n)*D = {scaled:F5}");
        a2.Should().BeLessThan(GoodnessOfFit.AndersonDarlingCriticalValue(0.01),
            $"Anderson-Darling weights the upper tail, where a queueing model is most sensitive: A2 = {a2:F5}");
    }

    [Fact]
    public void TheDistributionalCheck_WouldRejectAWrongAnswer()
    {
        // The acceptance above proves nothing on its own: a test with no power accepts everything,
        // and at n = 2,000 it is a fair question whether this one has any. Re-testing the same
        // sample against a rate 15% wrong answers it — 15% being roughly the size of error the
        // mean-based benchmarks in this file would still pass at.
        double[] sample = systems.SojournTimes;
        Func<double, double> wrong = ExponentialCdf(AnalyticBatteryFixture.SojournRate * 1.15);

        double scaled = Math.Sqrt(sample.Length) * GoodnessOfFit.KolmogorovSmirnov(sample, wrong);
        double a2 = GoodnessOfFit.AndersonDarling(sample, wrong);

        output.WriteLine($"Power check (rate +15%)  sqrt(n)*D {scaled,7:F5} must exceed " +
                         $"{GoodnessOfFit.KolmogorovSmirnovCriticalValue(0.01):F5}   A2 {a2,7:F5} must exceed " +
                         $"{GoodnessOfFit.AndersonDarlingCriticalValue(0.01):F5}");

        scaled.Should().BeGreaterThan(GoodnessOfFit.KolmogorovSmirnovCriticalValue(0.01));
        a2.Should().BeGreaterThan(GoodnessOfFit.AndersonDarlingCriticalValue(0.01));
    }
}
