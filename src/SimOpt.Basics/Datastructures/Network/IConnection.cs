using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Basics.Datastructures.Graph;
using SimOpt.Basics.Datastructures.Geometry;

namespace SimOpt.Basics.Datastructures.Network
{
    public interface IConnection : IConnection<double>
    {
    }

    public interface IConnection<T> : IEdge<Point>, IComparable<IConnection<T>>
        where T : IComparable<T>
    {
        /// <summary>
        /// indicates if the connection is directed
        /// </summary>
        bool IsDirected { get; }

        /// <summary>
        /// the nodes between this connection exists
        /// </summary>
        Tuple<INode<T>, INode<T>> ConnectedNodes { get; }

        /// <summary>
        /// the length of this connection
        /// </summary>
        T Length { get; set; }

        /// <summary>
        /// the weight of this connection
        /// </summary>
        T Weight { get; set; }
    }
}
