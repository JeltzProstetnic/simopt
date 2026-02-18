using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;

namespace MatthiasToolbox.Semantics.Metamodel.Data
{
    [Table(Name = "tblInstanceData")]
    public class InstanceData
    {
        /// <summary>
        /// Unique, auto-generated identifier.
        /// </summary>
        [Column(AutoSync = AutoSync.OnInsert,
                IsPrimaryKey = true,
                IsDbGenerated = true)]
        public int ID { get; private set; }

        [Column]
        public int LanguageID { get; set; }

        [Column]
        public int InstanceID { get; set; }

        [Column]
        public int PropertyID { get; set; }

        [Column]
        public string LiteralValue { get; set; }

        [Column]
        public bool IsDeleted { get; set; }
    }
}
