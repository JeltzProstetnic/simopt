using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimOpt.Mathematics.Geometry.Projections.Geography
{
    public abstract class PseudoCylindrical : Projection
    {
        #region prop

        public bool IsRectilinear { get { return true; } }

        #endregion
    }
}
