using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Basics.Datastructures.Graph;
using MatthiasToolbox.Basics.Datastructures.Geometry;

namespace MatthiasToolbox.Basics.Datastructures.Network
{
    public interface INetwork : INetwork<double>
    {
    }

    public interface INetwork<T> : IGraph<Point>
        where T : IComparable<T>
    {
        /// <summary>
        /// the total number of nodes within the network
        /// </summary>
        int CountNodes { get; }

        /// <summary>
        /// enumerate all nodes within the network
        /// </summary>
        IEnumerable<INode<T>> Nodes { get; }

        /// <summary>
        /// add a node to the network
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        bool Add(INode<T> node);

        /// <summary>
        /// remove a node from the network. this will also 
        /// remove all associated connections and may 
        /// invalidate cached paths.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        bool Remove(INode<T> node);

        /// <summary>
        /// find a path if one exists between the nodes.
        /// </summary>
        /// <param name="fromNode"></param>
        /// <param name="toNode"></param>
        /// <returns>null if no path can be found</returns>
        IPath<T> FindPath(INode<T> fromNode, INode<T> toNode);

        /// <summary>
        /// factory for a new empty path.
        /// </summary>
        /// <returns></returns>
        IPath<T> CreateEmptyPath();
    }
}
