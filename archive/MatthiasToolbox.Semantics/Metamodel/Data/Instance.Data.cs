using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;
using MatthiasToolbox.Basics.Interfaces;
using MatthiasToolbox.Basics.Datastructures.Graph;
using MatthiasToolbox.Semantics.Interfaces;

namespace MatthiasToolbox.Semantics.Metamodel
{
    [Table(Name = "tblInstances")]
    public partial class Instance : ILINQTable, ISemanticNode
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
        public int ConceptID { get; set; }

        [Column]
        public int DisplayPropertyID { get; set; }

        [Column]
        public bool IsDeleted { get; set; }

        #endregion
        #region evnt

        #region INotifyPropertyChanging

        public event System.ComponentModel.PropertyChangingEventHandler PropertyChanging;

        #endregion

        #endregion
        #region prop

        public Ontology DataContext { get { return this.GetOntology(); } }

        #endregion
        #region ctor

        public Instance() { }

        #endregion
    }
}