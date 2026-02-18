using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Basics.Datastructures.Network;


namespace SimOpt.Basics.Datastructures.Graph
{
    public interface IGraph<TPoint>
    {
        IEnumerable<IVertex<TPoint>> Vertices { get; }
        IEnumerable<IEdge<TPoint>> Edges { get; }

        bool AddVertex(IVertex<TPoint> vertex);
        bool AddEdge(IEdge<TPoint> edge);

        bool RemoveVertex(IVertex<TPoint> vertex);
        bool RemoveEdge(IEdge<TPoint> edge);
    }
}