using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using SimOpt.Mathematics.Stochastics.Distributions;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Templates;
using Xunit;

namespace SimOpt.Tests.Simulation.Templates;

/// <summary>
/// SIM-139 (MG decision Q-SRC-START, 2026-11-25): an auto-starting source makes its first item after its
/// auto-start delay, on a freshly built model exactly as after <c>Model.Reset</c>. Before, a fresh model
/// started the source inside its constructor: with a delay of 0 the first item was raised before anything
/// was connected and lost, and the item one interval later came from the default generator, a phantom with
/// no identifier; real items began at the second interval.
/// </summary>
public class SourceStartTests
{
    /// <summary>A source, constant interval 1, into a sink that records when each item arrives and what it is.</summary>
    private static (Model Model, List<(double Time, string? Id)> Arrivals, Func<int> Made) Line(double autoStartDelay)
    {
        var model = new Model("start", 7, 0d);
        int made = 0;
        var source = new SimpleSource(model, 11, new ConstantDoubleDistribution(1.0, initialize: false),
            () => { made++; return new SimpleEntity(model, $"E{made}", $"E{made}"); },
            autoStartDelay: autoStartDelay, id: "src", name: "src");
        var sink = new SimpleSink(model, id: "snk", name: "snk");
        var arrivals = new List<(double, string?)>();
        sink.ConnectTo(source);
        sink.ItemReceived.AddHandler((_, e) => arrivals.Add((model.CurrentTime, e?.Identifier)));
        return (model, arrivals, () => made);
    }

    [Fact]
    public void AFreshModel_MakesItsFirstItemAtTimeZero_FromTheGenerator()
    {
        var (model, arrivals, made) = Line(autoStartDelay: 0d);

        model.Run(3.5);

        arrivals.Select(a => a.Time).Should().Equal(0d, 1d, 2d, 3d);
        arrivals.Select(a => a.Id).Should().Equal("E1", "E2", "E3", "E4");
        made().Should().Be(4, "every item that arrives was made by the source's generator, no phantom");
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(0.5d)]
    public void AFreshModel_AndTheSameModelAfterAReset_MakeTheSameItemsAtTheSameTimes(double autoStartDelay)
    {
        var (fresh, freshArrivals, _) = Line(autoStartDelay);
        fresh.Run(5.5);

        var (reset, resetArrivals, _) = Line(autoStartDelay);
        reset.Reset(7);
        reset.Run(5.5);

        freshArrivals.Should().Equal(resetArrivals);
        freshArrivals[0].Time.Should().Be(autoStartDelay);
    }
}
