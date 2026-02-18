using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Basics.Datastructures.Network;

namespace MatthiasToolbox.Simulation.Network
{
    [Serializable]
	public class Path : ISimulationPath
	{
		#region cvar

		private bool isValid;
		private double length;
		private ISimulationNetwork network;
		private bool hasNetwork;

		#endregion
		#region prop

		public bool IsDirected { get; private set; }

		public bool IsEmpty { get { return SimulationNodes.Count == 0; } }

		public bool IsValid {
			get
			{
				return isValid;
			}
		}

		public IEnumerable<INode<double>> Nodes
		{
			get { return SimulationNodes; }
		}

		public int NodeCount { get { return SimulationNodes.Count; } }

		public IEnumerable<IConnection<double>> Connections
		{
			get { return SimulationConnections; }
		}

		public List<ISimulationConnection> SimulationConnections { get; set; }
		
		public List<ISimulationNode> SimulationNodes { get; set; }
		
		
		public int ConnectionCount { get { return SimulationConnections.Count; } }

		/// <summary>
		/// meters
		/// </summary>
		public double Length
		{
			get { return length; }
			set { length = value; }
		}

		public ISimulationNode FirstNode { get; set; }

		public ISimulationConnection FirstConnection { get; set; }

		public ISimulationNode LastNode { get; set; }

		public ISimulationConnection LastConnection { get; set; }

		public ISimulationNetwork Network { get { return network; } }

		#endregion
		#region ctor

		public Path(ISimulationNetwork network = null)
		{
			length = 0;
			this.network = network;
			this.hasNetwork = network != null;
			SimulationNodes = new List<ISimulationNode>();
			SimulationConnections = new List<ISimulationConnection>();
		}

		#endregion
		#region impl

		#region create

		public bool CreateFrom(params ISimulationConnection[] connections)
		{
			return CreateFrom(connections.ToList());
		}

		public bool CreateFrom(IEnumerable<ISimulationConnection> connections)
		{
			if (!IsEmpty) return false;

			length = 0;
			ISimulationNode lastNode = null;
			ISimulationConnection lastConnection = null;

			foreach (ISimulationConnection con in connections)
			{
				// if we are currently enumerating
				// the second or a later connection
				if (lastNode != null)
				{
					// if the start node of this connection is not
					// equal to the first node of the current one
					if (con.Node1 != lastNode)
					{
						if(hasNetwork) this.Log<SIM_WARNING>(network.Name + " (id=" + network.Identifier + ") attempted to create an invalid path.", network.Model);
						Clear();
						return false;
					}
				}
				else
				{
					FirstNode = con.Node1;
					FirstConnection = con;
				}
				length += con.Length;
				this.SimulationConnections.Add(con);
				this.SimulationNodes.Add(con.Node1);
				lastNode = con.Node2;
				lastConnection = con;
			}
			SimulationNodes.Add(lastNode);
			
			LastNode = lastNode;
			LastConnection = lastConnection;

			isValid = true;
			return true;
		}

		public bool CreateFrom(params ISimulationNode[] nodes)
		{
			return CreateFrom(nodes.ToList());
		}

		public bool CreateFrom(IEnumerable<ISimulationNode> nodes)
		{
			if (!IsEmpty) return false;
			
			ISimulationNode lastNode = null;
			ISimulationConnection lastConnection = null;
			FirstConnection = null;
			length = 0;

			foreach (ISimulationNode n in nodes)
			{
				if (lastNode == null)
				{
					FirstNode = n;
				}
				else
				{
					if (!lastNode.IsConnectedTo(n))
					{
						Clear();
						return false;
					}
					lastConnection = lastNode.GetConnection(n) as ISimulationConnection;
					if (lastConnection == null)
					{
						throw new ApplicationException("OMG! Something terrible happened..."); // TODO: message
					}
					if (FirstConnection == null) FirstConnection = lastConnection;
					SimulationConnections.Add(lastConnection);
					length += lastConnection.Length;
				}
				lastNode = n;
				this.SimulationNodes.Add(n);
			}

			if (FirstConnection == null) FirstConnection = lastConnection;
			LastNode = lastNode;
			LastConnection = lastConnection;

			isValid = true;
			return true;
		}

		#endregion
		#region create static

