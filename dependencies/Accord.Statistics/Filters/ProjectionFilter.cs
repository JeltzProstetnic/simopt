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
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;

    /// <summary>
    ///   Projection DataTable filter.
    /// </summary>
    public class ProjectionFilter : IFilter
    {
        /// <summary>
        ///   List of columns to keep in the projection.
        /// </summary>
        public List<String> Columns { get; private set; }

        /// <summary>
        ///   Creates a new projection filter.
        /// </summary>
        public ProjectionFilter(params string[] columns)
        {
            this.Columns = columns.ToList();
        }

        /// <summary>
        ///   Creates a new projection filter.
        /// </summary>
        public ProjectionFilter()
        {
            this.Columns = new List<string>();
        }

        /// <summary>
        ///   Applies the filter to the DataTable.
        /// </summary>
        public DataTable Apply(DataTable data)
        {
            return data.DefaultView.ToTable(false, Columns.ToArray());
        }

    }
}
