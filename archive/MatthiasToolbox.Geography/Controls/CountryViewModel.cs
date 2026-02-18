using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Geography.Controls
{
    public class CountryViewModel : TreeViewItemViewModel
    {
        readonly Country country;

        public CountryViewModel(Country country, SubRegionViewModel parent)
            : base(parent, country, true)
        {
            this.country = country;
        }

        protected override void LoadChildren()
        {
            foreach(City city in country.Cities.Values)
                base.Children.Add(new CityViewModel(city, this));
            foreach (Location location in country.Locations.Values)
                base.Children.Add(new LocationViewModel(location, this));
            base.LoadChildren();
        }
    }
}
