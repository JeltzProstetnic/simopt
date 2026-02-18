using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Accord.Controls;
using Accord.MachineLearning.VectorMachines;
using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Math;
using Accord.Statistics.Analysis;
using Accord.Statistics.Kernels;
using Components;
using ZedGraph;


namespace SVMs
{
    public partial class MainForm : Form
    {

        private KernelSupportVectorMachine svm;

        string[] sourceColumns;



        public MainForm()
        {
            InitializeComponent();

            dgvLearningSource.AutoGenerateColumns = true;
            dgvPerformance.AutoGenerateColumns = false;
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
                        this.dgvLearningSource.DataSource = tableSource;
                        this.dgvTestingSource.DataSource = tableSource.Copy();

                        double[,] sourceMatrix = tableSource.ToMatrix(out sourceColumns);

                        // Detect the kind of problem loaded.
                        if (sourceMatrix.GetLength(1) == 2)
                            // If we have only two columns, assume we will regress from Y to X.
                            this.rbRegression.Checked = true;
                        else
                            // If we have three columns, assume it is a classification problem.
                            this.rbClassification.Checked = true;

                        CreateScatterplot(graphInput, sourceMatrix);
                    }
                }
            }
        }


        private void btnCreate_Click(object sender, EventArgs e)
        {
            // Finishes and save any pending changes to the given data
            dgvLearningSource.EndEdit();

            if (dgvLearningSource.DataSource == null) return;


            // Creates a matrix from the source data table
            double[,] sourceMatrix = (dgvLearningSource.DataSource as DataTable).ToMatrix(out sourceColumns);


            // Create the specified Kernel
            IKernel kernel;
            if (rbGaussian.Checked)
            {
                kernel = new Gaussian((double)numSigma.Value);
            }
            else if (rbPolynomial.Checked)
            {
                kernel = new Polynomial((int)numDegree.Value, (double)numConstant.Value);
            }
            else
            {
                kernel = new Sigmoid((double)numGamma.Value, (double)numSigmoidConstant.Value);
            }




            if (rbClassification.Checked)
            {
                // Perform classification
                SequentialMinimalOptimization smo;

                // Get only the input vector values
                double[,] inputs = sourceMatrix.Submatrix(0, sourceMatrix.GetLength(0) - 1, 0, 1);

                // Creates the Support Vector Machine using the selected kernel
                svm = new KernelSupportVectorMachine(kernel, 2);

                // Get only the label outputs
                int[] labels = new int[sourceMatrix.GetLength(0)];
                for (int i = 0; i < labels.Length; i++)
                    labels[i] = (int)sourceMatrix[i, 2];

                // Creates a new instance of the SMO Learning Algortihm
                smo = new SequentialMinimalOptimization(svm, inputs.ToArray(), labels);

                // Set learning parameters
                smo.Complexity = (double)numC.Value;
                smo.Epsilon = (double)numE.Value;
                smo.Tolerance = (double)numT.Value;

                // Run
                double error = smo.Run();
            }
            else
            {
                // Perform Regression
                SequentialMinimalOptimizationRegression smo;

                // Get only the input vector values
                double[,] inputs = sourceMatrix.Submatrix(0, sourceMatrix.GetLength(0) - 1, 0, 0);

                // Creates the Support Vector Machine using the selected kernel
                svm = new KernelSupportVectorMachine(kernel, 1);

                // Get only the scalar outputs
                double[] outputs = new double[sourceMatrix.GetLength(0)];
                for (int i = 0; i < outputs.Length; i++)
                    outputs[i] = sourceMatrix[i, 1];

                // Creates a new instance of the SMO Learning Algortihm
                smo = new SequentialMinimalOptimizationRegression(svm, inputs.ToArray(), outputs);

                // Set learning parameters
                smo.Complexity = (double)numC.Value;
                smo.Epsilon = (double)numE.Value;
                smo.Tolerance = (double)numT.Value;

                // Run
                double error = smo.Run();
            }



            // Show support vectors
            double[,] supportVectors = svm.SupportVectors.ToMatrix();
            double[,] supportVectorsWeights = supportVectors.InsertColumn(
                svm.Weights, supportVectors.GetLength(1));

            if (supportVectors.GetLength(0) == 0)
            {
                dgvSupportVectors.DataSource = null;
                graphSupportVectors.GraphPane.CurveList.Clear();
                return;
            }

            dgvSupportVectors.DataSource = new ArrayDataView(supportVectorsWeights,
                sourceColumns.Submatrix(0, supportVectors.GetLength(1) - 1).Combine("Weight"));

            double[,] graph = supportVectors;
            if (rbRegression.Checked)
            {
                int[] idx = new int[svm.SupportVectors.Length];
                double[] a = sourceMatrix.GetColumn(0);
                double[] o = sourceMatrix.GetColumn(1);
                for (int i = 0; i < idx.Length; i++)
                    idx[i] = Matrix.Find(a, x => x == svm.SupportVectors[i][0], true)[0];
                graph = graph.InsertColumn(o.Submatrix(idx), 1);
            }
            else
            {
                int[] idx = new int[svm.SupportVectors.Length];
                double[] a = sourceMatrix.GetColumn(0);
                double[] o = sourceMatrix.GetColumn(2);
                for (int i = 0; i < idx.Length; i++)
                    idx[i] = Matrix.Find(a, x => x == svm.SupportVectors[i][0], true)[0];
                graph = graph.InsertColumn(o.Submatrix(idx), 2);
            }

            // Plot support vectors
            CreateScatterplot(graphSupportVectors, graph);
        }


        private void btnTestingRun_Click(object sender, EventArgs e)
        {
            // Creates a matrix from the source data table
            double[,] sourceMatrix = (dgvTestingSource.DataSource as DataTable).ToMatrix();


            if (rbClassification.Checked)
            {
                // Extract inputs
                double[][] inputs = new double[sourceMatrix.GetLength(0)][];
                for (int i = 0; i < inputs.Length; i++)
                    inputs[i] = new double[] { sourceMatrix[i, 0], sourceMatrix[i, 1] };

                // Get only the label outputs
                int[] expected = new int[sourceMatrix.GetLength(0)];
                for (int i = 0; i < expected.Length; i++)
                    expected[i] = (int)sourceMatrix[i, 2];

                // Compute the machine outputs
                int[] output = new int[expected.Length];
                for (int i = 0; i < expected.Length; i++)
                    output[i] = System.Math.Sign(svm.Compute(inputs[i]));

                double[] expectedd = new double[expected.Length];
                double[] outputd = new double[expected.Length];
                for (int i = 0; i < expected.Length; i++)
                {
                    expectedd[i] = expected[i];
                    outputd[i] = output[i];
                }

                // Use confusion matrix to compute some statistics.
                ConfusionMatrix confusionMatrix = new ConfusionMatrix(output, expected, 1,-1);
                dgvPerformance.DataSource = new List<ConfusionMatrix> { confusionMatrix };

                foreach (DataGridViewColumn col in dgvPerformance.Columns) col.Visible = true;
                Column1.Visible = Column2.Visible = false;

                // Create performance scatterplot
                CreateResultScatterplot(zedGraphControl1, inputs, expectedd, outputd);
            }
            else
            {
                // Extract inputs
                double[][] inputs = new double[sourceMatrix.GetLength(0)][];
                for (int i = 0; i < inputs.Length; i++)
                    inputs[i] = new double[] { sourceMatrix[i, 0] };

                // Get only the scalar outputs
                double[] expected = new double[sourceMatrix.GetLength(0)];
                for (int i = 0; i < expected.Length; i++)
                    expected[i] = sourceMatrix[i, 1];

                // Compute the machine outputs
                double[] output = new double[expected.Length];
                for (int i = 0; i < expected.Length; i++)
                    output[i] = svm.Compute(inputs[i]);

                // Compute R² and Sum-of-squares error
                double rSquared = Accord.Statistics.Tools.Determination(output, expected);
                double error = expected.Subtract(output).ElementwisePower(2).Sum() / output.Length;

                // Anonymous magic! :D
                var r = new { RSquared = rSquared, Error = error };
                dgvPerformance.DataSource = (new[] { r }).ToList();

                foreach (DataGridViewColumn col in dgvPerformance.Columns) col.Visible = false;
                Column1.Visible = Column2.Visible = true;

                // Create performance scatterplot
                CreateResultScatterplot(zedGraphControl1, inputs, expected, output);
            }

        }



        public void CreateScatterplot(ZedGraphControl zgc, double[,] graph)
        {
            GraphPane myPane = zgc.GraphPane;
            myPane.CurveList.Clear();

            // Set the titles
            myPane.Title.IsVisible = false;
            myPane.XAxis.Title.Text = sourceColumns[0];
            myPane.YAxis.Title.Text = sourceColumns[1];


            if (rbClassification.Checked)
            {
                // Classification problem
                PointPairList list1 = new PointPairList(); // Z = -1
                PointPairList list2 = new PointPairList(); // Z = +1
                for (int i = 0; i < graph.GetLength(0); i++)
                {
                    if (graph[i, 2] == -1)
                        list1.Add(graph[i, 0], graph[i, 1]);
                    if (graph[i, 2] == 1)
                        list2.Add(graph[i, 0], graph[i, 1]);
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
            }
            else
            {
                // Regression problem
                PointPairList list1 = new PointPairList();
                for (int i = 0; i < graph.GetLength(0); i++)
                    list1.Add(graph[i, 0], graph[i, 1]);

                // Add the curve
                LineItem myCurve = myPane.AddCurve("Y", list1, Color.Blue, SymbolType.Diamond);
                myCurve.Line.IsVisible = false;
                myCurve.Symbol.Border.IsVisible = false;
                myCurve.Symbol.Fill = new Fill(Color.Blue);
            }

            // Fill the background of the chart rect and pane
            //myPane.Chart.Fill = new Fill(Color.White, Color.LightGoldenrodYellow, 45.0f);
            //myPane.Fill = new Fill(Color.White, Color.SlateGray, 45.0f);
            myPane.Fill = new Fill(Color.WhiteSmoke);

            zgc.AxisChange();
            zgc.Invalidate();
        }


        public void CreateResultScatterplot(ZedGraphControl zgc, double[][] inputs, double[] expected, double[] output)
        {
            GraphPane myPane = zgc.GraphPane;
            myPane.CurveList.Clear();

            // Set the titles
            myPane.Title.IsVisible = false;
            myPane.XAxis.Title.Text = sourceColumns[0];
            myPane.YAxis.Title.Text = sourceColumns[1];


            if (rbClassification.Checked)
            {
                // Classification problem
                PointPairList list1 = new PointPairList(); // Z = -1, OK
                PointPairList list2 = new PointPairList(); // Z = +1, OK
                PointPairList list3 = new PointPairList(); // Z = -1, Error
                PointPairList list4 = new PointPairList(); // Z = +1, Error
                for (int i = 0; i < output.Length; i++)
                {
                    if (output[i] == -1)
                    {
                        if (expected[i] == -1)
                            list1.Add(inputs[i][0], inputs[i][1]);
                        if (expected[i] == 1)
                            list3.Add(inputs[i][0], inputs[i][1]);
                    }
                    else
                    {
                        if (expected[i] == -1)
                            list4.Add(inputs[i][0], inputs[i][1]);
                        if (expected[i] == 1)
                            list2.Add(inputs[i][0], inputs[i][1]);
                    }
                }

                // Add the curve
                LineItem 
                myCurve = myPane.AddCurve("G1 Hits", list1, Color.Blue, SymbolType.Diamond);
                myCurve.Line.IsVisible = false;
                myCurve.Symbol.Border.IsVisible = false;
                myCurve.Symbol.Fill = new Fill(Color.Blue);

                myCurve = myPane.AddCurve("G2 Hits", list2, Color.Green, SymbolType.Diamond);
                myCurve.Line.IsVisible = false;
                myCurve.Symbol.Border.IsVisible = false;
                myCurve.Symbol.Fill = new Fill(Color.Green);

                myCurve = myPane.AddCurve("G1 Miss", list3, Color.Blue, SymbolType.Plus);
                myCurve.Line.IsVisible = false;
                myCurve.Symbol.Border.IsVisible = true;
                myCurve.Symbol.Fill = new Fill(Color.Blue);

                myCurve = myPane.AddCurve("G2 Miss", list4, Color.Green, SymbolType.Plus);
                myCurve.Line.IsVisible = false;
                myCurve.Symbol.Border.IsVisible = true;
                myCurve.Symbol.Fill = new Fill(Color.Green);
            }
            else
            {
                // Regression problem
                PointPairList list1 = new PointPairList(); // svm output
                PointPairList list2 = new PointPairList(); // expected output
                for (int i = 0; i < inputs.Length; i++)
                {
                    list1.Add(inputs[i][0], output[i]);
                    list2.Add(inputs[i][0], expected[i]);
                }

                // Add the curve
                LineItem myCurve = myPane.AddCurve("Model output", list1, Color.Blue, SymbolType.Diamond);
                myCurve.Line.IsVisible = true;
                myCurve.Symbol.Border.IsVisible = false;
                myCurve.Symbol.Fill = new Fill(Color.Blue);

                myCurve = myPane.AddCurve("Data output", list2, Color.Red, SymbolType.Diamond);
                myCurve.Line.IsVisible = false;
                myCurve.Symbol.Border.IsVisible = false;
                myCurve.Symbol.Fill = new Fill(Color.Red);
            }

            // Fill the background of the chart rect and pane
            //myPane.Chart.Fill = new Fill(Color.White, Color.LightGoldenrodYellow, 45.0f);
            //myPane.Fill = new Fill(Color.White, Color.SlateGray, 45.0f);
            myPane.Fill = new Fill(Color.WhiteSmoke);

            zgc.AxisChange();
            zgc.Invalidate();
        }


    }
}
