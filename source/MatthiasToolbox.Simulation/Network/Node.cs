using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Basics.Datastructures.Network;
using MatthiasToolbox.Basics.Datastructures.Geometry;
using MatthiasToolbox.Simulation.Interfaces;
using MatthiasToolbox.Basics.Exceptions;

namespace MatthiasToolbox.Simulation.Network
{
    [Serializable]
	public class Node : ISimulationNode
	{
		#region over

		public override string ToString()
		{
			return Name;
		}

		#endregion
		#region cvar

		private static int nodeCounter = 0;

		#endregion
		#region prop

		public Dictionary<ISimulationNode, ISimulationConnection> SimulationConnections { get; private set; }

        public bool IsTemporary { get; set; }

		#region INamedIdentifiable

		/// <summary>
		/// a unique id
		/// </summary>
		public string ID { get; private set; }

		/// <summary>
		/// a unique id
		/// </summary>
		public string Identifier { get { return ID; } }

		/// <summary>
		/// a unique name
		/// </summary>
		public string Name { get; internal set; }

		#endregion
		#region ISimulationNode

		/// <summary>
		/// the network to which this node belongs
		/// </summary>
		public INetwork<double> Network { get; set; }

		/// <summary>
		/// a weight associated with this node
		/// </summary>
		public double Weight { get; set; }

		/// <summary>
		/// all nodes which are connected to this instance
		/// CAUTION: in case of directed connections only the ones with
		/// an incoming connection from this node are returned!
		/// </summary>
		public IEnumerable<INode<double>> ConnectedNodes
		{
			get { return SimulationConnections.Keys; }
		}

		/// <summary>
		/// all connections leading to and from this node
		/// CAUTION: in case of directed connections only the
		/// outgoing ones are returned!
		/// </summary>
		public IEnumerable<IConnection<double>> Connections
		{
			get { return SimulationConnections.Values; }
		}

		/// <summary>
		/// position
		/// </summary>
        public Point Position { get; set; }

        /// <summary>
        /// The delegate to use for retrieving the distance between nodes.
        /// </summary>
        public Func<ISimulationNode, ISimulationNode, double> DistanceDelegate { get; set; }

        /// <summary>
        /// Reference to the actual node. Usually this is a self-reference.
        /// </summary>
        public ISimulationNode OriginalNode { get; set; }

		#endregion

		#endregion
		#region ctor

		/// <summary>
		/// if a network is provided, the node will
		/// automatically add itself to the network.
		/// (given that the id doesn't yet exist in the network)
		/// otherwise the network will set the network
		/// property when the node is added to it.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="name"></param>
		/// <param name="network"></param>
		/// <param name="weight"></param>
		/// <param name="position"></param>
		public Node(string id = "", string name = "", ISimulationNetwork network = null, double weight = 1, Point position = null)
		{
            DistanceDelegate = DefaultDistanceDelegate;

			SimulationConnections = new Dictionary<ISimulationNode, ISimulationConnection>();

			if (string.IsNullOrEmpty(id)) ID = nodeCounter.ToString();
			else ID = id;

			if (String.IsNullOrEmpty(name)) Name = "Node " + ID;
			else Name = name;

			Network = network;
			Weight = weight;
            Position = position;

			if (Network != null)
			{
				if (!network.Add(this))
					throw new IdNotUniqueException(ID, "The provided ID is not unique in the network!");
			}

			nodeCounter += 1;

            OriginalNode = this;
		}

		#endregion
		#region impl

		#region connections

		/// <summary>
		/// only returns true if there is a bidirectional
		/// or outgoing connection to the given node
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool IsConnectedTo(INode<double> other)
		{
			return SimulationConnections.ContainsKey(other as ISimulationNode);
		}

        public IConnection<double> ConnectTo(INode<double> other, double length = 0, bool directed = false) { return ConnectTo(other, length, 0, directed); }
        public IConnection<double> ConnectTo(INode<double> other, bool directed = false) { return ConnectTo(other, 0, 0, directed); }
        public IConnection<double> ConnectTo(INode<double> other) { return ConnectTo(other, 0, 0, false); }

		/// <summary>
		/// creates a path setting the length to the euclidean distance between the nodes
		/// if the given target is not
		/// the method will return null.
		/// CAUTION: if a connection to the given node was already existent, it will be
		/// overridden; this implementation doesn't support multiple connections.
		/// </summary>
		/// <param name="other"></param>
		/// <param name="directed"></param>
		/// <param name="length">
		/// if length is set to null, the euclidean distance will be used
		/// </param>
		/// <param name="weight"></param>
		/// <returns>A new ISimulationConnection to the given node.</returns>
		public virtual IConnection<double> ConnectTo(INode<double> other, double length = 0, double weight = 0, bool directed = false)
		{
			ISimulationNode target = other as ISimulationNode;
			if (target == null) return null;
            if (length == 0) length = Position.GetEuclideanDistance(target.Position);
			ISimulationConnection c = new Connection(this, target, length, weight, directed);
			SimulationConnections[target] = c;
			if (!directed)
				target.SimulationConnections[this] = c; // not really required but comfortable
			return c;
		}

