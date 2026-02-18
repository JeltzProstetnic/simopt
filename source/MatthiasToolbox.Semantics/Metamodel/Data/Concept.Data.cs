using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;
using MatthiasToolbox.Basics.Interfaces;
using MatthiasToolbox.Basics.Datastructures.Graph;
using MatthiasToolbox.Semantics.Interfaces;
using System.Windows;
using System.Windows.Media;

namespace MatthiasToolbox.Semantics.Metamodel
{
    [Table(Name = "tblConcepts")]
    public partial class Concept : ILINQTable, ISemanticNode
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
        public int SuperConceptID { get; set; }

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

        public Concept() { }

        #endregion
    }
}