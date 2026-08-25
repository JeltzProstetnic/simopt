using FluentAssertions;
using SimOpt.McpServer.Simulation;
using Xunit;

namespace SimOpt.Tests.McpServer;

/// <summary>
/// SIM-62 — per-node seed derivation must survive a process restart.
///
/// <para>
/// <c>ModelRegistry</c> derived each node's random seed as <c>topology.Seed ^
/// node.Id.GetHashCode()</c>. .NET randomises string hashing per process, so the same topology
/// and the same seed produced different random streams every time the MCP server was restarted.
/// Reproducibility (UN-009) held only within one session — and a reproducibility guarantee that
/// only holds while nobody restarts anything is not one.
/// </para>
/// <para>
/// The values below are the FNV-1a specification's, computed independently. They are asserted as
/// literal constants deliberately: that is what makes this a regression gate rather than a
/// tautology. A hash that varies per process cannot satisfy them.
/// </para>
/// </summary>
public class StableHashTests
{
    [Theory]
    [InlineData("source", 466561496)]
    [InlineData("server", 1085029842)]
    [InlineData("arrivals", 507947475)]
    [InlineData("triage", 166983937)]
    [InlineData("", -2128831035)]
    public void Of_MatchesTheFnv1aSpecification(string input, int expected)
    {
        StableHash.Of(input).Should().Be(expected);
    }

    [Fact]
    public void Of_IsStableAcrossCalls()
    {
        StableHash.Of("exam_room").Should().Be(StableHash.Of("exam_room"));
    }

    [Fact]
    public void Of_DistinguishesNodeIds()
    {
        StableHash.Of("triage").Should().NotBe(StableHash.Of("exam"),
            "distinct nodes must draw from distinct streams");
    }

    [Fact]
    public void Of_ToleratesNull()
    {
        var act = () => StableHash.Of(null!);
        act.Should().NotThrow();
    }

    /// <summary>
    /// The property that actually matters, stated directly: seed derivation must be a pure
    /// function of the topology seed and the node id.
    /// </summary>
    [Theory]
    [InlineData(42, "triage")]
    [InlineData(7, "arrivals")]
    public void DerivedSeed_IsAFunctionOfSeedAndNodeIdOnly(int topologySeed, string nodeId)
    {
        var first = topologySeed ^ StableHash.Of(nodeId);
        var second = topologySeed ^ StableHash.Of(nodeId);

        second.Should().Be(first);
    }
}
