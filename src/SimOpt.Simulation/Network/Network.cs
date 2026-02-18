using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Basics.Datastructures.Network;
using SimOpt.Simulation.Interfaces;
using SimOpt.Mathematics.Graphing.Interfaces;
using SimOpt.Simulation.Engine;
using SimOpt.Basics.Datastructures.Graph;
using SimOpt.Basics.Datastructures.Geometry;

namespace SimOpt.Simulation.Network
{
    [Serializable]
	public class Network : ISimulationNetwork
	{
		#region cvar

		private Dictionary<ISimulationNode, Dictionary<ISimulationNode, ISimulationPath>> fixedPaths;
		private Dictionary<string, ISimulationNode> nodes;
		private IPathFinder<double> pathFinder;
		private bool useFinderCaching;

		#endregion
		#region prop

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
		#region INetwork

		/// <summary>
		/// number of nodes in this network
		/// </summary>
		public int CountNodes
		{
			get { return nodes.Count; }
		}

		/// <summary>
		/// all nodes in this network
		/// </summary>
		public IEnumerable<INode<double>> Nodes
		{
			get { return nodes.Values; }
		}

		#endregion
		#region IEnumerable

		public IEnumerator<ISimulationNode> GetEnumerator()
		{
			return nodes.Values.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return nodes.Values.GetEnumerator();
		}

		#endregion
		#region Indexer

		public ISimulationNode this [string id]
		{
			get { return nodes[id]; }
		}

		#endregion
		#region other

		/// <summary>
		/// The model to which this network belongs
		/// </summary>
		public IModel Model { get; private set; }

		/// <summary>
		/// the predefined path sequences for this network
		/// </summary>
		public Dictionary<ISimulationNode, Dictionary<ISimulationNode, ISimulationPath>> FixedPaths
		{
			get { return fixedPaths; }
		}
		
		/// <summary>
		/// a delegate method to search for a path
		/// </summary>
		public Func<ISimulationNode, ISimulationNode, ISimulationPath> FindPathMethod { get; internal set; }

		/// <summary>
		/// all connections in this network
		/// </summary>
		public IEnumerable<ISimulationConnection> Connections
		{
			get
			{
				foreach (ISimulationNode n in nodes.Values)
				{
					foreach (ISimulationConnection c in n.Connections)
						yield return c;
				}
			}
		}

		#endregion

		#endregion
		#region ctor

		public Network(IModel model, string id, string name = "", Func<ISimulationNode, ISimulationNode, ISimulationPath> findPathMethod = null, bool useFinderCaching = true)
		{
			Model = model;

			nodes = new Dictionary<string, ISimulationNode>();
			fixedPaths = new Dictionary<ISimulationNode, Dictionary<ISimulationNode, ISimulationPath>>();

			this.FindPathMethod = findPathMethod;
			this.useFinderCaching = useFinderCaching;

			if (findPathMethod == null)
				this.FindPathMethod = FindNoPath;
		}

		public Network(IModel model, string id, string name = "", IPathFinder<double> pathFinder = null, bool useFinderCaching = true)
		{
			Model = model;

			nodes = new Dictionary<string, ISimulationNode>();
			fixedPaths = new Dictionary<ISimulationNode, Dictionary<ISimulationNode, ISimulationPath>>();

			this.pathFinder = pathFinder;
			this.useFinderCaching = useFinderCaching;

			if (pathFinder == null)
				this.FindPathMethod = FindNoPath;
			else
				this.FindPathMethod = InvokePathFinder;
		}
		
		#endregion
		#region impl

		/// <summary>
		/// Adds the connection to the internal connection dictionary/ies of the connected nodes.
		/// Caution: the distance will not be calculated.
		/// </summary>
		/// <param name="con"></param>
		public void CreateConnection(ISimulationConnection con)
		{
			ISimulationNode fromNode = con.ConnectedNodes.Item1 as ISimulationNode;
			ISimulationNode toNode = con.ConnectedNodes.Item2 as ISimulationNode;

			fromNode.SimulationConnections[toNode] = con;
			if (!con.IsDirected)
				toNode.SimulationConnections[fromNode] = con; // not really required but comfortable
		}

		#region manage nodes

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
		public bool Add(ISimulationNode node)
		{
			if (nodes.ContainsKey(node.Identifier)) return false;
			node.Network = this;
			nodes[node.Identifier] = node;
			return true;
		}

		/// <summary>
		/// add an existing node to the network
		/// caution: this will not add linked paths
		/// if there are any in the given node.
		/// CAUTION: if a node with the same ID
		/// alreday exists in this network, the
		/// node will not be added!
		/// caution: if the type of the node
		/// is not SimOpt.Simulation.Tools.Node
		/// it will not be added.
		/// </summary>
		/// <param name="node"></param>
		/// <returns>a success flag</returns>
		public bool Add(INode<double> node)
		{
			ISimulationNode n = node as ISimulationNode;
			if (n == null) return false;
			if (nodes.ContainsKey(n.Identifier)) return false;
			n.Network = this;
			nodes[n.Identifier] = n;
			return true;
		}

		public bool Remove(ISimulationNode node)
		{
			if (!nodes.ContainsKey(node.Identifier)) return false;
			nodes.Remove(node.Identifier);
			return true;
		}

