using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Basics.Datastructures.Network
{
    public interface IPath : IPath<double>
    {
    }

    public interface IPath<T> : IComparable<IPath<T>>
        where T : IComparable<T>
    {
        /// <summary>
        /// indicates if the path is directed.
        /// </summary>
        bool IsDirected { get; }

        /// <summary>
        /// the nodes on this path
        /// </summary>
        IEnumerable<INode<T>> Nodes { get; }

        /// <summary>
        /// the connections which make up this path
        /// </summary>
        IEnumerable<IConnection<T>> Connections { get; }

        /// <summary>
        /// add a new connection to the end of this path.
        /// </summary>
        /// <param name="connection"></param>
        /// <returns>a success flag</returns>
        bool AppendConnection(IConnection<T> connection);

        /// <summary>
        /// add a new node to the end of this path
        /// </summary>
        /// <param name="node"></param>
        /// <returns>a success flag</returns>
        bool AppendNode(INode<T> node);

        /// <summary>
        /// add a new connection to the beginning of this path
        /// </summary>
        /// <param name="connection"></param>
        /// <returns>a success flag</returns>
        bool PrependConnection(IConnection<T> connection);

        /// <summary>
        /// add a new node to the beginning of this path
        /// </summary>
        /// <param name="node"></param>
        /// <returns>a success flag</returns>
        bool PrependNode(INode<T> node);

        /// <summary>
        /// the total length of this path
        /// </summary>
        T Length { get; set; }
    }
}
