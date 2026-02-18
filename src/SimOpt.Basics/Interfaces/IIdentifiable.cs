using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimOpt.Basics.Interfaces
{
    public interface IIdentifiable : IIdentifiable<string> { }

    public interface IIdentifiable<T>
    {
        T Identifier { get; }
    }
}
