using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace Accord.Statistics.Filters
{
    public class EqualizingFilter : IFilter
    {
        
        public ColumnOptionCollection<Options> ColumnOptions { get; set; }


        public EqualizingFilter()
        {
            this.ColumnOptions = new ColumnOptionCollection<Options>();
        }

        public EqualizingFilter(string column)
        {
            this.ColumnOptions = new ColumnOptionCollection<Options>();
            this.ColumnOptions.Add(new Options(column));
        }

        public class Options : IColumnOptions
        {
            public string Column { get; set; }
            public int[] Classes { get; set; }

            public Options(String name)
            {
                this.Column = name;
            }

        }

        public DataTable Apply(DataTable data)
        {
            // Currently works with only one column and for the binary case

            int[] classes = ColumnOptions[0].Classes;
            string column = ColumnOptions[0].Column;

            // Get subsets with 0 and 1
            List<DataRow>[] subsets = new List<DataRow>[classes.Length];

            for (int i = 0; i < subsets.Length; i++)
            {
                subsets[i] = new List<DataRow>(data.Select("[" + column + "] = " + classes[i]));
            }
            
            while (subsets[0].Count != subsets[1].Count)
            {
                if (subsets[0].Count > subsets[1].Count)
                {
                    int diff = subsets[0].Count - subsets[1].Count;
                    for (int i = 0; i < diff && i < subsets[1].Count; i++)
                    {
                        subsets[1].Add(subsets[1][i]);
                    }
                }
                else
                {
                    int diff = subsets[1].Count - subsets[0].Count;
                    for (int i = 0; i < diff && i < subsets[0].Count; i++)
                    {
                        subsets[0].Add(subsets[0][i]);
                    }
                }
            }

            DataTable result = data.Clone();

            for (int i = 0; i < subsets.Length; i++)
            {
                foreach (DataRow row in subsets[i])
                    result.ImportRow(row);    
            }

            return result;
        }

    }
}
