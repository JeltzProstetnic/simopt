using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using MatthiasToolbox.Writer.Interfaces;

namespace MatthiasToolbox.Writer.ViewModel
{
    public class DocumentTreeViewModel : INotifyPropertyChanged
    {
        #region cvar

        private static readonly DocumentTreeViewModel dummyItem = new DocumentTreeViewModel();

        private readonly ObservableCollection<DocumentTreeViewModel> children;
        private readonly DocumentTreeViewModel parent;

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
        public DocumentTreeViewModel Parent
        {
            get { return parent; }
        }

        /// <summary>
        /// Returns the logical child items of this object.
        /// </summary>
        public ObservableCollection<DocumentTreeViewModel> Children
        {
            get { return children; }
        }

        /// <summary>
        /// Returns true if this object's Children have been populated yet.
        /// </summary>
        public bool ChildrenLoaded
        {
            get { return childrenLoaded; }
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

        public virtual string Name { get { return UnderlyingItem.Name; } }

        public virtual IDocumentTreeItem UnderlyingItem { get; set; }
        #endregion

        #endregion
        #region ctor

        /// <summary>
        /// This is used to create the dummyItem instance.
        /// </summary>
        private DocumentTreeViewModel() { }

        /// <summary>
        /// main constructor
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="lazyLoad"></param>
        protected DocumentTreeViewModel(DocumentTreeViewModel parent, IDocumentTreeItem underlyingItem, bool lazyLoad = true)
        {
            this.parent = parent;
            this.children = new ObservableCollection<DocumentTreeViewModel>();
            this.UnderlyingItem = underlyingItem;
            if (lazyLoad) this.children.Add(dummyItem);
        }

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