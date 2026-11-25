using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Enum;
using SimOpt.Simulation.Events;
using SimOpt.Simulation.Processes;
using SimOpt.Simulation.Templates;
using Xunit;

namespace SimOpt.Tests.Simulation.Processes;

/// <summary>
/// SIM-126, slice 4: a process waits for the next raise of a template's event, so process-style
/// logic can sit on top of a Source/Buffer/Server model: <c>Wait.Next(buffer.ItemReceivedEvent)</c>.
/// The process continues in the same instant, after the event and its other handlers, and reads what
/// the raise carried. A wait that is won by another, interrupted or abandoned by a reset leaves no
/// handler behind, so it cannot add an event to a later replication.
/// </summary>
public class TemplateEventWaitTests
{
    private static Buffer<string> Queue(Model model) => new Buffer<string>(model, QueueRule.FIFO, maxCapacity: int.MaxValue);

    [Fact]
    public void AProcessSeesEachItemABufferReceives_AtTheInstantItArrives()
    {
        var model = new Model("m", 1, 0d);
        var buffer = Queue(model);
        var seen = new List<(double, string)>();
        IEnumerable<Wait> Watcher(Process p)
        {
            for (int i = 0; i < 2; i++)
            {
                var arrival = Wait.Next(buffer.ItemReceivedEvent);
                yield return arrival;
                seen.Add((p.Now, arrival.Args));
            }
        }
        model.StartProcess(Watcher, id: "watcher");
        model.Schedule(3, () => buffer.Put("a"));
        model.Schedule(5, () => buffer.Put("b"));

        model.Run(10);

        seen.Should().Equal((3d, "a"), (5d, "b"));
        buffer.ItemReceivedEvent.HandlerCount.Should().Be(0, "a fired wait removes its handler");
    }

    [Fact]
    public void TheProcessContinuesAfterTheEventsOtherHandlers()
    {
        var model = new Model("m", 1, 0d);
        var buffer = Queue(model);
        var order = new List<string>();
        IEnumerable<Wait> Watcher(Process p)
        {
            yield return Wait.Next(buffer.ItemReceivedEvent);
            order.Add("process sees " + buffer.Count);
            buffer.Get();
        }
        model.StartProcess(Watcher, id: "watcher");
        model.Schedule(1, () =>
        {
            buffer.ItemReceivedEvent.AddHandler((_, item) => order.Add("handler " + item));
            buffer.Put("a");
            order.Add("put returned");
        });

        model.Run(10);

        order.Should().Equal("handler a", "put returned", "process sees 1");
        buffer.Count.Should().Be(0);
    }

    [Fact]
    public void AFilter_SkipsRaisesItDoesNotWant()
    {
        var model = new Model("m", 1, 0d);
        var buffer = Queue(model);
        var seen = new List<(double, string)>();
        IEnumerable<Wait> Watcher(Process p)
        {
            var wanted = Wait.Next(buffer.ItemReceivedEvent, (_, item) => item.StartsWith("b"));
            yield return wanted;
            seen.Add((p.Now, wanted.Args));
        }
        model.StartProcess(Watcher, id: "watcher");
        model.Schedule(1, () => buffer.Put("a1"));
        model.Schedule(2, () => buffer.Put("b1"));
        model.Schedule(3, () => buffer.Put("b2"));

        model.Run(10);

        seen.Should().Equal((2d, "b1"));
    }

    [Fact]
    public void LosingAnAnyOf_RemovesTheHandler_AndALaterRaiseResumesNothing()
    {
        var model = new Model("m", 1, 0d);
        var buffer = Queue(model);
        var log = new List<(double, string)>();
        IEnumerable<Wait> Watcher(Process p)
        {
            var arrival = Wait.Next(buffer.ItemReceivedEvent);
            var first = Wait.AnyOf(arrival, p.Timeout(2));
            yield return first;
            log.Add((p.Now, first.Winner == arrival ? "item" : "timeout"));
            yield return p.Timeout(100);
        }
        model.StartProcess(Watcher, id: "watcher");
        model.Schedule(5, () => buffer.Put("late"));

        model.Run(10);

        log.Should().Equal((2d, "timeout"));
        buffer.ItemReceivedEvent.HandlerCount.Should().Be(0);
    }

    [Fact]
    public void AnInterrupt_RemovesTheHandler()
    {
        var model = new Model("m", 1, 0d);
        var buffer = Queue(model);
        var log = new List<(double, bool)>();
        IEnumerable<Wait> Watcher(Process p)
        {
            yield return Wait.Next(buffer.ItemReceivedEvent);
            log.Add((p.Now, p.Interrupted));
        }
        Process watcher = model.StartProcess(Watcher, id: "watcher");
        model.Schedule(1, () => watcher.Interrupt());
        model.Schedule(2, () => buffer.Put("a"));

        model.Run(10);

        log.Should().Equal((1d, true));
        buffer.ItemReceivedEvent.HandlerCount.Should().Be(0);
    }

    [Fact]
    public void AResetAbandonsTheWait_SoEveryReplicationRunsTheSameEvents()
    {
        var model = new Model("m", 1, 0d);
        var buffer = Queue(model);
        var seen = new List<(double, string)>();
        IEnumerable<Wait> Watcher(Process p)
        {
            while (true)
            {
                var arrival = Wait.Next(buffer.ItemReceivedEvent);
                yield return arrival;
                seen.Add((p.Now, arrival.Args));
            }
        }
        model.StartProcess(Watcher, id: "watcher");
        IEnumerable<Wait> Feeder(Process p)
        {
            for (int i = 0; i < 3; i++)
            {
                yield return p.Timeout(2);
                buffer.Put("x" + i);
            }
        }
        model.StartProcess(Feeder, id: "feeder");

        var runs = new List<string>();
        for (int r = 0; r < 3; r++)
        {
            seen.Clear();
            model.Reset(7);
            model.Run(10);   // ends with the watcher waiting for an arrival that never comes
            runs.Add(model.EventCounter + ":" + string.Join(",", seen.Select(s => s.Item1 + s.Item2)));
            buffer.ItemReceivedEvent.HandlerCount.Should().Be(1, "only the live wait is subscribed");
        }

        runs.Distinct().Should().ContainSingle("a stale handler would add an event to the next replication");
        runs[0].Should().EndWith("2x0,4x1,6x2");
    }

    [Fact]
    public void UnaryAndTernaryEventsHandOverWhatTheyCarry()
    {
        var model = new Model("m", 1, 0d);
        var unary = new UnaryEvent<int>("u");
        var ternary = new TernaryEvent<object, string, double>("t");
        string got = "";
        IEnumerable<Wait> Watcher(Process p)
        {
            var u = Wait.Next(unary);
            yield return u;
            var t = Wait.Next(ternary, (_, s, _) => s == "yes");
            yield return t;
            got = $"{u.Value} {t.Sender} {t.Args} {t.Data} at {p.Now}";
        }
        model.StartProcess(Watcher, id: "watcher");
        model.Schedule(1, () => unary.Raise(42));
        model.Schedule(2, () => ternary.Raise("s", "no", 1.5));
        model.Schedule(3, () => ternary.Raise("s", "yes", 2.5));

        model.Run(10);

        got.Should().Be("42 s yes 2.5 at 3");
    }
}
