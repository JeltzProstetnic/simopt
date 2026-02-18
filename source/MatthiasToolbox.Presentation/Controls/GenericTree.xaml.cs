using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MatthiasToolbox.Basics.Datastructures.Trees;
using MatthiasToolbox.Basics.Interfaces;

namespace MatthiasToolbox.Presentation.Controls
{
    /// <summary>
    /// Interaktionslogik für GenericTree.xaml
    /// </summary>
    public partial class GenericTree : UserControl
    {
        #region cvar

        private List<GenericItemViewModel> selectedItems;

        #endregion
        #region dele

        /// <summary>
        /// signature to handle a DoubleClicked event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void GenericTreeItemDoubleClickedHandler(ITreeItem sender, MouseButtonEventArgs e);

        /// <summary>
        /// signature to handle a Selected event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void GenericTreeItemSelectedHandler(ITreeItem sender, RoutedPropertyChangedEventArgs<object> e);

        #endregion
        #region evnt

        /// <summary>
        /// occurs when a user double-clicks an item
        /// </summary>
        public event GenericTreeItemDoubleClickedHandler ItemDoubleClicked;

        /// <summary>
        /// occurs when a user double-clicks an item
        /// </summary>
        public event GenericTreeItemSelectedHandler ItemSelected;

        #endregion
        #region prop

        public List<ITreeItem> SelectedItems
        {
            get
            {
                List<ITreeItem> result = new List<ITreeItem>();
                foreach (GenericItemViewModel tvivm in selectedItems)
                {
                    result.Add(tvivm.UnderlyingItem);
                }
                return result;
            }
        }

        public ITreeItem SelectedItem
        {
            get
            {
                if (selectedItems.Count == 0) return null;
                return selectedItems[0].UnderlyingItem;
            }
        }

        #endregion
        #region ctor

        /// <summary>
        /// default constructor
        /// </summary>
        public GenericTree()
        {
            InitializeComponent();

            selectedItems = new List<GenericItemViewModel>();
        }

        #endregion
        #region init

        public void Intitialize(ITree tree)
        {
            GenericTreeViewModel viewModel = new GenericTreeViewModel(new List<ITreeItem> { tree.Root });
            base.DataContext = viewModel;
            // treeView1.ItemsSource
        }

        #endregion
        #region hand

        private void treeView1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // unselect all if user clicked nirvana
            foreach (GenericItemViewModel item in selectedItems)
            {
                item.IsSelected = false;
            }
            selectedItems.Clear();
        }

        private void treeView1_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (sender == null) return;

            GenericItemViewModel item = (GenericItemViewModel)e.NewValue;
            if (item != null)
            {
                selectedItems.Add(item);
                OnItemSelected(item.UnderlyingItem, e);
            }
        }

        private void treeView1_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TreeView tv = sender as TreeView;
            if (tv == null) return;
            GenericItemViewModel item = tv.SelectedItem as GenericItemViewModel;
            if (item == null) return;

            OnItemDoubleClicked(item.UnderlyingItem, e);
        }

        #endregion
        #region impl

        public void UnselectAll()
        {
            foreach (GenericItemViewModel item in selectedItems)
            {
                item.IsSelected = false;
            }
            selectedItems.Clear();
        }

        #region select item

        public void SelectItem(ITreeItem item)
        {
            SelectItem(item.Identifier);
        }

        public void SelectItem(string identifier)
        {
            selectedItems.Clear();
            foreach (GenericItemViewModel item in treeView1.Items)
            {
                if (item.UnderlyingItem.Identifier == identifier)
                {
                    item.IsSelected = true;
                    selectedItems.Add(item);
                    break;
                }
            }
        }

        #endregion
        #region expand item

        public void ExpandItem(ITreeItem item)
        {
            ExpandItem(item.Identifier);
        }

        public void ExpandItem(string identifier)
        {
            foreach (GenericItemViewModel item in treeView1.Items)
            {
                if (item.UnderlyingItem.Identifier == identifier)
                {
                    item.IsExpanded = true;
                    break;
                }
            }
        }

        #endregion

        #endregion
        #region util

        /// <summary>
        /// occurs when a tree item is double-clicked
        /// </summary>
        /// <param name="item"></param>
        /// <param name="e"></param>
        protected virtual void OnItemDoubleClicked(ITreeItem item, MouseButtonEventArgs e)
        {
            if (ItemDoubleClicked != null) ItemDoubleClicked.Invoke(item, e);
        }

        /// <summary>
        /// occurs when a tree item is selected
        /// </summary>
        /// <param name="item"></param>
        /// <param name="e"></param>
        protected virtual void OnItemSelected(ITreeItem item, RoutedPropertyChangedEventArgs<object> e)
        {
            if (ItemSelected != null) ItemSelected.Invoke(item, e);
        }

        #endregion
    }
}