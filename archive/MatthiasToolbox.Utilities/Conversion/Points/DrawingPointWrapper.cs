using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Basics.Interfaces;

namespace MatthiasToolbox.Utilities.Conversion.Points
{
    public class DrawingPointWrapper : IPoint3D<int>
    {
        #region cvar

        private System.Drawing.Point original;

        #endregion
        #region prop

        #region IPoint3D<int>

        public int Z
        {
            get { return 0; }
            set { throw new NotImplementedException(); }
        }

        #endregion
        #region IPoint2D<int>

        public int X
        {
            get { return original.X; }
            set { original.X = value; }
        }

        public int Y
        {
            get { return original.Y; }
            set { original.Y = value; }
        }

        #endregion

        #endregion
        #region ctor

        public DrawingPointWrapper(System.Drawing.Point p)
        {
            this.original = p;
        }

        #endregion
    }
}
