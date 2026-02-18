using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimOpt.Mathematics.Geometry.Projections.Geography
{
    public abstract class Cylindrical : Projection
    {
        #region prop

        public bool IsRectilinear { get { return false; } }

        #endregion
    }
}
