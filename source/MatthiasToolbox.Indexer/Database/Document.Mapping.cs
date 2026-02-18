using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;
using System.IO;
using System.Data.Linq;

namespace MatthiasToolbox.Indexer.Database
{
    [Table(Name = "tblDocuments")]
    public partial class Document
    {
        #region cvar

        private FileInfo file;

        #endregion
        #region ctor

        /// <summary>
        /// Empty constructor for deserialization (LINQ)
        /// </summary>
        public Document() 
        {
            RatingCount = 1;
            RatingSum = 50;
            MetaData = new EntitySet<MetaData>();
        }

        public Document(int checksum, string path, int tokenCount) : this()
        {
            this.Checksum = checksum;
            this.Path = path;
            this.file = new FileInfo(path);
            this.ModificationDate = File.LastWriteTime;
            this.TokenCount = tokenCount;
        }

        #endregion
        #region data

        /// <summary>
        /// Unique, auto-generated identifier.
        /// </summary>
        [Column(AutoSync = AutoSync.OnInsert,
                IsPrimaryKey = true,
                IsDbGenerated = true)]
        public int ID { get; private set; }

        /// <summary>
        /// A checksum for this document.
        /// </summary>
        [Column]
        public int Checksum { get; set; }

        /// <summary>
        /// The absolute path of this document.
        /// </summary>
        [Column]
        public string Path { get; set; }

        /// <summary>
        /// The total number of tokens in this document.
        /// </summary>
        [Column]
        public int TokenCount { get; set; }

        /// <summary>
        /// The number of times the document was opened / used as result.
        /// </summary>
        [Column]
        public int OpenCount { get; set; }

        /// <summary>
        /// The sum of ratings for this document.
        /// </summary>
        [Column]
        public float RatingSum { get; set; }

        /// <summary>
        /// The number of times the document was rated.
        /// </summary>
        [Column]
        public float RatingCount { get; set; }

        //float _r;
        ///// <summary>
        ///// Average rating as calculated from RatingSum and RatingCount
        ///// </summary>
        //[Column(Expression = "RatingSum * RatingCount", Storage="_r")]
        //public float Rating { get { return _r; } set { _r = value; } }
        public float Rating { get { return RatingSum / RatingCount; } }

        /// <summary>
        /// The modification date.
        /// </summary>
        [Column]
        public DateTime ModificationDate { get; set; }

        /// <summary>
        /// Document metadata.
        /// </summary>
        [Association(OtherKey = "DocumentID", DeleteRule = "CASCADE")]
        public EntitySet<MetaData> MetaData { get; set; }

        #endregion
        #region impl

        public void UpdateModificationDate()
        {
            this.ModificationDate = File.LastWriteTime;
        }

        #endregion
    }
}