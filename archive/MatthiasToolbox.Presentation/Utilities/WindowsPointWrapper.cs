using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Basics.Interfaces;

namespace MatthiasToolbox.Presentation.Utilities
{
    public class WindowsPointWrapper : IPoint3D<double>
    {
        #region cvar

        private System.Windows.Point original;

        #endregion
        #region prop

        #region IPoint3D<double>

        public double Z
        {
            get { return 0; }
            set { throw new NotImplementedException(); }
        }

        #endregion
        #region IPoint2D<double>

        public double X
        {
            get { return original.X; }
            set { original.X = value; }
        }

        public double Y
        {
            get { return original.Y; }
            set { original.Y = value; }
        }

        #endregion

        #endregion
        #region ctor

        public WindowsPointWrapper(System.Windows.Point p)
        {
            this.original = p;
        }

        #endregion
    }
}
