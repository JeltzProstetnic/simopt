using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace MatthiasToolbox.Basics.Interfaces
{
    public interface ILayout2D<TPoint, TSize> : ISize<TSize>, IPosition<TPoint>
    {
    }

    public interface ILayout2D<TPoint> : ISize, IPosition<TPoint>
    {
    }

    public interface ILayout2D : ISize, IPosition<Point>
    {
    }
}