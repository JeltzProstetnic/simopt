using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Accord.Statistics.Filters;
using System.IO;
using Components;
using System.Collections;
using Accord.Controls;
using DataProcessing.Filters;

namespace DataProcessing
{
    public partial class MainForm : Form
    {
        DataTable sourceTable;
        BindingList<FilterDescriptor> filters;
        

        public MainForm()
        {
            InitializeComponent();

            filters = new BindingList<FilterDescriptor>();
            dataGridView2.AutoGenerateColumns = false;
            dataGridView2.DataSource = filters;  
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
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
                        this.dataGridView1.DataSource = sourceTable;
                    }
                }
            }
        }

        private void normToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var f = new FilterDescriptor(new NormalizationFilter(), "Normalization", typeof(NormalizationForm));
            f.Table = sourceTable;
            filters.Add(f);
            //dataGridView2.DataSource = sequence.Filters;
        }

        private void equalizationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var f = new FilterDescriptor(new EqualizingFilter(), "Equalization", typeof(NormalizationForm));
            f.Table = sourceTable;
            filters.Add(f);
        }

        private void dataGridView2_CurrentCellChanged(object sender, EventArgs e)
        {
            if (dataGridView2.CurrentRow != null)
            {
                IFilter filter = (dataGridView2.CurrentRow.DataBoundItem as FilterDescriptor).Filter;
                propertyGrid1.SelectedObject = filter;
                // propertyGrid1.SelectedObject = new PropertyGridAdapter(filter);
            }
        }

        private void dataGridView2_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dataGridView2.CurrentRow != null)
            {
                FilterDescriptor filter = (dataGridView2.CurrentRow.DataBoundItem as FilterDescriptor);
                Form f = (Form)Activator.CreateInstance(filter.Form, filter);
                f.ShowDialog();
            }
        }

        private void scalingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var f = new FilterDescriptor(new LinearScalingFilter(), "Scaling", typeof(ScalingForm));
            f.Table = sourceTable;
            filters.Add(f);
        }

        private void dataGridView1_CurrentCellChanged(object sender, EventArgs e)
        {
            if (dataGridView1.DataSource != null)
            {
                
                string member = dataGridView1.CurrentCell.OwningColumn.DataPropertyName;

                histogramView1.DataSource = dataGridView1.DataSource;
                histogramView1.DataMember = member;
            }
        }


    }
}
