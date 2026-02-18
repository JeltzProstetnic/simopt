// Accord Statistics Library
// Accord.NET framework
// http://www.crsouza.com
//
// Copyright © César Souza, 2009
// cesarsouza at gmail.com
//

namespace Accord.Statistics.Filters
{
    using System;
    using System.Data;
    using AForge;

    /// <summary>
    ///   Linear Scaling Filter
    /// </summary>
    public class LinearScalingFilter : IFilter
    {
        /// <summary>
        ///   Column options.
        /// </summary>
        public ColumnOptionCollection<Options> ColumnOptions { private set; get; }

        /// <summary>
        ///   Options for the Linear Scaling filter.
        /// </summary>
        public class Options : IColumnOptions
        {
            public String Column { get; set; }
            public DoubleRange SourceRange { get; set; }
            public DoubleRange OutputRange { get; set; }

            /// <summary>
            ///   Creates a new column options.
            /// </summary>
            public Options(String name)
            {
                this.Column = name;
                this.SourceRange = new DoubleRange(0, 1);
                this.OutputRange = new DoubleRange(0, 1);
            }
        }

        /// <summary>
        ///   Creates a new Linear Scaling Filter.
        /// </summary>
        public LinearScalingFilter()
        {
            this.ColumnOptions = new ColumnOptionCollection<Options>();
        }

        /// <summary>
        ///   Creates a new Linear Scaling Filter.
        /// </summary>
        public LinearScalingFilter(params string[] columns)
        {
            this.ColumnOptions = new ColumnOptionCollection<Options>();
            foreach (string col in columns)
                this.ColumnOptions.Add(new Options(col));
        }

        /// <summary>
        ///   Applies the filter to the DataTable.
        /// </summary>
        public DataTable Apply(DataTable table)
        {
            DataTable result = table.Copy();

            // Scale each value from the original ranges to destination ranges
            foreach (DataColumn column in result.Columns)
            {
                string name = column.ColumnName;
                foreach (DataRow row in result.Rows)
                {
                    double value = (double)row[column];
                    Options options = ColumnOptions[name];
                    row[column] = Accord.Math.Tools.Scale(
                        options.SourceRange,
                        options.OutputRange,
                        value);
                }
            }

            return result;
        }

        /// <summary>
        ///   Detects filter parameters from a DataTable.
        /// </summary>
        /// <param name="data"></param>
        public void Detect(DataTable data)
        {
            // For each column
            foreach (DataColumn column in data.Columns)
            {
                string name = column.ColumnName;
                double max = (double)data.Compute("MAX(" + name + ")", String.Empty);
                double min = (double)data.Compute("MIN(" + name + ")", String.Empty);

                if (!ColumnOptions.Contains(name))
                    ColumnOptions.Add(new Options(name));

                ColumnOptions[name].SourceRange = new DoubleRange(min, max);
            }
        }

    }
}
