using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Basics.Interfaces
{
    //public interface IPosition
    //{
    //    double X { get; set; }
    //    double Y { get; set; }
    //    double Z { get; set; }
    //}

    public interface IPosition<TPoint> // : IPosition
    {
        TPoint Position { get; set; }
    }
}