using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;
using MatthiasToolbox.Basics.Interfaces;

namespace MatthiasToolbox.Semantics.Metamodel
{
    [Table(Name = "tblProperties")]
    public partial class Property : ILINQTable
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
        public int ConceptOrRelationID { get; set; }

        [Column]
        public bool IsConceptProperty { get; set; }

        [Column]
        public int LanguageID { get; set; }

        [Column]
        public bool IsDeleted { get; set; }

        [Column]
        public string DataType { get; set; }

        #endregion
        #region prop

        public Ontology DataContext { get { return this.GetOntology(); } }

        #endregion
        #region ctor

        public Property() { }

        #endregion
    }
}