using System;
using System.Collections.Generic;
using SimOpt.Basics.Datastructures.Network;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Interfaces;

namespace SimOpt.Simulation.Network
{
	/// <summary>
	/// Description of ISimulationNetwork.
	/// </summary>
	public interface ISimulationNetwork : INetwork<double>, IEnumerable<ISimulationNode>, INamedIdentifiable, IResettable
	{
		#region prop
		
		/// <summary>
		/// The model to which this network belongs
		/// </summary>
		IModel Model { get; }

		/// <summary>
		/// the predefined path sequences for this network
		/// </summary>
		Dictionary<ISimulationNode, Dictionary<ISimulationNode, ISimulationPath>> FixedPaths { get; }
		
		/// <summary>
		/// a delegate method to search for a path
		/// </summary>
		Func<ISimulationNode, ISimulationNode, ISimulationPath> FindPathMethod { get; }

		/// <summary>
		/// all connections in this network
		/// </summary>
		IEnumerable<ISimulationConnection> Connections { get; }
		
		#endregion
		#region impl
		
		#region nodes
		
		/// <summary>
		/// add an existing node to the network
		/// caution: this will not add linked paths
		/// if there are any in the given node.
		/// CAUTION: if a node with the same ID
		/// alreday exists in this network, the
		/// node will not be added!
		/// </summary>
		/// <param name="node"></param>
		/// <returns>a success flag</returns>
		bool Add(ISimulationNode node);

		bool Remove(ISimulationNode node);

		#endregion
		#region paths
		
		/// <summary>
		/// adds a predefined path sequence to be preferred
		/// such a path sequence will always be returned by
		/// FindPath before even searching.
		/// </summary>
		/// <param name="fromNode"></param>
		/// <param name="toNode"></param>
		/// <param name="pathSequence"></param>
		void SetFixedPath(ISimulationNode fromNode, ISimulationNode toNode, ISimulationPath path);
		
		#endregion
		#region connections
		
		/// <summary>
		/// Adds the connection to the internal connection dictionary/ies of the connected nodes.
		/// Caution: the distance will not be calculated.
		/// </summary>
		/// <param name="con"></param>
		void CreateConnection(ISimulationConnection con);
		
		#endregion
		
		#endregion
	}
}