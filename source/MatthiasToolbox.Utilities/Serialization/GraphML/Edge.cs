using MatthiasToolbox.Basics.Datastructures.Graph;
using MatthiasToolbox.Basics.Interfaces;
using System.Collections.Generic;
using System;
using System.Windows;

namespace MatthiasToolbox.Utilities.Serialization.GraphML
{
    public class Edge : IEdge<Point>, IIdentifiable
    {
        public Point Start { get; set; }
        public Point End { get; set; }

        #region IIdentifiable

        public string ID { get; set; }
        public string Identifier { get { return ID; } }

        #endregion
        #region INamedElement

        public string Name { get; set; }

        #endregion
        #region IEdge<Point>

        public IEnumerable<Point> Path
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public IVertex<Point> Vertex1 { get; set; }
        public IVertex<Point> Vertex2 { get; set; }

        public bool IsDirected
        {
            get { return true; }
            set { throw new NotImplementedException(); }
        }

        #endregion
    }
}