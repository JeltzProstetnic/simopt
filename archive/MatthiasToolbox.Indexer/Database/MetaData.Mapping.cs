using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;

namespace MatthiasToolbox.Indexer.Database
{
    [Table(Name="tblMetaData")]
    public class MetaData
    {
        public MetaData()
        {
        }

        public MetaData(Document doc, string fieldName, string dataType, string data)
        {
            this.DocumentID = doc.ID;
            this.FieldName = fieldName;
            this.DataType = dataType;
            this.Data = data;
        }

        /// <summary>
        /// Unique, auto-generated identifier.
        /// </summary>
        [Column(AutoSync = AutoSync.OnInsert,
                IsPrimaryKey = true,
                IsDbGenerated = true)]
        public int ID { get; private set; }

        [Column]
        public int DocumentID { get; set; }

        [Column]
        public string FieldName { get; set; }

        [Column]
        public string DataType { get; set; }

        [Column]
        public string Data { get; set; }
    }
}
