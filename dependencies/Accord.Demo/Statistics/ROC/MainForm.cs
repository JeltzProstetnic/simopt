using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;


using Accord.Statistics.Analysis;
using Accord.Controls;

using ZedGraph;

namespace ReceiverOperating
{

    public partial class MainForm : System.Windows.Forms.Form
    {

        private DataTable sourceTable;
        private ReceiverOperatingCharacteristic rocCurve;
        

        public MainForm()
        {
            InitializeComponent();
            
            dgvSource.AutoGenerateColumns = true;
            dgvPointDetails.AutoGenerateColumns = false;

            CreateCurveGraph(zedGraph1);
        }



        #region Buttons
        private void btnRunAnalysis_Click(object sender, EventArgs e)
        {
            if (sourceTable == null)
            {
                MessageBox.Show("Please load some data before attempting to plot a curve.");
                return;
            }


            // Finishes and save any pending changes to the given data
            dgvSource.EndEdit();

            // Creates a matrix from the source data table
            int n = sourceTable.Rows.Count;

            double[] realData = new double[n];
            double[] testData = new double[n];
            for (int i = 0; i < n; i++)
            {
                realData[i] = (double)sourceTable.Rows[i][0];
                testData[i] = (double)sourceTable.Rows[i][1];
            }

            // Creates the Receiver Operating Curve of the given source
            rocCurve = new ReceiverOperatingCharacteristic(realData, testData);

            // Compute the ROC curve
            if (rbNumPoints.Checked)
                rocCurve.Compute((int)numPoints.Value);
            else
                rocCurve.Compute((float)numIncrement.Value);

            // Update graphs
            CreateCurveGraph(zedGraph1);

            // Show point details
            dgvPointDetails.DataSource = new SortableBindingList<ReceiverOperatingCharacteristicPoint>(rocCurve.Points);

            // Show area and error
            tbArea.Text = rocCurve.Area.ToString();
            tbError.Text = rocCurve.Error.ToString();
        }

        #endregion


        #region Menus
        private void MenuFileOpen_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                string filename = openFileDialog.FileName;
                string extension = Path.GetExtension(filename);
                if (extension == ".xls" || extension == ".xlsx")
                {
                    ExcelReader db = new ExcelReader(filename, true, false);
                    TableSelectDialog t = new TableSelectDialog(db.GetWorksheetList());

                    if (t.ShowDialog(this) == DialogResult.OK)
                    {
                        this.sourceTable = db.GetWorksheet(t.Selection);
                        this.dgvSource.DataSource = sourceTable;
                    }
                }
                else if (extension == ".xml")
                {
                    DataTable dataTableAnalysisSource = new DataTable();
                    dataTableAnalysisSource.ReadXml(openFileDialog.FileName);

                    this.sourceTable = dataTableAnalysisSource;
                    this.dgvSource.DataSource = sourceTable;
                }
            }
        }

        #endregion


        #region Graphs
        public void CreateCurveGraph(ZedGraphControl zgc)
        {
            GraphPane myPane = zgc.GraphPane;
            
            myPane.CurveList.Clear();

            // Set the titles and axis labels
            myPane.Title.Text = "Receiver Operating Characteristic Curve";
            myPane.Title.FontSpec.Size = 24f;
            myPane.Title.FontSpec.Family = "Tahoma";
            myPane.XAxis.Title.Text = "(1-Specificity)";
            myPane.YAxis.Title.Text = "Sensitivity";

            PointPairList list = new PointPairList();
            if (rocCurve != null)
            {
                for (int i = 0; i < rocCurve.Points.Count; i++)
                {
                    list.Add(1 - rocCurve.Points[i].Specificity, rocCurve.Points[i].Sensitivity);
                }
            }

            // Hide the legend
            myPane.Legend.IsVisible = false;

            // Add a curve
            LineItem curve = myPane.AddCurve("label", list, Color.Red, SymbolType.Circle);
            curve.Line.Width = 2.0F;
            curve.Line.IsAntiAlias = true;
            curve.Symbol.Fill = new Fill(Color.White);
            curve.Symbol.Size = 7;

            myPane.XAxis.Scale.Max = 1.0;
            myPane.XAxis.Scale.Min = 0.0;

            myPane.YAxis.Scale.Max = 1.0;
            myPane.YAxis.Scale.Min = 0.0;


            // Calculate the Axis Scale Ranges
            zgc.AxisChange();
            zgc.Invalidate();
        }

        
        #endregion



    }
}