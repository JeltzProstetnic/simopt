using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;

namespace MatthiasToolbox.Indexer.Database
{
    [Table(Name = "tblTokens")]
    public partial class Token
    {
        #region ctor

        /// <summary>
        /// Empty constructor for deserialization (LINQ)
        /// </summary>
        public Token() { }

        public Token(string value)
        {
            this.TokenData = value;
        }

        #endregion
        #region data

        [Column(AutoSync = AutoSync.OnInsert,
                IsPrimaryKey = true,
                IsDbGenerated = true)]
        public int ID { get; private set; }

        [Column]
        public string TokenData { get; set; }

        /// <summary>
        /// Number of times the token was used in a search.
        /// </summary>
        [Column]
        public int SearchCount { get; set; }

        #endregion
    }
}
