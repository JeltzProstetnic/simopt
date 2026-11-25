using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using SimOpt.Mathematics.Stochastics.Distributions;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Processes;
using SimOpt.Simulation.Statistics;
using Xunit;

namespace SimOpt.Tests.Simulation.Processes;

/// <summary>
/// SIM-126, slice 5: a process draws from its own named streams, <c>p.Random("service", distribution)</c>.
/// A stream's numbers follow from the process's seed and the stream's name alone, so adding a stream,
/// or creating streams in another order, leaves the others' numbers alone (the SIM-125 idea applied
/// inside a process). The body can ask for the stream on every run: the first call makes it, later
/// calls return it, and a reset reseeds it, so a rebuilt model and a reset one draw the same numbers.
/// </summary>
public class ProcessRandomStreamTests
{
    private static UniformDoubleDistribution Uniform()
    {
        var d = new UniformDoubleDistribution();
        d.Configure(0, 1);
        return d;
    }

    private static List<double> Run(int modelSeed, string[] streamOrder, string watched, int draws = 3)
    {
        var model = new Model("m", modelSeed, 0d);
        var got = new List<double>();
        IEnumerable<Wait> Body(Process p)
        {
            foreach (string name in streamOrder) p.Random(name, Uniform());
            for (int i = 0; i < draws; i++)
            {
                got.Add(p.Random(watched).Next());
                yield return p.Timeout(1);
            }
        }
        model.StartProcess(Body, id: "worker");
        model.Run(100);
        return got;
    }

    [Fact]
    public void AStreamsNumbers_DoNotDependOnTheOtherStreamsOrTheirOrder()
    {
        List<double> alone = Run(5, new[] { "service" }, "service");
        List<double> withOthers = Run(5, new[] { "setup", "service", "scrap" }, "service");
        List<double> otherOrder = Run(5, new[] { "scrap", "setup", "service" }, "service");

        withOthers.Should().Equal(alone);
        otherOrder.Should().Equal(alone);
        Run(5, new[] { "setup", "service" }, "setup").Should().NotEqual(alone, "different names give different streams");
    }

    [Fact]
    public void TheSameNameInTwoProcesses_GivesTwoStreams()
    {
        var model = new Model("m", 5, 0d);
        var a = new List<double>();
        var b = new List<double>();
        IEnumerable<Wait> Body(Process p, List<double> into)
        {
            for (int i = 0; i < 3; i++)
            {
                into.Add(p.Random("service", Uniform()).Next());
                yield return p.Timeout(1);
            }
        }
        model.StartProcess(p => Body(p, a), id: "a");
        model.StartProcess(p => Body(p, b), id: "b");

        model.Run(100);

        a.Should().NotEqual(b);
    }

    [Fact]
    public void AskingAgain_ReturnsTheSameStream_AndAnUnknownNameIsAnError()
    {
        var model = new Model("m", 5, 0d);
        Process p = model.StartProcess(Idle, id: "p");

        Random<double> first = p.Random("service", Uniform());

        p.Random("service", Uniform()).Should().BeSameAs(first);
        p.Random("service").Should().BeSameAs(first);
        Action unknown = () => p.Random("setup");
        unknown.Should().Throw<ArgumentException>().WithMessage("*setup*");
    }

    private static IEnumerable<Wait> Idle(Process p) { yield return p.Timeout(1); }

    [Fact]
    public void AResetModel_DrawsWhatARebuiltOneDraws()
    {
        var rebuilt = new List<string>();
        foreach (int r in new[] { 0, 1, 2 })
            rebuilt.Add(string.Join(",", Replicate(ReplicationRunner.SeedFor(9, r), fresh: true)));

        var model = BuildDrawing(out List<double> got);
        var reset = new List<string>();
        foreach (int r in new[] { 0, 1, 2 })
        {
            got.Clear();
            model.Reset(ReplicationRunner.SeedFor(9, r));
            model.Run(100);
            reset.Add(string.Join(",", got));
        }

        reset.Should().Equal(rebuilt);
        reset.Distinct().Should().HaveCount(3, "each replication has its own numbers");
    }

    private static List<double> Replicate(int seed, bool fresh)
    {
        var model = BuildDrawing(out List<double> got);
        model.Reset(seed);
        model.Run(100);
        return got;
    }

    private static Model BuildDrawing(out List<double> got)
    {
        var model = new Model("m", 1, 0d);
        var into = new List<double>();
        got = into;
        IEnumerable<Wait> Body(Process p)
        {
            for (int i = 0; i < 3; i++)
            {
                into.Add(p.Random("service", Uniform()).Next());
                yield return p.Timeout(1);
            }
        }
        model.StartProcess(Body, id: "worker");
        return model;
    }
}
