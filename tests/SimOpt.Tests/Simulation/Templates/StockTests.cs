using System;
using System.Collections.Generic;
using FluentAssertions;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Statistics;
using SimOpt.Simulation.Templates;
using Xunit;

namespace SimOpt.Tests.Simulation.Templates;

/// <summary>
/// SIM-127: a stock block, the counterpart of SimPy's <c>Container</c>. It holds a level (pieces,
/// kilograms) rather than entities. A take is all-or-nothing and waits until enough is there; a put
/// waits while it would exceed the capacity. Both queues are strictly first come, first served, so a
/// small request never overtakes a large one that came first (as in SimPy). A waiting request is
/// resumed by a zero-delay event at the instant it is served (SIM-128), in the order served.
/// Integrators built this themselves before (stock at a filling plant, a shelf).
/// </summary>
public class StockTests
{
    [Fact]
    public void AnImmediateTake_LowersTheLevel_AndResumesTheTakerInTheSameInstant()
    {
        var model = new Model("m", 1, 0d);
        var stock = new Stock(model, initialLevel: 10, id: "stock", name: "stock");
        var log = new List<(double, double)>();
        model.Schedule(2, () => stock.Take(4, () => log.Add((model.CurrentTime, stock.Level))));

        model.Run(10);

        log.Should().Equal((2d, 6d));
        stock.Level.Should().Be(6);
    }

    [Fact]
    public void ATakeWaitsForTheWholeAmount_AndTakesNothingUntilThen()
    {
        var model = new Model("m", 1, 0d);
        var stock = new Stock(model, id: "stock", name: "stock");
        var taken = new List<double>();
        var levels = new List<double>();
        model.Schedule(0, () => stock.Take(5, () => taken.Add(model.CurrentTime)));
        model.Schedule(1, () => { stock.Put(3); levels.Add(stock.Level); });
        model.Schedule(2, () => { stock.Put(3); levels.Add(stock.Level); });

        model.Run(10);

        levels.Should().Equal(3, 1); // nothing taken at t = 1; at t = 2 the 5 go at once
        taken.Should().Equal(2);
        stock.WaitingTakers.Should().Be(0);
    }

    [Fact]
    public void TakersAreServedFirstComeFirstServed_ASmallOneNeverOvertakesALargeOne()
    {
        var model = new Model("m", 1, 0d);
        var stock = new Stock(model, id: "stock", name: "stock");
        var order = new List<(string, double)>();
        model.Schedule(0, () =>
        {
            stock.Take(5, () => order.Add(("large", model.CurrentTime)));
            stock.Take(1, () => order.Add(("small", model.CurrentTime)));
        });
        model.Schedule(1, () => stock.Put(2)); // enough for the small one, but it waits behind the large
        model.Schedule(2, () => stock.Put(4));

        model.Run(10);

        order.Should().Equal(("large", 2d), ("small", 2d));
        stock.Level.Should().Be(0);
    }

    [Fact]
    public void APutWaitsWhileItWouldExceedTheCapacity()
    {
        var model = new Model("m", 1, 0d);
        var stock = new Stock(model, initialLevel: 8, capacity: 10, id: "stock", name: "stock");
        var put = new List<(double, double)>();
        model.Schedule(1, () => stock.Put(5, () => put.Add((model.CurrentTime, stock.Level))));
        model.Schedule(3, () => stock.Take(4, () => { }));

        model.Run(10);

        put.Should().Equal((3d, 9d));
        stock.WaitingPutters.Should().Be(0);
    }

    [Fact]
    public void ResetRestoresTheInitialLevel_AndDropsWaitingRequests()
    {
        var model = new Model("m", 1, 0d);
        var stock = new Stock(model, initialLevel: 2, id: "stock", name: "stock");
        model.Schedule(1, () => stock.Take(7, () => { }));
        model.Schedule(2, () => stock.Put(1));
        model.Run(10);
        stock.WaitingTakers.Should().Be(1);
        stock.Level.Should().Be(3);

        model.Reset(2);

        stock.Level.Should().Be(2);
        stock.WaitingTakers.Should().Be(0);
    }

    [Fact]
    public void TheLevelAndTheEmptyShare_AreTimeWeighted()
    {
        var model = new Model("m", 1, 0d);
        var stock = new Stock(model, id: "stock", name: "stock");
        var level = Instrumentation.ObserveStockLevel(stock);
        var empty = Instrumentation.ObserveStockEmpty(stock);
        model.Schedule(2, () => stock.Put(10));          // 0 on [0, 2), 10 on [2, 6)
        model.Schedule(6, () => stock.Take(6, () => { })); // 4 on [6, 10)
        model.Schedule(10, () => { });

        model.Run(10);

        level.TimeAverage(10).Should().BeApproximately((0 * 2 + 10 * 4 + 4 * 4) / 10d, 1e-12);
        empty.TimeAverage(10).Should().BeApproximately(0.2, 1e-12, "empty on [0, 2) of [0, 10)");
    }

    [Fact]
    public void ImpossibleRequests_AreRefusedAtOnce()
    {
        var model = new Model("m", 1, 0d);
        var stock = new Stock(model, initialLevel: 1, capacity: 10, id: "stock", name: "stock");

        ((Action)(() => stock.Take(11, () => { }))).Should().Throw<ArgumentOutOfRangeException>("more than the capacity can never be taken");
        ((Action)(() => stock.Put(11))).Should().Throw<ArgumentOutOfRangeException>("more than the capacity can never be put");
        ((Action)(() => stock.Put(0))).Should().Throw<ArgumentOutOfRangeException>();
        ((Action)(() => stock.Take(-1, () => { }))).Should().Throw<ArgumentOutOfRangeException>();
        ((Action)(() => new Stock(model, initialLevel: 11, capacity: 10, id: "s2", name: "s2"))).Should().Throw<ArgumentOutOfRangeException>();
    }
}
