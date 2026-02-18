// Accord Statistics Library
// Accord.NET framework
// http://www.crsouza.com
//
// Copyright © César Souza, 2009
// cesarsouza at gmail.com
//

using System;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace Accord.Statistics.Filters
{
    /// <summary>
    ///   Category Filter class.
    /// </summary>
    /// <remarks>
    ///   The category filter performs categorization of classes in string form.
    ///   An unique numeric identifier will be assigned for each of the string
    ///   classes.
    /// </remarks>
    public class CategoryFilter : IFilter
    {

        public ColumnOptionCollection<Options> ColumnOptions { private set; get; }

        public class Options : IColumnOptions
        {
            public String Column { get; set; }
            public Dictionary<string, int> Mapping { get; private set; }

            public Options(String name)
            {
                this.Column = name;
                this.Mapping = new Dictionary<string, int>();
            }

            public Options(String name, Dictionary<string,int> map)
            {
                this.Column = name;
                this.Mapping = map;
            }
        }


        /// <summary>
        ///   Creates a new Category Filter.
        /// </summary>
        public CategoryFilter()
        {
            this.ColumnOptions = new ColumnOptionCollection<Options>();
        }



        public DataTable Apply(DataTable input)
        {
            // Copy only the schema (Clone)
            DataTable result = input.Clone();

            // For each column having a mapping
            foreach (Options options in ColumnOptions)
            {
                // Change its type from string to integer
                result.Columns[options.Column].DataType = typeof(int);
            }


            // Now for each row on the original table
            foreach (DataRow inputRow in input.Rows)
            {
                // We'll import to the result table
                DataRow resultRow = result.NewRow();

                // For each column in original table
                foreach (DataColumn column in input.Columns)
                {
                    string name = column.ColumnName;

                    // If the column has a mapping
                    if (ColumnOptions.Contains(name))
                    {
                        var map = ColumnOptions[name].Mapping;

                        // Retrieve string value
                        string label = inputRow[name] as string;

                        // Get its corresponding integer
                        int value = map[label];

                        // Set the row to the integer
                        resultRow[name] = value;
                    }
                    else
                    {
                        // The column does not have a mapping
                        //  so we'll just copy the value over
                        resultRow[name] = inputRow[name];
                    }
                }

                // Finally, add the row into the result table
                result.Rows.Add(resultRow);
            }

            return result;
        }

 

        public void Detect(DataTable table)
        {
            foreach (DataColumn column in table.Columns)
            {
                // If the column has string type
                if (column.DataType == typeof(String))
                {
                    // We'll create a mapping
                    string name = column.ColumnName;
                    var map = new Dictionary<string,int>();
                    var options = new Options(name,map);
                    ColumnOptions.Add(options);

                    // Do a select distinct to get distinct values
                    DataTable d = table.DefaultView.ToTable(true, name);
                    
                    // For each distinct value, create a corresponding integer
                    for (int i = 0; i < d.Rows.Count; i++)
                    {
                        // And register the String->Integer mapping
                        map.Add(d.Rows[i][0] as string, i);
                    }
                }
            }
        }
    }
}
