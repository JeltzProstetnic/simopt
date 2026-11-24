using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using SimOpt.Mathematics.Stochastics.Distributions;
using SimOpt.Simulation.Engine;
using Xunit;
using SimRandom = SimOpt.Simulation.Engine.Random<double>;

namespace SimOpt.Tests.Simulation.Engine;

/// <summary>
/// SIM-131: <c>Model.Reset</c> reset every model-level random stream (a <c>Random&lt;T&gt;</c> seeded from
/// the model) with the same seed, the model's own. After any reset all of them drew the same uniforms,
/// so two "independent" streams were perfectly correlated: an M/M/1 queue written with one stream for
/// gaps and one for work gave a mean time in system of 2.41 instead of 5, because every service time
/// was 0.8 times its own interarrival gap. Each model-level stream now gets its own seed from the
/// model seed and its registration index, when it is registered and on every reset.
/// </summary>
public class ModelRandomStreamTests
{
    private static UniformDoubleDistribution Uniform()
    {
        var d = new UniformDoubleDistribution();
        d.Configure(0d, 1d);
        return d;
    }

    private static List<double> Draw(SimRandom r, int n) => Enumerable.Range(0, n).Select(_ => r.Next()).ToList();

    [Fact]
    public void TwoModelLevelStreams_DrawDifferentNumbersAfterAReset()
    {
        var model = new Model("m", 5, 0d);
        var a = new SimRandom(model, Uniform());
        var b = new SimRandom(model, Uniform());

        model.Reset(7);

        Draw(a, 5).Should().NotEqual(Draw(b, 5), "two streams of one model must not be copies of each other");
    }

    [Fact]
    public void AResetWithTheSameSeed_ReproducesTheStreams()
    {
        var model = new Model("m", 5, 0d);
        var a = new SimRandom(model, Uniform());
        var b = new SimRandom(model, Uniform());

        model.Reset(7);
        var firstA = Draw(a, 5);
        var firstB = Draw(b, 5);
        model.Reset(7);

        Draw(a, 5).Should().Equal(firstA);
        Draw(b, 5).Should().Equal(firstB);
    }

    [Fact]
    public void BuildingAndRunning_EqualsBuildingResettingWithTheSameSeedAndRunning()
    {
        var fresh = new Model("m", 5, 0d);
        var a1 = new SimRandom(fresh, Uniform());
        var b1 = new SimRandom(fresh, Uniform());
        var withoutResetA = Draw(a1, 5);
        var withoutResetB = Draw(b1, 5);

        var reset = new Model("m", 5, 0d);
        var a2 = new SimRandom(reset, Uniform());
        var b2 = new SimRandom(reset, Uniform());
        reset.Reset(5);

        Draw(a2, 5).Should().Equal(withoutResetA);
        Draw(b2, 5).Should().Equal(withoutResetB);
    }
}