		public static ISimulationPath Create(params ISimulationConnection[] connections)
		{
			return Create(connections.ToList(), connections[0].Node1.Network as ISimulationNetwork);
		}

        public static ISimulationPath Create(IEnumerable<ISimulationConnection> connections, ISimulationNetwork network = null)
		{
			ISimulationNetwork net = network;
			if (net == null) net = connections.First().Node1.Network as ISimulationNetwork;
			bool hasNetwork = net != null;
			Path result = new Path(net);

			result.length = 0;
			ISimulationNode lastNode = null;
			ISimulationConnection lastConnection = null;

			foreach (ISimulationConnection con in connections)
			{
				// if we are currently enumerating
				// the second or a later connection
				if (lastNode != null)
				{
					// if the start node of this connection is not
					// equal to the first node of the current one
					if (con.Node1 != lastNode)
					{
						if (hasNetwork) Logger.Log<SIM_WARNING>("MatthiasToolbox.Simulation.Tools.Path", net.Name + " (id=" + net.Identifier + ") attempted to create an invalid path.", net.Model);
						result.Clear();
						return null;
					}
				}
				else
				{
					result.FirstNode = con.Node1;
					result.FirstConnection = con;
				}
				result.length += con.Length;
				result.SimulationConnections.Add(con);
				result.SimulationNodes.Add(con.Node1);
				lastNode = con.Node2;
				lastConnection = con;
			}
			result.SimulationNodes.Add(lastNode);

			result.LastNode = lastNode;
			result.LastConnection = lastConnection;

			result.isValid = true;
			return result;
		}

        public static Path Create(params ISimulationNode[] nodes)
		{
			return Create(nodes.ToList(), nodes[0].Network as ISimulationNetwork);
		}

        public static Path Create(IEnumerable<ISimulationNode> nodes, ISimulationNetwork network = null)
		{
			ISimulationNetwork net = network;
			if (net == null) net = nodes.First().Network as ISimulationNetwork;
			bool hasNetwork = net != null;
			Path result = new Path(net);

			ISimulationNode lastNode = null;
			ISimulationConnection lastConnection = null;
			result.FirstConnection = null;
			result.length = 0;

			foreach (ISimulationNode n in nodes)
			{
				if (lastNode == null)
				{
					result.FirstNode = n;
				}
				else
				{
					if (!lastNode.IsConnectedTo(n))
					{
						result.Clear();
						return null;
					}
					lastConnection = lastNode.GetConnection(n) as ISimulationConnection;
					if (lastConnection == null)
					{
						throw new ApplicationException("OMG! Something terrible happened..."); // TODO: message
					}
					if (result.FirstConnection == null) result.FirstConnection = lastConnection;
					result.SimulationConnections.Add(lastConnection);
					result.length += lastConnection.Length;
				}
				lastNode = n;
				result.SimulationNodes.Add(n);
			}

			result.LastNode = lastNode;
			result.LastConnection = lastConnection;

			result.isValid = true;
			return result;
		}

		#endregion
		#region extend

		/// <summary>
		/// If the path is yet empty, create will be used. Caution: when creating paths from
		/// connections, the path may take any direction.
		/// </summary>
		/// <param name="connection"></param>
		/// <returns></returns>
		public bool AppendConnection(IConnection<double> connection)
		{
			if(!(connection is ISimulationConnection)) throw new ArgumentException("This path can only be extended with a connection of the type ISimulationConnection.");
			ISimulationConnection con = connection as ISimulationConnection;
			
			if (IsEmpty) return CreateFrom(con);
			if(LastNode == con.ConnectedNodes.Item1)
			{
				LastNode = con.Node2;
				LastConnection = con;
				length += LastConnection.Length;

				this.SimulationNodes.Add(LastNode);
				SimulationConnections.Add(LastConnection);

				return true;
			}
			else if(LastNode == con.ConnectedNodes.Item2 &&
			        !con.IsDirected)
			{
				LastNode = con.Node1;
				LastConnection = con;
				length += LastConnection.Length;

				this.SimulationNodes.Add(LastNode);
				SimulationConnections.Add(LastConnection);

				return true;
			}
			if(hasNetwork) this.Log<SIM_WARNING>("Attempted to append a connection to the path of which this " +
			                                     "path's last node was not a part (or the connection was directed and the " +
			                                     "last node was not the outgoing one).", network.Model);
			return false;
		}