		public bool Remove(INode<double> node)
		{
			ISimulationNode n = node as ISimulationNode;
			if (n == null) return false;
			if (!nodes.ContainsKey(n.Identifier)) return false;
			nodes.Remove(n.Identifier);
			return true;
		}

		#endregion
		#region manage paths

		/// <summary>
		/// returns the path between the nodes either containing the a single
		/// direct connection between the nodes or a path predefined through a
		/// fixed path sequences or if neither are defined a path as returned
		/// by the current search method. if no search method is given null
		/// will be returned. if no path is found by the search method, an
		/// empty path or null may be returned, depending on the finder.
		/// CAUTION: there is no default search implemented; so if you have not
		/// defined a fixed path and have not set a FindPathMethod this returns null!
		/// </summary>
		/// <param name="fromNode"></param>
		/// <param name="toNode"></param>
		/// <returns>An ISimulationPath between the given nodes.</returns>
		public IPath<double> FindPath(INode<double> fromNode, INode<double> toNode)
		{
			if(!(fromNode is ISimulationNode && toNode is ISimulationNode)) return null;
			
			IPath<double> result = new Path(this);

			// check for direct connection
			if (fromNode.IsConnectedTo(toNode))
			{
				// result.AppendConnection(fromNode.GetConnection(toNode)); // not good: order will be lost
				result.AppendNode(fromNode);
				result.AppendNode(toNode);
				return result;
			}

			// check for predefined path
			ISimulationNode fr0m = fromNode as ISimulationNode;
			ISimulationNode t0 = toNode as ISimulationNode;
			if (fixedPaths.ContainsKey(fr0m)
			    && fixedPaths[fr0m].ContainsKey(t0))
				return fixedPaths[fr0m][t0];
			
			// use find path method
			return FindPathMethod.Invoke(fr0m, t0);
		}

		/// <summary>
		/// adds a predefined path sequence to be preferred
		/// such a path sequence will always be returned by
		/// FindPath before even searching.
		/// </summary>
		/// <param name="fromNode"></param>
		/// <param name="toNode"></param>
		/// <param name="pathSequence"></param>
		public void SetFixedPath(ISimulationNode fromNode, ISimulationNode toNode, ISimulationPath path)
		{
			bool isReplace = fixedPaths.ContainsKey(fromNode) && fixedPaths[fromNode].ContainsKey(toNode);

			if (!fixedPaths.ContainsKey(fromNode))
				fixedPaths[fromNode] = new Dictionary<ISimulationNode, ISimulationPath>();
			
			fixedPaths[fromNode][toNode] = path;

			if (isReplace)
				this.Log<SIM_INFO>("The current fixed path from " + fromNode.Name + " to " + toNode.Name + " was replaced!", Model);
		}

		#endregion

		#endregion
		#region util

		private ISimulationPath FindNoPath(ISimulationNode fromNode, ISimulationNode toNode)
		{
			return null;
		}

		private ISimulationPath InvokePathFinder(ISimulationNode fromNode, ISimulationNode toNode)
		{
			IPath<double> result = new Path(this);
			bool success = pathFinder.FindShortestPath(this, fromNode, toNode, out result, useFinderCaching);
			return (ISimulationPath)result;
		}

		public IPath<double> CreateEmptyPath()
		{
			return new Path(this);
		}

		#endregion
		#region rset

		/// <summary>
		/// Resets nodes and connections which implement IResettable
		/// </summary>
		public virtual void Reset()
		{
			foreach (INode<double> n in Nodes)
				if (n is IResettable) (n as IResettable).Reset();
			foreach (IConnection<double> c in Connections)
				if (c is IResettable) (c as IResettable).Reset();
		}

		#endregion

        //#region IGraph<INode<double>,IEdge<INode<double>>> Member

        //public IEnumerable<INode<double>> Vertices
        //{
        //    get { return nodes.Values; }
        //}

        //public IEnumerable<Basics.Datastructures.Graph.IEdge<INode<double>>> Edges
        //{
        //    get { throw new NotImplementedException(); }
        //}

        //public bool AddVertex(INode<double> vertex)
        //{
        //    return this.Add(vertex);
        //}

        //public bool AddEdge(Basics.Datastructures.Graph.IEdge<INode<double>> edge)
        //{
        //    throw new NotImplementedException();
        //}

        //public bool Add(object child)
        //{
        //    if (child == null) 
        //        return false;
        //    else if (child is ISimulationNode)
        //        return this.Add(child as ISimulationNode);
        //    else if (child is INode<double>)
        //        return this.Add(child as INode<double>);

        //    return false;
        //}

        //#endregion


        public IEnumerable<IVertex<Point>> Vertices
        {
            get { return nodes.Values; }
        }

        public IEnumerable<IEdge<Point>> Edges
        {
            get { return Connections; }
        }

        public bool AddVertex(IVertex<Point> vertex)
        {
            if ((vertex as ISimulationNode) == null) return false;
            return Add(vertex as ISimulationNode);
        }

        public bool AddEdge(IEdge<Point> edge)
        {
            throw new NotImplementedException();
        }

        public bool RemoveVertex(IVertex<Point> vertex)
        {
            throw new NotImplementedException();
        }

        public bool RemoveEdge(IEdge<Point> edge)
        {
            throw new NotImplementedException();
        }
    }
}