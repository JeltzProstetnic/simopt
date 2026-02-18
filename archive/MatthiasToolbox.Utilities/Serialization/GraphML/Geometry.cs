using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Basics.Interfaces;
using System.Windows;

namespace MatthiasToolbox.Utilities.Serialization.GraphML
{
    public class Geometry : ILayout2D
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public double X { get; set; }
        public double Y { get; set; }

        // public IPoint2D<double> Position { get { return new Point<double>(X, Y);  } }
        // public IPoint2D<double> Center { get { return new Point<double>(X + (Width / 2), Y + (Height / 2)); } }
        public Point Center { get { return new Point(X + (Width / 2), Y + (Height / 2)); } }

        #region IPosition<Point>

        public Point Position
        {
            get
            {
                return new Point(X, Y);
            }
            set
            {
                X = value.X;
                Y = value.Y;
            }
        }

        #endregion
    }
}
