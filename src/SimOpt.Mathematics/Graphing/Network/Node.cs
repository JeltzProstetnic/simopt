using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Basics.Datastructures.Network;

namespace SimOpt.Mathematics.Graphing.Network
{
    [Serializable]
    public class Node : INode
    {
        #region cvar

        private Network network;
        private List<Connection> connections;

        #endregion
        #region prop

        #region IVertex

        public string Name
        {
            get;
            set;
        }

        public Basics.Datastructures.Geometry.Point Position
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        #endregion
        #region INode<double>

        public INetwork<double> Network
        {
            get
            {
                return network;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public double Weight { get; set; }

        // TODO: one time setter
        public bool InMemory { get; set; }

        public IEnumerable<INode<double>> ConnectedNodes
        {
            get { throw new NotImplementedException(); }
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
        #region ctor

        public Node(Network network)
        {
            network.AddNode(this);
        }

        #endregion
        #region impl

        public void ConnectTo(Node other)
        {
            Connection c = new Connection(network); // cannot use other ctor!
            c.ConnectedNodes = new Tuple<INode<double>, INode<double>>(this, other);
            if (InMemory) connections.Add(c);
        }

        #region INode<double>

        public bool IsConnectedTo(INode<double> other)
        {
            throw new NotImplementedException();
        }

        public IConnection<double> GetConnection(INode<double> toNode)
        {
            throw new NotImplementedException();
        }

        public double DistanceTo(INode<double> other)
        {
            throw new NotImplementedException();
        }

        public IConnection<double> ConnectTo(INode<double> other, double length, double weight, bool directed)
        {
            throw new NotImplementedException();
        }

        public IConnection<double> ConnectTo(INode<double> other, double length, bool directed)
        {
            throw new NotImplementedException();
        }

        public IConnection<double> ConnectTo(INode<double> other, bool directed)
        {
            throw new NotImplementedException();
        }

        public IConnection<double> ConnectTo(INode<double> other)
        {
            
            throw new NotImplementedException();
        }

        #endregion
        #region IEquatable<INode<double>>

        public bool Equals(INode<double> other)
        {
            throw new NotImplementedException();
        }

        #endregion

        #endregion
    }
}
