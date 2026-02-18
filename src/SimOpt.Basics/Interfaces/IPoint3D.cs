using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimOpt.Basics.Interfaces
{
    public interface IPoint3D<T> : IPoint2D<T>
    {
        T Z { get; set; }
    }
}