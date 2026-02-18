using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Accord.Statistics.Analysis;
using Accord.Statistics.Kernels;
using Accord.Controls;
using Accord.Math;

using Components;
using System.IO;
using ZedGraph;
using System.Diagnostics;


namespace KernelComponents
{
    public partial class MainForm : Form
    {
        /*
            Best values for the given examples
         
              Curve     = Correlation,    Center, Gaussian, sigma = 20
              Lindsay   = Covariance,     Center, Polynomial, degree = 1
              Linear    = Covariance,     Center, Polynomia, degree = 1
              Schopholf = Covariance,     Center, Gaussian, sigma = 0.2236
              Wikipedia = Covariance,  No center, Gaussian, sigma = 10
              Wikipedia = Covariance,  No center, polynomial, degree = 2
              Wikipedia = Correlation, No center, polynomial, degree = 2
          
         */

        // Colors used in the pie graphics.
        private readonly Color[] colors = { Color.YellowGreen, Color.DarkOliveGreen, Color.DarkKhaki, Color.Olive,
            Color.Honeydew, Color.PaleGoldenrod, Color.Indigo, Color.Olive, Color.SeaGreen };


        private KernelPrincipalComponentAnalysis kpca;
        private DescriptiveAnalysis sda;

        string[] sourceColumns;


        public MainForm()
        {
            InitializeComponent();

            dgvAnalysisSource.AutoGenerateColumns = true;
            dgvDistributionMeasures.AutoGenerateColumns = false;
            dgvFeatureVectors.AutoGenerateColumns = true;
            dgvPrincipalComponents.AutoGenerateColumns = false;
            dgvProjectionComponents.AutoGenerateColumns = false;
            dgvReversionComponents.AutoGenerateColumns = false;
            dgvProjectionResult.AutoGenerateColumns = true;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Array methods = Enum.GetValues(typeof(AnalysisMethod));
            this.tscbMethod.ComboBox.DataSource = methods;
            this.cbMethod.DataSource = methods;
        }



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
                        DataTable tableSource = db.GetWorksheet(t.Selection);
                        this.dgvAnalysisSource.DataSource = tableSource;
                        this.dgvProjectionSource.DataSource = tableSource.Copy();

