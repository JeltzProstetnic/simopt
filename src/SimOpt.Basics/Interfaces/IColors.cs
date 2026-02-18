using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimOpt.Basics.Interfaces
{
    public interface IColors<T>
    {
        T BackgroundColor { get; set; }
        T ForegroundColor { get; set; }
    }
}