		/// <summary>
		/// if the path is yet empty, create will be used
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public bool AppendNode(INode<double> node)
		{
			if(!(node is ISimulationNode)) throw new ArgumentException("This path can only be extended with a connection of the type ISimulationConnection.");
			ISimulationNode n = node as ISimulationNode;
			
			if (IsEmpty) return CreateFrom(n);
			if (LastNode.IsConnectedTo(n))
			{
				LastConnection = LastNode.GetConnection(n) as ISimulationConnection;
				LastNode = n;
				length += LastConnection.Length;
				
				this.SimulationNodes.Add(n);
				SimulationConnections.Add(LastConnection);
				
				return true;
			}
			else if (n.IsConnectedTo(LastNode))
			{
				ISimulationConnection con = n.GetConnection(LastNode) as ISimulationConnection;
				if (!con.IsDirected)
				{
					LastNode = n;
					LastConnection = con;
					length += LastConnection.Length;
					
					this.SimulationNodes.Add(n);
					SimulationConnections.Add(LastConnection);
					
					return true;
				}
			}
			if (hasNetwork) this.Log<SIM_WARNING>("Attempted to append a node to the path which is " +
			                                      "not connected to the path's last node (or the connection was directed and the " +
			                                      "last node was not the outgoing one).", network.Model);
			return false;
		}

		/// <summary>
		/// if the path is yet empty, create will be used
		/// </summary>
		/// <param name="connection"></param>
		/// <returns></returns>
		public bool PrependConnection(IConnection<double> connection)
		{
			if(!(connection is ISimulationConnection)) throw new ArgumentException("This path can only be extended with a connection of the type ISimulationConnection.");
			ISimulationConnection con = connection as ISimulationConnection;
			
			if (IsEmpty) CreateFrom(con);
			if (FirstNode == con.ConnectedNodes.Item2)
			{
				FirstNode = con.Node1;
				FirstConnection = con;
				length += FirstConnection.Length;

				this.SimulationNodes.Insert(0, FirstNode);
				SimulationConnections.Insert(0, FirstConnection);

				return true;
			}
			else if (FirstNode == con.ConnectedNodes.Item1 &&
			         !con.IsDirected)
			{
				FirstNode = con.Node2;
				FirstConnection = con;
				length += FirstConnection.Length;

				this.SimulationNodes.Insert(0, FirstNode);
				SimulationConnections.Insert(0, FirstConnection);

				return true;
			}
			if (hasNetwork) this.Log<SIM_WARNING>("Attempted to prepend a connection to the path of which this " +
			                                      "path's first node was not a part (or the connection was directed and the " +
			                                      "first node was not the incoming one).", network.Model);
			return false;
		}

		/// <summary>
		/// if the path is yet empty, create will be used
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public bool PrependNode(INode<double> node)
		{
			if(!(node is ISimulationNode)) throw new ArgumentException("This path can only be extended with a connection of the type ISimulationConnection.");
			ISimulationNode n = node as ISimulationNode;
			
			if (IsEmpty) CreateFrom(n);
			if (n.IsConnectedTo(FirstNode))
			{
				FirstConnection = n.GetConnection(FirstNode) as ISimulationConnection;
				FirstNode = n;
				length += FirstConnection.Length;

				this.SimulationNodes.Insert(0, n);
				SimulationConnections.Insert(0, FirstConnection);

				return true;
			}
			else if (FirstNode.IsConnectedTo(n))
			{
				ISimulationConnection con = FirstNode.GetConnection(n) as ISimulationConnection;
				if (!con.IsDirected)
				{
					FirstNode = n;
					FirstConnection = con;
					length += FirstConnection.Length;

					this.SimulationNodes.Insert(0, n);
					SimulationConnections.Insert(0, FirstConnection);

					return true;
				}
			}
			if (hasNetwork) this.Log<SIM_WARNING>("Attempted to prepend a node to the path which is " +
			                                      "not connected to the path's first node (or the connection was directed and the " +
			                                      "first node was not the incoming one).", network.Model);
			return false;
		}

		#endregion
        #region switch nodes

        /// <summary>
        /// replaces the first occurrence of oldNode with newNode
        /// </summary>
        /// <param name="oldNode"></param>
        /// <param name="newNode"></param>
        /// <returns></returns>
        public bool ReplaceNode(ISimulationNode oldNode, ISimulationNode newNode)
        {
            return ReplaceNode(oldNode, newNode, true);
        }

