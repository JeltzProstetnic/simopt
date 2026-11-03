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
/// SIM-106: a server that fails mid-batch and carries on (<c>ContinueProductAfterFailure</c> with
/// <c>AutoRecover</c> and <c>AutoRestart</c>).
///
/// <para>
/// Two defects, reported by a downstream integrator and reproduced on <c>b171eb8</c>.
/// <c>StartWhatever</c> compared <c>nextTimeToFinished &lt; nextTimeToFailure</c> strictly, so a
/// tie (work left equals time to failure) took the failure branch and left zero work. The next
/// recovery then scheduled the resumed finish at +0, which <c>Model.AddEventAt</c> refuses
/// (<c>CausalityException</c>). Mined durations are whole minutes, which makes such ties common in
/// real data. Separately, the deferred failure instance never kept the product it made, so the
/// finish after a repair handed on <c>null</c>, and every item lost that way biases lead times low.
/// </para>
/// <para>
/// Semantics pinned here: time to failure counts operating time; a tie finishes the batch, and the
/// failure then happens between batches, before the next one starts; the batch a failure interrupts
/// leaves as its own product after the repair; and a failure between batches never invents a
/// continuation.
/// </para>
/// </summary>
public class ServerFailureContinuationTests
{
    private static ConstantDoubleDistribution Constant(double v) => new(v, initialize: false);

    private sealed class Run
    {
        public readonly List<(double Time, SimpleEntity? Product)> Finished = new();
        public readonly List<SimpleEntity> Loaded = new();
        public Model Model = null!;
    }

    /// <summary>All <paramref name="items"/> wait in the buffer at t = 0, so every time is exact.</summary>
    private static Run Line(double work, double timeToFailure, double repair, int items)
    {
        var run = new Run { Model = new Model("m", 1, 0d) };
        var model = run.Model;

        var buffer = new SimpleBuffer(model, QueueRule.FIFO, id: "buf", name: "buf", maxCapacity: 100);
        var server = new SimpleServer(model, Constant(work), id: "srv", name: "srv",
            createProduct: m => m[0],
            timeToFailure: Constant(timeToFailure), timeToRecover: Constant(repair))
        {
            AutoContinue = true,
            AutoRecover = true,
            AutoRestart = true,
            ContinueProductAfterFailure = true,
        };
        var sink = new SimpleSink(model, id: "snk", name: "snk");
        server.ConnectTo(buffer);
        sink.ConnectTo(server);
        server.EntityFinishedEvent.AddHandler((_, product) => run.Finished.Add((model.CurrentTime, product)));

        model.Reset(1);
        for (int i = 1; i <= items; i++)
        {
            var e = new SimpleEntity(model, $"E{i}", $"E{i}");
            run.Loaded.Add(e);
            buffer.Put(e);
        }
        server.Start();
        return run;
    }

    [Fact]
    public void AFailureDueExactlyAsABatchFinishes_FinishesTheBatch_ThenFailsBetweenBatches()
    {
        // work 50, failure after 100 operating minutes = exactly two batches, repair 5
        var run = Line(work: 50, timeToFailure: 100, repair: 5, items: 5);

        var act = () => run.Model.Run(1000);

        act.Should().NotThrow();
        run.Finished.Select(f => f.Time).Should().Equal(50, 100, 155, 205, 260);
        run.Finished.Select(f => f.Product).Should().Equal(run.Loaded,
            "each batch leaves as its own entity, and a failure between batches invents no extra product");
    }

    [Fact]
    public void ABatchInterruptedByAFailure_LeavesAsItsOwnProductAfterTheRepair()
    {
        // fails after 30 of the 50 minutes, repaired by 35, the remaining 20 finish at 55
        var run = Line(work: 50, timeToFailure: 30, repair: 5, items: 1);

        run.Model.Run(1000);

        run.Finished.Should().ContainSingle();
        run.Finished[0].Time.Should().Be(55);
        run.Finished[0].Product.Should().BeSameAs(run.Loaded[0]);
    }

    [Fact]
    public void RepeatedMidBatchFailures_ReachingATie_KeepEveryProductAndDoNotStopTheRun()
    {
        // The tie arrives on the third batch: remaining 30 = time to failure 30 at t = 140.
        // E1: fail 30, repaired 35, done 55 (10 to failure left).
        // E2: fail 65, repaired 70, fail again 100 (10 left), repaired 105, done 115 (20 left).
        // E3: fail 135 (30 left), repaired 140, tie -> finishes at 170, failure due between batches.
        var run = Line(work: 50, timeToFailure: 30, repair: 5, items: 3);

        var act = () => run.Model.Run(1000);

        act.Should().NotThrow();
        run.Finished.Select(f => f.Time).Should().Equal(55, 115, 170);
        run.Finished.Select(f => f.Product).Should().Equal(run.Loaded);
    }
}
