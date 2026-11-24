using System.Collections.Generic;
using FluentAssertions;
using SimOpt.Basics.Utilities;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Processes;
using SimOpt.Simulation.Templates;
using Xunit;

namespace SimOpt.Tests.Simulation.Processes;

/// <summary>
/// SIM-126, first slice: a behaviour written as one iterator method that yields waits, as in SimPy,
/// driven by SimOpt's event engine (`docs/plans/2026-11-25-sim-126-process-modelling.md`).
/// </summary>
public class ProcessTests
{
    [Fact]
    public void ATimeout_ResumesTheProcessAfterItsDelay()
    {
        var model = new Model("m", 1, 0d);
        var log = new List<double>();
        IEnumerable<Wait> Body(Process p)
        {
            log.Add(p.Now);
            yield return p.Timeout(5);
            log.Add(p.Now);
            yield return p.Timeout(3);
            log.Add(p.Now);
        }
        var process = model.StartProcess(Body, id: "p");

        model.Run(20);

        log.Should().Equal(0, 5, 8);
        process.IsAlive.Should().BeFalse();
    }

    [Fact]
    public void AProcessStartedInsideAHandler_RunsAfterThatHandler_InTheSameInstant()
    {
        var model = new Model("m", 1, 0d);
        var log = new List<(double, string)>();
        IEnumerable<Wait> Body(Process p)
        {
            log.Add((p.Now, "process"));
            yield break;
        }
        model.Schedule(2, () =>
        {
            model.StartProcess(Body, id: "late");
            log.Add((model.CurrentTime, "handler"));
        });

        model.Run(10);

        log.Should().Equal((2d, "handler"), (2d, "process"));
    }

    [Fact]
    public void StockWaits_SuspendTheProcessUntilTheStockAllowsIt()
    {
        var model = new Model("m", 1, 0d);
        var stock = new Stock(model, capacity: 25, id: "stock", name: "stock");
        var log = new List<(double, string)>();
        IEnumerable<Wait> Producer(Process p)
        {
            for (int i = 0; i < 3; i++)
            {
                yield return p.Timeout(2);
                yield return stock.PutWait(10);        // 10 at t = 2, 4 and 6
                log.Add((p.Now, "put"));
            }
        }
        IEnumerable<Wait> Consumer(Process p)
        {
            yield return stock.TakeWait(15);           // there are 20 at t = 4
            log.Add((p.Now, "took"));
        }
        model.StartProcess(Producer, id: "producer");
        model.StartProcess(Consumer, id: "consumer");

        model.Run(20);

        log.Should().Equal((2d, "put"), (4d, "put"), (4d, "took"), (6d, "put"));
        stock.Level.Should().Be(15);
    }

    [Fact]
    public void YieldingAnotherProcess_WaitsUntilItHasFinished()
    {
        var model = new Model("m", 1, 0d);
        var log = new List<(double, string)>();
        IEnumerable<Wait> Child(Process p)
        {
            yield return p.Timeout(7);
            log.Add((p.Now, "child done"));
        }
        IEnumerable<Wait> Parent(Process p)
        {
            Process child = p.Model.StartProcess(Child, id: "child");
            yield return child;
            log.Add((p.Now, "parent resumed"));
            yield return child;                        // already finished: resumes in the same instant
            log.Add((p.Now, "parent again"));
        }
        model.StartProcess(Parent, id: "parent");

        model.Run(20);

        log.Should().Equal((7d, "child done"), (7d, "parent resumed"), (7d, "parent again"));
    }

    [Fact]
    public void AProcessStartedWhileStopped_StartsAgainFromTheBeginningOnEveryReset()
    {
        var model = new Model("m", 1, 0d);
        var log = new List<double>();
        IEnumerable<Wait> Body(Process p)
        {
            log.Add(p.Now);
            yield return p.Timeout(5);
            log.Add(p.Now);
        }
        model.StartProcess(Body, id: "p");

        model.Reset(1);
        model.Run(20);
        model.Reset(2);
        model.Run(20);

        log.Should().Equal(0, 5, 0, 5);
    }

    [Fact]
    public void AProcessStartedDuringARun_DoesNotSurviveAReset()
    {
        var model = new Model("m", 1, 0d);
        var log = new List<(int, double)>();
        int run = 0;
        IEnumerable<Wait> Child(Process p)
        {
            yield return p.Timeout(10);
            log.Add((run, p.Now));
        }
        IEnumerable<Wait> Parent(Process p)
        {
            yield return p.Timeout(1);
            p.Model.StartProcess(Child, id: "child");
        }
        model.StartProcess(Parent, id: "parent");

        run = 1; model.Reset(1); model.Run(5);   // the child is still waiting when the run stops
        run = 2; model.Reset(2); model.Run(20);

        log.Should().Equal((2, 11d));
    }

    [Fact]
    public void AProcessWithAnId_HasTheDefaultSeedId()
    {
        var model = new Model("m", 1, 0d);
        IEnumerable<Wait> Body(Process p) { yield break; }

        var process = model.StartProcess(Body, id: "filler");

        process.SeedID.Should().Be(StableHash.Of("filler"));
    }
}
