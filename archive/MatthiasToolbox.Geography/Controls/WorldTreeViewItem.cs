using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.ComponentModel;
using MatthiasToolbox.Geography;
using System.Windows;
using MatthiasToolbox.Logging;

namespace MatthiasToolbox.Geography.Controls
{
    /// <summary>
    /// a hacked tree view item for a hierarchical data template in the world tree control
    /// </summary>
    public class WorldTreeViewItem : ListBox, INotifyPropertyChanged // ListBox, INotifyPropertyChanged
    {
        #region cvar

        private static volatile int instanceCounter = 0;
        private List<WorldTreeViewItem> children;
        internal bool firstTimeExpanded = true;
        private bool isMacroRegion = false;
        private bool isSubRegion = false;
        private bool isCountry = false;
        private bool isCity = false;

        #endregion
        #region prop

        #region main

        /// <summary>
        /// 
        /// </summary>
        public int ID { get; private set; }
        
        /// <summary>
        /// 
        /// </summary>
        public int TreeOrderID { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public ILocation UnderlyingLocation { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<WorldTreeViewItem> Children { get { return children; } }

        #endregion
        #region flag

        /// <summary>
        /// 
        /// </summary>
        public bool IsParent { get; private set; }
        
        /// <summary>
        /// 
        /// </summary>
        public bool IsMacroRegion { get { return isMacroRegion; } }

        /// <summary>
        /// 
        /// </summary>
        public bool IsCountry { get { return isCountry; } }

        /// <summary>
        /// 
        /// </summary>
        public bool IsCity { get { return isCity; } }

        /// <summary>
        /// 
        /// </summary>
        public new bool HasItems { get { return children.Count > 0; } }

        #endregion
        #region propdp

        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty ContentProperty =
            DependencyProperty.Register("Content", typeof(object), typeof(WorldTreeViewItem), new UIPropertyMetadata("empty"));
        
        /// <summary>
        /// 
        /// </summary>
        public object Content
        {
            get { return (object)GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        // public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register("IsSelected", typeof(bool), typeof(PaymentTreeViewItem));
        
        /// <summary>
        /// 
        /// </summary>
        [Bindable(true)]
        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool? IsCapital
        {
            get { return (bool?)GetValue(IsCapitalProperty); }
            set { SetValue(IsCapitalProperty, value); }
        }

        /// <summary>
        /// Using a DependencyProperty as the backing store for IsCapital.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty IsCapitalProperty =
            DependencyProperty.Register("IsCapital", typeof(bool?), typeof(WorldTreeViewItem), new UIPropertyMetadata(false));

        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty IsExpandedProperty =
            DependencyProperty.Register("IsExpanded", typeof(bool?), typeof(WorldTreeViewItem), new UIPropertyMetadata(false));
        
        /// <summary>
        /// 
        /// </summary>
        // [Bindable(true)]
        public bool? IsExpanded
        {
            get { return (bool?)GetValue(IsExpandedProperty); }
            set
            {
                SetValue(IsExpandedProperty, value);
                //NotifyPropertyChanged(IsExpandedProperty.Name);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty HasHeaderProperty = DependencyProperty.Register("HasHeader", typeof(bool), typeof(WorldTreeViewItem));
        
        /// <summary>
        /// 
        /// </summary>
        [Bindable(false), Browsable(false)]
        public bool HasHeader
        {
            get { return (bool)GetValue(HasHeaderProperty); }
        }

        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register("Header", typeof(object), typeof(WorldTreeViewItem));
        
        /// <summary>
        /// 
        /// </summary>
        [Bindable(true)]
        public object Header
        {
            get { return GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty HeaderStringFormatProperty = DependencyProperty.Register("HeaderStringFormat", typeof(string), typeof(WorldTreeViewItem));
        
        /// <summary>
        /// 
        /// </summary>
        [Bindable(true)]
        public string HeaderStringFormat
        {
            get { return (string)GetValue(HeaderStringFormatProperty); }
            set { SetValue(HeaderStringFormatProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty HeaderTemplateProperty = DependencyProperty.Register("HeaderTemplate", typeof(DataTemplate), typeof(WorldTreeViewItem));
        
        /// <summary>
        /// 
        /// </summary>
        [Bindable(true)]
        public DataTemplate HeaderTemplate
        {
            get { return (DataTemplate)GetValue(HeaderTemplateProperty); }
            set { SetValue(HeaderTemplateProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty HeaderTemplateSelectorProperty = DependencyProperty.Register("HeaderTemplateSelector", typeof(DataTemplateSelector), typeof(WorldTreeViewItem));
        
        /// <summary>
        /// 
        /// </summary>
        [Bindable(true)]
        public DataTemplateSelector HeaderTemplateSelector
        {
            get { return (DataTemplateSelector)GetValue(HeaderTemplateSelectorProperty); }
            set { SetValue(HeaderTemplateSelectorProperty, value); }
        }

        #endregion

        #endregion
        #region evnt

        /// <summary>
        /// 
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
        #region ctor

        static WorldTreeViewItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WorldTreeViewItem), new FrameworkPropertyMetadata(typeof(WorldTreeViewItem)));
        }

        private WorldTreeViewItem(int id, ILocation location, string typ)
        {
            children = new List<WorldTreeViewItem>();
            TreeOrderID = instanceCounter;
            ID = instanceCounter;
            Name = "WorldTreeViewItem" + instanceCounter.ToString();
            Header = location.Name;
            Content = location.Name;
            UnderlyingLocation = location;
            instanceCounter += 1;
        }

        internal void LoadChildren()
        {
            firstTimeExpanded = false;
            if (isSubRegion)
            {
                SubRegion sr = (SubRegion)UnderlyingLocation;
                foreach (Country c in sr.Countries.Values)
                    children.Add(new WorldTreeViewItem(c));
            }
            else if (isCountry)
            {
                Country c = (Country)UnderlyingLocation;
                foreach (City ci in c.Cities.Values)
                    children.Add(new WorldTreeViewItem(ci));
                foreach (Location l in c.Locations.Values)
                    children.Add(new WorldTreeViewItem(l));
            }
        }

        internal WorldTreeViewItem(MacroRegion mr)
            : this(mr.ID, mr, "MacroRegion")
        {
            foreach (SubRegion sr in mr.SubRegions.Values)
                children.Add(new WorldTreeViewItem(sr));
            IsParent = true;
            IsExpanded = true;
            isMacroRegion = true;
        }

        internal WorldTreeViewItem(SubRegion sr)
            : this(sr.ID, sr, "SubRegion")
        {
            isSubRegion = true;
        }

        internal WorldTreeViewItem(Country c)
            : this(c.ID, c, "Country")
        {
            IsParent = c.HasCities || c.HasLocations;
            isCountry = true;
        }

        internal WorldTreeViewItem(City c)
            : this(c.ID, c, "City")
        {
            IsCapital = c.Capital;
            IsParent = false;
            isCity = true;
        }

        internal WorldTreeViewItem(Location l)
            : this(l.ID, l, "Location")
        {
            Visibility = System.Windows.Visibility.Hidden;
            IsParent = false;
        }

        #endregion
        #region impl

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
