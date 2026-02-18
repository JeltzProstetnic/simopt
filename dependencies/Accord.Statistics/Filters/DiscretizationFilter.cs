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
    ///   Value discretization preprocessing filter.
    /// </summary>
    public class DiscretizationFilter : DataTableFilterBase<DiscretizationFilter.Options>
    {

        public DiscretizationFilter()
            : base()
        {
        }

        public DiscretizationFilter(params string[] columns)
            : base(columns)
        {
        }

        public override DataTable Apply(DataTable data)
        {
            // Copy the datatable
            DataTable result = data.Copy();

            foreach (Options options in ColumnOptions)
            {
                foreach (DataRow row in result.Rows)
                {
                    double value = (double)row[options.Column];


                    double x = options.Symmetric ? System.Math.Abs(value) : value;

                    double floor = System.Math.Floor(x);

                    x = (x >= (floor + options.Threshold)) ?
                        System.Math.Ceiling(x) : floor;


                    value = (options.Symmetric && value < 0) ? -x : x;

                    row[options.Column] = value;
                }
            }

            return result;
        }



        public class Options : IColumnOptions
        {
            public string Column { get; set; }
            public double Threshold { get; set; }
            public bool Symmetric { get; set; }

            public Options(String name)
            {
                this.Column = name;
                this.Threshold = 0.5;
            }

            public Options()
                : this(String.Empty)
            {
            }
        }
    }
}
