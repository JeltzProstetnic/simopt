using System;
using System.Collections.Generic;
using FluentAssertions;
using SimOpt.Mathematics.Stochastics.Distributions;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Enum;
using SimOpt.Simulation.Templates;
using Xunit;

namespace SimOpt.Tests.Simulation.Engine;

/// <summary>
/// SIM-89 — <see cref="Model.Reset(int)"/> must tolerate the model's item collection growing
/// while it is resetting.
///
/// <para>
/// A <see cref="Source{TEntity}"/> constructed with <c>autoStartDelay: 0</c> calls
/// <c>Start(0)</c> from its own <c>Reset</c>, which raises <c>EntityCreatedEvent</c>
/// <b>synchronously</b> — so the generator runs, a new entity is constructed, and the
/// <see cref="Entity"/> constructor registers it with the model. That registration mutates the
/// very dictionary <c>Model.Reset</c> is enumerating, and .NET throws
/// <c>InvalidOperationException: Collection was modified</c> out of the middle of the reset.
/// </para>
/// <para>
/// This is not an exotic configuration. It is what every model the MCP head builds looks like
/// (<c>ModelRegistry</c> passes <c>autoStartDelay: 0</c> for every source), so <b>every</b>
/// <c>run_simulation</c> call was hitting it. It stayed invisible because model construction threw
/// earlier for an unrelated reason and the tool layer serialises exceptions into an <c>error</c>
/// field rather than failing loudly.
/// </para>
/// <para>
/// The fix snapshots the collection before iterating, which is the same remedy SIM-58 applied to
/// <c>ResourceManager.Reset</c>'s aliasing defect. An entity created <i>during</i> the reset is
/// newly constructed and therefore already in its initial state, so it does not need resetting.
/// </para>
/// </summary>
public class ModelResetDuringEntityCreationTests
{
    private static SimpleSource BuildAutoStartSource(Model model)
    {
        var interval = new ConstantDoubleDistribution();
        interval.Configure(1.0);

        int counter = 0;
        return new SimpleSource(
            model,
            interval,
            () => new SimpleEntity(model, $"E{++counter}", $"E{counter}"),
            autoStartDelay: 0d,
            id: "src",
            name: "src");
    }

    [Fact]
    public void Reset_WithAnAutoStartingSource_DoesNotThrow()
    {
        var model = new Model("m", 1, 0d);
        BuildAutoStartSource(model);

        Action reset = () => model.Reset(2);

        reset.Should().NotThrow<InvalidOperationException>(
            "an auto-starting source creates an entity during Reset, and registering that entity " +
            "must not invalidate the enumeration Reset is performing");
    }

    [Fact]
    public void Reset_IsRepeatable_WithAnAutoStartingSource()
    {
        var model = new Model("m", 1, 0d);
        BuildAutoStartSource(model);
        var sink = new SimpleSink(model, id: "snk", name: "snk");
        sink.ConnectTo(BuildServerFedBy(model));

        Action resetTwice = () =>
        {
            model.Reset(2);
            model.Reset(3);
            model.Reset(2);
        };

        // Reset is the innermost loop of IProblem.Evaluate and of the replication runner, so it is
        // called far more often than Run. A reset path that only survives its first call would
        // corrupt every optimisation and every replicated experiment built on top of it.
        resetTwice.Should().NotThrow();
    }

    private static SimpleServer BuildServerFedBy(Model model)
    {
        var service = new ConstantDoubleDistribution();
        service.Configure(0.5);
        return new SimpleServer(model, service, id: "srv", name: "srv");
    }

    /// <summary>
    /// SIM-91 — every run of a model must be identical, including the first, and the arrival a
    /// source generates at time zero must actually be served rather than destroyed by the reset
    /// that generated it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Before the fix this single topology exhibited two different wrong answers depending on how
    /// the previous run had left the server. <c>Source.Reset</c> raised its t=0 arrival
    /// synchronously, mid-way through <c>Model.Reset</c>'s entity pass, so the arrival met
    /// downstream entities that had not been reset yet:
    /// </para>
    /// <list type="bullet">
    /// <item>server still idle ⇒ it pulled the entity and scheduled finish events into the
    /// already-cleared calendar. <c>Server.Reset</c> then cleared <c>working</c> and the material
    /// but could not cancel those events, so they fired during the run and delivered a phantom
    /// product.</item>
    /// <item>server still busy ⇒ the entity waited in the buffer and <c>Buffer.Reset</c> destroyed
    /// it moments later.</item>
    /// </list>
    /// <para>
    /// The count of 5 discriminates all three behaviours, which is why it is asserted as a literal:
    /// 4 means the t=0 arrival was destroyed, 5 with a differing event count means the phantom
    /// variant, and 5 stable across every run means fixed.
    /// </para>
    /// </remarks>
    [Fact]
    public void Reset_WithAutoStartSourceFeedingABuffer_IsIdempotent_AndServesTheTimeZeroArrival()
    {
        var model = new Model("m", 42, 0d);

        var interval = new ConstantDoubleDistribution();
        interval.Configure(2.0);
        var service = new ConstantDoubleDistribution();
        service.Configure(1.5);

        int n = 0;
        var source = new SimpleSource(
            model, interval, () => new SimpleEntity(model, $"E{++n}", $"E{n}"),
            autoStartDelay: 0d, id: "src", name: "src");
        var buffer = new SimpleBuffer(model, QueueRule.FIFO, id: "buf", name: "buf", maxCapacity: 10);
        var server = new SimpleServer(model, service, id: "srv", name: "srv") { AutoContinue = true };
        var sink = new SimpleSink(model, id: "snk", name: "snk");

        buffer.ConnectTo(source);
        server.ConnectTo(buffer);
        buffer.ItemReceivedEvent.AddHandler((_, _) => { if (server.Idle) server.Start(); });
        sink.ConnectTo(server);

        model.Reset(42);

        var results = new List<(int Sink, long Events)>();
        for (int i = 0; i < 3; i++)
        {
            model.Reset();
            model.Run(10.0);
            results.Add((sink.Count, model.EventCounter));
        }

        // Arrivals at t = 0, 2, 4, 6, 8, 10 with a constant service of 1.5 < 2, so nothing ever
        // queues and completions fall at 1.5, 3.5, 5.5, 7.5, 9.5 — exactly five by t = 10.
        results[0].Sink.Should().Be(5, "the arrival generated at t=0 must be served, not discarded");
        results[1].Should().Be(results[0], "the first run must not differ from the second");
        results[2].Should().Be(results[0]);
    }
}
