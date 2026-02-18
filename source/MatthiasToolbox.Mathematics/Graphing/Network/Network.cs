using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Basics.Datastructures.Network;

namespace MatthiasToolbox.Mathematics.Graphing.Network
{
    [Serializable]
    public class Network : INetwork
    {
        #region cvar

        private List<Node> nodes;
        private List<Connection> connections;

        #endregion
        #region prop

        // TODO: should be a one-time setter
        public bool InMemory { get; set; }

        #region INetwork<double>

        public int CountNodes
        {
            get
            {
                if (InMemory) return nodes.Count;
                else throw new NotImplementedException();
            }
        }

        public int CountConnections
        {
            get
            {
                if (InMemory) return connections.Count;
                else throw new NotImplementedException();
            }
        }

        public IEnumerable<INode<double>> Nodes
        {
            get 
            {
                if (InMemory) return nodes;
                else throw new NotImplementedException(); 
            }
        }

        public IEnumerable<IConnection<double>> Connections
        {
            get
            {
                if (InMemory) return connections;
                else throw new NotImplementedException();
            }
        }

        #endregion

        #endregion
        #region impl

        internal void AddNode(Node n)
        {
            if (InMemory) nodes.Add(n);
        }

        internal void AddConnection(Connection c)
        {
            if (InMemory) connections.Add(c);
        }

        #region INetwork<double>

        public bool Add(INode<double> node)
        {
            throw new NotImplementedException();
        }

        public bool Remove(INode<double> node)
        {
            throw new NotImplementedException();
        }

        public IPath<double> FindPath(INode<double> fromNode, INode<double> toNode)
        {
            throw new NotImplementedException();
        }

        public IPath<double> CreateEmptyPath()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Basics.Datastructures.Graph.IVertex<Basics.Datastructures.Geometry.Point>> Vertices
        {
            get { throw new NotImplementedException(); }
        }

        public IEnumerable<Basics.Datastructures.Graph.IEdge<Basics.Datastructures.Geometry.Point>> Edges
        {
            get { throw new NotImplementedException(); }
        }

        public bool AddVertex(Basics.Datastructures.Graph.IVertex<Basics.Datastructures.Geometry.Point> vertex)
        {
            throw new NotImplementedException();
        }

        public bool AddEdge(Basics.Datastructures.Graph.IEdge<Basics.Datastructures.Geometry.Point> edge)
        {
            throw new NotImplementedException();
        }

        public bool RemoveVertex(Basics.Datastructures.Graph.IVertex<Basics.Datastructures.Geometry.Point> vertex)
        {
            throw new NotImplementedException();
        }

        public bool RemoveEdge(Basics.Datastructures.Graph.IEdge<Basics.Datastructures.Geometry.Point> edge)
        {
            throw new NotImplementedException();
        }

        #endregion

        #endregion
    }
}
