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
    using System.Linq;
    using System.Text;
    using System.Data;
    using System.Collections;

    /// <summary>
    ///   Sequence of table processing filters.
    /// </summary>
    public class FiltersSequence : CollectionBase, IFilter
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="FiltersSequence"/> class.
        /// </summary>
        /// 
        public FiltersSequence() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="FiltersSequence"/> class.
        /// </summary>
        /// 
        /// <param name="filters">Sequence of filters to apply.</param>
        /// 
        public FiltersSequence(params IFilter[] filters)
        {
            InnerList.AddRange(filters);
        }

        /// <summary>
        /// Get filter at the specified index.
        /// </summary>
        /// 
        /// <param name="index">Index of filter to get.</param>
        /// 
        /// <returns>Returns filter at specified index.</returns>
        /// 
        public IFilter this[int index]
        {
            get { return ((IFilter)InnerList[index]); }
        }

        /// <summary>
        /// Add new filter to the sequence.
        /// </summary>
        /// 
        /// <param name="filter">Filter to add to the sequence.</param>
        /// 
        public void Add(IFilter filter)
        {
            InnerList.Add(filter);
        }



        public DataTable Apply(DataTable table)
        {
            DataTable result = table;
            foreach (IFilter filter in this)
            {
                result = filter.Apply(result);
            }

            return result;
        }
    }
}
