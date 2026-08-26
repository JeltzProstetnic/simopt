using System.Collections.Generic;
using FluentAssertions;
using SimOpt.Mathematics.Stochastics.Distributions;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Enum;
using SimOpt.Simulation.Templates;
using Xunit;

namespace SimOpt.Tests.Simulation.Templates;

/// <summary>
/// SIM-90 — what a server emits when its service completes.
///
/// <para>
/// The default product generator returns <c>new TProduct()</c>, a fresh entity whose
/// <c>Identifier</c> is never set. `Buffer.Put` keys on that Identifier, so any server feeding a
/// downstream buffer threw <c>ArgumentNullException</c> and **every multi-stage topology was
/// unbuildable**. It also destroys entity identity, which makes an end-to-end cycle time
/// unmeasurable, because the thing arriving at the sink is not the thing that left the source.
/// </para>
/// <para>
/// The product factory is deferred: <c>StartWorking</c> hands the event instance a delegate that is
/// invoked once, when the finish event is raised. Measurement established that this happens
/// <i>before</i> <c>InternalFinishedHandler</c> clears <c>activeMaterial</c>, and that the finish
/// instance is raised before the auto-continue repeater regardless of insertion order, because
/// priority type is compared ahead of insertion order. So the ordinary path was never the problem.
/// The list can still be emptied out from under a pending finish though — by
/// <c>ClearActiveMaterial</c> mid-service, or by a reset-ordering regression of the SIM-91 kind —
/// so the batch is now snapshotted into the closure at scheduling time.
/// </para>
/// </summary>
public class ServerProductIdentityTests
{
    private static ConstantDoubleDistribution Constant(double v) => new(v, initialize: false);

    /// <summary>
    /// The snapshot: a pending finish must still build its product from the batch the service
    /// started with, even if the live material list is emptied while the service is in flight.
    /// </summary>
    [Fact]
    public void ClearingActiveMaterialMidService_DoesNotStarveThePendingProductFactory()
    {
        var model = new Model("m", 1, 0d);
        var seen = new List<int>();

        var server = new SimpleServer(model, Constant(0.5), id: "srv", name: "srv",
            createProduct: materials =>
            {
                seen.Add(materials.Count);
                return materials[0];
            })
        { PushAllowed = true };
        var sink = new SimpleSink(model, id: "snk", name: "snk");
        sink.ConnectTo(server);

        model.Reset(1);
        var entity = new SimpleEntity(model, "E1", "E1");
        server.Put(entity);
        server.Start();

        // Empty the live list while the service is still running. Before the snapshot the deferred
        // factory ran against this now-empty list and a pass-through factory threw IndexOutOfRange;
        // measured on current main, it saw a material count of 0.
        model.Schedule(0.2, () => server.ClearActiveMaterial());
        model.Run(10.0);

        seen.Should().ContainSingle("the factory runs exactly once per scheduled service")
            .Which.Should().Be(1, "it must see the batch the service started with, not an emptied list");
        sink.Count.Should().Be(1);
    }

    /// <summary>
    /// Identity survives a server: the entity reaching the sink is the entity that left the source.
    /// This is the precondition for any end-to-end cycle-time measurement (SIM-63).
    /// </summary>
    [Fact]
    public void WithAPassThroughFactory_TheEntityReachingTheSink_IsTheOneThatLeftTheSource()
    {
        var model = new Model("m", 1, 0d);

        var created = new List<SimpleEntity>();
        var delivered = new List<SimpleEntity>();

        int n = 0;
        var source = new SimpleSource(model, Constant(2.0),
            () =>
            {
                var e = new SimpleEntity(model, $"E{++n}", $"E{n}");
                created.Add(e);
                return e;
            },
            autoStartDelay: 0d, id: "src", name: "src");
        var buffer = new SimpleBuffer(model, QueueRule.FIFO, id: "buf", name: "buf", maxCapacity: 100);
        var server = new SimpleServer(model, Constant(0.5), id: "srv", name: "srv",
            createProduct: m => m[0])
        { AutoContinue = true };
        var sink = new SimpleSink(model, id: "snk", name: "snk");

        buffer.ConnectTo(source);
        server.ConnectTo(buffer);
        buffer.ItemReceivedEvent.AddHandler((_, _) => { if (server.Idle) server.Start(); });
        sink.ConnectTo(server);
        server.EntityFinishedEvent.AddHandler((_, product) => delivered.Add(product));

        model.Reset(1);
        model.Run(10.0);

        delivered.Should().NotBeEmpty();
        // Reference equality, not Identifier equality. A generator that copied the identifier onto
        // a fresh entity would satisfy a string comparison while still breaking cycle time.
        foreach (SimpleEntity product in delivered)
            created.Should().Contain(e => ReferenceEquals(e, product));
    }

    /// <summary>
    /// A two-stage line flows end to end, with an exactly computable count.
    /// </summary>
    [Fact]
    public void ATwoStageLine_FlowsEndToEnd_WithAnExactlyComputableCount()
    {
        var model = new Model("m", 1, 0d);

        int n = 0;
        var source = new SimpleSource(model, Constant(2.0),
            () => new SimpleEntity(model, $"E{++n}", $"E{n}"),
            autoStartDelay: 0d, id: "src", name: "src");
        var buf1 = new SimpleBuffer(model, QueueRule.FIFO, id: "b1", name: "b1", maxCapacity: 100);
        var srv1 = new SimpleServer(model, Constant(0.7), id: "s1", name: "s1", createProduct: m => m[0])
        { AutoContinue = true };
        var buf2 = new SimpleBuffer(model, QueueRule.FIFO, id: "b2", name: "b2", maxCapacity: 100);
        var srv2 = new SimpleServer(model, Constant(0.5), id: "s2", name: "s2", createProduct: m => m[0])
        { AutoContinue = true };
        var sink = new SimpleSink(model, id: "snk", name: "snk");

        buf1.ConnectTo(source);
        srv1.ConnectTo(buf1);
        buf1.ItemReceivedEvent.AddHandler((_, _) => { if (srv1.Idle) srv1.Start(); });
        buf2.ConnectTo(srv1);
        srv2.ConnectTo(buf2);
        buf2.ItemReceivedEvent.AddHandler((_, _) => { if (srv2.Idle) srv2.Start(); });
        sink.ConnectTo(srv2);

        model.Reset(1);
        model.Run(100.0);

        // Arrivals at t = 0, 2, 4, …; both stations are faster than the arrival rate so nothing
        // queues, and entity k leaves the second station at 2k + 1.2. Entities 0..49 complete by
        // t = 100 (the 49th at 99.2), so exactly 50 reach the sink. The constants are chosen to
        // avoid a tie at the horizon, so the expected count is unambiguous.
        sink.Count.Should().Be(50);

        // Before SIM-90 this topology could not run at all: the first server emitted an entity with
        // a null Identifier and buf2.Put threw ArgumentNullException at simulated time 1.170.
        buf2.Count.Should().Be(0);
    }
}
