using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Data.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MatthiasToolbox.Geography.Controls
{
    /// <summary>
    /// Interaction logic for WorldTree.xaml
    /// with lazy loading of tree items
    /// </summary>
    public partial class WorldTree : UserControl
    {
        #region cvar

        private List<TreeViewItemViewModel> selectedItems;

        #endregion
        #region dele

        /// <summary>
        /// signature to handle a WorldTreeViewItemDoubleClickedHandler event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void WorldTreeViewItemDoubleClickedHandler(ILocation sender, MouseButtonEventArgs e);

        /// <summary>
        /// signature to handle a WorldTreeViewItemSelectedHandler event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void WorldTreeViewItemSelectedHandler(ILocation sender, RoutedPropertyChangedEventArgs<object> e);

        #endregion
        #region evnt

        /// <summary>
        /// occurs when a user double-clicks an item
        /// </summary>
        public event WorldTreeViewItemDoubleClickedHandler ItemDoubleClicked;

        /// <summary>
        /// occurs when a user double-clicks an item
        /// </summary>
        public event WorldTreeViewItemSelectedHandler ItemSelected;

        #endregion
        #region prop

        public List<ILocation> SelectedItems
        {
            get
            {
                List<ILocation> result = new List<ILocation>();
                foreach (TreeViewItemViewModel tvivm in selectedItems)
                {
                    result.Add(tvivm.UnderlyingLocation);
                }
                return result;
            }
        }

        public ILocation SelectedItem
        {
            get
            {
                if (selectedItems.Count == 0) return null;
                return selectedItems[0].UnderlyingLocation;
            }
        }

        #endregion
        #region ctor

        /// <summary>
        /// default constructor
        /// </summary>
        public WorldTree()
        {
            InitializeComponent();

            selectedItems = new List<TreeViewItemViewModel>();

            WorldTreeViewModel viewModel = new WorldTreeViewModel(GeoDatabase.MacroRegionsByName.Values);
            base.DataContext = viewModel;
        }

        #endregion
        #region hand

        private void treeViewWorld_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // unselect all if user clicked nirvana
            foreach (TreeViewItemViewModel item in selectedItems)
            {
                item.IsSelected = false;
            }
            selectedItems.Clear();
        }

        private void treeViewWorld_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (sender == null) return;

            TreeViewItemViewModel item = (TreeViewItemViewModel)e.NewValue;
            if (item != null)
            {
                selectedItems.Add(item);
                OnItemSelected(item.UnderlyingLocation, e);
            }
        }

        private void treeViewWorld_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TreeView tv = sender as TreeView;
            if (tv == null) return;
            TreeViewItemViewModel item = tv.SelectedItem as TreeViewItemViewModel;
            if (item == null) return;

            OnItemDoubleClicked(item.UnderlyingLocation, e);
        }

        #endregion
        #region impl

        public void UnselectAll()
        {
            foreach (TreeViewItemViewModel item in selectedItems)
            {
                item.IsSelected = false;
            }
            selectedItems.Clear();
        }

        #region select item

        public void SelectMacroRegion(MacroRegion macroRegion)
        {
            SelectMacroRegion(macroRegion.ID);
        }

        public void SelectMacroRegion(int macroRegionID)
        {
            selectedItems.Clear();
            foreach (TreeViewItemViewModel item in treeViewWorld.Items)
            {
                if (item.UnderlyingLocation.ID == macroRegionID)
                {
                    item.IsSelected = true;
                    selectedItems.Add(item);
                    break;
                }
            }
        }

        /// <summary>
        /// This will only work if the item has been loaded yet.
        /// Subitems are always lazy loaded here, so expand the item first.
        /// </summary>
        /// <param name="subRegion"></param>
        public void SelectSubRegion(SubRegion subRegion)
        {
            SelectSubRegion(subRegion.ID);
        }

        /// <summary>
        /// This will only work if the item has been loaded yet.
        /// Subitems are always lazy loaded here, so expand the item first.
        /// </summary>
        /// <param name="countryID"></param>
        public void SelectSubRegion(int subRegionID)
        {
            selectedItems.Clear();
            foreach (TreeViewItemViewModel item in treeViewWorld.Items)
            {
                foreach (TreeViewItemViewModel subItem in item.Children)
                {
                    if (subItem.UnderlyingLocation.ID == subRegionID)
                    {
                        subItem.IsSelected = true;
                        selectedItems.Add(subItem);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// This will only work if the item has been loaded yet.
        /// Subitems are always lazy loaded here, so expand the item first.
        /// </summary>
        /// <param name="country"></param>
        public void SelectCountry(Country country)
        {
            SelectCountry(country.ID);
        }

        /// <summary>
        /// This will only work if the item has been loaded yet.
        /// Subitems are always lazy loaded here, so expand the item first.
        /// </summary>
        /// <param name="countryID"></param>
        public void SelectCountry(int countryID)
        {
            selectedItems.Clear();
            foreach (TreeViewItemViewModel item in treeViewWorld.Items)
            {
                if (item.ChildrenLoaded)
                {
                    foreach (TreeViewItemViewModel subItem in item.Children)
                    {
                        if (subItem.ChildrenLoaded)
                        {
                            foreach (TreeViewItemViewModel countryItem in subItem.Children)
                            {
                                if (countryItem.UnderlyingLocation.ID == countryID)
                                {
                                    countryItem.IsSelected = true;
                                    selectedItems.Add(countryItem);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        public void SelectCity(City city)
        {
            SelectCityOrLocation(city.ID);
        }

        public void SelectLocation(Location location)
        {
            SelectCityOrLocation(location.ID);
        }

        public void SelectCityOrLocation(ILocation item)
        {
            SelectCityOrLocation(item.ID);
        }

        public void SelectCityOrLocation(int itemID)
        {
            selectedItems.Clear();
            foreach (TreeViewItemViewModel item in treeViewWorld.Items)
            {
                if (item.ChildrenLoaded)
                {
                    foreach (TreeViewItemViewModel subItem in item.Children)
                    {
                        if (subItem.ChildrenLoaded)
                        {
                            foreach (TreeViewItemViewModel countryItem in subItem.Children)
                            {
                                if (countryItem.ChildrenLoaded)
                                {
                                    foreach (TreeViewItemViewModel cityItem in countryItem.Children)
                                    {
                                        if (cityItem.UnderlyingLocation.ID == itemID)
                                        {
                                            cityItem.IsSelected = true;
                                            selectedItems.Add(cityItem);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion
        #region expand item

        public void ExpandMacroRegion(MacroRegion macroRegion)
        {
            ExpandMacroRegion(macroRegion.ID);
        }

        public void ExpandMacroRegion(int macroRegionID)
        {
            foreach (TreeViewItemViewModel item in treeViewWorld.Items)
            {
                if (item.UnderlyingLocation.ID == macroRegionID)
                {
                    item.IsExpanded = true;
                    break;
                }
            }
        }

        public void ExpandSubRegion(SubRegion subRegion)
        {
            foreach (TreeViewItemViewModel item in treeViewWorld.Items)
            {
                MacroRegion mr = item.UnderlyingLocation as MacroRegion;
                if (mr.SubRegions.ContainsKey(subRegion.Name))
                {
                    item.IsExpanded = true;
                    foreach (TreeViewItemViewModel subItem in item.Children)
                    {
                        if (subItem.UnderlyingLocation.ID == subRegion.ID)
                        {
                            subItem.IsExpanded = true;
                            break;
                        }
                    }
                    break;
                }
            }
        }

        public void ExpandSubRegion(int subRegionID)
        {
            var q = from row in GeoDatabase.Instance.SubRegionTable
                    where row.ID == subRegionID
                    select row;
            if(q.Any()) ExpandSubRegion(q.First());
        }

        public void ExpandCountry(Country country)
        {
            SubRegion sr = country.SubRegion;
            MacroRegion mr = sr.ParentRegion;

            foreach (TreeViewItemViewModel item in treeViewWorld.Items)
            {
                if (item.UnderlyingLocation.ID == mr.ID)
                {
                    item.IsExpanded = true;
                    foreach (TreeViewItemViewModel subItem in item.Children)
                    {
                        if (subItem.UnderlyingLocation.ID == sr.ID)
                        {
                            subItem.IsExpanded = true;
                            foreach (TreeViewItemViewModel countryItem in subItem.Children)
                            {
                                if (countryItem.UnderlyingLocation.ID == country.ID)
                                {
                                    countryItem.IsExpanded = true;
                                    break;
                                }
                            }
                            break;
                        }
                    }
                    break;
                }
            }
        }

        public void ExpandCountry(int countryID)
        {
            var q = from row in GeoDatabase.Instance.CountryTable
                    where row.ID == countryID
                    select row;
            if (q.Any()) ExpandCountry(q.First());
        }

        public void ExpandCountry(string countryName)
        {
            var q = from row in GeoDatabase.Instance.CountryTable
                    where row.Name == countryName
                    select row;
            if (q.Any()) ExpandCountry(q.First());
        }

        #endregion

        #endregion
        #region util

        /// <summary>
        /// occurs when a tree item is double-clicked
        /// </summary>
        /// <param name="item"></param>
        /// <param name="e"></param>
        protected virtual void OnItemDoubleClicked(ILocation item, MouseButtonEventArgs e)
        {
            if (ItemDoubleClicked != null) ItemDoubleClicked.Invoke(item, e);
        }

        /// <summary>
        /// occurs when a tree item is selected
        /// </summary>
        /// <param name="item"></param>
        /// <param name="e"></param>
        protected virtual void OnItemSelected(ILocation item, RoutedPropertyChangedEventArgs<object> e)
        {
            if (ItemSelected != null) ItemSelected.Invoke(item, e);
        }

        #endregion
    }
}