                        double[,] graph = tableSource.ToMatrix(out sourceColumns);
                        CreateScatterplot(graphInput, graph);
                        CreateScatterplot(graphMapInput, graph);
                    }
                }
            }
        }


        private void button1_Click(object sender, EventArgs e)
        {
            // Finishes and save any pending changes to the given data
            dgvAnalysisSource.EndEdit();

            if (dgvAnalysisSource.DataSource == null) return;

            // Creates a matrix from the source data table
            double[,] sourceMatrix = (dgvAnalysisSource.DataSource as DataTable).ToMatrix(out sourceColumns);


            // Creates a new Simple Descriptive Analysis
            sda = new DescriptiveAnalysis(sourceMatrix, sourceColumns);
            dgvDistributionMeasures.DataSource = sda.Measures;


            // Populates statistics overview tab with analysis data
            dgvDistributionMeasures.DataSource = sda.Measures;

            IKernel kernel;
            if (rbGaussian.Checked)
            {
                kernel = new Gaussian((double)numSigma.Value);
            }
            else
            {
                kernel = new Polynomial((int)numDegree.Value, (double)numConstant.Value);
            }


            // Gets only the unlabelled values
            double[,] data = sourceMatrix.Submatrix(0, sourceMatrix.GetLength(0) - 1, 0, 1);

            // Gets only the labels
            double[] labels = new double[sourceMatrix.GetLength(0)];
            for (int i = 0; i < labels.Length; i++)
            {
                labels[i] = sourceMatrix[i, 2];
            }


            // Creates the Kernel Principal Component Analysis of the given source
            kpca = new KernelPrincipalComponentAnalysis(data, kernel,
                (AnalysisMethod)cbMethod.SelectedValue);

            kpca.Center = cbCenter.Checked;

            // Compute the Kernel Principal Component Analysis
            kpca.Compute();


            // Perform the transformation of the data using two components
            double[,] result = kpca.Transform(data, 2);

            // Create a new plot with the original Z column
            double[,] graph = new double[data.GetLength(0), 3];
            for (int i = 0; i < graph.GetLength(0); i++)
            {
                graph[i, 0] = result[i, 0];
                graph[i, 1] = result[i, 1];
                graph[i, 2] = labels[i];
            }


            // Create output scatter plot
            CreateScatterplot(graphOutput, graph);
            CreateScatterplot(graphMapFeature, graph);

            // Create output table
            dgvProjectionResult.DataSource = new ArrayDataView(graph, sourceColumns);
            dgvReversionSource.DataSource = new ArrayDataView(kpca.Result);


            // Populates components overview with analysis data
            dgvFeatureVectors.DataSource = new ArrayDataView(kpca.ComponentMatrix);
            dgvPrincipalComponents.DataSource = kpca.Components;

            dgvProjectionComponents.DataSource = kpca.Components;
            dgvReversionComponents.DataSource = kpca.Components;

            numComponents.Maximum = kpca.Components.Count;
            numNeighbor.Maximum = kpca.Result.GetLength(0);
            numNeighbor.Value = System.Math.Min(10, numNeighbor.Maximum);

            CreateComponentCumulativeDistributionGraph(graphCurve);
            CreateComponentDistributionGraph(graphShare);

        }

        private void btnProject2_Click(object sender, EventArgs e)
        {
            // Finishes and save any pending changes to the given data
            dgvProjectionSource.EndEdit();

            // Creates a matrix from the source data table
            double[,] sourceMatrix = (dgvProjectionSource.DataSource as DataTable).ToMatrix(out sourceColumns);

            // Gets only the X and Y
            double[,] data = sourceMatrix.Submatrix(0, sourceMatrix.GetLength(0) - 1, 0, 1);

            // Perform the transformation of the data using two components
            double[,] result = kpca.Transform(data, 2);

            // Create a new plot with the original Z column
            double[,] graph = new double[sourceMatrix.GetLength(0), 3];
            for (int i = 0; i < graph.GetLength(0); i++)
            {
                graph[i, 0] = result[i, 0];
                graph[i, 1] = result[i, 1];
                graph[i, 2] = sourceMatrix[i, 2];
            }

            // Create output scatter plot
            CreateScatterplot(graphOutput, graph);
            CreateScatterplot(graphMapFeature, graph);

            // Create output table
            dgvProjectionResult.DataSource = new ArrayDataView(graph, sourceColumns);
        }

        private void btnReversion_Click(object sender, EventArgs e)
        {
            double[,] reversionSource = (double[,])(dgvReversionSource.DataSource as ArrayDataView).Data;
            double[,] m = kpca.Revert(reversionSource, (int)numNeighbor.Value);
            dgvReversionResult.DataSource = new ArrayDataView(m);

            // Creates a matrix from the source data table
            double[,] sourceMatrix = (dgvProjectionSource.DataSource as DataTable).ToMatrix();


            // Create a new plot with the original Z column
            double[,] graph = new double[sourceMatrix.GetLength(0), 3];
            for (int i = 0; i < graph.GetLength(0); i++)
            {
                graph[i, 0] = m[i, 0];
                graph[i, 1] = m[i, 1];
                graph[i, 2] = sourceMatrix[i, 2];
            }

            CreateScatterplot(zedGraphControl1, graph);
        }

        private void bindingSource1_CurrentChanged(object sender, EventArgs e)
        {
            if (dgvDistributionMeasures.CurrentRow != null)
            {
                DataGridViewRow row = (DataGridViewRow)dgvDistributionMeasures.CurrentRow;
                dataHistogramView1.DataSource =
                    ((DescriptiveMeasures)row.DataBoundItem).Samples;
            }
        }

        public void CreateComponentCumulativeDistributionGraph(ZedGraphControl zgc)
        {
            GraphPane myPane = zgc.GraphPane;

            myPane.CurveList.Clear();

            // Set the titles and axis labels
            myPane.Title.Text = "Component Distribution";
            myPane.Title.FontSpec.Size = 24f;
            myPane.Title.FontSpec.Family = "Tahoma";
            myPane.XAxis.Title.Text = "Components";
            myPane.YAxis.Title.Text = "Percentage";

            PointPairList list = new PointPairList();
            for (int i = 0; i < kpca.Components.Count; i++)
            {
                list.Add(kpca.Components[i].Index, kpca.Components[i].CumulativeProportion);
            }

            // Hide the legend
            myPane.Legend.IsVisible = false;

            // Add a curve
            LineItem curve = myPane.AddCurve("label", list, Color.Red, SymbolType.Circle);
            curve.Line.Width = 2.0F;
            curve.Line.IsAntiAlias = true;
            curve.Symbol.Fill = new Fill(Color.White);
            curve.Symbol.Size = 7;

            myPane.XAxis.Scale.MinAuto = true;
            myPane.XAxis.Scale.MaxAuto = true;
            myPane.YAxis.Scale.MinAuto = true;
            myPane.YAxis.Scale.MaxAuto = true;
            myPane.XAxis.Scale.MagAuto = true;
            myPane.YAxis.Scale.MagAuto = true;


            // Calculate the Axis Scale Ranges
            zgc.AxisChange();
            zgc.Invalidate();
        }

        public void CreateComponentDistributionGraph(ZedGraphControl zgc)
        {
            GraphPane myPane = zgc.GraphPane;
            myPane.CurveList.Clear();

            // Set the GraphPane title
            myPane.Title.Text = "Component Proportion";
            myPane.Title.FontSpec.Size = 24f;
            myPane.Title.FontSpec.Family = "Tahoma";

            // Fill the pane background with a color gradient
            //  myPane.Fill = new Fill(Color.White, Color.WhiteSmoke, 45.0f);
            // No fill for the chart background
            myPane.Chart.Fill.Type = FillType.None;

            myPane.Legend.IsVisible = false;

            // Add some pie slices
            for (int i = 0; i < kpca.Components.Count; i++)
            {
                myPane.AddPieSlice(kpca.Components[i].Proportion, colors[i % colors.Length], 0.1, kpca.Components[i].Index.ToString());
            }


            myPane.XAxis.Scale.MinAuto = true;
            myPane.XAxis.Scale.MaxAuto = true;
            myPane.YAxis.Scale.MinAuto = true;
            myPane.YAxis.Scale.MaxAuto = true;
            myPane.XAxis.Scale.MagAuto = true;
            myPane.YAxis.Scale.MagAuto = true;


            // Calculate the Axis Scale Ranges
            zgc.AxisChange();

            zgc.Invalidate();
        }


        public void CreateScatterplot(ZedGraphControl zgc, double[,] graph)
        {
            GraphPane myPane = zgc.GraphPane;
            myPane.CurveList.Clear();

            // Set the titles
            myPane.Title.Text = "Scatter Plot";
            myPane.XAxis.Title.Text = "X";
            myPane.YAxis.Title.Text = "Y";


            PointPairList list1 = new PointPairList();
            PointPairList list2 = new PointPairList();
            PointPairList list3 = new PointPairList();
            for (int i = 0; i < graph.GetLength(0); i++)
            {
                if (graph[i, 2] == 1.0)
                    list1.Add(graph[i, 0], graph[i, 1]);
                if (graph[i, 2] == 2.0)
                    list2.Add(graph[i, 0], graph[i, 1]);
                if (graph[i, 2] == 3.0)
                    list3.Add(graph[i, 0], graph[i, 1]);
            }

            // Add the curve
            LineItem myCurve = myPane.AddCurve("G1", list1, Color.Blue, SymbolType.Diamond);
            myCurve.Line.IsVisible = false;
            myCurve.Symbol.Border.IsVisible = false;
            myCurve.Symbol.Fill = new Fill(Color.Blue);

            myCurve = myPane.AddCurve("G2", list2, Color.Green, SymbolType.Diamond);
            myCurve.Line.IsVisible = false;
            myCurve.Symbol.Border.IsVisible = false;
            myCurve.Symbol.Fill = new Fill(Color.Green);

            myCurve = myPane.AddCurve("G3", list3, Color.Orange, SymbolType.Diamond);
            myCurve.Line.IsVisible = false;
            myCurve.Symbol.Border.IsVisible = false;
            myCurve.Symbol.Fill = new Fill(Color.Orange);

            myCurve = myPane.AddCurve("M", new PointPairList(), Color.Black, SymbolType.Diamond);
            myCurve.Line.IsVisible = false;
            myCurve.Symbol.Border.IsVisible = false;
            myCurve.Symbol.Fill = new Fill(Color.Black);

            // Fill the background of the chart rect and pane
            //myPane.Chart.Fill = new Fill(Color.White, Color.LightGoldenrodYellow, 45.0f);
            //myPane.Fill = new Fill(Color.White, Color.SlateGray, 45.0f);
            myPane.Fill = new Fill(Color.WhiteSmoke);

            zgc.AxisChange();
            zgc.Invalidate();
        }


        private void graphMapInput_MouseMove(object sender, MouseEventArgs e)
        {
            double x;
            double y;
            graphMapInput.GraphPane.ReverseTransform(new PointF(e.X, e.Y), out x, out y);

            double[,] data = new double[1, 2];
            data[0, 0] = x;
            data[0, 1] = y;


            double[,] result = kpca.Transform(data);

            graphMapFeature.GraphPane.CurveList["M"].Clear();
            graphMapFeature.GraphPane.CurveList["M"].AddPoint(result[0, 0], result[0, 1]);
            graphMapFeature.Invalidate();
        }





    }
}
