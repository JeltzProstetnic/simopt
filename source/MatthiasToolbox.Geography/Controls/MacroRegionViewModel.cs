using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Geography.Controls
{
    public class MacroRegionViewModel : TreeViewItemViewModel
    {
        readonly MacroRegion macroRegion;

        public MacroRegionViewModel(MacroRegion region) 
            : base(null, region, true)
        {
            this.macroRegion = region;
        }

        protected override void LoadChildren()
        {
            foreach(SubRegion subRegion in macroRegion.SubRegions.Values)
                base.Children.Add(new SubRegionViewModel(subRegion, this));
            base.LoadChildren();
        }
    }
}
