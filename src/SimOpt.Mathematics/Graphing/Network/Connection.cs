using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Basics.Datastructures.Network;
using SimOpt.Basics.Datastructures.Graph;
using SimOpt.Basics.Datastructures.Geometry;

namespace SimOpt.Mathematics.Graphing.Network
{
    [Serializable]
    public class Connection : IConnection
    {
        #region prop

        #region IEdge<Point>

        public IVertex<Point> Vertex1
        {
            get { return ConnectedNodes.Item1; }
            set { throw new NotImplementedException(); }
        }

        public IVertex<Point> Vertex2
        {
            get { return ConnectedNodes.Item2; }
            set { throw new NotImplementedException(); }
        }

        public IEnumerable<Point> Path
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public string Name
        {
            get;
            set;
        }

        #endregion
        #region IConnection<double>

        public bool IsDirected
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public Tuple<INode<double>, INode<double>> ConnectedNodes
        {
            get;
            set;
        }

        public double Length { get; set; }

        public double Weight { get; set; }

        #endregion

        #endregion
        #region ctor

        internal Connection(Network network)
        {
            network.AddConnection(this);
        }

        public Connection(Network network, Node node1, Node node2)
        {
            network.AddConnection(this);
            ConnectedNodes = new Tuple<INode<double>, INode<double>>(node1, node2);
            node1.ConnectTo(node2);
        }

        #endregion
        #region impl

        #region IComparable<IConnection<double>>

        public int CompareTo(IConnection<double> other)
        {
            throw new NotImplementedException();
        }

        #endregion

        #endregion
    }
}
