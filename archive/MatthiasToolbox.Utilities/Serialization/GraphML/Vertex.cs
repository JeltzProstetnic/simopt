using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Basics.Datastructures.Graph;
using System.Drawing;
using MatthiasToolbox.Basics.Interfaces;

namespace MatthiasToolbox.Utilities.Serialization.GraphML
{
    public class Vertex : IVertex<System.Windows.Point>, IText, IColors<Color> // , ISize
    {
        private Color foregroundColor = Color.Black;

        public string ID { get; set; }
        public string Label { get; set; }
        public Geometry Geometry { get; set; }
        public Color Fill { get; set; }

        #region INamedElement

        public string Name
        {
            get; set;
        }

        #endregion
        #region IText

        public string Text
        {
            get
            {
                return Label;
            }
            set
            {
                Label = value;
            }
        }

        #endregion
        #region IColors<Color>

        public Color BackgroundColor
        {
            get { return Fill; }
            set { Fill = value; }
        }

        public Color ForegroundColor
        {
            get
            {
                return foregroundColor;
            }
            set
            {
                foregroundColor = value;
            }
        }

        #endregion
        #region IPosition

        public double X
        {
            get
            {
                return Geometry.X;
            }
            set
            {
                Geometry.X = value;
            }
        }

        public double Y
        {
            get
            {
                return Geometry.Y;
            }
            set
            {
                Geometry.Y = value;
            }
        }

        #endregion
        #region ISize

        public double Width
        {
            get
            {
                return Geometry.Width;
            }
            set
            {
                Geometry.Width = value;
            }
        }

        public double Height
        {
            get
            {
                return Geometry.Height;
            }
            set
            {
                Geometry.Height = value;
            }
        }

        #endregion

        public System.Windows.Point Center
        {
            get
            {
                return new System.Windows.Point((int)(X + (Width / 2)), (int)(Y + (Height / 2)));
            }
        }

        #region IPosition<Point> Member

        public System.Windows.Point Position
        {
            get
            {
                return Geometry.Position;
            }
            set
            {
                Geometry.Position = value;
            }
        }

        #endregion
    }
}