using System;
using System.Diagnostics;
using System.Collections.Generic;

using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Forms.DataVisualization.Charting;

using MatthiasToolbox.Logging;
using MatthiasToolbox.Logging.Loggers;

using MatthiasToolbox.Geography;

using MatthiasToolbox.Presentation;
using MatthiasToolbox.Presentation.Datagrid;

using MatthiasToolbox.SupplyChain.Database;
using MatthiasToolbox.SupplyChain.Simulator;
using MatthiasToolbox.SupplyChain.Database.ModelTables;
using MatthiasToolbox.SupplyChain.Optimizer;

using MatthiasToolbox.Simulation.Tools;
using MatthiasToolbox.Optimization.Strategies;

namespace MatthiasToolbox.SupplyChain
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region cvar

        private Dictionary<DataGrid, DataGridInfo> dataGrids;
        private Random rnd = new Random();

        #region Switches

        private int currentMapMode = 0;

        private bool simMode = false;
        private bool optMode = false;

        private bool splineMode = false;
        private bool threeDmode = true;

        #endregion
        #region Climate

        private Chart chartCityClimate1;
        private ChartArea chartAreaCityClimate1;
        private Chart chartCityClimate2;
        private ChartArea chartAreaCityClimate2;
        private Chart chartCityClimate3;
        private ChartArea chartAreaCityClimate3;

        #endregion
        #region Simulation

        SupplyChain.Simulator.Simulation sim;

        #region Charts

        Chart recruitingChart3D;
        ChartArea recruitingChart3DArea1;

        Chart recruitingChart2D;
        ChartArea recruitingChart2DArea1;

        private System.Drawing.Point mouseDownPoint;
        private int origRotation;
        private int origInclination;

        private List<System.Drawing.Color> chartSeriesColors
            = new List<System.Drawing.Color> {
                System.Drawing.Color.ForestGreen,
                System.Drawing.Color.DeepSkyBlue,
                System.Drawing.Color.Aquamarine,
                System.Drawing.Color.Gold,
                System.Drawing.Color.Moccasin,
                System.Drawing.Color.LightSalmon,
                System.Drawing.Color.Firebrick,
                System.Drawing.Color.Maroon
            };

        #endregion

        #endregion
        #region Optimization

        Optimisation opt;
        Series optSeries;

        #region Charts

        Chart optChart3D;
        ChartArea optChart3DArea1;

        Chart optChart2D;
        ChartArea optChart2DArea1;

        //private System.Drawing.Point mouseDownPoint;
        //private int origRotation;
        //private int origInclination;

        #endregion

        #endregion

        #endregion
        #region prop

        #endregion
        #region ctor

        /// <summary>
        /// default constructor
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            Logger.Add<STATUS>(new WPFStatusTextBlockLogger(textBlockStatus1));

            dataGrids = new Dictionary<DataGrid, DataGridInfo>();

            if (!(Global.GodMode || Global.TestMode))
            {
                tabItemLog.Visibility = Visibility.Hidden;
                tabItemSettings.Visibility = Visibility.Hidden;
            }

            InitializeStrings();

            InitializeDataGrids();

            RestoreMainWindowSettings();

            InitGMap();

            InitSim();
        }

        #endregion
        #region hand

        #region Window

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            PopulateDataGrids();
            imageBox1.LoadFromURI("pack://application:,,,/MatthiasToolbox.SupplyChain;component/Resources/testImage.jpg");

            SetupCharts();

            RestoreLastSessionSettings();

            this.Log<STATUS>("Ready.");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Log<STATUS>("Shutting down...");

            foreach (DataGrid dg in dataGrids.Keys) dg.CommitEdit();

            SaveSettings();

            try
            {
                Global.UserDatabase.SubmitChanges();
                Global.ModelDatabase.SubmitChanges();
            }
            catch (Exception ex)
            {
                this.Log(ex);
            }
        }

        #endregion
        #region WorldTree

        private void worldTree1_ItemSelected(ILocation item, RoutedPropertyChangedEventArgs<object> e)
        {
            bool updateChart = false;
            City climateCity = null;
            if (item is Country) climateCity = (item as Country).Capital;
            else if (item is City) climateCity = (item as City);
            updateChart = climateCity != null;

            if (updateChart && climateCity.HasClimateInfo) BuildCityClimateCharts(climateCity);
            else ClearCharts();
        }

        private void WorldTree_ItemDoubleClicked(ILocation item, MouseButtonEventArgs e)
        {
            if (item == null) return;
            //if (worldTree1.SelectedItems.Count != 1) return;
            this.Log<STATUS>(item.Name + " selected.");
        }

        #endregion
        #region DataGrids

        #region geography

        private void dataGridDepots_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "ID" ||
                e.PropertyName == "SeedID" ||
                e.PropertyName == "Position" ||
                e.PropertyName == "Seed" ||
                e.PropertyName == "SeedGenerator" ||
                e.PropertyName == "RandomGenerators" ||
                e.PropertyName == "Identifier" ||
                e.PropertyName == "EntityName" ||
                e.PropertyName == "Model" ||
                e.PropertyName == "Initialized")
                e.Column.Visibility = Visibility.Hidden;
            else if (e.PropertyName == "CategoryID")
                LookupColumnBuilder.CreateComboBoxColumn(e, Global.ModelDatabase.DepotCategoryTable, "Name", "Category");
            else if (e.PropertyName == "StartDate")
                TemplateColumnBuilder.CreateDateCellTemplate(e);
            else if (e.PropertyName == "PreparationTime" || e.PropertyName == "TransportTime")
                TemplateColumnBuilder.CreateLongTimeSpanCellTemplate(e);
            else if (e.PropertyName == "ZoneID")
                LookupColumnBuilder.CreateComboBoxColumn(e, Global.ModelDatabase.ShelfLifeZoneTable, "Name", "Zone");
            else if (e.PropertyName == "AreaID")
                LookupColumnBuilder.CreateComboBoxColumn(e, Global.ModelDatabase.AreaTable, "Name", "Area");
        }

        private void dataGridSiteCategories_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "ID")
                e.Column.Visibility = Visibility.Hidden;
        }

        private void dataGridSites_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "ID" || 
                e.PropertyName == "RecruitingChartSeries" ||
                e.PropertyName == "SupplyDepot" ||
                e.PropertyName == "SeedID" ||
                e.PropertyName == "Position" ||
                e.PropertyName == "Seed" ||
                e.PropertyName == "SeedGenerator" ||
                e.PropertyName == "RandomGenerators" ||
                e.PropertyName == "Identifier" ||
                e.PropertyName == "EntityName" ||
                e.PropertyName == "Model" ||
                e.PropertyName == "Initialized")
                e.Column.Visibility = Visibility.Hidden;
            else if (e.PropertyName == "TransportTime")
                TemplateColumnBuilder.CreateLongTimeSpanCellTemplate(e);
            else if (e.PropertyName == "AreaID")
                LookupColumnBuilder.CreateComboBoxColumn(e, Global.ModelDatabase.AreaTable, "Name", "Area");
            else if (e.PropertyName == "CategoryID")
                LookupColumnBuilder.CreateComboBoxColumn(e, Global.ModelDatabase.SiteCategoryTable, "Name", "Category");
            else if (e.PropertyName == "DepotID")
                LookupColumnBuilder.CreateComboBoxColumn(e, Global.ModelDatabase.DepotTable, "Name", "Depot");
        }

        private void dataGridShelfLifeZones_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "ID") e.Column.Visibility = Visibility.Hidden;
        }

        private void dataGridDepotCategories_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "ID") e.Column.Visibility = Visibility.Hidden;
        }

        #endregion
        #region products

        private void dataGridProducts_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "ID") e.Column.Visibility = Visibility.Hidden;
        }

        private void dataGridProductTypes_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "ID") e.Column.Visibility = Visibility.Hidden;
        }

        private void dataGridPackSizes_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "ID")
                e.Column.Visibility = Visibility.Hidden;
            else if (e.PropertyName == "ProductID")
                LookupColumnBuilder.CreateComboBoxColumn(e, Global.ModelDatabase.ProductTable, "Name", "Product");
        }

        private void dataGridShelfLife_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "ID")
                e.Column.Visibility = Visibility.Hidden;
            else if (e.PropertyName == "ProductID")
                LookupColumnBuilder.CreateComboBoxColumn(e, Global.ModelDatabase.ProductTable, "Name", "Product");
            else if (e.PropertyName == "ZoneID")
                LookupColumnBuilder.CreateComboBoxColumn(e, Global.ModelDatabase.ShelfLifeZoneTable, "Name", "Zone");
            else if (e.PropertyName == "ShelfLifeValue")
                TemplateColumnBuilder.CreateLongTimeSpanCellTemplate(e);
        }

        private void dataGridPackTypes_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "ID") e.Column.Visibility = Visibility.Hidden;
        }

        #endregion
        #region customers

        private void dataGridCustomerTypes_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "ID")
                e.Column.Visibility = Visibility.Hidden;
        }

        private void dataGridCustomerDistribution_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "ID")
                e.Column.Visibility = Visibility.Hidden;
            else if (e.PropertyName == "AreaID")
                LookupColumnBuilder.CreateComboBoxColumn(e, Global.ModelDatabase.AreaTable, "Name", "Area");
            else if (e.PropertyName == "CategoryID")
                LookupColumnBuilder.CreateComboBoxColumn(e, Global.ModelDatabase.CustomerCategoryTable, "Name", "Category");
        }

        #endregion
        #region randomization

        private void dataGridBlocks_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "ID") e.Column.Visibility = Visibility.Hidden;
        }

        private void dataGridAreas_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "ID")
                e.Column.Visibility = Visibility.Hidden;
            else if (e.PropertyName == "BlockID")
                LookupColumnBuilder.CreateComboBoxColumn(e, Global.ModelDatabase.RandomizationBlockTable, "Name", "Block");
        }

        #endregion
        #region schedules

        private void dataGridProductionSchedule_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "ID")
                e.Column.Visibility = Visibility.Hidden;
            else if (e.PropertyName == "DepotID")
                LookupColumnBuilder.CreateComboBoxColumn(e, Global.ModelDatabase.DepotTable, "Name", "Depot");
            else if (e.PropertyName == "StartDate" || e.PropertyName == "EndDate")
                TemplateColumnBuilder.CreateDateCellTemplate(e);
        }

        private void dataGridSupplySchedule_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "ID")
                e.Column.Visibility = Visibility.Hidden;
            else if (e.PropertyName == "SiteID")
                LookupColumnBuilder.CreateComboBoxColumn(e, Global.ModelDatabase.SiteTable, "Name", "Site");
            else if (e.PropertyName == "StartDate" || e.PropertyName == "EndDate")
                TemplateColumnBuilder.CreateDateCellTemplate(e);
        }

        private void dataGridSalesSchedule_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "ID")
                e.Column.Visibility = Visibility.Hidden;
            else if (e.PropertyName == "SiteID")
                LookupColumnBuilder.CreateComboBoxColumn(e, Global.ModelDatabase.SiteTable, "Name", "Site");
            else if (e.PropertyName == "StartDate" || e.PropertyName == "EndDate")
                TemplateColumnBuilder.CreateDateCellTemplate(e);
        }

        #endregion
        #region simulation

        private void dataGridDepotSim_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "ID" ||
                e.PropertyName == "SeedID" ||
                e.PropertyName == "Position" ||
                e.PropertyName == "Seed" ||
                e.PropertyName == "SeedGenerator" ||
                e.PropertyName == "RandomGenerators" ||
                e.PropertyName == "Identifier" ||
                e.PropertyName == "EntityName" ||
                e.PropertyName == "Model" ||
                e.PropertyName == "Initialized" ||
                e.PropertyName == "ZoneID" ||
                e.PropertyName == "AreaID")
                e.Column.Visibility = Visibility.Hidden;
            else if (e.PropertyName == "CategoryID")
                LookupColumnBuilder.CreateComboBoxColumn(e, Global.ModelDatabase.DepotCategoryTable, "Name", "Category");
            else if (e.PropertyName == "StartDate")
                TemplateColumnBuilder.CreateDateCellTemplate(e);
            else if (e.PropertyName == "PreparationTime" || e.PropertyName == "TransportTime")
                TemplateColumnBuilder.CreateLongTimeSpanCellTemplate(e);
        }

        private void dataGridSiteSim_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "ID" ||
                e.PropertyName == "RecruitingChartSeries" ||
                e.PropertyName == "SupplyDepot" ||
                e.PropertyName == "SeedID" ||
                e.PropertyName == "Position" ||
                e.PropertyName == "Seed" ||
                e.PropertyName == "SeedGenerator" ||
                e.PropertyName == "RandomGenerators" ||
                e.PropertyName == "Identifier" ||
                e.PropertyName == "EntityName" ||
                e.PropertyName == "Model" ||
                e.PropertyName == "Initialized" ||
                e.PropertyName == "Comment" ||
                e.PropertyName == "AreaID")
                e.Column.Visibility = Visibility.Hidden;
            else if (e.PropertyName == "TransportTime")
                TemplateColumnBuilder.CreateLongTimeSpanCellTemplate(e);
            else if (e.PropertyName == "CategoryID")
                LookupColumnBuilder.CreateComboBoxColumn(e, Global.ModelDatabase.SiteCategoryTable, "Name", "Category");
            else if (e.PropertyName == "DepotID")
                LookupColumnBuilder.CreateComboBoxColumn(e, Global.ModelDatabase.DepotTable, "Name", "Depot");
        }

        #endregion
        #region debug

        private void dataGridSettings_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "ID") e.Column.Visibility = Visibility.Hidden;
        }

        private void dataGridLog_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName.Contains("Ticks")) TemplateColumnBuilder.CreateExactDateTimeCellTemplate(e);
            else if (e.PropertyName == "ID") e.Column.Visibility = Visibility.Hidden;
        }

        #endregion

        #endregion
        #region ToolBars

        private void buttonStartSim_Click(object sender, RoutedEventArgs e)
        {
            if (simMode)
            {
                StartSim();
            }
            else if (optMode)
            {
                StartOpt();
            }
            else
            {
                tabControlMain.SelectedItem = tabItemSimulation;
                StartSim();
            }
        }

        private void SwitchMap_Click(object sender, RoutedEventArgs e)
        {
            //switch (currentMapMode)
            //{
            //    case 0:
            //        gMapControl1.MapType = GMap.NET.MapType.GoogleHybrid;
            //        gMapControl1.Visibility = Visibility.Visible;
            //        imageBox1.Visibility = Visibility.Hidden;
            //        break;
            //    case 1:
            //        gMapControl1.MapType = GMap.NET.MapType.GoogleTerrain;
            //        break;
            //    case 2:
            //        gMapControl1.MapType = GMap.NET.MapType.GoogleSatellite;
            //        break;
            //    case 3:
            //        gMapControl1.MapType = GMap.NET.MapType.OpenStreetMap;
            //        break;
            //    default:
            //        imageBox1.Visibility = Visibility.Visible;
            //        gMapControl1.Visibility = Visibility.Hidden;
            //        break;
            //}

            currentMapMode = (currentMapMode + 1) % 5;
        }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            buttonSave.IsEnabled = false;
        }

        private void buttonChart_Click(object sender, RoutedEventArgs e)
        {
            if (splineMode)
            {
                foreach (Series s in recruitingChart2D.Series)
                {
                    s["DrawingStyle"] = "Cylinder";
                    s.ChartType = SeriesChartType.Column;
                    recruitingChart2D.Invalidate();
                }

                splineMode = false;
            }
            else if (!threeDmode)
            {
                windowsFormsHostSimChart1.Visibility = Visibility.Visible;
                windowsFormsHostSimChart2.Visibility = Visibility.Hidden;
                threeDmode = true;
            }
            else
            {
                windowsFormsHostSimChart2.Visibility = Visibility.Visible;
                windowsFormsHostSimChart1.Visibility = Visibility.Hidden;
                threeDmode = false;
            }
        }

        private void buttonChart2_Click(object sender, RoutedEventArgs e)
        {
            if (!splineMode)
            {
                foreach (Series s in recruitingChart2D.Series)
                {
                    s.ChartType = SeriesChartType.Spline;
                    s["DrawingStyle"] = "Spline";
                    recruitingChart2D.Invalidate();
                }
                
                splineMode = true;
            }
            else if (!threeDmode)
            {
                windowsFormsHostSimChart1.Visibility = Visibility.Visible;
                windowsFormsHostSimChart2.Visibility = Visibility.Hidden;
                threeDmode = true;
            }
            else
            {
                windowsFormsHostSimChart2.Visibility = Visibility.Visible;
                windowsFormsHostSimChart1.Visibility = Visibility.Hidden;
                threeDmode = false;
            }
        }

        private void buttonCalc_Click(object sender, RoutedEventArgs e)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "CALC.EXE";
            Process.Start(startInfo);
        }

        private void buttonExcel_Click(object sender, RoutedEventArgs e)
        {
            /*
             * grid.SelectionMode = DataGridSelectionMode.Extended;
                grid.SelectAllCells();
                grid.ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader;
                ApplicationCommands.Copy.Execute(null, grid);

                Then you can paste the data to Excel or if you need to create a file you can call Clipboard.GetData(DataFormats.CommaSeparatedValue)
                to get the data from the clipboard in CSV.
             */

            // dynamic excel = AutomationFactory.CreateObject("Excel.Application");
            //dynamic rg = worksheet.Range[worksheet.Cells[top, left], worksheet.Cells[bottom, right]];
            //rg.Value = data;

            // interop.excel.dll

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "EXCEL.EXE";
            Process.Start(startInfo);
        }

        #endregion
        #region Charts

        #region sim

        private void recruitingChart_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            recruitingChart3D.Cursor = System.Windows.Forms.Cursors.NoMove2D;
            mouseDownPoint = new System.Drawing.Point(e.X, e.Y);
            origRotation = this.recruitingChart3D.ChartAreas[0].Area3DStyle.Rotation;
            origInclination = this.recruitingChart3D.ChartAreas[0].Area3DStyle.Inclination;
        }

        private void recruitingChart_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            recruitingChart3D.Cursor = System.Windows.Forms.Cursors.Hand;
            mouseDownPoint = System.Drawing.Point.Empty;
        }

        private void recruitingChart_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (!mouseDownPoint.IsEmpty)
            {
                int RotationDelta = (mouseDownPoint.X - e.X);
                int Rotation = origRotation;
                for (int i = 0; i != RotationDelta; )
                {
                    if (RotationDelta > 0)
                    {
                        if (Rotation >= 180)
                        {
                            Rotation = -180;
                        }
                        ++Rotation;
                    }
                    else
                    {
                        if (Rotation <= -180)
                        {
                            Rotation = 180;
                        }
                        --Rotation;
                    }

                    i += (RotationDelta > 0) ? 1 : -1;
                }
                this.recruitingChart3D.ChartAreas[0].Area3DStyle.Rotation = Rotation;

                int InclinationDelta = (e.Y - mouseDownPoint.Y);
                int Inclination = origInclination;
                for (int i = 0; i != InclinationDelta; )
                {
                    if (InclinationDelta > 0)
                    {
                        if (Inclination >= 90)
                        {
                            Inclination = -90;
                        }
                        ++Inclination;
                    }
                    else
                    {
                        if (Inclination <= -90)
                        {
                            Inclination = 90;
                        }
                        --Inclination;
                    }

                    i += (InclinationDelta > 0) ? 1 : -1;
                }
                this.recruitingChart3D.ChartAreas[0].Area3DStyle.Inclination = Inclination;

                this.recruitingChart3D.Invalidate();
                this.recruitingChart3D.Update();
            }
        }

        #endregion
        #region opt

        private void optChart_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            optChart3D.Cursor = System.Windows.Forms.Cursors.NoMove2D;
            mouseDownPoint = new System.Drawing.Point(e.X, e.Y);
            origRotation = this.optChart3D.ChartAreas[0].Area3DStyle.Rotation;
            origInclination = this.optChart3D.ChartAreas[0].Area3DStyle.Inclination;
        }

        private void optChart_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            optChart3D.Cursor = System.Windows.Forms.Cursors.Hand;
            mouseDownPoint = System.Drawing.Point.Empty;
        }

        private void optChart_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (!mouseDownPoint.IsEmpty)
            {
                int RotationDelta = (mouseDownPoint.X - e.X);
                int Rotation = origRotation;
                for (int i = 0; i != RotationDelta; )
                {
                    if (RotationDelta > 0)
                    {
                        if (Rotation >= 180)
                        {
                            Rotation = -180;
                        }
                        ++Rotation;
                    }
                    else
                    {
                        if (Rotation <= -180)
                        {
                            Rotation = 180;
                        }
                        --Rotation;
                    }

                    i += (RotationDelta > 0) ? 1 : -1;
                }
                this.optChart3D.ChartAreas[0].Area3DStyle.Rotation = Rotation;

                int InclinationDelta = (e.Y - mouseDownPoint.Y);
                int Inclination = origInclination;
                for (int i = 0; i != InclinationDelta; )
                {
                    if (InclinationDelta > 0)
                    {
                        if (Inclination >= 90)
                        {
                            Inclination = -90;
                        }
                        ++Inclination;
                    }
                    else
                    {
                        if (Inclination <= -90)
                        {
                            Inclination = 90;
                        }
                        --Inclination;
                    }

                    i += (InclinationDelta > 0) ? 1 : -1;
                }
                this.optChart3D.ChartAreas[0].Area3DStyle.Inclination = Inclination;

                this.optChart3D.Invalidate();
                this.optChart3D.Update();
            }
        }

        #endregion

        #endregion
        #region Tabs

        private void tabControlMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            simMode = tabControlMain.SelectedItem == tabItemSimulation;
            optMode = tabControlMain.SelectedItem == tabItemOptimization;
        }

        #endregion
        #region Simulation

        private void buttonRndSeedSim_Click(object sender, RoutedEventArgs e)
        {
            textBoxSimSeed.Text = rnd.Next().ToString();
        }

        private void dataGridSiteSim_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Site site = dataGridSiteSim.CurrentItem as Site;
            if (site == null && sender == null) return;
            else if (site == null)
            {
                foreach (Site s in Global.ModelDatabase.SiteTable)
                {
                    if (s.RecruitingChartSeries == null) return;
                    s.RecruitingChartSeries.Color =
                        System.Drawing.Color.FromArgb(175,
                        s.RecruitingChartSeries.Color.R,
                        s.RecruitingChartSeries.Color.G,
                        s.RecruitingChartSeries.Color.B);
                }
            }
            else
            {
                foreach (Site s in Global.ModelDatabase.SiteTable)
                    s.RecruitingChartSeries.Color =
                        System.Drawing.Color.FromArgb(20,
                        s.RecruitingChartSeries.Color.R,
                        s.RecruitingChartSeries.Color.G,
                        s.RecruitingChartSeries.Color.B);

                foreach (Site s in dataGridSiteSim.SelectedItems)
                {
                    s.RecruitingChartSeries.Color =
                            System.Drawing.Color.FromArgb(175,
                            s.RecruitingChartSeries.Color.R,
                            s.RecruitingChartSeries.Color.G,
                            s.RecruitingChartSeries.Color.B);
                }
            }
            recruitingChart2D.Invalidate();
        }

        #endregion
        #region Optimization

        private void buttonRndSeedOpt_Click(object sender, RoutedEventArgs e)
        {
            textBoxOptSeed.Text = rnd.Next().ToString();
        }

        #endregion

        #endregion
        #region init

        private void InitGMap()
        {
            //gMapControl1.DragButton = MouseButton.Left;
            //gMapControl1.Zoom = 2;
        }

        private void InitSim()
        {
            MatthiasToolbox.Simulation.Simulator.RegisterSimulationLogger(new WPFRichTextBoxLogger(richTextBoxSimLog));
            InitCharts();
        }

        private void InitCharts()
        {
            #region sim

            #region 3D

            recruitingChart3D = new Chart();
            recruitingChart3D.MouseDown += recruitingChart_MouseDown;
            recruitingChart3D.MouseUp += recruitingChart_MouseUp;
            recruitingChart3D.MouseMove += recruitingChart_MouseMove;

            recruitingChart3DArea1 = new ChartArea("Recruiting");
            recruitingChart3D.ChartAreas.Add(recruitingChart3DArea1);

            recruitingChart3D.Legends.Add(new Legend("Legend 1"));
            recruitingChart3D.Legends[0].Docking = Docking.Top;
            recruitingChart3D.AntiAliasing = AntiAliasingStyles.All;
            recruitingChart3D.TextAntiAliasingQuality = TextAntiAliasingQuality.High;

            recruitingChart3DArea1.AxisX.Minimum = 0;
            recruitingChart3DArea1.AxisX.Maximum = 150;
            recruitingChart3DArea1.AxisY.Minimum = 0;
            recruitingChart3DArea1.AxisY.Maximum = 15;
            recruitingChart3DArea1.AxisY.Interval = 5;

            recruitingChart3DArea1.Area3DStyle.Enable3D = true;

            recruitingChart3DArea1.Area3DStyle.Rotation = 30;
            recruitingChart3DArea1.Area3DStyle.Inclination = 10;
            recruitingChart3DArea1.Area3DStyle.Perspective = 30;
            recruitingChart3DArea1.Area3DStyle.LightStyle = LightStyle.Realistic;

            recruitingChart3DArea1.Area3DStyle.IsClustered = false;
            recruitingChart3DArea1.Area3DStyle.IsRightAngleAxes = false;
            recruitingChart3DArea1.Area3DStyle.PointGapDepth = 100;
            recruitingChart3DArea1.Area3DStyle.PointDepth = 100;
            recruitingChart3DArea1.BackColor = System.Drawing.Color.White;

            windowsFormsHostSimChart1.Child = recruitingChart3D;

            #endregion
            #region 2D

            recruitingChart2D = new Chart();

            recruitingChart2DArea1 = new ChartArea("Recruiting");
            recruitingChart2D.ChartAreas.Add(recruitingChart2DArea1);

            recruitingChart2D.Legends.Add(new Legend("Legend 1"));
            recruitingChart2D.Legends[0].Docking = Docking.Top;
            recruitingChart2D.AntiAliasing = AntiAliasingStyles.All;
            recruitingChart2D.TextAntiAliasingQuality = TextAntiAliasingQuality.High;

            recruitingChart2DArea1.AxisX.Minimum = 0;
            recruitingChart2DArea1.AxisX.Maximum = 150;
            recruitingChart2DArea1.AxisY.Minimum = 0;
            recruitingChart2DArea1.AxisY.Maximum = 15;
            recruitingChart2DArea1.AxisY.Interval = 5;

            recruitingChart3DArea1.BackColor = System.Drawing.Color.White;

            windowsFormsHostSimChart2.Child = recruitingChart2D;

            #endregion

            #endregion
            #region opt

            #region 3D

            optChart3D = new Chart();
            optChart3D.MouseDown += optChart_MouseDown;
            optChart3D.MouseUp += optChart_MouseUp;
            optChart3D.MouseMove += optChart_MouseMove;

            optChart3DArea1 = new ChartArea("Optimization");
            optChart3D.ChartAreas.Add(optChart3DArea1);

            //optChart3D.Legends.Add(new Legend("Legend 1"));
            //optChart3D.Legends[0].Docking = Docking.Top;
            optChart3D.AntiAliasing = AntiAliasingStyles.All;
            optChart3D.TextAntiAliasingQuality = TextAntiAliasingQuality.High;

            //optChart3DArea1.AxisX.Minimum = 0;
            //optChart3DArea1.AxisX.Maximum = 150;
            //optChart3DArea1.AxisY.Minimum = 0;
            //optChart3DArea1.AxisY.Maximum = 15;
            //optChart3DArea1.AxisY.Interval = 5;

            optChart3DArea1.Area3DStyle.Enable3D = true;

            optChart3DArea1.Area3DStyle.Rotation = 30;
            optChart3DArea1.Area3DStyle.Inclination = 10;
            optChart3DArea1.Area3DStyle.Perspective = 30;
            optChart3DArea1.Area3DStyle.LightStyle = LightStyle.Realistic;

            optChart3DArea1.Area3DStyle.IsClustered = false;
            optChart3DArea1.Area3DStyle.IsRightAngleAxes = false;
            optChart3DArea1.Area3DStyle.PointGapDepth = 100;
            optChart3DArea1.Area3DStyle.PointDepth = 100;
            optChart3DArea1.BackColor = System.Drawing.Color.White;

            windowsFormsHostOptChart1.Child = optChart3D;

            #endregion
            #region 2D

            optChart2D = new Chart();

            optChart2DArea1 = new ChartArea("Optimization");
            optChart2D.ChartAreas.Add(optChart2DArea1);

            //optChart2D.Legends.Add(new Legend("Legend 1"));
            //optChart2D.Legends[0].Docking = Docking.Top;
            optChart2D.AntiAliasing = AntiAliasingStyles.All;
            optChart2D.TextAntiAliasingQuality = TextAntiAliasingQuality.High;

            //optChart2DArea1.AxisX.Minimum = 0;
            //optChart2DArea1.AxisX.Maximum = 150;
            //optChart2DArea1.AxisY.Minimum = 0;
            //optChart2DArea1.AxisY.Maximum = 15;
            //optChart2DArea1.AxisY.Interval = 5;

            optChart2DArea1.BackColor = System.Drawing.Color.White;

            windowsFormsHostOptChart2.Child = optChart2D;

            #endregion

            #endregion
        }

        #endregion
        #region rset

        private void ResetCharts()
        {
            foreach (Site site in Global.ModelDatabase.SiteTable)
            {
                if (site.RecruitingChartSeries != null)
                    site.RecruitingChartSeries.Points.Clear();
            }

            if (optSeries != null) optSeries.Points.Clear();

            recruitingChart2D.Invalidate();
            optChart2D.Invalidate();
        }

        #endregion
        #region impl

        #region Simulation

        private void StartSim()
        {
            buttonStartSim.IsEnabled = false;

            // prepare GUI
            ResetCharts();
            int colorCounter = 0;
            foreach (Site site in Global.ModelDatabase.SiteTable)
            {
                if (site.RecruitingChartSeries == null) 
                    site.RecruitingChartSeries = new Series(site.Name);

                if (splineMode)
                {
                    site.RecruitingChartSeries.ChartType = SeriesChartType.Spline;
                    site.RecruitingChartSeries["DrawingStyle"] = "Spline";
                }
                else
                {
                    site.RecruitingChartSeries.ChartType = SeriesChartType.Column;
                    site.RecruitingChartSeries["DrawingStyle"] = "Cylinder";
                }

                site.RecruitingChartSeries.Color =
                    System.Drawing.Color.FromArgb(175,
                    chartSeriesColors[colorCounter].R,
                    chartSeriesColors[colorCounter].G,
                    chartSeriesColors[colorCounter].B);
                
                colorCounter++;

                site.RecruitingChartSeries.Legend = "Legend 1";
                site.RecruitingChartSeries.LegendText = site.Name;
                site.RecruitingChartSeries.BorderWidth = 2;

                if (!recruitingChart2D.Series.Contains(site.RecruitingChartSeries)) 
                    recruitingChart2D.Series.Add(site.RecruitingChartSeries);

                if (!recruitingChart3D.Series.Contains(site.RecruitingChartSeries)) 
                    recruitingChart3D.Series.Add(site.RecruitingChartSeries);
            }

            // windowsFormsHostSimChart.InvalidateVisual();
            recruitingChart3D.Update();
            this.DoEvents();

            // init simulation
            sim = new SupplyChain.Simulator.Simulation(int.Parse(textBoxSimSeed.Text), new DateTime(2010, 5, 31, 16, 30, 0), this);
            sim.BuildModel(Global.ModelDatabase);
            sim.SupplyChainModel.SimulationFinished += SimulationFinished;
            sim.RecruitingUpdated += UpdateRecruiting;

            // run
            sim.StartSimulation(checkBoxSimLog.IsChecked);
        }

        private void UpdateRecruiting(Site site, int day)
        {
            site.RecruitingChartSeries.Points.Add(new DataPoint(day, (double)site.Patients + 0.1));
            recruitingChart2D.Invalidate();
            this.DoEvents();
        }

        private void SimulationFinished(object sender, SimulationEventArgs e)
        {
            buttonStartSim.IsEnabled = true;
        }

        #endregion
        #region Optimization

        private void StartOpt()
        {
            opt = new Optimisation();
            // opt.AnnealingAlgorithm.BestSolutionChanged += AnnealingAlgorithm_BestSolutionChanged;
            opt.EvolutionaryAlgorithm.BestSolutionChanged += AnnealingAlgorithm_BestSolutionChanged;
            opt.Problem.TenEvaluationsDone += p_TenEvaluationsDone;

            if (optSeries == null)
                optSeries = new Series("Optimization");

            if (splineMode)
            {
                optSeries.ChartType = SeriesChartType.Spline;
                optSeries["DrawingStyle"] = "Spline";
            }
            else
            {
                optSeries.ChartType = SeriesChartType.Column;
                optSeries["DrawingStyle"] = "Cylinder";
            }

            optSeries.Color =
                System.Drawing.Color.FromArgb(175,
                255,
                20,
                20);

            //optSeries.Legend = "Legend 1";
            //optSeries.LegendText = site.Name;
            optSeries.BorderWidth = 2;

            if (!optChart2D.Series.Contains(optSeries))
                optChart2D.Series.Add(optSeries);

            if (!optChart3D.Series.Contains(optSeries))
                optChart3D.Series.Add(optSeries);

            opt.Solve();
        }

        void p_TenEvaluationsDone()
        {
            optSeries.Points.Add(new DataPoint(opt.Problem.EvaluationCounter, opt.EvolutionaryAlgorithm.BestSolution.Fitness));
            optChart2D.Invalidate();
            optChart3D.Invalidate();
            this.DoEvents();
        }

        void AnnealingAlgorithm_BestSolutionChanged(object sender, BestSolutionChangedEventArgs e)
        {
        }

        #endregion

        #endregion
        #region util

        #region datagrids

        private void InitializeDataGrids()
        {
            #region model

            #region geography

            dataGrids[dataGridShelfLifeZones] = new DataGridInfo(dataGridShelfLifeZones, "dataGridShelfLifeZones", Global.ModelDatabase.ShelfLifeZoneTable.GetNewBindingList);
            dataGrids[dataGridDepotCategories] = new DataGridInfo(dataGridDepotCategories, "dataGridDepotCategories", Global.ModelDatabase.DepotCategoryTable.GetNewBindingList);
            dataGrids[dataGridDepots] = new DataGridInfo(dataGridDepots, "dataGridDepots", Global.ModelDatabase.DepotTable.GetNewBindingList);
            dataGrids[dataGridSiteCategories] = new DataGridInfo(dataGridSiteCategories, "dataGridSiteCategories", Global.ModelDatabase.SiteCategoryTable.GetNewBindingList);
            dataGrids[dataGridSites] = new DataGridInfo(dataGridSites, "dataGridSites", Global.ModelDatabase.SiteTable.GetNewBindingList);

            #endregion
            #region products

            dataGrids[dataGridPackTypes] = new DataGridInfo(dataGridPackTypes, "dataGridPackTypes", Global.ModelDatabase.PackageCategoryTable.GetNewBindingList);
            dataGrids[dataGridPackSizes] = new DataGridInfo(dataGridPackSizes, "dataGridPackSizes", Global.ModelDatabase.PackageSizeTable.GetNewBindingList);
            dataGrids[dataGridProductTypes] = new DataGridInfo(dataGridProductTypes, "dataGridProductTypes", Global.ModelDatabase.ProductCategoryTable.GetNewBindingList);
            dataGrids[dataGridProducts] = new DataGridInfo(dataGridProducts, "dataGridProducts", Global.ModelDatabase.ProductTable.GetNewBindingList);
            dataGrids[dataGridShelfLife] = new DataGridInfo(dataGridShelfLife, "dataGridShelfLife", Global.ModelDatabase.ShelfLifeTable.GetNewBindingList);

            #endregion
            #region customers

            dataGrids[dataGridCustomerTypes] = new DataGridInfo(dataGridCustomerTypes, "dataGridCustomerTypes", Global.ModelDatabase.CustomerCategoryTable.GetNewBindingList);
            dataGrids[dataGridCustomerDistribution] = new DataGridInfo(dataGridCustomerDistribution, "dataGridCustomerDistribution", Global.ModelDatabase.CustomerDistributionTable.GetNewBindingList);

            #endregion
            #region schedules

            dataGrids[dataGridSalesSchedule] = new DataGridInfo(dataGridSalesSchedule, "dataGridSalesSchedule", Global.ModelDatabase.TrialScheduleTable.GetNewBindingList);
            dataGrids[dataGridProductionSchedule] = new DataGridInfo(dataGridProductionSchedule, "dataGridProductionSchedule", Global.ModelDatabase.ProductionScheduleTable.GetNewBindingList);

            #endregion
            #region randomization

            dataGrids[dataGridAreas] = new DataGridInfo(dataGridAreas, "dataGridAreas", Global.ModelDatabase.AreaTable.GetNewBindingList);
            dataGrids[dataGridBlocks] = new DataGridInfo(dataGridBlocks, "dataGridBlocks", Global.ModelDatabase.RandomizationBlockTable.GetNewBindingList);

            #endregion

            #endregion
            #region sim

            dataGrids[dataGridDepotSim] = new DataGridInfo(dataGridDepotSim, "dataGridDepotSim", Global.ModelDatabase.DepotTable.GetNewBindingList);
            dataGrids[dataGridSiteSim] = new DataGridInfo(dataGridSiteSim, "dataGridSiteSim", Global.ModelDatabase.SiteTable.GetNewBindingList);

            #endregion
            #region debug

            dataGrids[dataGridLog] = new DataGridInfo(dataGridLog, "dataGridLog", Global.UserDatabase.DatabaseLogTable.GetNewBindingList, "TimeStampTicks");
            dataGrids[dataGridSettings] = new DataGridInfo(dataGridSettings, "dataGridSettings", Global.UserDatabase.SettingTable.GetNewBindingList);

            #endregion
        }

        private void PopulateDataGrids()
        {
            foreach (DataGridInfo dgi in dataGrids.Values) dgi.Initialize();
        }

        #endregion
        #region local settings

        private void SaveSettings()
        {
            Setting.Set("WindowWidth", Width);
            Setting.Set("WindowHeight", Height);
            Setting.Set("WindowOriginY", Top);
            Setting.Set("WindowOriginX", Left);
        }

        private void RestoreMainWindowSettings()
        {
            if (Setting.Exists("WindowWidth")) Width = Setting.Get<double>("WindowWidth");
            if (Setting.Exists("WindowHeight")) Height = Setting.Get<double>("WindowHeight");
            if (Setting.Exists("WindowOriginY")) Top = Setting.Get<double>("WindowOriginY");
            if (Setting.Exists("WindowOriginX")) Left = Setting.Get<double>("WindowOriginX");
        }

        private void RestoreLastSessionSettings()
        {
            if (Setting.Exists("LastSelectedLocationID"))
            {
                int locationID = Setting.Get<int>("LastSelectedLocationID");
                int locationType = Setting.Get<int>("LastSelectedLocationType");

                switch (locationType)
                {
                    case 1: // macroregion
                        //worldTree1.SelectMacroRegion(locationID);
                        break;
                    case 2: // subregion
                        //worldTree1.SelectSubRegion(locationID);
                        break;
                    case 3: // country
                        //worldTree1.SelectCountry(locationID);
                        break;
                    case 4: // city
                        //worldTree1.SelectCity(locationID);
                        break;
                    default: // unknown
                        this.Log<WARN>("The settings data may be corrupted, an unknown location type was set in LastSelectedLocationType.");
                        return;
                }
            }
            else
            {
                //worldTree1.ExpandCountry(GeoDatabase.CountriesByCode["CH"].ID);
                //worldTree1.SelectCountry(GeoDatabase.CountriesByCode["CH"].ID);
            }
        }

        #endregion
        #region gui generation

        private void InitializeStrings()
        {
            tabItemCustomers.Header = Global.CustomerTabTitle;
            tabItemProducts.Header = Global.ProductTabTitle;
            tabItemSalesSchedule.Header = Global.SalesTabTitle;
        }

        private void ClearCharts()
        {
            chartCityClimate1.Series.Clear();
            chartCityClimate2.Series.Clear();
            chartCityClimate3.Series.Clear();
        }

        private void SetupCharts()
        {
            chartCityClimate1 = new Chart();
            chartAreaCityClimate1 = new ChartArea();
            chartAreaCityClimate1.AxisX.Interval = 1;
            chartAreaCityClimate1.AxisX.IntervalOffset = 1;
            chartCityClimate1.ChartAreas.Add(chartAreaCityClimate1);

            chartCityClimate2 = new Chart();
            chartAreaCityClimate2 = new ChartArea();
            chartAreaCityClimate2.AxisX.Interval = 1;
            chartAreaCityClimate2.AxisX.IntervalOffset = 1;
            chartCityClimate2.ChartAreas.Add(chartAreaCityClimate2);

            chartCityClimate3 = new Chart();
            chartAreaCityClimate3 = new ChartArea();
            chartAreaCityClimate3.AxisX.Interval = 1;
            chartAreaCityClimate3.AxisX.IntervalOffset = 1;
            chartCityClimate3.ChartAreas.Add(chartAreaCityClimate3);

            winFormsHost1.Child = chartCityClimate1;
            winFormsHost2.Child = chartCityClimate2;
            winFormsHost3.Child = chartCityClimate3;
        }

        private void BuildCityClimateCharts(City city)
        {
            BuildCityClimateChart1(city);
            BuildCityClimateChart2(city);
            BuildCityClimateChart3(city);
        }

        private void BuildCityClimateChart1(City city)
        {
            chartCityClimate1.Series.Clear();
            chartCityClimate1.Legends.Clear();
            chartAreaCityClimate1.RecalculateAxesScale();

            Legend l = new Legend("Default");
            chartCityClimate1.Legends.Add(l);
            chartCityClimate1.Legends["Default"].Docking = Docking.Top;

            chartCityClimate1.BorderSkin.SkinStyle = BorderSkinStyle.Raised;
            chartCityClimate1.BorderSkin.BorderWidth = 1;
            chartCityClimate1.BorderSkin.BorderDashStyle = ChartDashStyle.Solid;
            chartCityClimate1.BorderSkin.BackColor = System.Drawing.Color.MidnightBlue;

            chartCityClimate1.BorderSkin.BorderColor = System.Drawing.Color.MidnightBlue; //funkt net

            chartAreaCityClimate1.AxisX.LineColor = System.Drawing.Color.Red;
            chartAreaCityClimate1.AxisY.LineColor = System.Drawing.Color.Red;
            chartAreaCityClimate1.AxisX.Title = "Month";
            chartAreaCityClimate1.AxisX.IsMarginVisible = true; //funkt net

            Series seriesAvgMax = new Series("seriesAvgMax");
            seriesAvgMax.ChartType = SeriesChartType.Spline;
            seriesAvgMax.BorderWidth = 2;
            seriesAvgMax.ToolTip = "Average Maximum Temperature in °C";
            seriesAvgMax.LegendText = "Average Maximum Temperature";

            Series seriesAvgMin = new Series("seriesAvgMin");
            seriesAvgMin.ChartType = SeriesChartType.Spline;
            seriesAvgMin.BorderWidth = 2;
            seriesAvgMin.ToolTip = "Average Minimum Temperature in °C";
            seriesAvgMin.LegendText = "Average Minimum Temperature";

            Series seriesAbsMax = new Series("seriesAbsMax");
            seriesAbsMax.ToolTip = "Absolute Maximum Temperature in °C";
            seriesAbsMax.LegendText = "Absolute Maximum Temperature";

            Series seriesAbsMin = new Series("seriesAbsMin");
            seriesAbsMin.ToolTip = "Absolute Minimum Temperature in °C";
            seriesAbsMin.LegendText = "Absolute Minimum Temperature";


            foreach (float val in city.ClimateInfo.AverageMaxTemperatures)
            {
                if (float.IsNaN(val)) seriesAvgMax.Points.Add(new DataPoint());
                else seriesAvgMax.Points.Add(val);
            }

            foreach (float val in city.ClimateInfo.AverageMinTemperatures)
            {
                if (float.IsNaN(val)) seriesAvgMin.Points.Add(new DataPoint());
                else seriesAvgMin.Points.Add(val);
            }

            foreach (float val in city.ClimateInfo.AbsoluteMaxTemperatures)
            {
                if (float.IsNaN(val)) seriesAbsMax.Points.Add(new DataPoint());
                else seriesAbsMax.Points.Add(val);
            }

            foreach (float val in city.ClimateInfo.AbsoluteMinTemperatures)
            {
                if (float.IsNaN(val)) seriesAbsMin.Points.Add(new DataPoint());
                else seriesAbsMin.Points.Add(val);
            }

            chartCityClimate1.Series.Add(seriesAbsMax);
            chartCityClimate1.Series.Add(seriesAbsMin);
            chartCityClimate1.Series.Add(seriesAvgMax);
            chartCityClimate1.Series.Add(seriesAvgMin);

            chartCityClimate1.Series["seriesAvgMax"].Color = System.Drawing.Color.OrangeRed;

            chartCityClimate1.Series["seriesAvgMin"].Color = System.Drawing.Color.DarkSlateBlue;

            chartCityClimate1.Series["seriesAbsMax"].Color = System.Drawing.Color.Goldenrod;
            chartCityClimate1.Series["seriesAbsMax"].BackSecondaryColor = System.Drawing.Color.Gold;
            chartCityClimate1.Series["seriesAbsMax"].BackGradientStyle = GradientStyle.VerticalCenter;

            chartCityClimate1.Series["seriesAbsMin"].Color = System.Drawing.Color.DarkSlateBlue;
            chartCityClimate1.Series["seriesAbsMin"].BackSecondaryColor = System.Drawing.Color.MediumSlateBlue;
            chartCityClimate1.Series["seriesAbsMin"].BackGradientStyle = GradientStyle.VerticalCenter;

        }

        private void BuildCityClimateChart2(City city)
        {
            chartCityClimate2.Series.Clear();
            chartCityClimate2.Legends.Clear();
            chartAreaCityClimate2.RecalculateAxesScale();

            Legend l = new Legend("Default");
            chartCityClimate2.Legends.Add(l);
            chartCityClimate2.Legends["Default"].Docking = Docking.Top;

            chartCityClimate2.BorderSkin.SkinStyle = BorderSkinStyle.Raised;
            chartCityClimate2.BorderSkin.BorderWidth = 1;
            //chartCityClimate2.BorderSkin.BorderDashStyle = ChartDashStyle.Solid;
            //chartCityClimate2.BorderSkin.BackColor = System.Drawing.Color.MidnightBlue;

            //chartCityClimate2.BorderSkin.BorderColor = System.Drawing.Color.MidnightBlue; //funkt net

            chartAreaCityClimate2.AxisX.LineColor = System.Drawing.Color.Red;
            chartAreaCityClimate2.AxisY.LineColor = System.Drawing.Color.Red;
            chartAreaCityClimate2.AxisX.Title = "Month";
            chartAreaCityClimate2.AxisX.IsMarginVisible = true; //funkt net
            chartAreaCityClimate2.AxisY.Title = "Relative Humidity";
            chartAreaCityClimate2.AxisY2.Title = "Rain in mm";

            Series seriesRelHum = new Series("seriesRelHum");
            seriesRelHum.ChartType = SeriesChartType.Spline;
            seriesRelHum.BorderWidth = 2;
            seriesRelHum.ToolTip = "Relative Humidity";
            seriesRelHum.LegendText = "Relative Humidity";
            seriesRelHum.YAxisType = AxisType.Primary;

            Series seriesAvgRainMM = new Series("seriesAvgRainMM"); //AverageRainInMM
            seriesAvgRainMM.ToolTip = "Average Rain in mm";
            seriesAvgRainMM.LegendText = "Average Rain in mm";
            seriesAvgRainMM.YAxisType = AxisType.Secondary;

            Series seriesRainyDays = new Series("seriesRainyDays"); //AverageRainInMM
            seriesRainyDays.ToolTip = "Rainy Days";
            seriesRainyDays.LegendText = "Rainy Days";

            foreach (float val in city.ClimateInfo.RelativeHumidity)
            {
                if (float.IsNaN(val)) seriesRelHum.Points.Add(new DataPoint());
                else if (val == -1) seriesRelHum.Points.Add(new DataPoint());
                else seriesRelHum.Points.Add(val);
            }

            foreach (float val in city.ClimateInfo.AverageRainInMM)
            {
                if (float.IsNaN(val)) seriesAvgRainMM.Points.Add(new DataPoint());
                else if (val == -1) seriesAvgRainMM.Points.Add(new DataPoint());
                else seriesAvgRainMM.Points.Add(val);
            }

            foreach (float val in city.ClimateInfo.RainyDaysPerMonth)
            {
                if (float.IsNaN(val)) seriesRainyDays.Points.Add(new DataPoint());
                else seriesRainyDays.Points.Add(val);
            }

            chartCityClimate2.Series.Add(seriesAvgRainMM);
            chartCityClimate2.Series.Add(seriesRainyDays);
            chartCityClimate2.Series.Add(seriesRelHum);

            chartCityClimate2.Series["seriesRelHum"].Color = System.Drawing.Color.DarkSlateBlue;

            chartCityClimate2.Series["seriesAvgRainMM"].Color = System.Drawing.Color.DarkSlateBlue;
            chartCityClimate2.Series["seriesAvgRainMM"].BackSecondaryColor = System.Drawing.Color.MediumSlateBlue;
            chartCityClimate2.Series["seriesAvgRainMM"].BackGradientStyle = GradientStyle.VerticalCenter;

            chartCityClimate2.Series["seriesRainyDays"].Color = System.Drawing.Color.OrangeRed;          //OrangeRed
            chartCityClimate2.Series["seriesRainyDays"].BackSecondaryColor = System.Drawing.Color.DarkOrange; //DarkOrange
            chartCityClimate2.Series["seriesRainyDays"].BackGradientStyle = GradientStyle.VerticalCenter;

        }

        private void BuildCityClimateChart3(City city)
        {
            chartCityClimate3.Series.Clear();
            chartCityClimate3.Legends.Clear();
            chartAreaCityClimate3.RecalculateAxesScale();

            Legend l = new Legend("Default");
            chartCityClimate3.Legends.Add(l);
            chartCityClimate3.Legends["Default"].Docking = Docking.Top;

            chartCityClimate3.BorderSkin.SkinStyle = BorderSkinStyle.Raised;
            chartCityClimate3.BorderSkin.BorderWidth = 1;
            chartCityClimate3.BorderSkin.BorderDashStyle = ChartDashStyle.Solid;
            chartCityClimate3.BorderSkin.BackColor = System.Drawing.Color.MidnightBlue;

            chartCityClimate3.BorderSkin.BorderColor = System.Drawing.Color.MidnightBlue; //funkt net

            chartAreaCityClimate3.AxisX.LineColor = System.Drawing.Color.Red;
            chartAreaCityClimate3.AxisY.LineColor = System.Drawing.Color.Red;
            chartAreaCityClimate3.AxisX.Title = "Month";
            chartAreaCityClimate3.AxisX.IsMarginVisible = true; //funkt net
            chartAreaCityClimate3.AxisY.Title = "Water Temperature";
            chartAreaCityClimate3.AxisY2.Title = "Sunshine Hours";

            Series seriesWater = new Series("seriesWater");
            seriesWater.ChartType = SeriesChartType.Spline;
            seriesWater.BorderWidth = 2;
            seriesWater.ToolTip = "Water Temperature in °C";
            seriesWater.LegendText = "Water Temperature in °C";
            seriesWater.YAxisType = AxisType.Primary;

            Series seriesSunshine = new Series("seriesSunshine"); //AverageRainInMM
            seriesSunshine.ToolTip = "Sunshine Hours";
            seriesSunshine.LegendText = "Sunshine Hours";
            seriesSunshine.YAxisType = AxisType.Secondary;


            foreach (float val in city.ClimateInfo.WaterTemperatures)
            {
                if (float.IsNaN(val)) seriesWater.Points.Add(new DataPoint());
                else seriesWater.Points.Add(val);
            }

            foreach (float val in city.ClimateInfo.SunshineHoursPerDay)
            {
                if (float.IsNaN(val)) seriesSunshine.Points.Add(new DataPoint());
                else seriesSunshine.Points.Add(val);
            }

            chartCityClimate3.Series.Add(seriesSunshine);
            chartCityClimate3.Series.Add(seriesWater);

            chartCityClimate3.Series["seriesSunshine"].Color = System.Drawing.Color.Goldenrod;
            chartCityClimate3.Series["seriesSunshine"].BackSecondaryColor = System.Drawing.Color.Gold;
            chartCityClimate3.Series["seriesSunshine"].BackGradientStyle = GradientStyle.VerticalCenter;

            chartCityClimate3.Series["seriesWater"].Color = System.Drawing.Color.DarkSlateBlue;
        }

        #endregion

        #endregion
        #region todo

        private void dataGridDepotSim_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        #endregion
    }
}