using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;

namespace MatthiasToolbox.Indexer.Database
{
    [Table(Name = "tblStopWords")]
    public class StopWord
    {
        #region ctor

        /// <summary>
        /// Empty constructor for deserialization (LINQ)
        /// </summary>
        public StopWord() { }

        public StopWord(string value)
        {
            this.Word = value;
        }

        #endregion
        #region data

        [Column(AutoSync = AutoSync.OnInsert,
                IsPrimaryKey = true,
                IsDbGenerated = true)]
        public int ID { get; private set; }

        [Column]
        public string Word { get; set; }

        #endregion
    }
}