        /// <summary>
        /// replaces the first occurrence of oldNode with newNode
        /// </summary>
        /// <param name="oldNode"></param>
        /// <param name="newNode"></param>
        /// <returns></returns>
        public bool ReplaceNode(ISimulationNode oldNode, ISimulationNode newNode, bool createMissingConnections = true)
        {
            #region declarations

            bool hasIncomingConnection = true;
            bool hasOutgoingConnection = true;
            
            ISimulationConnection incomingOld;
            ISimulationConnection outgoingOld;

            ISimulationConnection incomingNew = null;
            ISimulationConnection outgoingNew = null;

            ISimulationNode pre = null;
            ISimulationNode post = null;

            int nodeIndex;
            int incomingIndex = -1;
            int outgoingIndex = -1;

            #endregion
            #region check connections

            bool connectionsOK = false;

            if (oldNode == FirstNode)
            {
                hasIncomingConnection = false;
                post = GetNodeAfter(oldNode);
                connectionsOK = newNode.IsConnectedTo(post);
                if (!connectionsOK && createMissingConnections)
                {
                    newNode.ConnectTo(post);
                    connectionsOK = true;
                }
            }
            else if (oldNode == LastNode)
            {
                hasOutgoingConnection = false;
                pre = GetNodeBefore(oldNode);
                connectionsOK = pre.IsConnectedTo(newNode);
                if (!connectionsOK && createMissingConnections)
                {
                    pre.ConnectTo(newNode);
                    connectionsOK = true;
                }
            }
            else
            {
                pre = GetNodeBefore(oldNode);
                post = GetNodeAfter(oldNode);
                connectionsOK = pre.IsConnectedTo(newNode) && newNode.IsConnectedTo(post);
                if (!connectionsOK && createMissingConnections)
                {
                    if (!pre.IsConnectedTo(newNode)) pre.ConnectTo(newNode);
                    if (!newNode.IsConnectedTo(post)) newNode.ConnectTo(post);
                    connectionsOK = true;
                }
            }

            if (!connectionsOK) throw new InvalidOperationException("The replacement node must be connected to its neighbours in the path.");

            #endregion
            #region retrieve indices

            nodeIndex = SimulationNodes.FindIndex(n => n == oldNode);

            if (hasIncomingConnection)
            {
                incomingOld = (ISimulationConnection)pre.GetConnection(oldNode);
                incomingNew = (ISimulationConnection)pre.GetConnection(newNode);
                incomingIndex = SimulationConnections.FindIndex(c => c == incomingOld);
            }

            if (hasOutgoingConnection)
            {
                outgoingOld = (ISimulationConnection)oldNode.GetConnection(post);
                outgoingNew = (ISimulationConnection)newNode.GetConnection(post);
                outgoingIndex = SimulationConnections.FindIndex(c => c == outgoingOld);
            }

            #endregion
            #region replace

            SimulationNodes[nodeIndex] = newNode;
            if (hasIncomingConnection) SimulationConnections[incomingIndex] = incomingNew;
            if (hasOutgoingConnection) SimulationConnections[outgoingIndex] = outgoingNew;

            #endregion

            return true;
        }

        #endregion
        #region shorten

        // TODO: implement path shortening methods

		public bool RemoveFirstConnection()
		{
			throw new NotImplementedException();
		}

		public bool RemoveFirstNode()
		{
			throw new NotImplementedException();
		}

		public bool RemoveLastConnection()
		{
			throw new NotImplementedException();
		}

		public bool RemoveLastNode()
		{
			throw new NotImplementedException();
		}

		#endregion
		#region subpath

		/// <summary>
		/// Creates a new subpath from the sourceNode to the given targetNode.
		/// Throws an exception if the given nodes are not on the path.
		/// </summary>
		/// <param name="sourceNode">The first node for the new path.</param>
		/// <param name="targetNode">The last node for the new path.</param>
		/// <returns>A new path object containing a part of this path.</returns>
		public ISimulationPath GetSubPath(ISimulationNode sourceNode, ISimulationNode targetNode)
		{
			ISimulationPath result = new Path(network);
			bool foundSrc = false;
			
			if (!SimulationNodes.Contains(sourceNode) || !SimulationNodes.Contains(targetNode))
				throw new ArgumentException("One of the given nodes is not part of this path.");

			#if DEBUG
			if (sourceNode == targetNode)
				this.Log<SIM_WARNING>("A path with only one node was extracted from an existing path.", network.Model);
			#endif

			foreach (ISimulationNode n in SimulationNodes)
			{
				if (n == sourceNode) foundSrc = true;
				if (foundSrc) result.AppendNode(n);
				if (n == targetNode) break;
			}

			return result;
		}

