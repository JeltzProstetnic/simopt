using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Basics.Interfaces
{
    public interface IUserInterfaceElementBase : INamedElement, ITitle, IText, ITooltip, IOpacity
    {
        object Tag { get; set; }
    }

    public interface IUserInterfaceElement<TVisibility, TPosition, TSize> : IUserInterfaceElementBase, ILayout2D<TPosition, TSize>, IVisibility<TVisibility>
    {
    }

    public interface IUserInterfaceElement<TVisibility, TPosition> : IUserInterfaceElementBase, ILayout2D<TPosition>, IVisibility<TVisibility>
    {
    }

    public interface IUserInterfaceElement<TVisibility> : IUserInterfaceElementBase, ILayout2D, IVisibility<TVisibility>
    {
    }

    public interface IUserInterfaceElement : IUserInterfaceElementBase, ILayout2D, IVisibility
    {
    }
}