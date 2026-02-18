using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;

namespace MatthiasToolbox.Semantics.Metamodel.Data
{
    [Table(Name = "tblInstanceRelations")]
    public class InstanceRelation
    {
        /// <summary>
        /// Unique, auto-generated identifier.
        /// </summary>
        [Column(AutoSync = AutoSync.OnInsert,
                IsPrimaryKey = true,
                IsDbGenerated = true)]
        public int ID { get; private set; }

        [Column]
        public int InstanceID { get; set; }

        [Column]
        public int RelationID { get; set; }

        [Column]
        public int TargetInstanceID { get; set; }

        [Column]
        public bool IsDeleted { get; set; }
    }
}
