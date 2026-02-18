using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;
using System.Data.Linq;
using MatthiasToolbox.Logging;

namespace MatthiasToolbox.Presentation.Datagrid
{
    /// <summary>
    /// This class holds some additional information 
    /// about a DataGrid and its columns
    /// </summary>
    public class DataGridInfo
    {
        #region cvar

        private string defaultSortProperty = "";
        private ListSortDirection defaultSortDirection = ListSortDirection.Ascending;
        private DataGrid grid = null;
        private string name = "";
        private Predicate<object> filter = null;
        private Func<IBindingList> getBindingList;

        private Dictionary<DataGridColumn, DataGridColumnInfo> columnInfosByColumn;
        private Dictionary<string, DataGridColumnInfo> columnInfosByProperty;

        #endregion
        #region prop

        /// <summary>
        /// the DataGrid on which this is based
        /// </summary>
        public DataGrid Grid { get { return grid; } }
        
        /// <summary>
        /// a name for this object
        /// </summary>
        public string Name { get { return name; } }

        #endregion
        #region ctor

        /// <summary>
        /// this constructor will set the grid's filter
        /// the item source will be set on Initialize()
        /// (using the given getBindingList function)
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="getBindingList"></param>
        /// <param name="defaultSortProperty"></param>
        /// <param name="filter"></param>
        /// <param name="defaultSortDirection"></param>
        public DataGridInfo(DataGrid grid,
            string name,
            Func<IBindingList> getBindingList,
            string defaultSortProperty = "",
            Predicate<object> filter = null,
            ListSortDirection defaultSortDirection = ListSortDirection.Ascending)
        {
            this.grid = grid;
            this.name = name;
            this.defaultSortProperty = defaultSortProperty;
            this.defaultSortDirection = defaultSortDirection;
            this.filter = filter;
            this.getBindingList = getBindingList;

            columnInfosByColumn = new Dictionary<DataGridColumn, DataGridColumnInfo>();
            columnInfosByProperty = new Dictionary<string, DataGridColumnInfo>();
        }

        #endregion
        #region init

        /// <summary>
        /// Initialize the datagrid. This will retrieve
        /// the IBindingList and wrap it in a ListCollectionView
        /// to use it as ItemsSource for the DataGrid
        /// </summary>
        public void Initialize()
        {
            grid.ItemsSource = new ListCollectionView(getBindingList.Invoke());
            ((ListCollectionView)grid.ItemsSource).Filter = filter;
        }

        #endregion
        #region impl

        /// <summary>
        /// retrieve a column by the name of the bound property
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public DataGridColumn GetColumnByProperty(string propertyName)
        {
            return columnInfosByProperty[propertyName].Column;
        }

        /// <summary>
        /// Store information about a column belonging to the grid.
        /// </summary>
        /// <param name="column"></param>
        /// <param name="propertyName"></param>
        public void AddColumnInfo(DataGridColumn column, string propertyName)
        {
            DataGridColumnInfo info = new DataGridColumnInfo(column, propertyName);
            columnInfosByColumn[column] = info;
            columnInfosByProperty[propertyName] = info;
        }

        /// <summary>
        /// this will invoke the getBindingList function to create a new 
        /// ListCollectionView as the grid's ItemsSource, and set the
        /// filter if a filter was defined.
        /// </summary>
        public void RefreshData()
        {
            // makes no difference:
            //SortDescriptionCollection sorting = new SortDescriptionCollection();
            //foreach (SortDescription desc in CollectionViewSource.GetDefaultView(Grid.ItemsSource).SortDescriptions)
            //    sorting.Add(desc);
            SortDescriptionCollection sorting = CollectionViewSource.GetDefaultView(Grid.ItemsSource).SortDescriptions;
            Grid.ItemsSource = new ListCollectionView(getBindingList.Invoke());
            ICollectionView view = CollectionViewSource.GetDefaultView(Grid.ItemsSource);
            view.Filter = filter;
            // TODO: restore prior sorting; the following has no effect:
            foreach (SortDescription desc in sorting)
            {
                view.SortDescriptions.Add(desc);
            }
            view.Refresh();
        }

        /// <summary>
        /// Set the grid's sorting to its default sorting.
        /// Caution: sorting can only be set after the column has been created and added to the grid.
        /// If no defaultSortProperty is set, this function will do nothing.
        /// </summary>
        public void SetSorting()
        {
            if (defaultSortProperty != "" && columnInfosByProperty.ContainsKey(defaultSortProperty))
            {
                DataGridColumnInfo info = columnInfosByProperty[defaultSortProperty];
                info.Column.SortDirection = defaultSortDirection;
                ICollectionView view = CollectionViewSource.GetDefaultView(Grid.ItemsSource);
                view.SortDescriptions.Clear();
                view.SortDescriptions.Add(new SortDescription(defaultSortProperty, defaultSortDirection));
                view.Refresh();
            }
            else if (defaultSortProperty != "" && !columnInfosByProperty.ContainsKey(defaultSortProperty))
            {
                this.Log<ERROR>("No column info found for the property \"" + defaultSortProperty + "\" in the DataGrid \"" + name + "\".");
            }
        }

        /// <summary>
        /// set a filter to this datagrid
        /// </summary>
        /// <param name="newFilter"></param>
        public void SetFilter(Predicate<object> newFilter)
        {
            ((ListCollectionView)grid.ItemsSource).Filter = newFilter;
        }

        #endregion

        public static Predicate<T> CombinedFilter<T>(params Predicate<T>[] filters)
        {
            return o => AllTrue<T>(o, filters);
        }

        private static bool AllTrue<T>(T o, params Predicate<T>[] filters)
        {
            foreach (Predicate<T> p in filters)
            {
                if (!(p.Invoke(o))) return false;
            }
            return true;
        }
    }
}
