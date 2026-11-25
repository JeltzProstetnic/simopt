using System;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.AI;
using ModelContextProtocol;
using SimOpt.McpServer.Models;
using SimOpt.McpServer.Simulation;
using SimOpt.McpServer.Tools;
using Xunit;

namespace SimOpt.Tests.McpServer;

/// <summary>
/// SIM-65 step 1: a node parameter is a number, a string or a distribution, so a topology can say
/// <c>"service": {"type": "triangular", "min": 20, "mode": 25, "max": 40}</c>. Params were a
/// <c>Dictionary&lt;string, double&gt;</c>, which refused any such topology outright. Numbers keep
/// working exactly as before, in JSON and in C# (implicit conversions), and a parameter of the wrong
/// kind is refused with the node and the parameter named.
/// </summary>
public class ParamValueTests
{
    private const string Mixed = """
        {"id": "exam", "type": "server",
         "params": {"capacity": 15, "rate": 0.25, "discipline": "fifo",
                    "service": {"type": "triangular", "min": 20, "mode": 25, "max": 40}}}
        """;

    [Fact]
    public void JsonNumbersStringsAndObjects_BecomeTheThreeKinds()
    {
        NodeDefinition node = JsonSerializer.Deserialize<NodeDefinition>(Mixed)!;

        node.Params["capacity"].Kind.Should().Be(ParamValueKind.Number);
        node.Params["capacity"].Number.Should().Be(15);
        node.Params["rate"].Number.Should().Be(0.25);
        node.Params["discipline"].Kind.Should().Be(ParamValueKind.Text);
        node.Params["discipline"].Text.Should().Be("fifo");
        DistributionSpec service = node.Params["service"].Distribution!;
        service.Type.Should().Be("triangular");
        (service.Min, service.Mode, service.Max).Should().Be((20d, 25d, 40d));
    }

    [Fact]
    public void ParamsSerialiseBackToTheSameForms()
    {
        NodeDefinition node = JsonSerializer.Deserialize<NodeDefinition>(Mixed)!;

        string json = JsonSerializer.Serialize(node.Params);
        NodeDefinition again = JsonSerializer.Deserialize<NodeDefinition>("{\"params\":" + json + "}")!;

        json.Should().Contain("\"capacity\":15").And.Contain("\"discipline\":\"fifo\"").And.Contain("\"type\":\"triangular\"");
        again.Params["service"].Distribution!.Max.Should().Be(40);
        again.Params["rate"].Number.Should().Be(0.25);
    }

    [Fact]
    public void NumbersIntegersStringsAndDistributionsConvertImplicitly()
    {
        var node = new NodeDefinition
        {
            Params =
            {
                ["mean_interval"] = 2.0,
                ["capacity"] = 15,
                ["discipline"] = "fifo",
                ["service"] = new DistributionSpec { Type = "exponential", Mean = 3 },
            },
        };

        node.Params["mean_interval"].Number.Should().Be(2.0);
        node.Params["capacity"].Number.Should().Be(15);
        node.Params["discipline"].Text.Should().Be("fifo");
        node.Params["service"].Distribution!.Mean.Should().Be(3);
    }

    [Theory]
    [InlineData("[1, 2]")]
    [InlineData("true")]
    [InlineData("null")]
    public void OtherJsonValues_AreRefused_NamingWhereTheyAre(string value)
    {
        Action parse = () => JsonSerializer.Deserialize<NodeDefinition>("{\"params\": {\"capacity\": " + value + "}}");

        parse.Should().Throw<JsonException>().Where(e => (e.Path ?? "").Contains("capacity") && e.Message.Contains("number"));
    }

    [Theory]
    [InlineData("source", "mean_interval")]
    [InlineData("buffer", "capacity")]
    [InlineData("server", "service_time")]
    public void ANumberParameterGivenAsSomethingElse_IsRefused_WithTheNodeAndParameterNamed(string type, string param)
    {
        var topology = new TopologyDefinition
        {
            Nodes =
            {
                new NodeDefinition { Id = "a", Type = "source", Params = { ["mean_interval"] = 2.0 } },
                new NodeDefinition { Id = "q", Type = "buffer" },
                new NodeDefinition { Id = "s", Type = "server", Params = { ["service_time"] = 1.0 } },
                new NodeDefinition { Id = "done", Type = "sink" },
            },
        };
        topology.Nodes.Find(n => n.Type == type)!.Params[param] = "fast";

        Action build = () => new ModelRegistry().Create(topology);

        build.Should().Throw<InvalidOperationException>()
             .Where(e => e.Message.Contains("'" + topology.Nodes.Find(n => n.Type == type)!.Id + "'")
                      && e.Message.Contains(param) && e.Message.Contains("number"));
    }

    [Fact]
    public async Task CreateModel_BindsAJsonTopologyThroughTheMcpSerializer()
    {
        var tools = new SimulationTools(new ModelRegistry());
        AIFunction createModel = AIFunctionFactory.Create(
            typeof(SimulationTools).GetMethod(nameof(SimulationTools.CreateModel))!, tools,
            new AIFunctionFactoryOptions { SerializerOptions = McpJsonUtilities.DefaultOptions });
        JsonElement topology = JsonDocument.Parse("""
            {"name": "bound", "seed": 7,
             "nodes": [{"id": "src", "type": "source", "params": {"mean_interval": 2}},
                       {"id": "buf", "type": "buffer", "params": {"capacity": 10, "note": "x"}},
                       {"id": "srv", "type": "server", "params": {"service_time": 1.5,
                                                                  "later": {"type": "constant", "value": 1}}},
                       {"id": "snk", "type": "sink", "params": {}}],
             "connections": [{"from": "src", "to": "buf"}, {"from": "buf", "to": "srv"}, {"from": "srv", "to": "snk"}]}
            """).RootElement;

        object? result = await createModel.InvokeAsync(new AIFunctionArguments { ["topology"] = topology });

        result!.ToString().Should().Contain("model_id").And.NotContain("error");
        createModel.JsonSchema.ToString().Should().Contain("params");
    }
}
