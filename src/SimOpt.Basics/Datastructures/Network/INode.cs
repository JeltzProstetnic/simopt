using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Basics.Datastructures.Graph;
using SimOpt.Basics.Datastructures.Geometry;

namespace SimOpt.Basics.Datastructures.Network
{
    public interface INode : INode<double>
    {
    }

    public interface INode<T> : IVertex<Point>, IEquatable<INode<T>>
        where T : IComparable<T>
    {
        #region prop

        /// <summary>
        /// the network with which the node is associated
        /// </summary>
        INetwork<T> Network { get; set; }

        /// <summary>
        /// returns all connected nodes
        /// </summary>
        IEnumerable<INode<T>> ConnectedNodes { get; }

        /// <summary>
        /// the connections associated with this node
        /// </summary>
        IEnumerable<IConnection<T>> Connections { get; }

        /// <summary>
        /// the weight of the node
        /// </summary>
        T Weight { get; }

        #endregion
        #region impl
        
        #region retrieve connection infos

        /// <summary>
        /// indicate if a connection from this to the given
        /// node exists. returns only true if either an
        /// undirected connection exists or the direction
        /// of the connection is correct. (this -> other)
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        bool IsConnectedTo(INode<T> other);

        /// <summary>
        /// find a connection between two nodes. return null if no connection is
        /// available or - in case of a directed connection - if the only connection
        /// if from toNode to fromNode (wrong direction, return null).
        /// if multiple connections exist, only one will be returned.
        /// </summary>
        /// <param name="fromNode"></param>
        /// <param name="toNode"></param>
        /// <returns>null if no connection in the given direction was found</returns>
        IConnection<T> GetConnection(INode<T> toNode);

        /// <summary>
        /// the distance from this to the other node.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        T DistanceTo(INode<T> other);

        #endregion
        #region connect to other node

        /// <summary>
        /// create a connection from this node to the given node.
        /// if directed is set to true, the connection will be 
        /// directed according to semantics (this -> other)
        /// </summary>
        /// <param name="other"></param>
        /// <param name="directed"></param>
        /// <returns>null if creating the connection failed</returns>
        IConnection<double> ConnectTo(INode<T> other, T length, T weight, bool directed);

        /// <summary>
        /// create a connection from this node to the given node.
        /// if directed is set to true, the connection will be 
        /// directed according to semantics (this -> other)
        /// </summary>
        /// <param name="other"></param>
        /// <param name="directed"></param>
        /// <returns>null if creating the connection failed</returns>
        IConnection<double> ConnectTo(INode<T> other, T length, bool directed);

        /// <summary>
        /// create a connection from this node to the given node.
        /// if directed is set to true, the connection will be 
        /// directed according to semantics (this -> other)
        /// </summary>
        /// <param name="other"></param>
        /// <param name="directed"></param>
        /// <returns>null if creating the connection failed</returns>
        IConnection<double> ConnectTo(INode<T> other, bool directed);

        /// <summary>
        /// create a connection from this node to the given node.
        /// if directed is set to true, the connection will be 
        /// directed according to semantics (this -> other)
        /// </summary>
        /// <returns>null if creating the connection failed</returns>
        IConnection<double> ConnectTo(INode<T> other);

        #endregion

        #endregion
    }
}