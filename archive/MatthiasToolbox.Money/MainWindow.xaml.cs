using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Input;
using System.Windows.Media;
using MatthiasToolbox.Logging;
using MatthiasToolbox.Logging.Loggers;
using MatthiasToolbox.Money.Data;
using MatthiasToolbox.Money.Tools;
using MatthiasToolbox.Presentation;
using MatthiasToolbox.Presentation.Datagrid;
using Microsoft.Win32;

namespace MatthiasToolbox.Money
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region cvar

        private volatile bool eventHandlerLock = false; // TODO: replace occurances with handlerLock mechanism below
        private volatile Dictionary<object, bool> handlerLock;

        // filter stuff
        private bool isSearching = false; // prevents the text "Search..." from being searched :-)
        private bool isFiltering = false;
        private bool isFromDateFiltering = false;
        private bool isToDateFiltering = false;
        private bool isMinAmountFiltering = false;
        private bool isMaxAmountFiltering = false;
        private DateTime fromDate = DateTime.MinValue;
        private DateTime toDate = DateTime.MaxValue;
        private float minAmount = float.MinValue;
        private float maxAmount = float.MaxValue;

        // datagrids
        private Dictionary<DataGrid, DataGridInfo> dataGrids;
        private DataGrid currentlyVisibleDataGrid;
        private TabItem currentlySelectedTabItem;
        private TabItem previousActiveTabItem;
        private List<PaymentTreeViewItem> selectedTreeItems;

        // charts
        private Chart chartOverview;
        private ChartArea chartAreaOverview;

        #endregion
        #region prop

        public Account CurrentAccount 
        {
            get 
            {
                int? accountID = comboBoxAccount.SelectedValue as int?;
                if (accountID == null || accountID < 0) return null;
                return Database.AccountsByID[(int)accountID];
            }
        }

        #endregion
        #region ctor

        public MainWindow()
        {
            InitializeComponent();
            
            DataContext = Database.OpenInstance;
            
            Logger.Add<STATUS>(new WPFStatusLabelLogger(statusLabel));
            this.Log<STATUS>("GUI Initializing...");
            
            dataGrids = new Dictionary<DataGrid, DataGridInfo>();
            selectedTreeItems = new List<PaymentTreeViewItem>();
            handlerLock = new Dictionary<object, bool>();

            chartOverview = new Chart();
            chartAreaOverview = new ChartArea();
            chartOverview.ChartAreas.Add(chartAreaOverview);

            if (!Global.GodMode)
            {
                tabControl1.Items.Remove(tabItemLog);
                tabControl1.Items.Remove(tabItemUsers);
                tabControl1.Items.Remove(tabItemTypes);
                tabControl1.Items.Remove(tabItemGroups);
                tabControl1.Items.Remove(tabItemAccounts);
                tabControl1.Items.Remove(tabItemSettings);
                tabControl1.Items.Remove(tabItemSubGroups);
            }

            InitializeHandlerLocks();

            if (Setting.Exists("WindowWidth")) Width = Setting.Get<double>("WindowWidth");
            if (Setting.Exists("WindowHeight")) Height = Setting.Get<double>("WindowHeight");
            if (Setting.Exists("WindowOriginY")) Top = Setting.Get<double>("WindowOriginY");
            if (Setting.Exists("WindowOriginX")) Left = Setting.Get<double>("WindowOriginX");
        }

        #region helpers

        private void InitializeHandlerLocks() 
        {
            handlerLock[datePickerFrom] = false;
            handlerLock[datePickerTo] = false;
        }

        #endregion

        #endregion
        #region form

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            calendar1.DisplayMode = CalendarMode.Decade;
            calendar2.DisplayMode = CalendarMode.Year;

            comboBoxPerson.ItemsSource = Database.OpenInstance.UserTable;
            comboBoxPerson.DisplayMemberPath = "Name";
            comboBoxPerson.SelectedValuePath = "ID";

            datePickerFrom.IsEnabled = false;
            datePickerTo.IsEnabled = false;
            
            textBoxMinAmount.IsEnabled = false;
            textBoxMaxAmount.IsEnabled = false;

            CreateDataGridInfos();
            foreach (DataGridInfo info in dataGrids.Values) info.Initialize();
            
            windowsFormsHost1.Child = chartOverview;
            
            CreateMenuItems();

            RestoreSettings();

            this.Log<STATUS>("GUI Loaded.");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            tabControl1.Focus();
            this.Log<STATUS>("Saving...");
            foreach(DataGrid dg in dataGrids.Keys) dg.CommitEdit();

            SaveSettings();

            try
            {
                Database.OpenInstance.SubmitChanges();
            }
            catch (Exception ex)
            {
                this.Log(ex);
            }

            this.Log<STATUS>("GUI closing...");
        }

        #region helpers

        private void CreateDataGridInfos()
        {
            dataGrids[dataGridLog] = new DataGridInfo(dataGridLog, "dataGridLog",
                () => Database.OpenInstance.DatabaseLogTable.GetNewBindingList(), "TimeStampTicks");

            dataGrids[dataGridUsers] = new DataGridInfo(dataGridUsers, "dataGridUsers",
                () => Database.OpenInstance.UserTable.GetNewBindingList(), "Name");

            dataGrids[dataGridTypes] = new DataGridInfo(dataGridTypes, "dataGridTypes",
                () => Database.OpenInstance.PaymentTypeTable.GetNewBindingList(), "Name");

            dataGrids[dataGridGroups] = new DataGridInfo(dataGridGroups, "dataGridGroups",
                () => Database.OpenInstance.PaymentGroupTable.GetNewBindingList(), ""); // could sort by Name

            dataGrids[dataGridBookings] = new DataGridInfo(dataGridBookings, "dataGridBookings",
                () => Database.OpenInstance.BookingTable.GetNewBindingList(), "ValutaDateTicks", BookingFilter);

            dataGrids[dataGridAccounts] = new DataGridInfo(dataGridAccounts, "dataGridAccounts",
                () => Database.OpenInstance.AccountTable.GetNewBindingList(), "Name");

            dataGrids[dataGridSettings] = new DataGridInfo(dataGridSettings, "dataGridSettings",
                () => Database.OpenInstance.SettingTable.GetNewBindingList(), "Name");

            dataGrids[dataGridPayments] = new DataGridInfo(dataGridPayments, "dataGridPayments",
                () => Database.OpenInstance.PaymentTable.GetNewBindingList(), "TransactionDateTicks");

            dataGrids[dataGridSubGroups] = new DataGridInfo(dataGridSubGroups, "dataGridSubGroups",
                () => Database.OpenInstance.PaymentSubGroupTable.GetNewBindingList(), ""); // could sort by FullName

            dataGrids[dataGridPeriodical] = new DataGridInfo(dataGridPeriodical, "dataGridPeriodical",
                () => Database.OpenInstance.PeriodicalPaymentTable.GetNewBindingList(), "Amount");
        }

        private void CreateMenuItems()
        {
            foreach (PaymentSubGroup sub in Database.OpenInstance.PaymentSubGroupTable)
            {
                MenuItem mi = new MenuItem();
                mi.Header = sub.FullName;
                mi.Tag = sub;
                mi.Click += MultiSubGroupChange_Click;
                menuItemMultiGroupChange.Items.Add(mi);
            }

            foreach (PaymentType typ in Database.OpenInstance.PaymentTypeTable)
            {
                MenuItem mi = new MenuItem();
                mi.Header = typ.Name;
                mi.Tag = typ;
                mi.Click += MultiTypeChange_Click;
                menuItemMultiTypeChange.Items.Add(mi);
            }
        }

        #endregion

        #endregion
        #region data

        private void anyDataGrid_AutoGeneratedColumns(object sender, EventArgs e)
        {
            // this is called some times even before the grids are visible, 
            // so we return in case sender is null or columns are missing
            if (sender == null) return;
            DataGrid dg = ((DataGrid)sender);
            if (dg.Columns.Count == 0) return;

            // for datagrids which are not in the collection: (currently this should not happen)
            if (!dataGrids.ContainsKey(dg)) 
                return;

            // set default sorting and hide ID column
            dataGrids[dg].SetSorting();
            dataGrids[dg].GetColumnByProperty("ID").Visibility = System.Windows.Visibility.Hidden;
        }

        #region payments

        private void dataGridPayments_InitializingNewItem(object sender, InitializingNewItemEventArgs e)
        {
            Payment p = (Payment)e.NewItem;
            p.TransactionDateTicks = calendar3.SelectedDate.Value.Ticks;
            int subGroupID = GetSelectedSubGroupID();
            if (subGroupID == 0) subGroupID = 1;
            p.SubGroupID = subGroupID;
            p.PayedByUserID = (int)comboBoxPerson.SelectedValue;
            User u = (from row in Database.OpenInstance.UserTable where row.ID == p.PayedByUserID select row).First();
            p.PayedFromAccountID = u.Accounts.First().ID;
            p.TypeID = 1;
        }

        private void dataGridPayments_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (dataGridPayments == null) return;
            DataGridInfo info = dataGrids[dataGridPayments];

            if (e.PropertyName == "GroupID" || e.PropertyName == "SubGroup")
            {
                e.Column.Visibility = System.Windows.Visibility.Hidden;
            }
            else if (e.PropertyName == "PayedByUserID")
            {
                DataGridComboBoxColumn boundColumn = new DataGridComboBoxColumn();
                boundColumn.Header = "Payed By";
                boundColumn.ItemsSource = Database.OpenInstance.UserTable;
                boundColumn.DisplayMemberPath = "Name";
                boundColumn.SelectedValuePath = "ID";
                boundColumn.SelectedValueBinding = new Binding("PayedByUserID");
                e.Column = boundColumn;
            }
            else if (e.PropertyName == "SubGroupID")
            {
                DataGridComboBoxColumn boundColumn = new DataGridComboBoxColumn();
                boundColumn.Header = "Group";
                boundColumn.ItemsSource = Database.OpenInstance.PaymentSubGroupTable;
                boundColumn.DisplayMemberPath = "FullName";
                boundColumn.SortMemberPath = "SubGroupID";
                boundColumn.SelectedValuePath = "ID";
                boundColumn.SelectedValueBinding = new Binding("SubGroupID");
                e.Column = boundColumn;
            }
            else if (e.PropertyName == "TypeID")
            {
                DataGridComboBoxColumn boundColumn = new DataGridComboBoxColumn();
                boundColumn.Header = "Type";
                boundColumn.ItemsSource = Database.OpenInstance.PaymentTypeTable;
                boundColumn.DisplayMemberPath = "Name";
                boundColumn.SelectedValuePath = "ID";
                boundColumn.SelectedValueBinding = new Binding("TypeID");
                e.Column = boundColumn;
            }
            else if (e.PropertyName == "PayedFromAccountID")
            {
                DataGridComboBoxColumn boundColumn = new DataGridComboBoxColumn();
                boundColumn.Header = "Account";
                boundColumn.ItemsSource = Database.OpenInstance.AccountTable;
                boundColumn.DisplayMemberPath = "Name";
                boundColumn.SelectedValuePath = "ID";
                boundColumn.SelectedValueBinding = new Binding("PayedFromAccountID");
                e.Column = boundColumn;
            }
            else if (e.PropertyName.Contains("Ticks"))
            {
                TemplateColumnBuilder.CreateDateCellTemplate(e);
            }
            else if (e.PropertyName == "Amount")
            {
                TemplateColumnBuilder.CreateCurrencyCellTemplate(e, Colors.Black, Colors.Green);
            }

            info.AddColumnInfo(e.Column, e.PropertyName);
        }

        private void dataGridPayments_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender == null) return;
            if (dataGridPayments.SelectedItems.Count > 1)
            {
                try
                {
                    contextMenuPayments.Items.Add(seperatorMultiPaymentOps);
                    contextMenuPayments.Items.Add(menuItemMultiGroupChange);
                    contextMenuPayments.Items.Add(menuItemMultiTypeChange);
                }
                catch { }
                this.Log<STATUS>(dataGridPayments.SelectedItems.Count.ToString() + " payment items selected.");
            }
            else
            {
                try
                {
                    contextMenuPayments.Items.Remove(seperatorMultiPaymentOps);
                    contextMenuPayments.Items.Remove(menuItemMultiGroupChange);
                    contextMenuPayments.Items.Remove(menuItemMultiTypeChange);
                }
                catch { }
            }
        }

        #endregion
        #region periodical

        private void dataGridPeriodical_InitializingNewItem(object sender, InitializingNewItemEventArgs e)
        {
            PeriodicalPayment p = (PeriodicalPayment)e.NewItem;
            p.DueDateTicks = calendar3.SelectedDate.Value.Ticks;
            int subGroupID = GetSelectedSubGroupID();
            if (subGroupID == 0) subGroupID = 1;
            p.SubGroupID = subGroupID;
            p.PayedByUserID = (int)comboBoxPerson.SelectedValue;
            User u = (from row in Database.OpenInstance.UserTable where row.ID == p.PayedByUserID select row).First();
            p.PayedFromAccountID = u.Accounts.First().ID;
            p.TypeID = 1;
        }

        private void dataGridPeriodical_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (dataGridPeriodical == null) return;
            DataGridInfo info = dataGrids[dataGridPeriodical];

            if (e.PropertyName == "PayedByUserID")
            {
                DataGridComboBoxColumn boundColumn = new DataGridComboBoxColumn();
                boundColumn.Header = "Payed By";
                boundColumn.ItemsSource = Database.OpenInstance.UserTable;
                boundColumn.DisplayMemberPath = "Name";
                boundColumn.SelectedValuePath = "ID";
                boundColumn.SelectedValueBinding = new Binding("PayedByUserID");
                e.Column = boundColumn;
            }
            else if (e.PropertyName == "GroupID")
            {
                e.Column.Visibility = System.Windows.Visibility.Hidden;
            }
            else if (e.PropertyName == "SubGroupID")
            {
                DataGridComboBoxColumn boundColumn = new DataGridComboBoxColumn();
                boundColumn.Header = "Group";
                boundColumn.ItemsSource = Database.OpenInstance.PaymentSubGroupTable;
                boundColumn.DisplayMemberPath = "FullName";
                boundColumn.SortMemberPath = "FullName";
                boundColumn.SelectedValuePath = "ID";
                boundColumn.SelectedValueBinding = new Binding("SubGroupID");
                e.Column = boundColumn;
            }
            else if (e.PropertyName == "TypeID")
            {
                DataGridComboBoxColumn boundColumn = new DataGridComboBoxColumn();
                boundColumn.Header = "Type";
                boundColumn.ItemsSource = Database.OpenInstance.PaymentTypeTable;
                boundColumn.DisplayMemberPath = "Name";
                boundColumn.SelectedValuePath = "ID";
                boundColumn.SelectedValueBinding = new Binding("TypeID");
                e.Column = boundColumn;
            }
            else if (e.PropertyName == "PayedFromAccountID")
            {
                DataGridComboBoxColumn boundColumn = new DataGridComboBoxColumn();
                boundColumn.Header = "Account";
                boundColumn.ItemsSource = Database.OpenInstance.AccountTable;
                boundColumn.DisplayMemberPath = "Name";
                boundColumn.SelectedValuePath = "ID";
                boundColumn.SelectedValueBinding = new Binding("PayedFromAccountID");
                e.Column = boundColumn;
            }
            else if (e.PropertyName == "Amount")
            {
                TemplateColumnBuilder.CreateCurrencyCellTemplate(e, Colors.Red, Colors.Black);
            }

            if (e.PropertyName.Contains("Ticks"))
                TemplateColumnBuilder.CreateDateCellTemplate(e);

            info.AddColumnInfo(e.Column, e.PropertyName);
        }

        private void dataGridPeriodical_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender == null) return;
            if (dataGridPeriodical.SelectedItems.Count > 1)
                this.Log<STATUS>(dataGridPeriodical.SelectedItems.Count.ToString() + " periodical payment items selected.");
        }

        #endregion
        #region bookings

        private void dataGridBookings_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (dataGridBookings == null) return;
            DataGridInfo info = dataGrids[dataGridBookings];

            if (e.PropertyName == "AccountID")
            {
                DataGridComboBoxColumn boundColumn = new DataGridComboBoxColumn();
                boundColumn.Header = "Account";
                boundColumn.ItemsSource = Database.OpenInstance.AccountTable;
                boundColumn.DisplayMemberPath = "Name";
                boundColumn.SelectedValuePath = "ID";
                boundColumn.SelectedValueBinding = new Binding("AccountID");
                e.Column = boundColumn;
            }
            else if (e.PropertyName == "Hash")
            {
                if(!Global.GodMode) e.Column.Visibility = System.Windows.Visibility.Hidden;
            }
            else if (e.PropertyName == "Account")
            {
                e.Column.Visibility = System.Windows.Visibility.Hidden;
            }
            else if (e.PropertyName.Contains("Ticks"))
            {
                TemplateColumnBuilder.CreateDateCellTemplate(e);
            }
            else if (e.PropertyName == "Amount")
            {
                TemplateColumnBuilder.CreateCurrencyCellTemplate(e, Colors.Red, Colors.Black);
            }

            info.AddColumnInfo(e.Column, e.PropertyName);
        }

        private void dataGridBookings_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender == null) return;
            if (dataGridBookings.SelectedItems.Count > 1)
                this.Log<STATUS>(dataGridBookings.SelectedItems.Count.ToString() + " transaction items selected.");
        }

        #endregion
        #region accounts

        private void dataGridAccounts_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (dataGridAccounts == null) return;
            DataGridInfo info = dataGrids[dataGridAccounts];

            if (e.PropertyName == "OwnerUserID")
            {
                DataGridComboBoxColumn boundColumn = new DataGridComboBoxColumn();
                boundColumn.Header = "Owner";
                boundColumn.ItemsSource = Database.OpenInstance.UserTable;
                boundColumn.DisplayMemberPath = "Name";
                boundColumn.SelectedValuePath = "ID";
                boundColumn.SelectedValueBinding = new Binding("OwnerUserID");
                e.Column = boundColumn;
            }

            info.AddColumnInfo(e.Column, e.PropertyName);
        }

        #endregion
        #region users

        private void dataGridUsers_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (dataGridUsers == null) return;
            DataGridInfo info = dataGrids[dataGridUsers];

            if (e.PropertyName == "ID" || e.PropertyName == "Accounts")
            {
                e.Column.Visibility = System.Windows.Visibility.Hidden;
            }

            info.AddColumnInfo(e.Column, e.PropertyName);
        }

        #endregion
        #region log

        private void dataGridLog_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (dataGridLog == null) return;
            DataGridInfo info = dataGrids[dataGridLog];

            if (e.PropertyName.Contains("Ticks"))
            {
                //Binding b = new Binding(e.PropertyName);
                //b.Converter = new TicksConverter();
                //((DataGridTextColumn)e.Column).Binding = b;
                e.Column.Header = e.PropertyName.Replace("Ticks", "");
                TemplateColumnBuilder.CreateExactDateTimeCellTemplate(e);
                //((DataGridTextColumn)e.Column).Binding.StringFormat = "{0:dd. MM. yyyy hh:mm:ss:fff}";
            }

            info.AddColumnInfo(e.Column, e.PropertyName);
        }

        #endregion
        #region other

        private void otherDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e) 
        {
            if (sender == null) return;
            DataGridInfo info = dataGrids[(DataGrid)sender];
            
            if (info.Name == "dataGridGroups")
            {
                if (e.PropertyName == "SubGroupsByName") e.Column.Visibility = System.Windows.Visibility.Hidden;
                else if (e.PropertyName == "SubGroupsByID") e.Column.Visibility = System.Windows.Visibility.Hidden;
                else if (e.PropertyName == "SubGroupIDsByName") e.Column.Visibility = System.Windows.Visibility.Hidden;
            }
            else if (info.Name == "dataGridSubGroups")
            {
                if (e.PropertyName == "Parent") e.Column.Visibility = System.Windows.Visibility.Hidden;
                else if (e.PropertyName == "GroupID") e.Column.Visibility = System.Windows.Visibility.Hidden;
            }

            info.AddColumnInfo(e.Column, e.PropertyName);
        }

        #endregion

        #endregion
        #region hand

        #region calendar

        private void calendar1_DisplayModeChanged(object sender, CalendarModeChangedEventArgs e)
        {
            if (eventHandlerLock) return;
            calendar1.DisplayMode = CalendarMode.Decade;

            if (calendar1.DisplayDate != null)
            {
                eventHandlerLock = true;
                DateTime sel = calendar1.DisplayDate;
                calendar1.DisplayDate = sel;
                calendar1.SelectedDate = sel;
                calendar2.DisplayDate = sel;
                calendar2.SelectedDate = sel;
                calendar3.DisplayDate = sel;
                calendar3.SelectedDate = sel;
            }
            eventHandlerLock = false;
        }

        private void calendar2_DisplayModeChanged(object sender, CalendarModeChangedEventArgs e)
        {
            if (eventHandlerLock) return;
            eventHandlerLock = true;
            calendar2.DisplayMode = CalendarMode.Year;

            if (calendar2.SelectedDate != null)
            {
                DateTime sel = calendar2.DisplayDate;
                calendar2.DisplayDate = sel;
                calendar2.SelectedDate = sel;
                calendar3.DisplayDate = sel;
                calendar3.SelectedDate = sel;
            }
            eventHandlerLock = false;
        }

        #endregion
        #region toolbuttons

        // save
        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            SaveChangedData();
        }

        // start calc
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("calc");
        }

        // excel
        private void Button_Click_8(object sender, RoutedEventArgs e)
        {
            // currentlyVisibleDataGrid.OpenInExcel();
        }

        // open
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            LoadBookingsFromHypo();
        }

        // refresh
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            RefreshData();
        }

        // statistics
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            RefreshStats();
        }

        private bool StatisticsFilter(Payment item) 
        {
            if (isFiltering && 
                item.PayedByUserID != (int)comboBoxPerson.SelectedValue &&
                (int)comboBoxPerson.SelectedValue != 2) return false;
            bool found = false;
            foreach (PaymentTreeViewItem ti in selectedTreeItems)
            {
                if (!ti.IsParent && item.SubGroupID == ti.ID) found = true;
            }
            return found;
        }

        // filter
        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            if (isFiltering)
            {
                buttonFilter.Background = new SolidColorBrush(Colors.White);
                isFiltering = false;
                RefreshCurrentTab();
            }
            else
            {
                buttonFilter.Background = new SolidColorBrush(Colors.Orange);
                isFiltering = true;
                RefreshCurrentTab();
            }
        }

        #endregion
        #region comboboxes

        private void comboBoxPerson_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBoxPerson.SelectedIndex < 0) return;
            if (comboBoxPerson.SelectedValue != null && comboBoxPerson.SelectedValue is int)
            {
                if (comboBoxAccount.Items.Count == 0) // first time
                {
                    User u = (from row in Database.OpenInstance.UserTable where row.ID == (int)comboBoxPerson.SelectedValue select row).First();
                    foreach (Account acc in u.Accounts) comboBoxAccount.Items.Add(acc);
                    comboBoxAccount.DisplayMemberPath = "Name";
                    comboBoxAccount.SelectedValuePath = "ID";
                    if (Setting.Exists("LastSelectedAccount")) comboBoxAccount.SelectedIndex = Setting.Get<int>("LastSelectedAccount");
                    else comboBoxAccount.SelectedIndex = 0;
                }
                else // user triggered
                {
                    comboBoxAccount.Items.Clear();
                    User u = (from row in Database.OpenInstance.UserTable where row.ID == (int)comboBoxPerson.SelectedValue select row).First();
                    foreach (Account acc in u.Accounts) comboBoxAccount.Items.Add(acc);
                    comboBoxAccount.SelectedIndex = 0;
                    if(isFiltering) RefreshCurrentTab();
                }
            }
        }

        #endregion
        #region filter

        #region date

        private void cbFromDate_Checked(object sender, RoutedEventArgs e)
        {
            if (sender == null || ((CheckBox)sender).IsChecked == null) return;

            datePickerFrom.IsEnabled = true;

            if (datePickerFrom.SelectedDate == DateTime.MinValue)
            {
                handlerLock[datePickerFrom] = true;
                datePickerFrom.SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1); // TODO: only if valid!
                handlerLock[datePickerFrom] = false;
            }

            isFromDateFiltering = true;
            RefreshCurrentTab();
        }

        private void cbFromDate_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender == null || ((CheckBox)sender).IsChecked == null) return;
            datePickerFrom.IsEnabled = false;
            isFromDateFiltering = false;
            RefreshCurrentTab();
        }

        private void cbToDate_Checked(object sender, RoutedEventArgs e)
        {
            if (sender == null || ((CheckBox)sender).IsChecked == null) return;

            datePickerTo.IsEnabled = true;

            if (datePickerTo.SelectedDate != null && ((DateTime)datePickerTo.SelectedDate).Year == DateTime.MaxValue.Year)
            {
                handlerLock[datePickerTo] = true;
                datePickerTo.SelectedDate = DateTime.Now; // TODO: only if valid!
                handlerLock[datePickerTo] = false;
            }
            
            isToDateFiltering = true;
            RefreshCurrentTab();
        }

        private void cbToDate_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender == null || ((CheckBox)sender).IsChecked == null) return;
            datePickerTo.IsEnabled = false;
            isToDateFiltering = false;
            RefreshCurrentTab();
        }
        
        private void datePickerTo_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (handlerLock[datePickerTo] || sender == null || datePickerTo.SelectedDate == null) return;
            handlerLock[datePickerTo] = true;
            DateTime to = (DateTime)datePickerTo.SelectedDate;
            if (isFromDateFiltering && isToDateFiltering && to.Ticks <= fromDate.Ticks)
            {
                datePickerTo.Background = new SolidColorBrush(Colors.Yellow);
                isToDateFiltering = false;
            }
            else
            {
                datePickerTo.Background = new SolidColorBrush(Colors.White);
                toDate = to;
                isToDateFiltering = (bool)cbToDate.IsChecked;
            }
            RefreshCurrentTab();
            handlerLock[datePickerTo] = false;
        }

        private void datePickerFrom_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (handlerLock[datePickerFrom] || sender == null || datePickerFrom.SelectedDate == null) return;
            handlerLock[datePickerFrom] = true;
            DateTime from = (DateTime)datePickerFrom.SelectedDate;
            if (isFromDateFiltering && isToDateFiltering && from.Ticks >= toDate.Ticks)
            {
                datePickerFrom.Background = new SolidColorBrush(Colors.Yellow);
                isFromDateFiltering = false;
            }
            else
            {
                datePickerFrom.Background = new SolidColorBrush(Colors.White);
                fromDate = from;
                isFromDateFiltering = (bool)cbFromDate.IsChecked;
            }
            RefreshCurrentTab();
            handlerLock[datePickerFrom] = false;
        }

        #endregion
        #region amount

        private void cbMinAmount_Checked(object sender, RoutedEventArgs e)
        {
            if (sender == null || ((CheckBox)sender).IsChecked == null) return;
            textBoxMinAmount.IsEnabled = true;
            isMinAmountFiltering = true;
            RefreshCurrentTab();
        }

        private void cbMinAmount_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender == null || ((CheckBox)sender).IsChecked == null) return;
            textBoxMinAmount.IsEnabled = false;
            isMinAmountFiltering = false;
            RefreshCurrentTab();
        }
        
        private void cbMaxAmount_Checked(object sender, RoutedEventArgs e)
        {
            if (sender == null || ((CheckBox)sender).IsChecked == null) return;
            textBoxMaxAmount.IsEnabled = true;
            isMaxAmountFiltering = true;
            RefreshCurrentTab();
        }

        private void cbMaxAmount_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender == null || ((CheckBox)sender).IsChecked == null) return;
            textBoxMaxAmount.IsEnabled = false;
            isMaxAmountFiltering = false;
            RefreshCurrentTab();
        }

        private void textBoxMinAmount_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (dataGridBookings == null || sender == null) return;

            float dummy;
            bool bothChecked = (bool)cbMinAmount.IsChecked && (bool)cbMaxAmount.IsChecked;
            bool isFloat = float.TryParse(textBoxMinAmount.Text, out dummy);
            bool valid = isFloat;
            if (isFloat && bothChecked) valid = dummy < maxAmount;

            if (valid)
            {
                isMinAmountFiltering = true;
                minAmount = dummy;
                textBoxMinAmount.Background = new SolidColorBrush(Colors.White);
            }
            else
            {
                isMinAmountFiltering = false;
                textBoxMinAmount.Background = new SolidColorBrush(Colors.Yellow);
            }

            RefreshCurrentTab();
        }

        private void textBoxMaxAmount_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (dataGridBookings == null || sender == null) return;

            float dummy;
            bool bothChecked = (bool)cbMinAmount.IsChecked && (bool)cbMaxAmount.IsChecked;
            bool isFloat = float.TryParse(textBoxMaxAmount.Text, out dummy);
            bool valid = isFloat;
            if (isFloat && bothChecked) valid = dummy > minAmount;

            if (valid)
            {
                isMaxAmountFiltering = true;
                maxAmount = dummy;
                textBoxMaxAmount.Background = new SolidColorBrush(Colors.White);
            }
            else
            {
                isMaxAmountFiltering = false;
                textBoxMaxAmount.Background = new SolidColorBrush(Colors.Yellow);
            }

            RefreshCurrentTab();
        }

        #endregion

        #endregion
        #region search

        // search
        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            isSearching = true;
            ((ListCollectionView)currentlyVisibleDataGrid.ItemsSource).Filter = BookingFilter; // TODO: FIXME!!!
            RefreshCurrentTab();
        }

        // clear search
        private void Button_Click_7(object sender, RoutedEventArgs e)
        {
            isSearching = false;
            textBoxSearch.Text = "Search...";
            RefreshCurrentTab();
        }

        private void textBoxSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            // textBoxSearch.SelectAll();
            if (textBoxSearch.Text == "Search...") textBoxSearch.Clear();
        }

        private void textBoxSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (dataGridBookings == null || sender == null) return;
            RefreshCurrentTab();
        }

        #endregion
        #region treeview

        private void treeView1_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (eventHandlerLock) return;
            eventHandlerLock = true;
            if (sender == null) return;

            PaymentTreeViewItem item = (PaymentTreeViewItem)e.NewValue;

            // clear current selection if not shift or control
            if (!IsCtrlPressed && !IsShiftPressed)
            {
                selectedTreeItems.ForEach(f => f.IsSelected = false);
                selectedTreeItems.Clear();
            }

            // select from - to on shift + select
            if (IsShiftPressed) 
            {
                PaymentTreeViewItem lastSelected = selectedTreeItems[selectedTreeItems.Count - 1];
                bool reverseOrder = lastSelected.TreeOrderID > item.TreeOrderID;
                foreach (PaymentTreeViewItem i in Database.OpenInstance.PaymentGroupsAndSubGroups)
                {
                    if (!reverseOrder && (i.TreeOrderID >= lastSelected.TreeOrderID && i.TreeOrderID < item.TreeOrderID && !i.IsSelected) ||
                        reverseOrder && (i.TreeOrderID <= lastSelected.TreeOrderID && i.TreeOrderID > item.TreeOrderID && !i.IsSelected))
                    {
                        i.IsSelected = true;
                        selectedTreeItems.Add(i);
                    }
                }
            }

            // select clicked item
            item.IsSelected = true;
            selectedTreeItems.Add(item);

            // select all sub items of selected parents
            List<PaymentTreeViewItem> tmp = new List<PaymentTreeViewItem>();
            foreach (PaymentTreeViewItem i in selectedTreeItems)
            {
                if (i.IsParent)
                {
                    foreach (PaymentTreeViewItem ii in i.Children)
                    {
                        if (!ii.IsSelected)
                        {
                            ii.IsSelected = true;
                            tmp.Add(ii);
                        }
                    }
                }
            }
            foreach (PaymentTreeViewItem i in tmp) selectedTreeItems.Add(i);

            if (selectedTreeItems.Count > 1) this.Log<STATUS>(selectedTreeItems.Count.ToString() + " tree items selected.");
            if (isFiltering || currentlySelectedTabItem == tabItemStats) RefreshCurrentTab();
            eventHandlerLock = false;
        }

        #endregion

        #endregion
        #region tabs

        private void tabControl1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender == null || tabControl1.SelectedItem == null) return;

            if (previousActiveTabItem == tabControl1.SelectedItem) return;

            previousActiveTabItem = currentlySelectedTabItem;
            currentlySelectedTabItem = (TabItem)tabControl1.SelectedItem;

            if (currentlySelectedTabItem == tabItemAccounts)
            {
                currentlyVisibleDataGrid = dataGridAccounts;
            }
            else if (currentlySelectedTabItem == tabItemPayments)
            {
                currentlyVisibleDataGrid = dataGridPayments;
            }
            else if (currentlySelectedTabItem == tabItemPeriodical)
            {
                currentlyVisibleDataGrid = dataGridPeriodical;
            }
            else if (currentlySelectedTabItem == tabItemPredict)
            {
                currentlyVisibleDataGrid = dataGridNextPayments;
            }
            //else if (activeTabItem == tabItemStats)
            //{
            //    activeDataGrid = dataGridAvgByType;
            //}
            else if (currentlySelectedTabItem == tabItemUsers)
            {
                currentlyVisibleDataGrid = dataGridUsers;
            }
            else if (currentlySelectedTabItem == tabItemSettings)
            {
                currentlyVisibleDataGrid = dataGridSettings;
            }
            else if (currentlySelectedTabItem == tabItemLog)
            {
                currentlyVisibleDataGrid = dataGridLog;
            }
            else if (currentlySelectedTabItem == tabItemTransactions)
            {
                currentlyVisibleDataGrid = dataGridBookings;
            }
            else if (currentlySelectedTabItem == tabItemTypes)
            {
                currentlyVisibleDataGrid = dataGridTypes;
            }

            if (currentlySelectedTabItem == tabItemStats)
            {
                RefreshStats();
            }
            else if (currentlySelectedTabItem == tabItemPredict)
            {
                RefreshPrediction();
            }
            else if (currentlySelectedTabItem == tabItemGroups)
            {
                currentlyVisibleDataGrid = dataGridGroups;
            }
            else if (currentlySelectedTabItem == tabItemSubGroups)
            {
                currentlyVisibleDataGrid = dataGridSubGroups;
            }
            else
            {
                if (currentlyVisibleDataGrid != null && currentlyVisibleDataGrid.ItemsSource != null)
                    ((ListCollectionView)currentlyVisibleDataGrid.ItemsSource).Refresh();
            }
        }

        private bool lock1done = false;
        private void tabItemAccounts_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            if(lock1done) return;
            DataGridRow row0 = (DataGridRow)dataGridAccounts.ItemContainerGenerator.ContainerFromIndex(0);
            if (row0 != null) row0.IsEnabled = false; // could be virtualized
            DataGridRow row1 = (DataGridRow)dataGridAccounts.ItemContainerGenerator.ContainerFromIndex(1);
            if (row1 != null) row1.IsEnabled = false; // could be virtualized
            lock1done = true;
        }

        private bool lock2done = false;
        private void tabItemUsers_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            if (lock2done) return;
            DataGridRow row0 = (DataGridRow)dataGridUsers.ItemContainerGenerator.ContainerFromIndex(0);
            if (row0 != null) row0.IsEnabled = false; // could be virtualized
            DataGridRow row1 = (DataGridRow)dataGridUsers.ItemContainerGenerator.ContainerFromIndex(1);
            if (row1 != null) row1.IsEnabled = false; // could be virtualized
            lock2done = true;
        }

        private bool lock3done = false;
        private void tabItemTypes_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            if (lock3done) return;
            DataGridRow row0 = (DataGridRow)dataGridTypes.ItemContainerGenerator.ContainerFromIndex(0);
            if (row0 != null) row0.IsEnabled = false; // could be virtualized
            lock3done = true;
        }

        private bool lock4done = false;
        private void tabItemGroups_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            if (lock4done) return;
            DataGridRow row0 = (DataGridRow)dataGridUsers.ItemContainerGenerator.ContainerFromIndex(0);
            if (row0 != null) row0.IsEnabled = false; // could be virtualized
            lock4done = true;
        }

        private bool lock5done = false;
        private void tabItemSubGroups_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            if (lock5done) return;
            DataGridRow row0 = (DataGridRow)dataGridTypes.ItemContainerGenerator.ContainerFromIndex(0);
            if (row0 != null) row0.IsEnabled = false; // could be virtualized
            lock5done = true;
        }

        #endregion
        #region menu

        #region main

        // import hypo csv file
        private void MenuItemImportHypo_Click(object sender, RoutedEventArgs e)
        {
            LoadBookingsFromHypo();
        }

        // import mastercard csv file
        private void MenuItemImportMastercard_Click(object sender, RoutedEventArgs e)
        {
            LoadBookingsFromMastercard();
        }

        // save
        private void MenuItemSaveData_Click(object sender, RoutedEventArgs e)
        {
            SaveChangedData();
        }

        // quit
        private void MenuItemQuit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MenuItemImportPayments_Click(object sender, RoutedEventArgs e)
        {
            this.Log<STATUS>("Importing...");

            OpenFileDialog o = new OpenFileDialog();
            o.DefaultExt = ".csv";
            o.Filter = "CSV documents (.csv)|*.csv";
            o.CheckFileExists = true;

            bool? result = o.ShowDialog();

            if (result != null && result == true)
            {
                FileInfo fi = new FileInfo(o.FileName);
                Database.OpenInstance.ImportPaymentCSV(fi);
            }

            dataGridPayments.ItemsSource = new ListCollectionView(Database.OpenInstance.PaymentTable.GetNewBindingList()); // Database.OpenInstance.BookingTable.GetNewBindingList();
            // ((ListCollectionView)dataGridPayments.ItemsSource).Filter = BookingFilter;
            dataGridPayments.Items.Refresh();

            this.Log<STATUS>("Import finished.");
        }

        private void MenuItemExportPayments_Click(object sender, RoutedEventArgs e)
        {
            this.Log<STATUS>("Exporting...");

            SaveFileDialog o = new SaveFileDialog();
            o.DefaultExt = ".csv";
            o.Filter = "CSV documents (.csv)|*.csv";

            bool? result = o.ShowDialog();

            if (result != null && result == true)
            {
                FileInfo fi = new FileInfo(o.FileName);
                Database.OpenInstance.ExportPaymentCSV(fi);
            }

            this.Log<STATUS>("Export finished.");
        }

        #endregion
        #region tool

        // change default user name
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            string newName = Microsoft.VisualBasic.Interaction.InputBox("Enter a new name for the default user:", "Enter new name", "Default User");
            (from row in Database.OpenInstance.UserTable where row.ID == 1 select row).First().Name = newName;
            Database.OpenInstance.SubmitChanges();
            IBindingList l = Database.OpenInstance.UserTable.GetNewBindingList();
            comboBoxPerson.ItemsSource = l;
            comboBoxPerson.SelectedIndex = 0;
            comboBoxPerson.Items.Refresh();
            dataGridUsers.ItemsSource = l;
            dataGridUsers.Items.Refresh();
        }

        // change default account
        private void MenuItem_Click_3(object sender, RoutedEventArgs e)
        {
            string newName = Microsoft.VisualBasic.Interaction.InputBox("Enter a new name for the default account:", "Enter new name", "Default Account");
            (from row in Database.OpenInstance.AccountTable where row.ID == 1 select row).First().Name = newName;
            string newNumber = Microsoft.VisualBasic.Interaction.InputBox("Enter a new account number for the default account:", "Enter new number", "");
            (from row in Database.OpenInstance.AccountTable where row.ID == 1 select row).First().AccountNumber = newNumber;
            Database.OpenInstance.SubmitChanges();
            RefreshData();
        }

        // calc
        private void MenuItem_Click_11(object sender, RoutedEventArgs e)
        {
            Process.Start("calc");
        }

        #endregion
        #region help

        // info screen
        private void MenuItem_Click_22(object sender, RoutedEventArgs e)
        {
            Info info = new Info();
            info.ShowDialog();
        }

        #endregion
        #region options

        // splash checked
        private void MenuItem_Checked(object sender, RoutedEventArgs e)
        {
            if (File.Exists("splash.off")) File.Delete("splash.off");
        }

        // splash unchecked
        private void MenuItem_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!File.Exists("splash.off"))
            {
                FileStream fs = File.Create("splash.off");
                fs.Flush();
                fs.Close();
            }
        }

        #endregion
        #region context menus

        #region bookings

        // account transaction: set whole accounted for
        private void MenuItem_Click_4(object sender, RoutedEventArgs e)
        {
            DataGridRow row = (DataGridRow)dataGridBookings.ItemContainerGenerator.ContainerFromIndex(dataGridBookings.SelectedIndex);
            Booking b = ((Booking)row.Item);
            b.AmountAccountedFor = b.Amount;
            Database.OpenInstance.SubmitChanges();
            RefreshCurrentTab();
        }

        // account transaction: create periodical payment
        private void MenuItem_Click_5(object sender, RoutedEventArgs e)
        {
            if (dataGridBookings.SelectedItems.Count != 1)
            {
                MessageBox.Show("Exactly one row must be selected to create a periodical payment.");
                this.Log<INFO>("User attempted to create a periodical payment from an account entry but " + 
                    dataGridBookings.SelectedItems.Count.ToString() + " rows were selected.");
            }
            else
            {
                DataGridRow row = (DataGridRow)dataGridBookings.ItemContainerGenerator.ContainerFromIndex(dataGridBookings.SelectedIndex);
                Booking b = ((Booking)row.Item);
                int subGroupID = Math.Max(1, GetSelectedSubGroupID());
                int groupID = GetGroupIDfromSub(subGroupID);
                PeriodicalPayment pp = new PeriodicalPayment(true, true, new DateTime(b.ValutaDateTicks), b.Amount, b.Account.OwnerUserID, groupID, subGroupID, 1, b.AccountID, b.TransferText);
                Database.OpenInstance.PeriodicalPaymentTable.InsertOnSubmit(pp);
                Database.OpenInstance.SubmitChanges();
                tabControl1.SelectedItem = tabItemPeriodical;
                RefreshCurrentTab();
            }
        }

        private bool doLink = false;
        private Booking linkBooking;
        private Payment linkPayment;
        // TODO: secure this - account transaction: link to payment
        private void MenuItem_Click_6(object sender, RoutedEventArgs e)
        {
            DataGridRow row = (DataGridRow)dataGridBookings.ItemContainerGenerator.ContainerFromIndex(dataGridBookings.SelectedIndex);
            Booking b = ((Booking)row.Item);
            if (!doLink)
            {
                linkBooking = b;
                this.Log<STATUS>("Link Transaction(" + b.ID.ToString() + ") => Payment(...)");
                doLink = true;
                tabControl1.SelectedItem = tabItemPayments;
            }
            else
            {
                linkPayment.BookingID = b.ID;
                b.AmountAccountedFor = -b.Amount;
                Database.OpenInstance.SubmitChanges();
                RefreshCurrentTab();
                this.Log<STATUS>("Linked Payment(" + linkPayment.ID.ToString() + ") => Transaction(" + b.ID.ToString() + ")");
                doLink = false;
            }
        }

        // transaction: create new payment
        private void MenuItem_Click_12(object sender, RoutedEventArgs e)
        {
            if (dataGridBookings.SelectedItems.Count > 0)
            {
                CreatePaymentFromBookings();
            }
            else
            {
                MessageBox.Show("Please select one or multiple rows first.", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Log<STATUS>("Cannot create payment entry, no data was selected.");
            }
        }

        #region helpers

        private void CreatePaymentFromBookings()
        {
            foreach (Booking b in dataGridBookings.SelectedItems)
            {
                int subGroupID = Math.Max(1, GetSelectedSubGroupID());
                int groupID = GetGroupIDfromSub(subGroupID);
                b.AmountAccountedFor = b.Amount;
                Payment pp = new Payment(-b.Amount, new DateTime(b.ValutaDateTicks), b.Account.OwnerUserID, groupID, subGroupID, 1, b.ID, b.AccountID, false, b.BookingText + " " + b.TransferText);
                Database.OpenInstance.PaymentTable.InsertOnSubmit(pp);
            }
            Database.OpenInstance.SubmitChanges();
            dataGridBookings.Items.Refresh(); // TODO: use correct refresher
            tabControl1.SelectedItem = tabItemPayments;
            RefreshCurrentTab();
        }

        #endregion

        #endregion
        #region payments

        // payment: split
        private void MenuItem_Click_7(object sender, RoutedEventArgs e)
        {
            CultureInfo cult = new CultureInfo("de-DE");
            float amount;
            if (float.TryParse(Microsoft.VisualBasic.Interaction.InputBox("Enter amount to split away:", "Split Payment", ""), NumberStyles.Any, cult, out amount))
            {
                DataGridRow row = (DataGridRow)dataGridPayments.ItemContainerGenerator.ContainerFromIndex(dataGridPayments.SelectedIndex);
                Payment b = ((Payment)row.Item);
                int subGroupID = Math.Max(1, GetSelectedSubGroupID());
                int groupID = GetGroupIDfromSub(subGroupID);
                Payment pp = new Payment(amount, new DateTime(b.TransactionDateTicks), b.PayedByUserID, groupID, subGroupID, 1, b.BookingID, b.PayedFromAccountID);
                b.Amount -= amount;
                Database.OpenInstance.PaymentTable.InsertOnSubmit(pp);
                Database.OpenInstance.SubmitChanges();
                RefreshCurrentTab();
            } else this.Log<INFO>("Cancelled split operation.");
        }

        // payment: create periodical payment from normal payment
        private void MenuItem_Click_8(object sender, RoutedEventArgs e)
        {
            if (dataGridBookings.SelectedItems.Count != 1)
            {
                MessageBox.Show("Exactly one row must be selected to create a periodical payment.");
                this.Log<INFO>("User attempted to create a periodical payment from an account entry but " +
                    dataGridBookings.SelectedItems.Count.ToString() + " rows were selected.");
            }
            else
            {
                DataGridRow row = (DataGridRow)dataGridPayments.ItemContainerGenerator.ContainerFromIndex(dataGridPayments.SelectedIndex);
                Payment b = ((Payment)row.Item);
                int subGroupID = Math.Max(1, GetSelectedSubGroupID());
                int groupID = GetGroupIDfromSub(subGroupID);
                PeriodicalPayment pp = new PeriodicalPayment(true, true, new DateTime(b.TransactionDateTicks), b.Amount, b.PayedByUserID, groupID, subGroupID, 1, b.PayedFromAccountID, b.Comment);
                Database.OpenInstance.PeriodicalPaymentTable.InsertOnSubmit(pp);
                Database.OpenInstance.SubmitChanges();
                tabControl1.SelectedItem = tabItemPeriodical;
                RefreshCurrentTab();
            }
        }

        // TODO: secure this - link to transaction
        private void MenuItem_Click_9(object sender, RoutedEventArgs e)
        {
            DataGridRow row = (DataGridRow)dataGridPayments.ItemContainerGenerator.ContainerFromIndex(dataGridPayments.SelectedIndex);
            Payment b = ((Payment)row.Item);
            if (!doLink)
            {
                linkPayment = b;
                this.Log<STATUS>("Link Payment(" + b.ID.ToString() + ") => Transaction(...)");
                doLink = true;
                tabControl1.SelectedItem = tabItemTransactions;
            }
            else
            {
                b.BookingID = linkBooking.ID;
                linkBooking.AmountAccountedFor = -b.Amount;
                Database.OpenInstance.SubmitChanges();
                RefreshCurrentTab();
                this.Log<STATUS>("Linked Transaction(" + linkBooking.ID.ToString() + ") => Payment(" + b.ID.ToString() + ")");
                doLink = false;
            }
        }

        // TODO: retrieve virtualized items also!
        void MultiSubGroupChange_Click(object sender, RoutedEventArgs e)
        {
            PaymentSubGroup sub = (PaymentSubGroup)((MenuItem)sender).Tag;
            foreach (object o in dataGridPayments.SelectedItems)
            {
                DataGridRow row = (DataGridRow)dataGridPayments.ItemContainerGenerator.ContainerFromItem(o);
                ((Payment)row.Item).GroupID = sub.GroupID;
                ((Payment)row.Item).SubGroupID = sub.ID;
            }
            RefreshCurrentTab();
        }

        void MultiTypeChange_Click(object sender, RoutedEventArgs e)
        {
            PaymentType typ = (PaymentType)((MenuItem)sender).Tag;
            foreach (object o in dataGridPayments.SelectedItems)
            {
                DataGridRow row = (DataGridRow)dataGridPayments.ItemContainerGenerator.ContainerFromItem(o);
                ((Payment)row.Item).TypeID = typ.ID;
            }
            RefreshCurrentTab();
        }

        #endregion

        #endregion

        #endregion
        #region impl

        #region refreshers

        private void RefreshData() 
        {
            this.Log<STATUS>("Refreshing data...");

            comboBoxPerson.ItemsSource = Database.OpenInstance.UserTable.GetNewBindingList();
            comboBoxPerson.Items.Refresh();

            RefreshCurrentTab();
        }

        public void RefreshStats()
        {
            try
            {
                textBoxAccountBalance.Text = (from row in Database.OpenInstance.BookingTable select row.Amount).Sum().ToString(); // TODO: filters!!!
                List<Payment> payments = (from row in Database.OpenInstance.PaymentTable select row).ToList();
                float sum = 0;
                float avg = 0;
                int n = 0;
                foreach (Payment p in payments)
                {
                    if (StatisticsFilter(p))
                    {
                        n += 1;
                        sum += p.Amount;
                    }
                }
                avg = sum / n;
                textBoxSumPayments.Text = sum.ToString();
                textBoxAvgPayments.Text = avg.ToString();
                tabControl1.SelectedItem = tabItemStats;
            }
            catch (Exception ex)
            {
                this.Log<STATUS>("No statistics available.");
                this.Log<ERROR>("Error calculating statistics - ", ex);
            }

            chartOverview.Series.Clear();

            foreach (PaymentGroup pg in Database.OpenInstance.PaymentGroupTable)
            {
                Series series = new Series();
                // series.Label = pg.Name;
                series.ToolTip = pg.Name;
                series.LegendText = pg.Name;
                series.ChartType = SeriesChartType.Line;
                // Legend l = new Legend();
                
                // chartOverview.Legends.Add(l);

                for (int i = 1; i <= 12; i += 1)
                {
                    long minTicks = (new DateTime(DateTime.Now.Year, i, 1)).Ticks;
                    long maxTicks = (new DateTime(DateTime.Now.Year, i, DateTime.DaysInMonth(DateTime.Now.Year, i))).Ticks;

                    var q = from row in Database.OpenInstance.PaymentTable
                            where row.GroupID == pg.ID && row.TransactionDateTicks >= minTicks && row.TransactionDateTicks <= maxTicks
                            select row.Amount;
                    
                    if (q.Any())
                    {
                        float n = q.Sum();
                        series.Points.Add(n);
                    }
                }
                chartOverview.Series.Add(series);
            }
        }

        public void RefreshPrediction()
        {
            // TODO: prediction
        }

        public void RefreshCurrentTab()
        {
            this.Log<STATUS>("Refreshing current tab page...");

            if (currentlySelectedTabItem == tabItemStats)
            {
                RefreshStats();
            }
            else if (currentlySelectedTabItem == tabItemPredict)
            {
                RefreshPrediction();
            }
            else
            {
                dataGrids[currentlyVisibleDataGrid].RefreshData();
            }

            this.Log<STATUS>("Up to date.");
        }

        #endregion
        #region persistence

        private void LoadBookingsFromMastercard()
        {
            int? accountID = comboBoxAccount.SelectedValue as int?;
            if (accountID == null || accountID == 2 || accountID < 0)
            {
                this.Log<STATUS>("Attempted to import account data without selecting a valid account.");
                MessageBox.Show("You have to select an account first. Cannot import into \"AllAccounts\".", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            this.Log<STATUS>("Importing...");

            OpenFileDialog o = new OpenFileDialog();
            o.DefaultExt = ".csv";
            o.Filter = "CSV documents (.csv)|*.csv";
            o.CheckFileExists = true;

            bool? result = o.ShowDialog();

            if (result == true)
            {
                FileInfo fi = new FileInfo(o.FileName);
                List<Booking> b = Database.OpenInstance.ImportMastercardCSV(fi, (int)accountID);
            }

            dataGridBookings.ItemsSource = new ListCollectionView(Database.OpenInstance.BookingTable.GetNewBindingList());
            ((ListCollectionView)dataGridBookings.ItemsSource).Filter = BookingFilter;
            dataGridBookings.Items.Refresh();

            this.Log<STATUS>("Import finished.");
        }

        private void LoadBookingsFromHypo()
        {
            this.Log<STATUS>("Importing...");

            OpenFileDialog o = new OpenFileDialog();
            o.DefaultExt = ".csv";
            o.Filter = "CSV documents (.csv)|*.csv";
            o.CheckFileExists = true;

            bool? result = o.ShowDialog();

            if (result != null && result == true)
            {
                FileInfo fi = new FileInfo(o.FileName);
                List<Booking> b = Database.OpenInstance.ImportHypoCSV(fi);
            }

            dataGridBookings.ItemsSource = new ListCollectionView(Database.OpenInstance.BookingTable.GetNewBindingList());
            ((ListCollectionView)dataGridBookings.ItemsSource).Filter = BookingFilter;
            dataGridBookings.Items.Refresh();

            this.Log<STATUS>("Import finished.");
        }

        private void SaveChangedData()
        {
            tabControl1.Focus();
            foreach (DataGrid dg in dataGrids.Keys) dg.CommitEdit();
            try
            {
                Database.OpenInstance.SubmitChanges();
            }
            catch (Exception ex)
            {
                MessageBox.Show("There was a problem saving data, please see logfile for further info.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Log(ex);
            }

            this.Log<STATUS>("Data saved.");
            RefreshData();
        }

        private void RestoreSettings()
        {
            if (Setting.Exists("LastSelectedPerson"))
                comboBoxPerson.SelectedIndex = Setting.Get<int>("LastSelectedPerson");
            else comboBoxPerson.SelectedIndex = 0;

            if (Setting.Exists("FromDateFiltering"))
            {
                bool b = Setting.Get<bool>("FromDateFiltering");
                cbFromDate.IsChecked = b;
                datePickerFrom.IsEnabled = b;
                datePickerFrom.SelectedDate = Setting.Get<DateTime>("FromDate", DateTime.MinValue, new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1));
            }

            if (Setting.Exists("ToDateFiltering"))
            {
                bool b = Setting.Get<bool>("ToDateFiltering");
                cbToDate.IsChecked = b;
                datePickerTo.IsEnabled = b;
                datePickerTo.SelectedDate = Setting.Get<DateTime>("ToDate", DateTime.MaxValue, DateTime.Now);
            }

            if (Setting.Exists("MinAmountFiltering"))
            {
                bool b = Setting.Get<bool>("MinAmountFiltering");
                cbMinAmount.IsChecked = b;
                textBoxMinAmount.IsEnabled = b;
                textBoxMinAmount.Text = Setting.Get<string>("MinAmount");
            }

            if (Setting.Exists("MaxAmountFiltering"))
            {
                bool b = Setting.Get<bool>("MaxAmountFiltering");
                cbMaxAmount.IsChecked = b;
                textBoxMaxAmount.IsEnabled = b;
                textBoxMaxAmount.Text = Setting.Get<string>("MaxAmount");
            }

            if (Setting.Exists("LastSelectedTab"))
                tabControl1.SelectedIndex = Setting.Get<int>("LastSelectedTab");

            if (Setting.Exists("ShowSplashScreen"))
                menuItemSplash.IsChecked = Setting.Get<bool>("ShowSplashScreen");
        }

        private void SaveSettings()
        {
            Setting.Set("LastSelectedTab", tabControl1.SelectedIndex);
            Setting.Set("LastSelectedPerson", comboBoxPerson.SelectedIndex);
            Setting.Set("LastSelectedAccount", comboBoxAccount.SelectedIndex);

            if (cbFromDate.IsChecked != null && datePickerFrom.SelectedDate != null)
            {
                Setting.Set("FromDateFiltering", (bool)cbFromDate.IsChecked);
                Setting.Set("FromDate", (DateTime)datePickerFrom.SelectedDate);
            }
            else
            {
                Setting.Set("FromDateFiltering", false);
                Setting.Set("FromDate", DateTime.MinValue);
            }

            if (cbToDate.IsChecked != null && datePickerTo.SelectedDate != null)
            {
                Setting.Set("ToDateFiltering", (bool)cbToDate.IsChecked);
                Setting.Set("ToDate", (DateTime)datePickerTo.SelectedDate);
            }
            else
            {
                Setting.Set("ToDateFiltering", false);
                Setting.Set("ToDate", DateTime.MaxValue);
            }

            if (cbMinAmount.IsChecked != null && !string.IsNullOrEmpty(textBoxMinAmount.Text))
            {
                Setting.Set("MinAmountFiltering", (bool)cbMinAmount.IsChecked);
                Setting.Set("MinAmount", textBoxMinAmount.Text);
            }
            else
            {
                Setting.Set("MinAmountFiltering", false);
                Setting.Set("MinAmount", "");
            }

            if (cbMaxAmount.IsChecked != null && !string.IsNullOrEmpty(textBoxMaxAmount.Text))
            {
                Setting.Set("MaxAmountFiltering", (bool)cbMaxAmount.IsChecked);
                Setting.Set("MaxAmount", textBoxMaxAmount.Text);
            }
            else
            {
                Setting.Set("MaxAmountFiltering", false);
                Setting.Set("MaxAmount", "");
            }

            Setting.Set("WindowWidth", Width);
            Setting.Set("WindowHeight", Height);
            Setting.Set("WindowOriginY", Top);
            Setting.Set("WindowOriginX", Left);

            Setting.Set("ShowSplashScreen", menuItemSplash.IsChecked);
        }

        #endregion
        #region searching & filtering

        private bool BookingSearch(Booking item)
        {
            string search = textBoxSearch.Text;
            if (!string.IsNullOrEmpty(search))
            {
                if (item.TransferText.ToLower().Contains(search.ToLower())) return true;
                else return false;
            }
            return true;
        }

        private bool BookingFilter(object sender)
        {
            if (sender == null) return true;
            Booking booking = (Booking)sender;
            if (isFromDateFiltering &&
                booking.ValutaDateTicks < fromDate.Ticks) return false;
            if (isToDateFiltering &&
                booking.ValutaDateTicks > toDate.Ticks) return false;
            if (isMinAmountFiltering &&
                booking.Amount < minAmount) return false;
            if (isMaxAmountFiltering &&
                booking.Amount > maxAmount) return false;
            if (isFiltering &&
                (int)comboBoxAccount.SelectedValue != 2 &&
                booking.AccountID != (int)comboBoxAccount.SelectedValue) return false;
            
            // TODO: filter group(s) (only for payment and periodical)

            if (isSearching) return BookingSearch(booking);
            return true;
        }

        #endregion

        #endregion
        #region tool

        private int GetSelectedSubGroupID() 
        {
            if (treeView1.SelectedItem != null)
            {
                PaymentTreeViewItem ti = ((PaymentTreeViewItem)treeView1.SelectedItem);
                if (ti.IsParent) return 0;
                return ti.ID;
            } else return 0;
        }

        private int GetGroupIDfromSub(int subGroupID) 
        {
            return (from row in Database.OpenInstance.PaymentSubGroupTable where row.ID == subGroupID select row).First().Parent.ID;
        }

        private DataGridRow GetSelectedRow(DataGrid grid)
        {
            return (DataGridRow)grid.ItemContainerGenerator.ContainerFromIndex(grid.SelectedIndex);
        }

        private bool IsCtrlPressed
        {
            get
            {
                return Keyboard.IsKeyDown(Key.LeftCtrl)
                    || Keyboard.IsKeyDown(Key.RightCtrl);
            }
        }

        private bool IsShiftPressed
        {
            get
            {
                return Keyboard.IsKeyDown(Key.LeftShift)
                    || Keyboard.IsKeyDown(Key.RightShift);
            }
        }

        #endregion
        #region todo

        // export account transactions to csv
        private void MenuItem_Click_13(object sender, RoutedEventArgs e)
        {
            
        }

        // print
        private void MenuItem_Click_14(object sender, RoutedEventArgs e)
        {

        }

        // add user
        private void MenuItem_Click_15(object sender, RoutedEventArgs e)
        {

        }

        // delete user
        private void MenuItem_Click_16(object sender, RoutedEventArgs e)
        {

        }

        // add account
        private void MenuItem_Click_17(object sender, RoutedEventArgs e)
        {

        }

        // delete account
        private void MenuItem_Click_18(object sender, RoutedEventArgs e)
        {

        }

        // backup
        private void MenuItem_Click_19(object sender, RoutedEventArgs e)
        {

        }

        // startup pwd
        private void MenuItem_Click_20(object sender, RoutedEventArgs e)
        {

        }

        // usb key
        private void MenuItem_Click_21(object sender, RoutedEventArgs e)
        {

        }

        #endregion
    }
}

// TODO: multiple payments can be linked to a transaction -> sum up amounts?
// TODO: somehow identify periodical payments in account transactions? by sum and or date and or text
// TODO: somehow auto indentify / suggest periodical payments
// TODO: persist last column sortings and column widths and arrangement
// TODO: arrange columns