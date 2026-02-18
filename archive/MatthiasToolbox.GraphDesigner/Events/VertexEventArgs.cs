using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Basics.Datastructures.Graph;
using System.Windows;

namespace MatthiasToolbox.GraphDesigner.Events
{
    public class VertexEventArgs
    {
        public IVertex<Point> Vertex { get; set; }
    }
}
