using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace MatthiasToolbox.Geography.Controls
{
    /// <summary>
    /// Base class for all ViewModel classes displayed by TreeViewItems.  
    /// This acts as an adapter between a raw data object and a TreeViewItem.
    /// </summary>
    public class TreeViewItemViewModel : INotifyPropertyChanged, IWorldTreeItem
    {
        #region cvar

        private static readonly TreeViewItemViewModel dummyItem = new TreeViewItemViewModel();

        private readonly ObservableCollection<TreeViewItemViewModel> children;
        private readonly TreeViewItemViewModel parent;

        private bool isExpanded;
        private bool isSelected;
        private bool isSelectionActive;

        private bool childrenLoaded;

        #endregion
        #region evnt

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #endregion
        #region prop

        #region TreeViewItem

        /// <summary>
        /// Returns the parent element or null if this is a root item.
        /// </summary>
        public TreeViewItemViewModel Parent
        {
            get { return parent; }
        }

        /// <summary>
        /// Returns the logical child items of this object.
        /// </summary>
        public ObservableCollection<TreeViewItemViewModel> Children
        {
            get { return children; }
        }

        /// <summary>
        /// Returns true if this object's Children have been populated yet.
        /// </summary>
        public bool ChildrenLoaded
        {
            get { return childrenLoaded; } // this.Children.Count == 1 && this.Children[0] == dummyItem
        }

        /// <summary>
        /// Gets / sets whether the TreeViewItem 
        /// associated with this object is expanded.
        /// </summary>
        public bool IsExpanded
        {
            get { return isExpanded; }
            set
            {
                if (value != isExpanded)
                {
                    isExpanded = value;
                    this.OnPropertyChanged("IsExpanded");
                }

                if (isExpanded && parent != null) parent.IsExpanded = true;

                if (!this.ChildrenLoaded)
                {
                    this.Children.Remove(dummyItem);
                    this.LoadChildren();
                }
            }
        }

        /// <summary>
        /// Gets / sets whether the TreeViewItem 
        /// associated with this object is selected.
        /// </summary>
        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                if (value != isSelected)
                {
                    isSelected = value;
                    this.OnPropertyChanged("IsSelected");
                }
            }
        }

        /// <summary>
        /// Gets / sets whether the selection of this item is active.
        /// </summary>
        public bool IsSelectionActive
        {
            get { return isSelectionActive; }
            set
            {
                if (value != isSelectionActive)
                {
                    isSelectionActive = value;
                    this.OnPropertyChanged("IsSelectionActive");
                }
            }
        }

        #endregion
        #region IWorldTreeItem

        public virtual string Name { get { return UnderlyingLocation.Name; } }

        public ILocation UnderlyingLocation { get; private set; }

        public bool IsLocation { get { return UnderlyingLocation is Location; } }
        
        public bool IsCity { get { return UnderlyingLocation is City; } }

        public bool IsCountry { get { return UnderlyingLocation is Country; } }

        public bool IsSubRegion { get { return UnderlyingLocation is SubRegion; } }

        public bool IsMacroRegion { get { return UnderlyingLocation is MacroRegion; } }

        #endregion

        #endregion
        #region ctor

        /// <summary>
        /// main constructor
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="lazyLoad"></param>
        protected TreeViewItemViewModel(TreeViewItemViewModel parent, ILocation underlyingLocation, bool lazyLoad = true)
        {
            this.parent = parent;
            this.children = new ObservableCollection<TreeViewItemViewModel>();
            this.UnderlyingLocation = underlyingLocation;
            if (lazyLoad) this.children.Add(dummyItem);
        }

        /// <summary>
        /// This is used to create the dummyItem instance.
        /// </summary>
        private TreeViewItemViewModel() { }

        #endregion
        #region impl

        /// <summary>
        /// Invoked when the child items need to be loaded on demand.
        /// Subclasses can override this to populate the Children collection.
        /// </summary>
        protected virtual void LoadChildren()
        {
            childrenLoaded = true;
        }

        #region INotifyPropertyChanged

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #endregion
    }
}