using System;
using System.Collections.Generic;
using MatthiasToolbox.Basics.Datastructures.Network;

namespace MatthiasToolbox.Simulation.Network
{
	/// <summary>
	/// Description of ISimulationPath.
	/// </summary>
	public interface ISimulationPath : IPath<double>, ICloneable
	{
		#region prop

		bool IsEmpty { get; }

		bool IsValid { get; }

		int NodeCount { get; }

		int ConnectionCount { get; }

		ISimulationNode FirstNode { get; set; }

		ISimulationConnection FirstConnection { get; set; }

		ISimulationNode LastNode { get; set; }

		ISimulationConnection LastConnection { get; set; }

		ISimulationNetwork Network { get; }

		List<ISimulationConnection> SimulationConnections { get; set; }
		
		List<ISimulationNode> SimulationNodes { get; set; }
		
		#endregion
		#region impl
		
		#region create
		
		bool CreateFrom(IEnumerable<ISimulationConnection> connections);
		
		bool CreateFrom(IEnumerable<ISimulationNode> nodes);
		
		#endregion
		#region manipulate

        bool ReplaceNode(ISimulationNode oldNode, ISimulationNode newNode);

		bool RemoveFirstConnection();

		bool RemoveFirstNode();

		bool RemoveLastConnection();

		bool RemoveLastNode();
		
		#endregion
		#region analyze

		/// <summary>
		/// Creates a new subpath from the sourceNode to the given targetNode.
		/// Throws an exception if the given nodes are not on the path.
		/// </summary>
		/// <param name="sourceNode">The first node for the new path.</param>
		/// <param name="targetNode">The last node for the new path.</param>
		/// <returns>A new path object containing a part of this path.</returns>
		ISimulationPath GetSubPath(ISimulationNode sourceNode, ISimulationNode targetNode);
		
		/// <summary>
		/// Calculate the distance from the first node to the given targetNode.
		/// Throws an exception if the given node is not on the path.
		/// </summary>
		/// <param name="targetNode"></param>
		/// <returns></returns>
		double GetDistanceTo(ISimulationNode targetNode);

		/// <summary>
		/// Calculate the distance between the given nodes via this path.
		/// Throws an exception if one of the given nodes is not on the path.
		/// </summary>
		/// <param name="sourceNode">The node from which to count the distance.</param>
		/// <param name="targetNode">The node to which to count the distance.</param>
		/// <returns>The distance between the given nodes when traveling on this path.</returns>
		double GetDistance(ISimulationNode sourceNode, ISimulationNode targetNode);

        /// <summary>
        /// Find the predecessor node.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        ISimulationNode GetNodeBefore(ISimulationNode node);

        /// <summary>
        /// Find the follower node.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        ISimulationNode GetNodeAfter(ISimulationNode node);
		
		#endregion
		
		void Clear();
		
		#endregion
	}
}