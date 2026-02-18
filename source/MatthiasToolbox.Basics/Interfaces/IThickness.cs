using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Basics.Interfaces
{
    public interface IThickness : IThickness<double>
    {
    }

    public interface IThickness<T>
    {
        T Thickness { get; set; }
    }
}
