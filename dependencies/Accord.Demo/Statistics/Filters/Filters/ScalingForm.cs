using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Accord.Statistics.Filters;
using AForge;

namespace DataProcessing.Filters
{
    public partial class ScalingForm : Form
    {
        private FilterDescriptor descriptor;
        private LinearScalingFilter filter;

        public ScalingForm(FilterDescriptor descriptor)
            : this()
        {
            this.descriptor = descriptor;
            filter = descriptor.Filter as LinearScalingFilter;
        }

        public ScalingForm()
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

                // Get Source Range
                DoubleRange sourceRange = new DoubleRange(
                    (double)row.Cells[1].Value,
                    (double)row.Cells[2].Value);

                // Get Output Range
                DoubleRange outputRange = new DoubleRange(
                    (double)row.Cells[3].Value,
                    (double)row.Cells[4].Value);
/*
                // If filter does not has the key
                if (!filter.SourceRanges.ContainsKey(key))
                    filter.SourceRanges.Add(key, sourceRange);
                else filter.SourceRanges[key] = sourceRange;

                if (!filter.DestinationRanges.ContainsKey(key))
                    filter.DestinationRanges.Add(key, sourceRange);
                else filter.DestinationRanges[key] = sourceRange;
 */
            }
        }

        private void NormalizationForm_Load(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();
/*
            foreach (String key in filter.SourceRanges.Keys)
            {
                DoubleRange sourceRange = filter.SourceRanges[key];
                DoubleRange outputRange = filter.DestinationRanges[key];
                dataGridView1.Rows.Add(key, sourceRange.Min, sourceRange.Max, outputRange.Min, outputRange.Max);
            }
 */ 
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
            foreach (String key in filter.SourceRanges.Keys)
            {
                DoubleRange sourceRange = filter.SourceRanges[key];
                DoubleRange outputRange = filter.DestinationRanges[key];

                dataGridView1.Rows.Add(key, sourceRange.Min, sourceRange.Max, outputRange.Min, outputRange.Max);
            }
 */ 
        }
    }

   
}

