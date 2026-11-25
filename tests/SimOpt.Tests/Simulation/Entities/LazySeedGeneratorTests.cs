using System;
using System.Linq;
using FluentAssertions;
using SimOpt.Mathematics.Stochastics.Distributions;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Processes;
using Xunit;
using SimRandom = SimOpt.Simulation.Engine.Random<double>;

namespace SimOpt.Tests.Simulation.Entities;

/// <summary>
/// SIM-129: every stochastic entity built a Mersenne Twister seed generator (a 2.5 kB state,
/// initialised in a loop) when it was constructed and re-initialised it on every reset, although
/// most entities never draw from it: a process started per customer has no random stream of its
/// own. The generator is now made on first use from the same seed. The draws below were recorded
/// with the eager generator and must not move.
/// </summary>
public class LazySeedGeneratorTests
{
    private static UniformDoubleDistribution Uniform()
    {
        var d = new UniformDoubleDistribution();
        d.Configure(0, 1);
        return d;
    }

    private static string Draws(SimRandom r) => string.Join(",", Enumerable.Range(0, 3).Select(_ => r.Next().ToString("R")));

    [Fact]
    public void AStreamMadeAtConstruction_DrawsAsBefore_AfterResets()
    {
        var model = new Model("m", 5, 0d);
        var entity = new StochasticEntity(model, name: "e");
        var r = new SimRandom(entity, Uniform());
        string first = Draws(r);
        model.Reset(77);
        string afterReset = Draws(r);

        first.Should().Be(PinnedAtConstruction);
        afterReset.Should().Be(PinnedAfterReset);
    }

    [Fact]
    public void AStreamMadeAfterResets_DrawsAsBefore()
    {
        var model = new Model("m", 5, 0d);
        var plain = new StochasticEntity(model, name: "plain");
        var named = new StochasticEntity(model, id: "named", name: "named");
        model.Reset(77);
        model.Reset(78);
        var r1 = new SimRandom(plain, Uniform());
        var r2 = new SimRandom(named, Uniform());

        (Draws(r1) + "|" + Draws(r2)).Should().Be(PinnedMadeLate);
    }

    [Fact]
    public void AnEntityThatNeverDraws_StaysSmall()
    {
        var model = new Model("m", 5, 0d);
        _ = new StochasticEntity(model, name: "warm");

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++) _ = new StochasticEntity(model, name: "e" + i);
        long perEntity = (GC.GetAllocatedBytesForCurrentThread() - before) / 100;

        perEntity.Should().BeLessThan(1024, "a stochastic entity without a stream of its own needs no 2.5 kB generator");
    }

    private const string PinnedAtConstruction = "0.4424797214765473,0.673310336970403,0.9420078214919231";
    private const string PinnedAfterReset = "0.2978228862852896,0.48394589334910076,0.8897623884909611";
    private const string PinnedMadeLate = "0.721744064577736,0.8070616828310591,0.861079499526452|0.09434144529250518,0.4000304222107075,0.6072975232299871";
}
