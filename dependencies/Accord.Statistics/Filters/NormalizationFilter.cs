// Accord Statistics Library
// Accord.NET framework
// http://www.crsouza.com
//
// Copyright © César Souza, 2009-2010
// cesarsouza at gmail.com
//

namespace Accord.Statistics.Filters
{

    using System;
    using System.Data;

    /// <summary>
    ///   Data normalization preprocessing filter.
    /// </summary>
    public class NormalizationFilter : DataTableFilterBase<NormalizationFilter.Options>
    {

        public NormalizationFilter()
            : base()
        {
        }

        public NormalizationFilter(params string[] columns)
            : base(columns)
        {
        }


        public override DataTable Apply(DataTable data)
        {
            DataTable result = data.Copy();

            // Scale each value from the original ranges to destination ranges
            foreach (Options column in this.ColumnOptions)
            {
                string name = column.Column;

                foreach (DataRow row in result.Rows)
                {
                    double value = (double)row[name];

                    // Center
                    value -= column.Mean;

                    if (column.Standardize)
                    {
                        // Normalize
                        value /= column.StandardDeviation;
                    }

                    row[name] = value;
                }
            }

            return result;
        }

        public void Detect(DataTable data)
        {
            // For each column
            foreach (Options column in this.ColumnOptions)
            {
                string name = column.Column;

                column.Mean = (double)data.Compute("AVG(" + name + ")", String.Empty);
                column.StandardDeviation = (double)data.Compute("STDEV(" + name + ")", String.Empty);
            }
        }



        public class Options : IColumnOptions
        {
            public String Column { get; set; }
            public double Mean { get; set; }
            public double StandardDeviation { get; set; }

            public bool Standardize { get; set; }

            public Options(String name)
            {
                this.Column = name;
                this.Mean = 0;
                this.StandardDeviation = 1;
                this.Standardize = true;
            }

            public Options()
                : this(String.Empty)
            {
            }
        }

    }
}
