using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimOpt.Basics.Interfaces
{
    public interface IPoint2D<T>
    {
        T X { get; set; }
        T Y { get; set; }
    }
}