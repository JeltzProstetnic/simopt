using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Geography.Controls
{
    public class SubRegionViewModel : TreeViewItemViewModel
    {
        readonly SubRegion subRegion;

        public SubRegionViewModel(SubRegion region, MacroRegionViewModel parent)
            : base(parent, region, true)
        {
            this.subRegion = region;
        }

        protected override void LoadChildren()
        {
            foreach(Country country in subRegion.Countries.Values)
                base.Children.Add(new CountryViewModel(country, this));
            base.LoadChildren();
        }
    }
}
