using System;
using System.Collections.Generic;
using FluentAssertions;
using SimOpt.Mathematics.Stochastics.Distributions;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Entities;
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
}
