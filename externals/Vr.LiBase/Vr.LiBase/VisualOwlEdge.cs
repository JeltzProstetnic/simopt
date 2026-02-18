//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using MatthiasToolbox.Presentation.Interfaces;
//using OwlDotNetApi;
//using System.Windows;
//using System.Windows.Shapes;
//using MatthiasToolbox.Basics.Datastructures.Network;
//using MatthiasToolbox.Basics.Datastructures.Graph;

//namespace Vr.LiBase
//{
//    public class VisualOwlEdge // : IVisualConnection
//    {
//        private IOwlEdge edge;

//        public VisualOwlEdge(IOwlEdge edge)
//        {
//            this.edge = edge;
//        }

//        #region IVisualConnection

//        public Point End { get; set; }

//        public FrameworkElement Shape { get; set; }

//        public Point Start { get; set; }

//        public string ToolTipText { get; set; }

//        #endregion
//        #region IEdge<IVertex>

//        public IVertex<Point> Vertex1 { get; set; }

//        public IVertex<Point> Vertex2 { get; set; }

//        //public IVisualNode Node1 { get; set; }
//        //public IVisualNode Node2 { get; set; }

//        #endregion
//        #region INamedElement

//        public string Name { get; set; }

//        #endregion
//    }
//}
