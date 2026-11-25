using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.AI;
using ModelContextProtocol;
using SimOpt.Engine.Api.Models;
using SimOpt.Engine.Api.Simulation;
using SimOpt.McpServer.Tools;
using SimOpt.Visualization.Models;
using Xunit;

namespace SimOpt.Tests.Visualization;

/// <summary>
/// SIM-110 step 4 (Q-VIEW-PATH): File → Open reads what the MCP head writes, a topology JSON from
/// <c>save_model</c>/<c>export_model</c> or a <c>simopt-run/1</c> record from <c>run_experiment</c>/<c>optimize</c>,
/// which also declares the run's length and warm-up. A document may carry a top-level <c>layout</c> block, by
/// node id, that the builder ignores, so a model saved from the viewer reopens where it was drawn and still
/// loads in the MCP head with the same model hash (UN-005, UN-025).
/// </summary>
public sealed class ViewerFileTests : IDisposable
{
    private readonly string folder = Directory.CreateTempSubdirectory("simopt-sim110-").FullName;

    public void Dispose() => Directory.Delete(folder, recursive: true);

    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    private static async Task<string> Call(SimulationTools tools, string name, object arguments)
    {
        var function = AIFunctionFactory.Create(typeof(SimulationTools).GetMethod(name)!, tools,
            new AIFunctionFactoryOptions { SerializerOptions = McpJsonUtilities.DefaultOptions });
        var args = new AIFunctionArguments();
        foreach (JsonProperty p in JsonSerializer.SerializeToElement(arguments).EnumerateObject()) args[p.Name] = p.Value;
        return (await function.InvokeAsync(args))!.ToString()!;
    }

    private static VizDocument Factory() => VizSchema.ToSchema(VizTopology.FactoryFloor());

    [Fact]
    public void A_topology_document_reads_as_the_model_it_describes()
    {
        string saved = ModelDocument.Write(Factory().Topology);

        VizOpened opened = VizFile.Read(saved);

        opened.Error.Should().BeNull();
        ModelDocument.Write(opened.Document!.Topology).Should().Be(saved);
        opened.Document.Layout.Should().BeEmpty();
        (opened.Duration, opened.Warmup).Should().Be((null, null));
        opened.Kind.Should().Be("model");
    }

    [Fact]
    public void The_layout_block_comes_back_by_node_and_does_not_change_the_model()
    {
        VizDocument doc = Factory();

        string written = VizFile.Write(doc);
        VizOpened opened = VizFile.Read(written);

        Json(written).GetProperty("layout").GetProperty("dock_a").GetProperty("x").GetDouble().Should().Be(8);
        opened.Document!.Layout.Should().BeEquivalentTo(doc.Layout);
        ModelDocument.Sha256(opened.Document.Topology).Should().Be(ModelDocument.Sha256(doc.Topology),
            "the builder ignores the layout, so the model keeps its hash");
    }

    [Fact]
    public void Without_a_layout_the_viewer_writes_what_save_model_writes()
    {
        var doc = new VizDocument(Factory().Topology, new Dictionary<string, NodeLayout>());

        VizFile.Write(doc).Should().Be(ModelDocument.Write(doc.Topology));
    }

    [Fact]
    public async Task The_MCP_head_loads_a_file_the_viewer_saved()
    {
        VizDocument doc = Factory();
        string path = Path.Combine(folder, "factory.json");
        File.WriteAllText(path, VizFile.Write(doc));
        var tools = new SimulationTools(new ModelRegistry());

        JsonElement loaded = Json(await Call(tools, nameof(SimulationTools.LoadModel), new { path }));
        string id = loaded.GetProperty("model_id").GetString()!;
        JsonElement exported = Json(await Call(tools, nameof(SimulationTools.ExportModel), new { model_id = id }));

        TopologyDefinition registered = exported.GetProperty("topology").Deserialize<TopologyDefinition>()!;
        ModelDocument.Sha256(registered).Should().Be(ModelDocument.Sha256(doc.Topology), "the layout block is ignored");
    }

    [Fact]
    public async Task A_run_record_opens_with_its_model_run_length_and_warm_up()
    {
        VizDocument doc = Factory();
        var tools = new SimulationTools(new ModelRegistry());
        string id = Json(await Call(tools, nameof(SimulationTools.CreateModel), new { topology = Json(ModelDocument.Write(doc.Topology)) }))
            .GetProperty("model_id").GetString()!;
        string path = Path.Combine(folder, "run.json");
        await Call(tools, nameof(SimulationTools.RunExperiment), new { model_id = id, duration = 300.0, warmup = 50.0, replications = 2, record = path });

        VizOpened opened = VizFile.Read(File.ReadAllText(path));

        opened.Error.Should().BeNull();
        opened.Kind.Should().Be("run record");
        (opened.Duration, opened.Warmup).Should().Be((300.0, 50.0));
        ModelDocument.Sha256(opened.Document!.Topology).Should().Be(ModelDocument.Sha256(doc.Topology));
    }

    [Theory]
    [InlineData("{ not json", "not JSON")]
    [InlineData("[1, 2]", "not a JSON object")]
    [InlineData("{\"name\": \"x\"}", "no \"nodes\"")]
    [InlineData("{\"record\": \"simopt-run/9\", \"model\": {}}", "simopt-run/9")]
    [InlineData("{\"record\": \"simopt-run/1\"}", "no model")]
    [InlineData("{\"nodes\": [{\"id\": \"a\", \"type\": \"source\"}], \"layout\": {\"a\": {\"x\": \"left\"}}}", "layout")]
    public void A_file_that_is_no_model_is_an_error_not_an_exception(string text, string named)
    {
        VizOpened opened = VizFile.Read(text);

        opened.Document.Should().BeNull();
        opened.Error.Should().Contain(named);
    }

    // ── problems on the nodes ────────────────────────────────────────────────────────

    [Fact]
    public void Each_problem_goes_to_its_node_and_the_rest_to_the_general_list()
    {
        var report = new ValidationReport();
        report.Errors.Add(new ValidationIssue("UNKNOWN_NODE_TYPE", "x is unknown", "x"));
        report.Warnings.Add(new ValidationIssue("SATURATED", "x is saturated", "x"));
        report.Errors.Add(new ValidationIssue("DUPLICATE_METRIC", "metric twice"));
        report.Warnings.Add(new ValidationIssue("UNKNOWN_NODE", "y is not declared", "y"));
        var drawing = new VizTopology { Nodes = { new VizNode { Id = "x" }, new VizNode { Id = "z" } } };

        ModelProblems problems = ModelProblems.Of(report, drawing);

        problems.ByNode.Keys.Should().Equal("x");
        problems.ByNode["x"].Select(i => i.Code).Should().Equal("UNKNOWN_NODE_TYPE", "SATURATED");
        problems.Errors.Should().Contain("x");
        problems.General.Select(i => i.Code).Should().Equal("DUPLICATE_METRIC", "UNKNOWN_NODE");
        problems.HasErrors.Should().BeTrue();
    }
}
