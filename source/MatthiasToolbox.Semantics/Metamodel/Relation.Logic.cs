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
    /// <summary>
    /// Synonymie: Natel, Handy, Mobiltelefon
    ///Relation ist reflexiv (x ist mit x synonym ), symmetrisch (wenn x mit y synonym ist, dann auch y mit x) ) und transitiv (wenn x mit y synonym und y mit z, dann ist auch x mit z synonym )
    ///Antonymie: symmetrisch aber nicht reflexiv oder transitiv!
    ///Kausation: Ursachenbeziehung: töten verursacht sterben. Nicht reflexiv und symmetrisch, aber transitiv
    /// </summary>
    public partial class Relation : ILINQTable, IEdge<Point>, IColors<Color>, INotifyPropertyChanged
    {
        #region evnt

        #region INotifyPropertyChanging

        public event System.ComponentModel.PropertyChangingEventHandler PropertyChanging;

        #endregion
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #endregion
        #region prop

        public Tuple<Concept, Concept> Members
        {
            get
            {
                if (DataContext == null) return new Tuple<Concept, Concept>(null, null);

                Concept c1 = (from row in DataContext.ConceptTable
                              where row.ID == ConceptID1
                              select row).FirstOrDefault();

                Concept c2 = (from row in DataContext.ConceptTable
                              where row.ID == ConceptID2
                              select row).FirstOrDefault();
                
                return new Tuple<Concept, Concept>(c1, c2);
            }
            set 
            {
                if (DataContext == null) throw new NullReferenceException("DataContext must be set first.");
                ConceptID1 = value.Item1.ID;
                ConceptID2 = value.Item2.ID;
                DataContext.SubmitChanges();
            }
        }

        public Cardinality Cardinality
        {
            get
            {
                return new Cardinality(
                    new CardinalityValue(SourceCardinalityMin, SourceCardinalityMax), 
                    new CardinalityValue(TargetCardinalityMin, TargetCardinalityMax)
                    );
            }
            set 
            {
                SourceCardinalityMin = (int)value.Item1.MinValue;
                SourceCardinalityMax = value.Item1.MaxValue == double.PositiveInfinity ? -1 : (int)value.Item1.MaxValue;
                TargetCardinalityMin = (int)value.Item2.MinValue;
                TargetCardinalityMax = value.Item2.MaxValue == double.PositiveInfinity ? -1 : (int)value.Item2.MaxValue;
            }
        }

        public bool IsTargetRequired
        {
            get
            {
                return TargetCardinalityMin > 0;
            }
        }

        #region IIdentifiable

        public int Identifier
        {
            get { return ID; }
        }

        #endregion
        #region IMultiNamedElement

        /// <summary>
        /// The preferred name in the current language.
        /// </summary>
        public string Name
        {
            get
            {
                if (ID == 0) return "Temporary Relation";
                if (DataContext == null) return "Unnamed Relation";
                return DataContext.GetRelationName(ID);
            }
            set
            {
                // if (String.IsNullOrWhiteSpace(value)) return;
                if (value == null) return;
                if (DataContext == null) return;
                DataContext.SetName(this, value);
                OnPropertyChanged("Name");
            }
        }

        /// <summary>
        /// All names in the current language.
        /// </summary>
        public IEnumerable<string> Names
        {
            get
            {
                if (ID == 0) yield break;
                if (DataContext == null) yield break;
                foreach (string n in DataContext.GetRelationNames(ID))
                    yield return n;
            }
        }

        /// <summary>
        /// All names in all languages.
        /// </summary>
        public IEnumerable<string> AllNames
        {
            get
            {
                if (ID == 0) yield break;
                if (DataContext == null) yield break;
                foreach (string n in DataContext.GetAllRelationNames(ID))
                    yield return n;
            }
        }

        /// <summary>
        /// The preferred name in each language.
        /// </summary>
        public IEnumerable<string> AllPreferredNames
        {
            get
            {
                if (ID == 0) yield break;
                if (DataContext == null) yield break;
                foreach (string n in DataContext.GetPreferredRelationNames(ID))
                    yield return n;
            }
        }

        #endregion
        #region Tree Helper

        public Instance FindRoot()
        {
            return DataContext.FindRoot(this);
        }

        #endregion

        #endregion
    }
}