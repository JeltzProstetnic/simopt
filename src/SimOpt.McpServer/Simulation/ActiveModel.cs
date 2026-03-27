using System.Collections.Generic;
using SimOpt.McpServer.Models;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Templates;

namespace SimOpt.McpServer.Simulation
{
    /// <summary>
    /// Holds a live <see cref="Model"/> together with its node references
    /// so that tools can inspect state after a run.
    /// </summary>
    public sealed class ActiveModel
    {
        public ActiveModel(
            Model model,
            Dictionary<string, SimpleSource> sources,
            Dictionary<string, SimpleBuffer> buffers,
            Dictionary<string, SimpleServer> servers,
            Dictionary<string, SimpleSink> sinks,
            TopologyDefinition topology)
        {
            Model = model;
            Sources = sources;
            Buffers = buffers;
            Servers = servers;
            Sinks = sinks;
            Topology = topology;
        }

        public Model Model { get; }
        public Dictionary<string, SimpleSource> Sources { get; }
        public Dictionary<string, SimpleBuffer> Buffers { get; }
        public Dictionary<string, SimpleServer> Servers { get; }
        public Dictionary<string, SimpleSink> Sinks { get; }
        public TopologyDefinition Topology { get; }
    }
}
