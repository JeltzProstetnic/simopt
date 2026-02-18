using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Basics.Datastructures.Graph;
using System.Windows;

namespace MatthiasToolbox.GraphDesigner.Events
{
    public class EdgeEventArgs
    {
        public IEdge<Point> Edge { get; set; }
    }
}
