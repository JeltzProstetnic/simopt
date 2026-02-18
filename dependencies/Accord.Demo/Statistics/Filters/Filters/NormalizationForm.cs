using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Accord.Statistics.Filters;

namespace DataProcessing
{
    public partial class NormalizationForm : Form
    {
        private FilterDescriptor descriptor;
        private NormalizationFilter filter;

        public NormalizationForm(FilterDescriptor descriptor) : this()
        {
            this.descriptor = descriptor;
            filter = descriptor.Filter as NormalizationFilter;
        }

        public NormalizationForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.IsNewRow)
                    continue;

                // Get the key
                String key = row.Cells[0].Value as String;

                // Get Mean
                double mean = (double)row.Cells[1].Value;

                // Get Standard Deviation
                double stdDev = (double)row.Cells[2].Value;

           /*     // If filter does not has the key
                if (!filter.Means.ContainsKey(key))
                    filter.Means.Add(key, mean);
                else filter.Means[key] = mean;
                
                if (!filter.StandardDeviations.ContainsKey(key))
                    filter.StandardDeviations.Add(key, stdDev);
                else filter.StandardDeviations[key] = stdDev;
            */ 
            }
        }

        private void NormalizationForm_Load(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();

            foreach (NormalizationFilter.Options options in filter.ColumnOptions)
            {
                dataGridView1.Rows.Add(options.Column, options.Mean, options.StandardDeviation);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            filter.Detect(descriptor.Table);
            reload();
        }

        private void reload()
        {
            dataGridView1.Rows.Clear();
/*
            foreach (String key in filter.Means.Keys)
            {
                dataGridView1.Rows.Add(key, filter.Means[key], filter.StandardDeviations[key]);
            }
 */ 
        }
    }
}
