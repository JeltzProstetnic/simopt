using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;

namespace MatthiasToolbox.Indexer.Database
{
    [Table(Name = "tblWhiteList")]
    public class WhiteListWord
    {
        #region ctor

        /// <summary>
        /// Empty constructor for deserialization (LINQ)
        /// </summary>
        public WhiteListWord() { }

        public WhiteListWord(string value)
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