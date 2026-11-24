using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using SimOpt.Mathematics.Stochastics.Distributions;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Processes;
using SimOpt.Simulation.Statistics;
using Xunit;
using Xunit.Abstractions;
using SimRandom = SimOpt.Simulation.Engine.Random<double>;

namespace SimOpt.Tests.Simulation.Processes;

/// <summary>
/// SIM-126, slice 2: a resource with a capacity, the counterpart of SimPy's <c>Resource</c>. A
/// process yields <c>Request()</c>, which completes when a unit is granted (first come, first served),
/// and calls <c>Release()</c> when done. Finished processes started during a run unregister
/// themselves, so a long run does not keep every customer it ever had.
/// </summary>
public class ResourceTests
{
    private readonly ITestOutputHelper output;

    public ResourceTests(ITestOutputHelper output) { this.output = output; }

    private static IEnumerable<Wait> Use(Process p, Resource r, double hold, List<(string, double, double)> log, string who)
    {
        yield return r.Request();
        double granted = p.Now;
        yield return p.Timeout(hold);
        r.Release();
        log.Add((who, granted, p.Now));
    }

    [Fact]
    public void AUnitIsGrantedFirstComeFirstServed_AndReleasedToTheNextInLine()
    {
        var model = new Model("m", 1, 0d);
        var machine = new Resource(model, capacity: 1, id: "machine", name: "machine");
        var log = new List<(string, double, double)>();
        model.StartProcess(p => Use(p, machine, 5, log, "A"), id: "A");
        model.StartProcess(p => Use(p, machine, 3, log, "B"), id: "B");

        model.Run(20);

        log.Should().Equal(("A", 0d, 5d), ("B", 5d, 8d));
        machine.InUse.Should().Be(0);
    }

    [Fact]
    public void ACapacityOfTwo_ServesTwoAtOnce()
    {
        var model = new Model("m", 1, 0d);
        var crew = new Resource(model, capacity: 2, id: "crew", name: "crew");
        var log = new List<(string, double, double)>();
        foreach (var (who, hold) in new[] { ("A", 4d), ("B", 6d), ("C", 1d) })
            model.StartProcess(p => Use(p, crew, hold, log, who), id: who);

        model.Run(20);

        log.Should().BeEquivalentTo(new[] { ("A", 0d, 4d), ("C", 4d, 5d), ("B", 0d, 6d) },
            o => o.WithStrictOrdering());
    }

    [Fact]
    public void ReleasingAnIdleResource_IsRefused()
    {
        var model = new Model("m", 1, 0d);
        var machine = new Resource(model, id: "machine", name: "machine");

        ((Action)machine.Release).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Utilization_IsTheTimeWeightedShareOfUnitsInUse()
    {
        var model = new Model("m", 1, 0d);
        var crew = new Resource(model, capacity: 2, id: "crew", name: "crew");
        var utilization = Instrumentation.ObserveResourceUtilization(crew);
        var log = new List<(string, double, double)>();
        model.StartProcess(p => Use(p, crew, 10, log, "A"), id: "A");                     // [0, 10)
        model.StartProcess(p => Later(p, 5, Use(p, crew, 5, log, "B")), id: "B");        // [5, 10)

        model.Run(10);

        utilization.TimeAverage(10).Should().BeApproximately((0.5 * 5 + 1.0 * 5) / 10, 1e-12);
    }

    private static IEnumerable<Wait> Later(Process p, double delay, IEnumerable<Wait> then)
    {
        yield return p.Timeout(delay);
        foreach (Wait w in then) yield return w;
    }

    [Fact]
    public void ResetFreesAllUnits_AndDropsTheQueue()
    {
        var model = new Model("m", 1, 0d);
        var machine = new Resource(model, id: "machine", name: "machine");
        var log = new List<(string, double, double)>();
        model.StartProcess(p => Use(p, machine, 50, log, "A"), id: "A");
        model.StartProcess(p => Use(p, machine, 50, log, "B"), id: "B");
        model.Reset(1);
        model.Run(10);
        machine.InUse.Should().Be(1);
        machine.Waiting.Should().Be(1);

        model.Reset(2);

        machine.InUse.Should().Be(0);
        machine.Waiting.Should().Be(0);
    }

    [Fact]
    public void AnMM1QueueWrittenAsProcesses_MatchesTheClosedForm()
    {
        // λ = 0.8, μ = 1: mean time in system W = 1 / (μ - λ) = 5.
        var model = new Model("mm1", 11, 0d);
        var server = new Resource(model, id: "server", name: "server");
        var gaps = new SimRandom(model, Exponential(0.8));
        var work = new SimRandom(model, Exponential(1.0));
        var sojourn = new List<double>();
        const double warmup = 1_000, length = 21_000;

        IEnumerable<Wait> Customer(Process p)
        {
            double arrived = p.Now;
            yield return server.Request();
            yield return p.Timeout(work.Next());
            server.Release();
            if (arrived >= warmup) sojourn.Add(p.Now - arrived);
        }
        IEnumerable<Wait> Arrivals(Process p)
        {
            while (true)
            {
                yield return p.Timeout(gaps.Next());
                p.Model.StartProcess(Customer);
            }
        }
        model.StartProcess(Arrivals, id: "arrivals");

        var means = new List<double>();
        for (int r = 0; r < 10; r++)
        {
            sojourn.Clear();
            model.Reset(ReplicationRunner.SeedFor(11, r));
            model.Run(length);
            means.Add(sojourn.Average());
        }

        double mean = means.Average();
        double halfWidth = ReplicationRunner.ConfidenceHalfWidth(means)!.Value;
        output.WriteLine($"M/M/1 as processes: W = {mean:F3} ± {halfWidth:F3} (closed form 5), {means.Count} replications");
        Math.Abs(mean - 5.0).Should().BeLessThan(halfWidth + 0.25,
            $"the replication mean {mean:F3} ± {halfWidth:F3} must cover W = 5 up to a small allowance");
        model.Items.Count.Should().BeLessThan(50, "finished customers unregister themselves");
    }

    private static NegExponentialDistribution Exponential(double rate)
    {
        var d = new NegExponentialDistribution();
        d.Configure(rate);
        return d;
    }
}
