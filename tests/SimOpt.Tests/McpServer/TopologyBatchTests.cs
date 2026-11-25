using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using ModelContextProtocol;
using SimOpt.Engine.Api.Models;
using SimOpt.Engine.Api.Simulation;
using SimOpt.Simulation.Statistics;
using Xunit;

namespace SimOpt.Tests.McpServer;

/// <summary>
/// SIM-110 step 2 (Q-VIEW-PATH): <c>batch: n</c> on a station, so the Ivotion preset's Roland printer
/// converts to the schema. The station waits until it holds n items, serves them together for one
/// service time (the time of the whole batch), and passes every one of them on; the Roland printer
/// passed on one representative item per batch and lost the rest. One server in this version: in a pool,
/// a part-filled batch on one server would compete with the others for the items arriving.
/// </summary>
public class TopologyBatchTests
{
    private static TopologyDefinition Read(string json) =>
        JsonSerializer.Deserialize<TopologyDefinition>(json, McpJsonUtilities.DefaultOptions)!;

    /// <summary>An item every 1, batches of {BATCH}, 2 per batch.</summary>
    private static string Printer(string batch = "3", string servers = "1") => """
        {"name": "printer", "seed": 1,
         "nodes": [{"id": "in", "type": "source", "params": {"arrival": {"type": "constant", "value": 1}}},
                   {"id": "jig", "type": "queue"},
                   {"id": "printer", "type": "station", "params": {"batch": {BATCH}, "servers": {SERVERS}, "service": {"type": "constant", "value": 2}}},
                   {"id": "out", "type": "sink"}],
         "connections": [["in", "jig"], ["jig", "printer"], ["printer", "out"]],
         "metrics": [{"id": "stay", "kind": "cycle_time"}, {"id": "busy", "kind": "utilization", "node": "printer"}]}
        """.Replace("{BATCH}", batch).Replace("{SERVERS}", servers);

    [Fact]
    public void ABatchStation_ServesNItemsTogether_AndPassesEveryOneOn()
    {
        var registry = new ModelRegistry();
        ActiveModel active = registry.Get(registry.Create(Read(Printer())));
        var released = new List<double>();
        active.Sinks["out"].ItemReceived.AddHandler((_, _) => released.Add(active.Model.CurrentTime));

        active.Model.Run(9.5);

        // items at 0, 1, 2 fill the first batch at 2, printed until 4; items at 3, 4, 5 until 7
        released.Should().Equal(4, 4, 4, 7, 7, 7);
    }

    [Fact]
    public void AnItemsStay_IncludesWaitingForItsBatchToFill()
    {
        var registry = new ModelRegistry();
        ActiveModel active = registry.Get(registry.Create(Read(Printer())));

        ReplicationResult run = new ReplicationRunner(active.Model, runLength: 3_000d, warmupTime: 0d).Run(1, 1);

        run[active.Metrics["stay"]].Mean.Should().BeApproximately(3, 0.01, "the three items of a batch stay 4, 3 and 2");
        run[active.Metrics["busy"]].Mean.Should().BeApproximately(2d / 3, 0.01, "a batch of three every 3 takes 2");
    }

    [Fact]
    public void TheOfferedLoad_IsPerItem()
    {
        ValidationReport report = ModelValidator.Validate(Read(Printer()));

        report.Info.Single(i => i.Code == "OFFERED_LOAD" && i.Node == "printer").Value
              .Should().BeApproximately(2d / 3, 1e-9, "one item a time unit, 2 per batch of 3");
    }

    [Theory]
    [InlineData("0", "1")]
    [InlineData("2.5", "1")]
    [InlineData("\"three\"", "1")]
    [InlineData("3", "2")]
    public void ABatchIsAWholeNumberOfAtLeastOne_OnOneServer(string batch, string servers)
    {
        var report = new ValidationReport();

        ModelRegistry.TryBuild(Read(Printer(batch, servers)), report).Should().BeNull();
        report.Errors.Should().ContainSingle().Which.Should().Match<ValidationIssue>(e =>
            e.Node == "printer" && e.Message.Contains("batch"));
    }

    [Fact]
    public void ABatchOfOne_IsAnOrdinaryStation_AndBatchIsNotListedAsAssumed()
    {
        var registry = new ModelRegistry();
        ActiveModel active = registry.Get(registry.Create(Read(Printer("1"))));
        var released = new List<double>();
        active.Sinks["out"].ItemReceived.AddHandler((_, _) => released.Add(active.Model.CurrentTime));

        active.Model.Run(6.5);

        released.Should().Equal(2, 4, 6);
        ModelDocument.Defaults(Read(Printer().Replace("\"batch\": 3, ", ""))).Select(d => d.Key).Should().NotContain("printer.batch");
    }
}
