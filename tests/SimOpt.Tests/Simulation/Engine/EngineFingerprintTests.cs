using FluentAssertions;
using SimOpt.Benchmarks;
using Xunit;

namespace SimOpt.Tests.Simulation.Engine;

/// <summary>
/// SIM-129: speed work on the event path must not move a single result. Each case of the speed
/// harness (<c>benchmarks/SimOpt.Benchmarks</c>) hashes every number it computes — every
/// replication's estimate of every statistic, the event count per replication, doubles by their
/// bits — and the hashes below were recorded on the engine as it was before SIM-129 (e3bb9e8).
/// </summary>
/// <remarks>
/// A change that is meant to move results (a new seeding rule, a fixed bug) updates these values in
/// the same commit and says so in the commit message, because a downstream integrator freezes
/// reference runs against a pinned SimOpt commit (SIM-105). Print the current values with
/// <c>dotnet run -c Release --project benchmarks/SimOpt.Benchmarks -- --scale 0.1 --reps 3 --repeat 1</c>.
/// </remarks>
public class EngineFingerprintTests
{
    private const double Scale = 0.1;
    private const int Replications = 3;
    private const int BaseSeed = 20_260_826;

    [Theory]
    [InlineData("mm1", 29568, "0719d2cf99b67b77")]
    [InlineData("md1", 18579, "16df715d9619269c")]
    [InlineData("mg1u", 18045, "619013c33c6f9273")]
    [InlineData("mmc", 90846, "5bf262a825503e2b")]
    [InlineData("jackson", 56070, "3401adfaa98b6d71")]
    [InlineData("mg1-breakdowns", 15718, "77304ff7c25e9316")]
    [InlineData("process-mm1", 20487, "b7cc264c91bc61a0")]
    [InlineData("process-stock", 28488, "c1bb10244161e6b4")]
    [InlineData("mm1-overload", 38982, "45961e9686adf336")]   // added with the FIFO change, recorded on e3bb9e8
    [InlineData("process-node-mmc", 90309, "afb0c48ddf662209")]   // SIM-126 schema slice 6, recorded when added
    public void EveryResultStaysByteIdentical(string name, long events, string fingerprint)
    {
        CaseOutcome outcome = BenchmarkCases.Run(name, Scale, Replications, BaseSeed);

        outcome.Events.Should().Be(events, "the same model and seeds must run the same events");
        outcome.Fingerprint.Should().Be(fingerprint, "every replication's results must stay byte-identical");
    }

    [Fact]
    public void TheFingerprintSeesAOneBitChange()
    {
        double x = 0.1 + 0.2;
        double y = System.BitConverter.Int64BitsToDouble(System.BitConverter.DoubleToInt64Bits(x) + 1);

        new Fingerprint().Add(x).Hex().Should().NotBe(new Fingerprint().Add(y).Hex());
    }
}
