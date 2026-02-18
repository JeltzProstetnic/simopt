using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Geography.Controls
{
    public class CityViewModel : TreeViewItemViewModel
    {
        readonly City city;

        public CityViewModel(City city, CountryViewModel parent)
            : base(parent, city, false)
        {
            this.city = city;
        }

        public bool IsCapital { get { return city.Capital; } }
    }
}
