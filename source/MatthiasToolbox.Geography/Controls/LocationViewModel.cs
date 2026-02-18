using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Geography.Controls
{
    public class LocationViewModel : TreeViewItemViewModel
    {
        readonly Location location;

        public LocationViewModel(Location location, CountryViewModel parent)
            : base(parent, location, false)
        {
            this.location = location;
        }
    }
}
