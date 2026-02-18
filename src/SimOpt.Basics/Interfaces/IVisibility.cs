using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimOpt.Basics.Interfaces
{
    public interface IVisibility<T>
    {
        T Visibility { get; set; }
        bool IsVisible { get; }
    }

    public interface IVisibility
    {
        bool IsVisible { get; set; }
    }
}