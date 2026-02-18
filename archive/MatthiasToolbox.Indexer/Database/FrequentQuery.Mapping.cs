using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;

namespace MatthiasToolbox.Indexer.Database
{
    [Table(Name = "tblFrequentQueries")]
    public partial class FrequentQuery
    {
        #region ctor

        /// <summary>
        /// Empty constructor for deserialization (LINQ)
        /// </summary>
        public FrequentQuery() { }

        public FrequentQuery(string value, int count = 1)
        {
            this.SearchString = value;
            this.SearchCount = 1;
        }

        #endregion
        #region data

        [Column(AutoSync = AutoSync.OnInsert,
                IsPrimaryKey = true,
                IsDbGenerated = true)]
        public int ID { get; private set; }

        /// <summary>
        /// The search string.
        /// </summary>
        [Column]
        public string SearchString { get; set; }

        /// <summary>
        /// Number of times the query was used.
        /// </summary>
        [Column]
        public int SearchCount { get; set; }

        #endregion
    }
}