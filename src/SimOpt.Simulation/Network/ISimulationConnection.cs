using System;
using SimOpt.Basics.Datastructures.Network;
using SimOpt.Basics.Datastructures.Geometry;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Interfaces;

namespace SimOpt.Simulation.Network
{
	/// <summary>
	/// Description of ISimulationConnection.
	/// </summary>
    public interface ISimulationConnection : IConnection<double>, IResource, IResettable, ICloneable
	{
		#region prop
		
        /// <summary>
        /// The first node of this connection (in order if the connection is directed)
        /// </summary>
        ISimulationNode Node1 { get; set; }

        /// <summary>
        /// The second node of this connection (in order if the connection is directed)
        /// </summary>
        ISimulationNode Node2 { get; set; }

        /// <summary>
        /// This is usually a reference to the connection itself but may be replaced
        /// with a different connection in some cases like offset nodes.
        /// </summary>
        ISimulationConnection OriginalConnection { get; set; }

        // bool IsTemporary { get; set; }

		#endregion
		#region impl
		
		/// <summary>
        /// Create and return a vector from the nodes in this connection.
        /// The direction will be from Node1 to Node2
        /// </summary>
        /// <returns></returns>
        Vector GetVector();
		
		#endregion
	}
}