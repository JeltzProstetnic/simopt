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
            Dictionary<string, SimpleDelay> delays,
            Dictionary<string, SimpleRouter> routers,
            Dictionary<string, SimpleSink> sinks,
            TopologyDefinition topology,
            IReadOnlyDictionary<string, string>? metrics = null)
        {
            Model = model;
            Sources = sources;
            Buffers = buffers;
            Servers = servers;
            Delays = delays;
            Routers = routers;
            Sinks = sinks;
            Topology = topology;
            Metrics = metrics ?? new Dictionary<string, string>();
        }

        public Model Model { get; }
        public Dictionary<string, SimpleSource> Sources { get; }
        public Dictionary<string, SimpleBuffer> Buffers { get; }
        public Dictionary<string, SimpleServer> Servers { get; }
        public Dictionary<string, SimpleDelay> Delays { get; }
        public Dictionary<string, SimpleRouter> Routers { get; }
        public Dictionary<string, SimpleSink> Sinks { get; }
        public TopologyDefinition Topology { get; }

        /// <summary>
        /// SIM-65 step 5: each declared metric's id, mapped to the identifier of its collector, which
        /// is the key a <c>ReplicationResult</c> is indexed by. Metrics of one kind at one node share
        /// a collector.
        /// </summary>
        public IReadOnlyDictionary<string, string> Metrics { get; }
    }
}
