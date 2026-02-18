using System;
using System.Collections.Generic;
using MatthiasToolbox.Basics.Datastructures.Network;
using MatthiasToolbox.Mathematics.Geometry;
using MatthiasToolbox.Simulation.Interfaces;

namespace MatthiasToolbox.Simulation.Network
{
	/// <summary>
	/// Description of ISimulationNode.
	/// </summary>
	public interface ISimulationNode : INode<double>, INamedIdentifiable, ICloneable
	{
        /// <summary>
        /// The connections from this node to others.
        /// </summary>
		Dictionary<ISimulationNode, ISimulationConnection> SimulationConnections { get; }

        /// <summary>
        /// The delegate to use for retrieving the distance do another node.
        /// </summary>
        Func<ISimulationNode, ISimulationNode, double> DistanceDelegate { get; set; }

        /// <summary>
        /// This is usually a reference to the node itself but may be replaced
        /// with a different node in some cases like offset nodes.
        /// </summary>
        ISimulationNode OriginalNode { get; set; }

        /// <summary>
        /// Retrieve the first connection between this node and the given targetNode
        /// </summary>
        /// <param name="targetNode">A connected node</param>
        /// <returns>The first connection between this node and the given targetNode</returns>
        ISimulationConnection GetSimulationConnection(ISimulationNode targetNode);
    }
}