using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Basics.Datastructures.Graph;
using System.Windows;

namespace MatthiasToolbox.Utilities.Serialization.GraphML
{
    public class Graph : IGraph<Point>
    {
        public List<Vertex> VertexList { get; set; }
        public List<Edge> EdgeList { get; set; }

        #region IGraph<Point>

        public IEnumerable<IVertex<Point>> Vertices
        {
            get { return VertexList; }
        }

        public IEnumerable<IEdge<Point>> Edges
        {
            get { return EdgeList; }
        }

        #endregion

        public bool AddVertex(IVertex<Point> vertex)
        {
            throw new NotImplementedException();
        }

        public bool AddEdge(IEdge<Point> edge)
        {
            throw new NotImplementedException();
        }

        public bool Add(object child)
        {
            throw new NotImplementedException();
        }

        public bool RemoveVertex(IVertex<Point> vertex)
        {
            throw new NotImplementedException();
        }

        public bool RemoveEdge(IEdge<Point> edge)
        {
            throw new NotImplementedException();
        }
    }
}