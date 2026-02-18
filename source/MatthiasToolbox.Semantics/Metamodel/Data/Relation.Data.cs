using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Media;
using MatthiasToolbox.Basics.Interfaces;
using MatthiasToolbox.Basics.Datastructures.Graph;
using System.Data.Linq.Mapping;
using System.Windows;
using MatthiasToolbox.Semantics.Interfaces;
using MatthiasToolbox.Semantics.Utilities;

namespace MatthiasToolbox.Semantics.Metamodel
{
    [Table(Name = "tblRelations")]
    public partial class Relation : ILINQTable, IEdge<Point>, IColors<Color>, INotifyPropertyChanged
    {
        #region data

        #region Main

        /// <summary>
        /// Unique, auto-generated identifier.
        /// </summary>
        [Column(AutoSync = AutoSync.OnInsert,
                IsPrimaryKey = true,
                IsDbGenerated = true)]
        public int ID { get; private set; }

        private int _conceptId1;

        [Column(Storage="_conceptId1")]
        public int ConceptID1
        {
            get { return _conceptId1; }
            set { _conceptId1 = value; }
        }

        [Column]
        public int ConceptID2 { get; set; }

        [Column]
        public bool IsDeleted { get; set; }

        [Column]
        public bool IsArchived { get; set; }

        #endregion
        #region Cardinality

        [Column]
        public int SourceCardinalityMin { get; set; }

        [Column]
        public int SourceCardinalityMax { get; set; }

        [Column]
        public int TargetCardinalityMin { get; set; }

        [Column]
        public int TargetCardinalityMax { get; set; }

        #endregion
        #region Semantics

        /// <summary>
        /// reflexive: xRx for all x in X
        /// Every reflexive relation is serial
        /// 
        /// equality is reflexive (x is equal to x)
        /// antonymity if non reflexive (x is not the opposite of x)
        /// </summary>
        [Column]
        public bool IsReflexive { get; set; }

        /// <summary>
        /// for all x in X: !xRx
        /// synonym: strict?
        /// </summary>
        [Column]
        public bool IsIrreflexive { get; set; }

        /// <summary>
        /// for all x, y in X: xRy => x = y
        /// </summary>
        [Column]
        public bool IsCoreflexive { get; set; }

        /// <summary>
        /// non-circular: xRy, yRz, zRy is not allowed
        /// parent-child relation is non-circular
        /// synonymity is not non-circular
        /// </summary>
        [Column]
        public bool IsNonCircular { get; set; }

        /// <summary>
        /// transitive: xRy, yRz -> xRz (for all x, y and z in X it holds that if xRy and yRz then xRz)
        /// causation is transitive
        /// antonymity is not transitive
        /// </summary>
        [Column]
        public bool IsTransitive { get; set; }

        /// <summary>
        /// symmetry: xRy -> yRx (for all x and y in X it holds that if xRy then yRx)
        /// antonymity is symmetric
        /// causation is not symmetric
        /// </summary>
        [Column]
        public bool IsSymmetric { get; set; }

        /// <summary>
        /// symmetry: xR1y -> yR2x where R1 is an inversion of R2 (for all x and y in X it holds that if xRy and yRx then x = y)
        /// parent-of relation is antisymmetric to child-of relation
        /// </summary>
        [Column]
        public bool IsAntiSymmetric { get; set; }

        /// <summary>
        /// for all x and y in X it holds that if xRy then not yRx.
        /// </summary>
        [Column]
        public bool IsAsymmetric { get; set; }

        [Column]
        public int AntiSymmetricRelationID { get; set; }

        /// <summary>
        /// for all x in X there exists a y in Y such that xRy
        /// </summary>
        [Column]
        public bool IsLeftTotal { get; set; }

        /// <summary>
        /// for all y in Y there exists an x in X such that xRy
        /// synonym: right-total
        /// </summary>
        [Column]
        public bool IsSurjective { get; set; }

        /// <summary>
        /// for all x in X, and y and z in Y it holds that if xRy and xRz then y = z
        /// synonymes: right-unique
        /// </summary>
        [Column]
        public bool IsFunctional { get; set; }

        /// <summary>
        /// for all x and z in X and y in Y it holds that if xRy and zRy then x = z
        /// synonyme: left-unique
        /// </summary>
        [Column]
        public bool IsInjective { get; set; }

        /// <summary>
        /// for all x and y in X it holds that xRy or yRx (or both).
        /// </summary>
        [Column]
        public bool IsTotal { get; set; }

        /// <summary>
        /// for all x and y in X exactly one of xRy, yRx or x = y holds
        /// </summary>
        [Column]
        public bool IsTrichotomous { get; set; }

        /// <summary>
        /// for all x, y and z in X it holds that if xRy and xRz, then yRz (and zRy).
        /// </summary>
        [Column]
        public bool IsEuclidean { get; set; }

        /// <summary>
        /// for all x in X, there exists y in X such that xRy. 
        /// Every reflexive relation is serial
        /// </summary>
        [Column]
        public bool IsSerial { get; set; }

        #endregion

        #endregion
        #region prop

        public Ontology DataContext { get { return this.GetOntology(); } }

        #endregion
        #region ctor

        public Relation() { }

        #endregion
    }
}