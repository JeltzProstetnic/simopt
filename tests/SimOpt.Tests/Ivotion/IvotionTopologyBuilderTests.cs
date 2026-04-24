using System;
using System.Linq;
using FluentAssertions;
using SimOpt.Ivotion;
using SimOpt.Simulation.Templates;
using Xunit;
using Xunit.Abstractions;

namespace SimOpt.Tests.Ivotion;

/// <summary>
/// Tests for IvotionTopologyBuilder — parameterized construction of the Ivotion
/// packing-line simulation model (headless, no visualization dependency).
/// </summary>
public class IvotionTopologyBuilderTests
{
    private readonly ITestOutputHelper _out;

    public IvotionTopologyBuilderTests(ITestOutputHelper output) => _out = output;

    [Fact]
    public void Build_SingleRoland_HasOneRolandPrinter()
    {
        var sol = new IvotionSolution(new[] { 1, 1, 1, 1, 1, 15 });

        var handles = IvotionTopologyBuilder.Build(sol);

        handles.Rolands.Should().HaveCount(1);
        handles.Rolands[0].BatchSize.Should().Be(15);
    }

    [Fact]
    public void Build_TwoRolands_HasTwoRolandPrinters()
    {
        var sol = new IvotionSolution(new[] { 2, 1, 1, 1, 1, 15 });

        var handles = IvotionTopologyBuilder.Build(sol);

        handles.Rolands.Should().HaveCount(2);
        handles.Rolands[0].EntityName.Should().Be("roland_a");
        handles.Rolands[1].EntityName.Should().Be("roland_b");
    }

    [Fact]
    public void Build_BatchSizePropagatesToRolandPrinters()
    {
        var sol = new IvotionSolution(new[] { 2, 1, 1, 1, 1, 20 });

        var handles = IvotionTopologyBuilder.Build(sol);

        handles.Rolands.Should().AllSatisfy(r => r.BatchSize.Should().Be(20));
    }

    [Fact]
    public void Build_OperatorsHalveManualServiceTime()
    {
        var one = IvotionTopologyBuilder.Build(new IvotionSolution(new[] { 1, 1, 1, 1, 1, 15 }));
        var many = IvotionTopologyBuilder.Build(new IvotionSolution(new[] { 1, 2, 3, 2, 2, 15 }));

        many.EffectiveInspectTime.Should().BeApproximately(one.EffectiveInspectTime / 2.0, 1e-9);
        many.EffectivePackTime.Should().BeApproximately(one.EffectivePackTime / 3.0, 1e-9);
        many.EffectiveLabelTime.Should().BeApproximately(one.EffectiveLabelTime / 2.0, 1e-9);
        many.EffectiveSsbTime.Should().BeApproximately(one.EffectiveSsbTime / 2.0, 1e-9);
    }

    [Fact]
    public void Build_SingleRoland_RunsHeadlessAndProducesShippedItems()
    {
        var sol = new IvotionSolution(new[] { 1, 2, 2, 1, 1, 15 });
        var handles = IvotionTopologyBuilder.Build(sol);

        handles.RunFor(minutes: 120.0);

        handles.ShippedSink.Count.Should().BeGreaterThan(0,
            "a 2h run with baseline settings should ship at least some dentures");
        _out.WriteLine($"1× Roland: shipped = {handles.ShippedSink.Count}");
    }

    [Fact]
    public void Build_TwoRolands_OutThroughputOneRoland_AtSaturation()
    {
        // Arrivals at 0.1 min saturate the Roland stage: inspect @ 3 ops feeds
        // buf2 at ~3 pc/min; 1 Roland drains at 2.5 pc/min (15pc / 6-min cycle),
        // so a second Roland gives extra headroom until upstream bottlenecks.
        var one = IvotionTopologyBuilder.Build(
            new IvotionSolution(new[] { 1, 3, 3, 2, 2, 15 }),
            arrivalIntervalMinutes: 0.1);
        var two = IvotionTopologyBuilder.Build(
            new IvotionSolution(new[] { 2, 3, 3, 2, 2, 15 }),
            arrivalIntervalMinutes: 0.1);

        one.RunFor(minutes: 480.0);
        two.RunFor(minutes: 480.0);

        _out.WriteLine($"1× Roland saturated: shipped = {one.ShippedSink.Count}");
        _out.WriteLine($"2× Roland saturated: shipped = {two.ShippedSink.Count}");

        two.ShippedSink.Count.Should().BeGreaterThan(one.ShippedSink.Count,
            "adding a second Roland at saturation must increase throughput");
    }

    [Fact]
    public void Build_ExposesSourceAndSink()
    {
        var sol = new IvotionSolution(new[] { 1, 1, 1, 1, 1, 15 });
        var handles = IvotionTopologyBuilder.Build(sol);

        handles.Source.Should().NotBeNull();
        handles.ShippedSink.Should().NotBeNull();
        handles.Source.Should().BeOfType<SimpleSource>();
        handles.ShippedSink.Should().BeOfType<SimpleSink>();
    }
}
