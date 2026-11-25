using System.Collections.Generic;
using FluentAssertions;
using SimOpt.Mathematics.Stochastics.Distributions;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Processes;
using SimOpt.Simulation.Statistics;
using Xunit;
using SimRandom = SimOpt.Simulation.Engine.Random<double>;

namespace SimOpt.Tests.Simulation.Processes;

/// <summary>
/// SIM-126, schema slice 1: the wait for a unit of a resource and the number of requests waiting,
/// measured as the queue of a station is (<c>wait_time</c>, <c>queue_length</c>), so a resource node
/// reports what a queue and station report. The wait runs from the request to the grant and counts
/// granted requests only; a request withdrawn before its grant (a driver who gave up) is not in it.
/// </summary>
public class ResourceStatisticsTests
{
    private static IEnumerable<Wait> Use(Process p, Resource r, double start, double hold)
    {
        if (start > 0) yield return p.Timeout(start);
        yield return r.Request();
        yield return p.Timeout(hold);
        r.Release();
    }

    [Fact]
    public void TheWaitRunsFromRequestToGrant_AndTheQueueLengthIsItsTimeAverage()
    {
        var model = new Model("m", 1, 0d);
        var machine = new Resource(model, capacity: 1, id: "machine", name: "machine");
        TallyCollector wait = Instrumentation.ObserveResourceWaitingTime(machine);
        TimeWeightedCollector queue = Instrumentation.ObserveResourceQueueLength(machine);
        model.StartProcess(p => Use(p, machine, 0, 4), id: "A");   // granted at 0, holds [0, 4)
        model.StartProcess(p => Use(p, machine, 1, 2), id: "B");   // waits [1, 4), holds [4, 6)
        model.StartProcess(p => Use(p, machine, 2, 1), id: "C");   // waits [2, 6), holds [6, 7)

        model.Run(10);

        wait.Count.Should().Be(3);
        wait.Mean.Should().BeApproximately((0d + 3d + 4d) / 3, 1e-12);
        wait.Max.Should().Be(4);
        // waiting: one in [1, 2), two in [2, 4), one in [4, 6): 1 + 4 + 2 = 7 over 10
        queue.TimeAverage(10).Should().BeApproximately(0.7, 1e-12);
        machine.Waiting.Should().Be(0);
        queue.CurrentValue.Should().Be(0);
    }

    [Fact]
    public void ARequestWithdrawnBeforeItsGrant_LeavesTheQueue_AndIsNotTimed()
    {
        var model = new Model("m", 1, 0d);
        var bay = new Resource(model, id: "bay", name: "bay");
        TallyCollector wait = Instrumentation.ObserveResourceWaitingTime(bay);
        TimeWeightedCollector queue = Instrumentation.ObserveResourceQueueLength(bay);
        model.StartProcess(p => Use(p, bay, 0, 10), id: "A");
        model.StartProcess(Impatient, id: "B");

        IEnumerable<Wait> Impatient(Process p)
        {
            yield return p.Timeout(1);
            yield return Wait.AnyOf(bay.Request(), p.Timeout(2));   // gives up at 3
        }

        model.Run(10);

        wait.Count.Should().Be(1, "only A was granted a unit");
        wait.Mean.Should().Be(0);
        queue.TimeAverage(10).Should().BeApproximately(2d / 10, 1e-12, "B waited in [1, 3)");
        bay.Waiting.Should().Be(0);
    }

    [Fact]
    public void AfterAReset_BothStartFromAnEmptyQueue()
    {
        var model = new Model("m", 1, 0d);
        var machine = new Resource(model, id: "machine", name: "machine");
        TallyCollector wait = Instrumentation.ObserveResourceWaitingTime(machine);
        TimeWeightedCollector queue = Instrumentation.ObserveResourceQueueLength(machine);
        model.StartProcess(p => Use(p, machine, 0, 50), id: "A");
        model.StartProcess(p => Use(p, machine, 0, 50), id: "B");
        model.Reset(1);
        model.Run(10);
        queue.CurrentValue.Should().Be(1);

        model.Reset(2);
        model.Run(10);

        queue.TimeAverage(10).Should().BeApproximately(1d, 1e-12, "B has waited the whole of the new run");
        wait.Count.Should().Be(1, "the previous run's grant was reset with the collector");
    }

    [Fact]
    public void AnMM1QueueWrittenAsProcesses_WaitsAsTheClosedFormSays()
    {
        // λ = 0.8, μ = 1, ρ = 0.8: Wq = ρ / (μ - λ) = 4, Lq = ρ² / (1 - ρ) = 3.2.
        const double warmup = 1_000, length = 41_000;
        var model = new Model("mm1", 11, 0d);
        var server = new Resource(model, id: "server", name: "server");
        TallyCollector wait = Instrumentation.ObserveResourceWaitingTime(server, warmup);
        TimeWeightedCollector queue = Instrumentation.ObserveResourceQueueLength(server, warmup);
        var gaps = new SimRandom(model, Exponential(0.8));
        var work = new SimRandom(model, Exponential(1.0));

        IEnumerable<Wait> Customer(Process p)
        {
            yield return server.Request();
            yield return p.Timeout(work.Next());
            server.Release();
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

        model.Run(length);

        wait.Mean.Should().BeApproximately(4.0, 0.6);
        queue.TimeAverage(length).Should().BeApproximately(3.2, 0.5);
        // Little's law ties the two together on the same run: Lq = λ Wq
        queue.TimeAverage(length).Should().BeApproximately(0.8 * wait.Mean, 0.15);
    }

    private static NegExponentialDistribution Exponential(double rate)
    {
        var d = new NegExponentialDistribution();
        d.ConfigureMean(1 / rate);
        return d;
    }
}
