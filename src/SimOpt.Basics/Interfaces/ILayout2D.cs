using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Basics.Datastructures.Geometry;

namespace SimOpt.Basics.Interfaces
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