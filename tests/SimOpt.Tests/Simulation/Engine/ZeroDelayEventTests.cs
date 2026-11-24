using System;
using System.Collections.Generic;
using FluentAssertions;
using SimOpt.Mathematics.Stochastics.Distributions;
using SimOpt.Mathematics.Stochastics.Interfaces;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Enum;
using SimOpt.Simulation.Exceptions;
using SimOpt.Simulation.Templates;
using Xunit;

namespace SimOpt.Tests.Simulation.Engine;

/// <summary>
/// SIM-128: while running, <c>Model.AddEventAt</c> refused the present ("You may not change the past
/// or present"), so a zero delay could only be raised by hand and a zero machining or repair draw
/// killed the run with a <c>CausalityException</c>. A process kernel such as SimPy treats a zero
/// delay as normal. An event scheduled at the current time now runs in the same instant, after the
/// event that scheduled it, and takes its place among the events still pending at that instant by
/// priority, ties in the order they were added. The past is still refused.
/// </summary>
public class ZeroDelayEventTests
{
    private static Priority Before() => new(type: PriorityType.SimWorldBeforeOthers);
    private static Priority User() => new(type: PriorityType.User);
    private static Priority After() => new(type: PriorityType.SimWorldAfterOthers);
    private static ConstantDoubleDistribution Constant(double v) => new(v, initialize: false);

    [Fact]
    public void AnEventScheduledAtTheCurrentTime_RunsInTheSameInstant_AfterTheEventThatScheduledIt()
    {
        var model = new Model("m", 1, 0d);
        var log = new List<(double, string)>();
        model.Schedule(5, () =>
        {
            log.Add((model.CurrentTime, "outer"));
            model.Schedule(0, () => log.Add((model.CurrentTime, "inner")));
        });

        model.Run(10);

        log.Should().Equal((5d, "outer"), (5d, "inner"));
    }

    [Fact]
    public void ZeroDelayEvents_JoinThePendingEventsOfTheInstant_ByPriority_TiesInTheOrderAdded()
    {
        var model = new Model("m", 1, 0d);
        var log = new List<string>();
        model.Schedule(5, User(), () =>
        {
            log.Add("A");
            model.Schedule(0, Before(), () => log.Add("Z-before"));
            model.Schedule(0, After(), () => log.Add("Z-after"));
            model.Schedule(0, User(), () => log.Add("Z-user"));
        });
        model.Schedule(5, User(), () => log.Add("B"));
        model.Schedule(5, After(), () => log.Add("C-after"));

        model.Run(10);

        log.Should().Equal("A", "Z-before", "B", "Z-user", "C-after", "Z-after");
    }

    [Fact]
    public void ThePast_IsStillRefused()
    {
        var model = new Model("m", 1, 0d);
        Exception? caught = null;
        model.Schedule(5, () =>
        {
            try { model.Schedule(-1, () => { }); }
            catch (Exception e) { caught = e; }
        });

        model.Run(10);

        caught.Should().BeOfType<CausalityException>();
    }

    private static (Model Model, SimpleSink Sink) Line(IDistribution<double> machining,
        IDistribution<double>? timeToFailure = null, IDistribution<double>? timeToRecover = null)
    {
        var model = new Model("m", 1, 0d);
        int n = 0;
        var source = new SimpleSource(model, Constant(1), () => new SimpleEntity(model, $"E{++n}", $"E{n}"),
            autoStartDelay: 0d, id: "src", name: "src");
        var buffer = new SimpleBuffer(model, QueueRule.FIFO, id: "buf", name: "buf", maxCapacity: int.MaxValue);
        var server = new SimpleServer(model, machining, id: "srv", name: "srv", createProduct: m => m[0],
            timeToFailure: timeToFailure, timeToRecover: timeToRecover)
        {
            AutoContinue = true,
            AutoRecover = timeToFailure != null,
            AutoRestart = timeToFailure != null,
            ContinueProductAfterFailure = timeToFailure != null,
        };
        var sink = new SimpleSink(model, id: "snk", name: "snk");
        buffer.ConnectTo(source);
        server.ConnectTo(buffer);
        buffer.ItemReceivedEvent.AddHandler((_, _) => { if (server.Idle) server.Start(); });
        sink.ConnectTo(server);
        model.Reset(1);
        return (model, sink);
    }

    [Fact]
    public void AServerWithZeroMachiningTime_PassesEveryItemOnInTheInstantItArrives()
    {
        var (model, sink) = Line(Constant(0));
        var arrivals = new List<double>();
        sink.ItemReceived.AddHandler((_, _) => arrivals.Add(model.CurrentTime));

        model.Run(10.5);

        arrivals.Should().Equal(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
    }

    [Fact]
    public void AZeroRepairTime_DoesNotStopTheRun()
    {
        // Work 2 a batch, a failure every 3 operating minutes, repaired in no time.
        var (model, sink) = Line(Constant(2), timeToFailure: Constant(3), timeToRecover: Constant(0));

        Action run = () => model.Run(100);

        run.Should().NotThrow();
        sink.Count.Should().BeGreaterThan(40, "an item every time unit, served in 2 with repairs that take no time");
    }
}
