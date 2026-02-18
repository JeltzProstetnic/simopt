using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Geography.Controls
{
    public interface IWorldTreeItem
    {
        string Name { get; }
        ILocation UnderlyingLocation { get; }
        bool IsLocation { get; }
        bool IsCity { get; }
        bool IsCountry { get; }
        bool IsSubRegion { get; }
        bool IsMacroRegion { get; }
    }
}
