using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;
using MatthiasToolbox.Mathematics;

namespace MatthiasToolbox.Indexer.Database
{
    [Table(Name = "tblResultEvaluations")]
    public partial class ResultEvaluation
    {
        #region ctor

        /// <summary>
        /// Empty constructor for deserialization (LINQ)
        /// </summary>
        public ResultEvaluation() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <param name="document"></param>
        /// <param name="search">the tokens used in the search</param>
        /// <param name="rating"></param>
        public ResultEvaluation(
            Document document, 
            string search, 
            float rating)
        {
            this.Search = search;
            this.DocumentID = document.ID;
            this.Rating = rating;
        }

        #endregion
        #region data

        [Column(AutoSync = AutoSync.OnInsert,
                IsPrimaryKey = true,
                IsDbGenerated = true)]
        public int ID { get; private set; }

        [Column]
        public int DocumentID { get; set; }

        [Column]
        public int UserID { get; set; }

        /// <summary>
        /// The search tokens which were used.
        /// </summary>
        [Column]
        public string Search { get; set; }

        /// <summary>
        /// Total user rating value. 0 is bad, 100 is good.
        /// </summary>
        [Column]
        public float Rating { get; set; }

        #endregion
    }
}