using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimOpt.Basics.Interfaces
{
    public interface IColoredUIElement<TColor, TPosition, TSize> : IUserInterfaceElement<TPosition, TSize>, IColors<TColor>
    {
    }

    public interface IColoredUIElement<TColor, TPosition> : IUserInterfaceElement<TPosition>, IColors<TColor>
    {
    }

    public interface IColoredUIElement<TColor> : IUserInterfaceElement, IColors<TColor>
    {
    }
}