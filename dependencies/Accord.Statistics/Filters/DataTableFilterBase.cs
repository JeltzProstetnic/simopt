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
    ///   Base abstract class for the Data Table preprocessing filters.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class DataTableFilterBase<T> : IFilter where T : IColumnOptions, new()
    {

        public ColumnOptionCollection<T> ColumnOptions { get; protected set; }


        public DataTableFilterBase()
        {
            this.ColumnOptions = new ColumnOptionCollection<T>();
        }

        public DataTableFilterBase(params string[] columns)
        {
            this.ColumnOptions = new ColumnOptionCollection<T>();

            foreach (String col in columns)
            {
                T options = new T();
                options.Column = col;

                ColumnOptions.Add(options);
            }
        }



        public abstract DataTable Apply(DataTable data);

    }
}
