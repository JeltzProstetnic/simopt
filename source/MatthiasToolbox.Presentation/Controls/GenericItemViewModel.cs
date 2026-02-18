using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Basics.Datastructures.Trees;
using System.ComponentModel;
using System.Collections.ObjectModel;
using MatthiasToolbox.Basics.Interfaces;

namespace MatthiasToolbox.Presentation.Controls
{
    public class GenericItemViewModel : INotifyPropertyChanged
    {
        //public GenericItemViewModel(ITreeItem item)
        //{
        //    UnderlyingItem = item;
        //}

        #region cvar

        private static readonly GenericItemViewModel dummyItem = new GenericItemViewModel();

        private readonly ObservableCollection<GenericItemViewModel> children;
        private readonly GenericItemViewModel parent;

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
        public GenericItemViewModel Parent
        {
            get { return parent; }
        }

        /// <summary>
        /// Returns the logical child items of this object.
        /// </summary>
        public ObservableCollection<GenericItemViewModel> Children
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

        public virtual string Name 
        { 
            get 
            { 
                if(UnderlyingItem is INamedElement) 
                    return (UnderlyingItem as INamedElement).Name; 
                else
                    return "Unnamed Item";
            }
        }

        public ITreeItem UnderlyingItem { get; private set; }

        #endregion

        #endregion
        #region ctor

        /// <summary>
        /// main constructor
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="lazyLoad"></param>
        /// <param name="underlyingItem"></param>
        public GenericItemViewModel(GenericItemViewModel parent, ITreeItem underlyingItem, bool lazyLoad = true)
        {
            this.parent = parent;
            this.children = new ObservableCollection<GenericItemViewModel>();
            this.UnderlyingItem = underlyingItem;
            if (lazyLoad) this.children.Add(dummyItem);
        }

        /// <summary>
        /// This is used to create the dummyItem instance.
        /// </summary>
        private GenericItemViewModel() { }

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
