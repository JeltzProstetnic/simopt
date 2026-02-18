using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using MatthiasToolbox.Basics.Interfaces;
using System.Windows;

namespace MatthiasToolbox.Basics.Datastructures.Graph
{
    public interface IEdge<TPoint> : INamedElement
    {
        /// <summary>
        /// If true, the edge points from Vertex1 to Vertex2.
        /// </summary>
        bool IsDirected { get; set; }

        /// <summary>
        /// Source node
        /// </summary>
        IVertex<TPoint> Vertex1 { get; set; }
        
        /// <summary>
        /// Target node
        /// </summary>
        IVertex<TPoint> Vertex2 { get; set; }

        /// <summary>
        /// The points over which the edge goes (usually at least two).
        /// </summary>
        IEnumerable<TPoint> Path { get; set; }
    }
}