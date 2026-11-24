using System.Collections.Generic;
using FluentAssertions;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Processes;
using SimOpt.Simulation.Templates;
using Xunit;

namespace SimOpt.Tests.Simulation.Processes;

/// <summary>
/// SIM-126, slice 3: waiting for all or for any of several waits, interrupts, and withdrawing a
/// request that is no longer wanted (a customer who gives up, a process that is interrupted).
/// </summary>
public class ConditionAndInterruptTests
{
    [Fact]
    public void AllOf_ResumesWhenTheLastWaitCompletes()
    {
        var model = new Model("m", 1, 0d);
        var log = new List<double>();
        IEnumerable<Wait> Body(Process p)
        {
            yield return Wait.AllOf(p.Timeout(3), p.Timeout(5));
            log.Add(p.Now);
        }
        model.StartProcess(Body, id: "p");

        model.Run(20);

        log.Should().Equal(5);
    }

    [Fact]
    public void AnyOf_ResumesAtTheFirst_NamesIt_AndIgnoresTheRest()
    {
        var model = new Model("m", 1, 0d);
        var log = new List<(double, bool)>();
        IEnumerable<Wait> Body(Process p)
        {
            Wait soon = p.Timeout(3), late = p.Timeout(5);
            var any = Wait.AnyOf(soon, late);
            yield return any;
            log.Add((p.Now, any.Winner == soon));
            yield return p.Timeout(10);
            log.Add((p.Now, false));
        }
        model.StartProcess(Body, id: "p");

        model.Run(30);

        log.Should().Equal((3d, true), (13d, false));
    }

    [Fact]
    public void ACustomerWhoGivesUp_LeavesTheQueue()
    {
        var model = new Model("m", 1, 0d);
        var machine = new Resource(model, id: "machine", name: "machine");
        var log = new List<(double, string)>();
        IEnumerable<Wait> Holder(Process p)
        {
            yield return machine.Request();
            yield return p.Timeout(10);
            machine.Release();
        }
        IEnumerable<Wait> Customer(Process p)
        {
            Wait request = machine.Request();
            var any = Wait.AnyOf(request, p.Timeout(4));
            yield return any;
            log.Add((p.Now, any.Winner == request ? "served" : "gave up"));
        }
        model.StartProcess(Holder, id: "holder");
        model.StartProcess(Customer, id: "customer");

        model.Run(5);
        machine.Waiting.Should().Be(0, "the request was withdrawn when the customer gave up");
        model.Run(20);

        log.Should().Equal((4d, "gave up"));
        machine.InUse.Should().Be(0, "nobody holds the machine after the holder released it");
    }

    [Fact]
    public void AGrantedRequestThatLosesTheRace_GivesItsUnitBack()
    {
        var model = new Model("m", 1, 0d);
        var machine = new Resource(model, id: "machine", name: "machine");
        var log = new List<string>();
        IEnumerable<Wait> Body(Process p)
        {
            Wait request = machine.Request();
            var any = Wait.AnyOf(p.Timeout(0), request);   // the timeout's event comes first in this instant
            yield return any;
            log.Add(any.Winner == request ? "request" : "timeout");
        }
        model.StartProcess(Body, id: "p");

        model.Run(10);

        log.Should().Equal("timeout");
        machine.InUse.Should().Be(0, "the unit granted to the losing request goes back");
    }

    [Fact]
    public void AnInterrupt_ResumesTheProcessAtOnce_AndItsOldWaitNoLongerCounts()
    {
        var model = new Model("m", 1, 0d);
        var log = new List<(double, bool)>();
        IEnumerable<Wait> Worker(Process p)
        {
            yield return p.Timeout(10);
            log.Add((p.Now, p.Interrupted));
            yield return p.Timeout(2);
            log.Add((p.Now, p.Interrupted));
        }
        Process worker = model.StartProcess(Worker, id: "worker");
        model.Schedule(3, () => worker.Interrupt());

        model.Run(30);

        log.Should().Equal((3d, true), (5d, false));
    }

    [Fact]
    public void AnInterruptedProcess_WithdrawsItsResourceRequest()
    {
        var model = new Model("m", 1, 0d);
        var machine = new Resource(model, id: "machine", name: "machine");
        IEnumerable<Wait> Holder(Process p)
        {
            yield return machine.Request();
            yield return p.Timeout(10);
            machine.Release();
        }
        IEnumerable<Wait> Waiter(Process p)
        {
            yield return machine.Request();
            if (!p.Interrupted) machine.Release();
        }
        model.StartProcess(Holder, id: "holder");
        Process waiter = model.StartProcess(Waiter, id: "waiter");
        model.Schedule(2, () => waiter.Interrupt());

        model.Run(3);
        machine.Waiting.Should().Be(0);
        model.Run(20);
        machine.InUse.Should().Be(0);
    }

    [Fact]
    public void AStockTakeThatTimesOut_IsWithdrawn_AndALaterPutStays()
    {
        var model = new Model("m", 1, 0d);
        var stock = new Stock(model, id: "stock", name: "stock");
        IEnumerable<Wait> Body(Process p)
        {
            yield return Wait.AnyOf(stock.TakeWait(5), p.Timeout(2));
        }
        model.StartProcess(Body, id: "p");
        model.Schedule(4, () => stock.Put(5));

        model.Run(10);

        stock.WaitingTakers.Should().Be(0);
        stock.Level.Should().Be(5, "the withdrawn take must not consume the later put");
    }
}
