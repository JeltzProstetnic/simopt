using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace MatthiasToolbox.Geography.Controls
{
    public class WorldTreeViewModel
    {
        private readonly ReadOnlyCollection<MacroRegionViewModel> _regions;

        public ReadOnlyCollection<MacroRegionViewModel> MacroRegions
        {
            get { return _regions; }
        }

        public WorldTreeViewModel(IEnumerable<MacroRegion> macroRegions)
        {
            _regions = new ReadOnlyCollection<MacroRegionViewModel>(
                (from region in macroRegions
                 select new MacroRegionViewModel(region))
                .ToList());
        }
    }
}
