using System.Collections.Generic;
using SimOpt.McpServer.Models;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Processes;
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
            IReadOnlyDictionary<string, string>? metrics = null,
            Dictionary<string, Resource>? resources = null,
            Dictionary<string, Stock>? stocks = null,
            Dictionary<string, ProcessNode>? processes = null)
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
            Resources = resources ?? new Dictionary<string, Resource>();
            Stocks = stocks ?? new Dictionary<string, Stock>();
            Processes = processes ?? new Dictionary<string, ProcessNode>();
        }

        public Model Model { get; }
        public Dictionary<string, SimpleSource> Sources { get; }
        public Dictionary<string, SimpleBuffer> Buffers { get; }
        public Dictionary<string, SimpleServer> Servers { get; }
        public Dictionary<string, SimpleDelay> Delays { get; }
        public Dictionary<string, SimpleRouter> Routers { get; }
        public Dictionary<string, SimpleSink> Sinks { get; }

        /// <summary>SIM-126: the resources, beside the item flow; process steps seize their units.</summary>
        public Dictionary<string, Resource> Resources { get; }

        /// <summary>SIM-126: the stocks, beside the item flow; process steps take from them and put into them.</summary>
        public Dictionary<string, Stock> Stocks { get; }

        /// <summary>SIM-126: the process nodes, on the item flow like delays.</summary>
        public Dictionary<string, ProcessNode> Processes { get; }
        public TopologyDefinition Topology { get; }

        /// <summary>
        /// SIM-65 step 5: each declared metric's id, mapped to the identifier of its collector, which
        /// is the key a <c>ReplicationResult</c> is indexed by. Metrics of one kind at one node share
        /// a collector.
        /// </summary>
        public IReadOnlyDictionary<string, string> Metrics { get; }
    }
}
