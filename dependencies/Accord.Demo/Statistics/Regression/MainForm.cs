using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Accord.Statistics.Analysis;
using Components;
using System.IO;

using Accord.Math;
using Accord.Controls;
using Accord.Statistics.Models.Regression.Linear;

namespace Regression
{
    public partial class MainForm : Form
    {

        private LogisticRegressionAnalysis lra;
        private MultipleLinearRegression mlr;
        private DataTable sourceTable;


        public MainForm()
        {
            InitializeComponent();

            dgvLogisticCoefficients.AutoGenerateColumns = false;
            dgvDistributionMeasures.AutoGenerateColumns = false;
            comboBox2.SelectedIndex = 0;
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
                        this.sourceTable = db.GetWorksheet(t.Selection);
                        this.dgvAnalysisSource.DataSource = sourceTable;

                        this.comboBox1.Items.Clear();
                        this.checkedListBox1.Items.Clear();
                        foreach (DataColumn col in sourceTable.Columns)
                        {
                            this.comboBox1.Items.Add(col.ColumnName);
                            this.checkedListBox1.Items.Add(col.ColumnName);
                        }

                        this.comboBox1.SelectedIndex = 0;
                    }
                }
            }
        }

        private void btnSampleRunAnalysis_Click(object sender, EventArgs e)
        {
            // Finishes and save any pending changes to the given data
            dgvAnalysisSource.EndEdit();
            sourceTable.AcceptChanges();

            // Gets the column of the dependent variable
            String dependentName = (string)comboBox1.SelectedItem;
            DataTable dependent = sourceTable.DefaultView.ToTable(false, dependentName);

            // Gets the columns of the independent variables
            List<string> names = new List<string>();
            foreach (string name in checkedListBox1.CheckedItems)
            {
                names.Add(name);
            }
            String[] independentNames = names.ToArray();
            DataTable independent = sourceTable.DefaultView.ToTable(false, independentNames);


            // Creates the input and output matrices from the source data table
            double[][] input = independent.ToArray();
            double[] output = dependent.Columns[dependentName].ToArray();

            String[] sourceColumns;
            double[,] sourceMatrix = sourceTable.ToMatrix(out sourceColumns);

            // Creates the Simple Descriptive Analysis of the given source
            DescriptiveAnalysis sda = new DescriptiveAnalysis(sourceMatrix, sourceColumns);

            // Populates statistics overview tab with analysis data
            dgvDistributionMeasures.DataSource = sda.Measures;


            // Creates the Logistic Regression Analysis of the given source
            lra = new LogisticRegressionAnalysis(input, output, independentNames, dependentName);


            // Compute the Logistic Regression Analysis
            lra.Compute();

            // Populates coefficient overview with analysis data
            dgvLogisticCoefficients.DataSource = lra.Coefficients;

            // Populate details about the fitted model
            tbChiSquare.Text = lra.ChiSquare.Statistic.ToString("N5");
            tbPValue.Text = lra.ChiSquare.PValue.ToString("N5");
            checkBox1.Checked = lra.ChiSquare.Significant;
            tbDeviance.Text = lra.Deviance.ToString("N5");
            tbLogLikelihood.Text = lra.LogLikelihood.ToString("N5");


            // Perform linear regression
            mlr = new MultipleLinearRegression(independentNames.Length, true);
            mlr.Regress(input, output);

            tbLinearExpression.Text = mlr.ToString();
            tbLinearR.Text = mlr.CoefficientOfDetermination(input, output, false).ToString("N5");
            tbLinearAdjustedR.Text = mlr.CoefficientOfDetermination(input, output, true).ToString("N5");

            DataTable projSource = sourceTable.DefaultView.ToTable(false, independentNames.Combine(dependentName));
            dgvProjectionSource.DataSource = projSource;
        }

        private void dgvDistributionMeasures_CurrentCellChanged(object sender, EventArgs e)
        {
            if (dgvDistributionMeasures.CurrentRow != null)
            {
                DescriptiveMeasures m = dgvDistributionMeasures.CurrentRow.DataBoundItem as DescriptiveMeasures;
                dataHistogramView1.DataSource = m.Samples;
            }
        }

        private void btnShift_Click(object sender, EventArgs e)
        {
            DataTable source = dgvProjectionSource.DataSource as DataTable;


            DataTable independent = source.DefaultView.ToTable(false, lra.Inputs);
            DataTable dependent = source.DefaultView.ToTable(false, lra.Output);

            double[][] input = independent.ToArray();
            double[] output;

            if (comboBox2.SelectedItem as string == "Logistic")
            {
                output = lra.Regression.Compute(input);
            }
            else
            {
                output = mlr.Compute(input);
            }

            DataTable result = source.Clone();
            for (int i = 0; i < input.Length; i++)
            {
                DataRow row = result.NewRow();
                for (int j = 0; j < lra.Inputs.Length; j++)
                {
                    row[lra.Inputs[j]] = input[i][j];
                }
                row[lra.Output] = output[i];

                result.Rows.Add(row);
            }

            dgvProjectionResult.DataSource = result;

        }

    }
}
