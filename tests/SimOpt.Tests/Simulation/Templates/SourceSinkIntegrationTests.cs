using FluentAssertions;
using SimOpt.Mathematics.Stochastics.Distributions;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Enum;
using SimOpt.Simulation.Templates;
using Xunit;

namespace SimOpt.Tests.Simulation.Templates;

public class SourceSinkIntegrationTests
{
    [Fact]
    public void SourceToSink_ItemsFlowCorrectly()
    {
        var model = new Model("Test", 42, 0.0);
        var interval = new ConstantDoubleDistribution(1.0, initialize: false);
        var source = new Source(model, interval, autoStartDelay: 0.0);
        var sink = new Sink(model, name: "TestSink");

        source.ConnectTo(sink);

        model.Run(5.5); // Should generate at t=0, 1, 2, 3, 4, 5 → 6 items

        sink.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public void SourceToSink_CountMatchesExpected()
    {
        var model = new Model("Test", 42, 0.0);
        var interval = new ConstantDoubleDistribution(2.0, initialize: false);
        var source = new Source(model, interval, autoStartDelay: 0.0);
        var sink = new Sink(model, name: "TestSink");

        source.ConnectTo(sink);

        // Source autostart=0 makes its first item at t=0, then one every 2: t=0, 2 and 4 by 4.5.
        // SIM-139 (Q-SRC-START): a fresh model used to lose the t=0 item and count a phantom at t=2.
        model.Run(4.5);

        sink.Count.Should().Be(3);
    }

    [Fact]
    public void SourceToSink_Reset_ClearsSinkCount()
    {
        var model = new Model("Test", 42, 0.0);
        var interval = new ConstantDoubleDistribution(1.0, initialize: false);
        var source = new Source(model, interval, autoStartDelay: 0.0);
        var sink = new Sink(model, name: "TestSink");

        source.ConnectTo(sink);

        model.Run(3.5);
        int firstRunCount = sink.Count;
        firstRunCount.Should().BeGreaterThan(0);

        model.Reset(42);
        sink.Count.Should().Be(0);
    }

    [Fact]
    public void SourceToSink_MultipleSourcesOneSink()
    {
        var model = new Model("Test", 42, 0.0);
        var interval1 = new ConstantDoubleDistribution(1.0, initialize: false);
        var interval2 = new ConstantDoubleDistribution(1.0, initialize: false);
        var source1 = new Source(model, interval1, autoStartDelay: 0.0, name: "Source1");
        var source2 = new Source(model, interval2, autoStartDelay: 0.0, name: "Source2");
        var sink = new Sink(model, name: "TestSink");

        source1.ConnectTo(sink);
        source2.ConnectTo(sink);

        model.Run(3.5);

        // Both sources generate items, sink should receive from both
        sink.Count.Should().BeGreaterThan(3);
    }

    [Fact]
    public void Source_Running_TracksState()
    {
        var model = new Model("Test", 42, 0.0);
        var interval = new ConstantDoubleDistribution(1.0, initialize: false);
        var source = new Source(model, interval, autoStartDelay: 0.0);

        // SIM-139: the auto-start fires when the model runs, on a fresh model as after a reset
        source.Running.Should().BeFalse();
        model.Run(0.5);
        source.Running.Should().BeTrue();
    }
}
