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
    using System.Collections.ObjectModel;
    using System.Data;

    /// <summary>
    ///   Sample processing filter interface.
    /// </summary>
    /// 
    /// <remarks>The interface defines the set of methods which should be
    /// provided by all table processing filters. Methods of this interface should
    /// keep the source table unchanged and return the result of data processing
    /// filter as new data table.</remarks>
    /// 
    /// 
    public interface IFilter
    {

        /// <summary>
        ///   Apply filter to a data table.
        /// </summary>
        /// 
        /// <param name="data">Source table to apply filter to.</param>
        /// 
        /// <returns>Returns filter's result obtained by applying the filter to
        /// the source table.</returns>
        /// 
        /// <remarks>The method keeps the source table unchanged and returns the
        /// the result of the table processing filter as new data table.</remarks> 
        ///
        DataTable Apply(DataTable data);

    }

    /// <summary>
    ///   Column options for filter which have per-column settings.
    /// </summary>
    public interface IColumnOptions
    {
        /// <summary>
        ///   Gets or sets the name of the column that the options will apply to.
        /// </summary>
        String Column { get; set; }
    }

    /// <summary>
    ///   Column option collection.
    /// </summary>
    public class ColumnOptionCollection<T> : KeyedCollection<String,T>
        where T : IColumnOptions
    {
        /// <summary>
        ///   Extracts the key from the specified element.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        protected override string GetKeyForItem(T item)
        {
            return item.Column;
        }
    }
}
