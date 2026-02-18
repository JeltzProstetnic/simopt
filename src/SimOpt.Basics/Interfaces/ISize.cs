using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimOpt.Basics.Interfaces
{
    public interface ISize
    {
        double Width { get; set; }
        double Height { get; set; }
    }

    public interface ISize<T>
    {
        T Size { get; set; }
    }
}