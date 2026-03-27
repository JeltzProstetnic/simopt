using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using SimOpt.McpServer.Simulation;
using SimOpt.McpServer.Tools;

// The MCP server communicates over stdio (stdin/stdout).
// All log output MUST go to stderr to avoid corrupting the JSON-RPC stream.
var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});
builder.Logging.SetMinimumLevel(LogLevel.Warning);

// Register the model registry as a singleton — it holds live simulation models
// for the duration of the server session.
builder.Services.AddSingleton<ModelRegistry>();

// Register the MCP server with stdio transport and attribute-based tool discovery.
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<SimulationTools>();

await builder.Build().RunAsync();
