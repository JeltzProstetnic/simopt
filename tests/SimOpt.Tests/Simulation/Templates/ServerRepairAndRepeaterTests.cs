using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using SimOpt.Mathematics.Stochastics.Distributions;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Enum;
using SimOpt.Simulation.Templates;
using Xunit;

namespace SimOpt.Tests.Simulation.Templates;

/// <summary>
/// SIM-115 and SIM-116, found by a review of a downstream integrator's host.
///
/// <para>
/// SIM-115: the time to repair was drawn once, at reset, and reused for every failure in the
/// replication, while the time to failure was redrawn after each failure. With a stochastic repair
/// distribution every repair lasted as long as the first.
/// </para>
/// <para>
/// SIM-116: a batch's finish runs at <c>SimWorldBeforeOthers</c> and its auto-continue repeater at
/// <c>LowLevelAfterOthers</c>. An item put into the buffer between the two, by a handler that
/// starts the now idle server, began batch k+1; the repeater then started batch k+2 alongside it,
/// because it checked neither <c>working</c> nor whether <c>AutoContinue</c> was still on.
/// </para>
/// </summary>
public class ServerRepairAndRepeaterTests
{
    private static ConstantDoubleDistribution Constant(double v) => new(v, initialize: false);

    private static UniformDoubleDistribution Uniform(double min, double max)
    {
        var d = new UniformDoubleDistribution();
        d.Configure(min, max);
        return d;
    }

    [Fact]
    public void EveryRepairDrawsItsOwnDuration()
    {
        // Work 10, failure every 23 operating minutes (never on a batch boundary, so no ties),
        // repair uniform on [1, 9]. A batch hit by a failure finishes 10 + repair after the
        // previous one, so the gaps between finishes reveal every repair duration.
        var model = new Model("m", 1, 0d);
        var buffer = new SimpleBuffer(model, QueueRule.FIFO, id: "buf", name: "buf", maxCapacity: 100);
        var server = new SimpleServer(model, Constant(10), id: "srv", name: "srv",
            createProduct: m => m[0],
            timeToFailure: Constant(23), timeToRecover: Uniform(1, 9))
        {
            AutoContinue = true,
            AutoRecover = true,
            AutoRestart = true,
            ContinueProductAfterFailure = true,
        };
        var sink = new SimpleSink(model, id: "snk", name: "snk");
        server.ConnectTo(buffer);
        sink.ConnectTo(server);
        var finishes = new List<double>();
        server.EntityFinishedEvent.AddHandler((_, _) => finishes.Add(model.CurrentTime));

        model.Reset(1);
        for (int i = 1; i <= 12; i++) buffer.Put(new SimpleEntity(model, $"E{i}", $"E{i}"));
        server.Start();
        model.Run(1000);

        finishes.Should().HaveCount(12);
        // Rounded: each gap is a difference of sums of times, so equal repairs differ by ~1e-15.
        var repairs = finishes.Zip(finishes.Skip(1), (a, b) => System.Math.Round(b - a - 10, 6))
            .Where(r => r > 0).ToList();
        repairs.Should().HaveCountGreaterThanOrEqualTo(4, "12 batches of 10 cross a failure every 23 minutes");
        repairs.Should().OnlyContain(r => r >= 1 && r <= 9);
        repairs.Distinct().Should().HaveCountGreaterThan(1,
            "each failure draws its own repair time; reusing the first draw makes every repair equal");
    }

    private static (Model model, SimpleBuffer buffer, SimpleServer server, List<(double Time, SimpleEntity? Product)> finished)
        WiredLikeProductionLine(int preload)
    {
        var model = new Model("m", 1, 0d);
        var buffer = new SimpleBuffer(model, QueueRule.FIFO, id: "buf", name: "buf", maxCapacity: 100);
        var server = new SimpleServer(model, Constant(10), id: "srv", name: "srv", createProduct: m => m[0])
        { AutoContinue = true };
        var sink = new SimpleSink(model, id: "snk", name: "snk");
        server.ConnectTo(buffer);
        sink.ConnectTo(server);
        // The ProductionLine example's wiring: an arrival starts the server if it is idle.
        buffer.ItemReceivedEvent.AddHandler((_, _) => { if (server.Idle) server.Start(); });
        var finished = new List<(double, SimpleEntity?)>();
        server.EntityFinishedEvent.AddHandler((_, product) => finished.Add((model.CurrentTime, product)));

        model.Reset(1);
        for (int i = 1; i <= preload; i++) buffer.Put(new SimpleEntity(model, $"E{i}", $"E{i}"));
        server.Start();
        return (model, buffer, server, finished);
    }

    [Fact]
    public void AnArrivalAtTheInstantABatchFinishes_DoesNotStartASecondBatchAlongsideTheNext()
    {
        var (model, buffer, _, finished) = WiredLikeProductionLine(preload: 3);
        // At t = 10, after E1's finish and before its repeater: E4 arrives and its handler starts E2.
        model.Schedule(10, new Priority(type: PriorityType.User),
            () => buffer.Put(new SimpleEntity(model, "E4", "E4")));

        model.Run(1000);

        finished.Select(f => f.Time).Should().Equal(new double[] { 10, 20, 30, 40 },
            "a server of capacity one never runs two batches at once");
        finished.Select(f => f.Product!.EntityName).Should().Equal("E1", "E2", "E3", "E4");
    }

    [Fact]
    public void SwitchingAutoContinueOffMidBatch_StopsTheContinuationAlreadyScheduled()
    {
        var (model, _, server, finished) = WiredLikeProductionLine(preload: 3);
        model.Schedule(5, () => server.AutoContinue = false);

        model.Run(1000);

        finished.Should().ContainSingle("the next batch must not start once AutoContinue is off")
            .Which.Time.Should().Be(10);
    }
}