		#endregion
		#region distances

		/// <summary>
		/// Calculate the distance from the first node to the given targetNode.
		/// Throws an exception if the given node is not on the path.
		/// </summary>
		/// <param name="targetNode"></param>
		/// <returns></returns>
		public double GetDistanceTo(ISimulationNode targetNode)
		{
			double result = 0;

			if (!SimulationNodes.Contains(targetNode)) throw new ArgumentException("The given node is not part of this path.");
			if (targetNode == FirstNode) return result;
			
			foreach (ISimulationConnection con in SimulationConnections)
			{
				result += con.Length;
				if (con.Node1 == targetNode || con.Node2 == targetNode) break;
			}

			return result;
		}

		/// <summary>
		/// Calculate the distance between the given nodes via this path.
		/// Throws an exception if one of the given nodes is not on the path.
		/// </summary>
		/// <param name="sourceNode">The node from which to count the distance.</param>
		/// <param name="targetNode">The node to which to count the distance.</param>
		/// <returns>The distance between the given nodes when traveling on this path.</returns>
		public double GetDistance(ISimulationNode sourceNode, ISimulationNode targetNode)
		{
			double result = 0;
			bool foundSrc = false;

			if (!SimulationNodes.Contains(sourceNode) || !SimulationNodes.Contains(targetNode))
				throw new ArgumentException("The given node is not part of this path.");
			if (sourceNode == targetNode) return result;

			ISimulationNode previousNode = null;
			foreach (ISimulationNode n in SimulationNodes)
			{
				if (previousNode == null)
				{
					previousNode = n;
				}
				else
				{
					if (previousNode == sourceNode) foundSrc = true;
					if (foundSrc) result += previousNode.GetConnection(n).Length;
					if (n == targetNode) break;
					previousNode = n;
				}
			}

			return result;
		}

		#endregion
		#region find

        public ISimulationNode GetNodeBefore(ISimulationNode node)
		{
			if (node == null || node == FirstNode) return null;
			ISimulationNode result = null;

			foreach (ISimulationNode n in SimulationNodes)
			{
				if (n == node) break;
				result = n;
			}
			
			return result;
		}

        public ISimulationNode GetNodeAfter(ISimulationNode node)
		{
			if (node == LastNode) return null;

			bool found = false;
			ISimulationNode result = null;
			
			foreach (ISimulationNode n in SimulationNodes)
			{
				if (found)
				{
					result = n;
					break;
				}
				if (n == node) found = true;
			}

			return result;
		}

		#endregion
		#region rset

		public void Clear()
		{
			SimulationConnections.Clear();
			SimulationNodes.Clear();
			length = 0;
			FirstConnection = null;
			FirstNode = null;
			LastConnection = null;
			LastNode = null;
			isValid = false;
		}

		#endregion
		#region IComparable<IPath<double>>

		/// <summary>
		/// compares two paths in their length
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public int CompareTo(IPath<double> other)
		{
			return Length.CompareTo(other.Length);
		}

		#endregion
        #region ICloneable

        /// <summary>
        /// Creates a cloned path.
        /// CAUTION: all the contained nodes and connections will also be cloned!
        /// </summary>
        /// <returns></returns>
        public virtual object Clone()
        {
            Path result = new Path(network);
            foreach (ISimulationNode node in SimulationNodes) result.SimulationNodes.Add((ISimulationNode)node.Clone());
            foreach (ISimulationConnection con in SimulationConnections) result.SimulationConnections.Add((ISimulationConnection)con.Clone());

            result.isValid = isValid;
            result.length = length;
            result.IsDirected = IsDirected;
            result.FirstNode = FirstNode;
            result.LastNode = LastNode;
            result.FirstConnection = FirstConnection;
            result.LastConnection = LastConnection;

            return result;
        }

        #endregion

        #endregion
    }
}