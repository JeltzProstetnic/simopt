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
using System.Windows.Shapes;
using Vr.WarehouseSimulator.Data.LayoutTables;
using MatthiasToolbox.Logging;
using Vr.WarehouseSimulator.Data.ProcessTables;

namespace Vr.WarehouseSimulator
{
    /// <summary>
    /// Interaction logic for TestWindow.xaml
    /// </summary>
    public partial class TestWindow : Window
    {
        #region cvar

        private List<DataGrid> dataGrids;

        #endregion
        #region ctor

        public TestWindow()
        {
            InitializeComponent();

            //Logger.Add<STATUS>(new WPFStatusLabelLogger(statusLabel));
            this.Log<STATUS>("GUI Initializing...");

            dataGrids = new List<DataGrid>();
        }

        #endregion
        #region main

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            dataGridLayout.ItemsSource = new ListCollectionView(Global.LayoutDatabase.LayoutTable.GetNewBindingList());
            // dataGridGeometry.ItemsSource = new ListCollectionView(Global.LayoutDatabase.GeometryTable.GetNewBindingList()); // HUGE

            dataGridArticle.ItemsSource = new ListCollectionView(Global.OrderDatabase.ArticleTable.GetNewBindingList());
            // dataGridOrder.ItemsSource = new ListCollectionView(Global.OrderDatabase.OrderPositionTable.GetNewBindingList()); // HUGE

            dataGridPNode.ItemsSource = new ListCollectionView(Global.ProcessDatabase.PNodeTable.GetNewBindingList());
            dataGridPData.ItemsSource = new ListCollectionView(Global.ProcessDatabase.PDataTable.GetNewBindingList());
            // ((ListCollectionView)dataGridLayout.ItemsSource).Filter = SomeFilter;

            dataGrids.Add(dataGridLayout);
            dataGrids.Add(dataGridGeometry);
            
            dataGrids.Add(dataGridArticle);
            dataGrids.Add(dataGridOrder);

            dataGrids.Add(dataGridPNode);
            dataGrids.Add(dataGridPData);

            foreach(PNode node in PNode.GetPNodes(11939)) 
            {
                TreeViewItem ti = new TreeViewItem();
                ti.Header = node;
                ti.ToolTip = PData.Get(node.ID);
                processTree1.Items.Add(ti);
                BuildTree(node, ti);
            }

            this.Log<STATUS>("GUI Loaded.");
        }

        #endregion
        #region util

        private void BuildTree(PNode node, TreeViewItem root)
        {
            if (!node.HasChildren()) return;
            foreach (PNode n in node.GetChildren())
            {
                if (!((n.NRCONNECTORSIN == 0 || n.NRCONNECTORSOUT == 0) && n.NAME == "PNode"))
                {
                    TreeViewItem ti = new TreeViewItem();
                    ti.Header = n + " (ID=" + n.ID.ToString() + ")";
                    ti.ToolTip = PData.Get(n.ID);
                    int id = root.Items.Add(ti);
                    BuildTree(n, ti);
                }
            }
        }

        #endregion

        private void noop() { }
    }
}