		/// <summary>
		/// find a connection between two nodes. return null if no connection is
		/// available or - in case of a directed connection - if the only connection
		/// if from toNode to fromNode (wrong direction, return null).
		/// if multiple connections exist, only the first one will be returned.
		/// </summary>
		/// <param name="fromNode"></param>
		/// <param name="toNode"></param>
		/// <returns>A ISimulationConnection or null if no connection in the given direction was found</returns>
		public IConnection<double> GetConnection(INode<double> toNode)
		{
			if (!SimulationConnections.ContainsKey(toNode as ISimulationNode)) return null;
			return SimulationConnections[toNode as ISimulationNode];
		}

        /// <summary>
        /// find a connection between two nodes. return null if no ISimulationConnection is
        /// available or - in case of a directed connection - if the only connection
        /// if from toNode to fromNode (wrong direction, return null).
        /// if multiple connections exist, only the first one will be returned.
        /// </summary>
        /// <param name="fromNode"></param>
        /// <param name="toNode"></param>
        /// <returns>A ISimulationConnection or null if no connection in the given direction was found</returns>
        public ISimulationConnection GetSimulationConnection(ISimulationNode targetNode)
        {
            if (!SimulationConnections.ContainsKey(targetNode)) return null;
            return SimulationConnections[targetNode];
        }

		/// <summary>
		/// Gets the stored distance to the given node if they are connected,
		/// double.PositiveInfinity otherwise. Be sure to use this only AFTER
		/// the nodes have been connected.
        /// CAUTION: this works only with an ISimulationNode
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public virtual double DistanceTo(INode<double> other)
		{
            if (other is ISimulationNode) return DistanceDelegate.Invoke(this, other as ISimulationNode);
            else return double.PositiveInfinity;
		}

        /// <summary>
        /// Gets the stored distance to the given node if they are connected,
        /// double.PositiveInfinity otherwise. Be sure to use this only AFTER
        /// the nodes have been connected.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        private double DefaultDistanceDelegate(ISimulationNode node1, ISimulationNode node2)
        {
            if (!node1.SimulationConnections.ContainsKey(node2)) return double.PositiveInfinity;
            return node1.SimulationConnections[node2].Length;
        }

		#endregion
		#region IEquatable<INode<double>>

		/// <summary>
		/// returns true only if the system types and id's are identical
		/// reference equality is ignored
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(INode<double> other)
		{
			ISimulationNode node = other as Node;
			if (node == null) return false;
			return ID == node.Identifier;
		}

		#endregion
        #region ICloneable

        /// <summary>
        /// The distance delegate will also be cloned except if the DefaultDistanceDelegate was used. In this case
        /// the clone's own DefaultDistanceDelegate function will be used.
        /// </summary>
        /// <returns></returns>
        public virtual object Clone()
        {
            Node result = new Node(ID, Name, (ISimulationNetwork)Network, Weight, Position);
            foreach (KeyValuePair<ISimulationNode, ISimulationConnection> kvp in SimulationConnections)
            {
                result.SimulationConnections[kvp.Key] = kvp.Value;
            }
            if (DistanceDelegate == DefaultDistanceDelegate) result.DistanceDelegate = result.DefaultDistanceDelegate;
            else result.DistanceDelegate = (Func<ISimulationNode, ISimulationNode, double>)DistanceDelegate.Clone();
            return result;
        }

        #endregion

		#endregion
        #region util

        /// <summary>
        /// CAUTION: incoming connections will not be cloned, only outgoing ones!
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="createNewConnections"></param>
        /// <returns></returns>
        public virtual Node CreateOffsetNode(Vector offset, bool createNewConnections = false)
        {
            Node result = (Node)this.Clone();
            result.IsTemporary = true;
            result.Position += offset;

            if (createNewConnections)
            {
                result.SimulationConnections.Clear();
                foreach (KeyValuePair<ISimulationNode, ISimulationConnection> kvp in SimulationConnections)
                {
                    ISimulationNode otherNode;
                    ISimulationConnection offsetConnection = (ISimulationConnection)kvp.Value.Clone();

                    if (this == offsetConnection.Node1)
                    {
                        offsetConnection.Node1 = result;
                        otherNode = offsetConnection.Node2;
                    }
                    else
                    {
                        offsetConnection.Node2 = result;
                        otherNode = offsetConnection.Node1;
                    }
                    offsetConnection.OriginalConnection = kvp.Value;
                    offsetConnection.Length = result.Position.GetEuclideanDistance(otherNode.Position);
                    result.SimulationConnections[kvp.Key] = offsetConnection;

                    if (offsetConnection is Connection) (offsetConnection as Connection).IsTemporary = true;
                }
            }

            result.OriginalNode = this.OriginalNode;
            return result;
        }

        #endregion
    }
}