using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Basics.Datastructures.Network;
using System.Windows;

namespace MatthiasToolbox.Basics.Datastructures.Graph
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