using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;

namespace MatthiasToolbox.Semantics.Metamodel.Data
{
    [Table(Name = "tblConceptNames")]
    public class ConceptName
    {
        #region data

        /// <summary>
        /// Unique, auto-generated identifier.
        /// </summary>
        [Column(AutoSync = AutoSync.OnInsert,
                IsPrimaryKey = true,
                IsDbGenerated = true)]
        public int ID { get; private set; }

        [Column]
        public bool IsPreferredName { get; set; }

        [Column]
        public int ConceptID { get; set; }

        [Column]
        public int LanguageID { get; set; }

        [Column]
        public string Value { get; set; }

        [Column]
        public bool IsDeleted { get; set; }
        
        #endregion
    }
}
