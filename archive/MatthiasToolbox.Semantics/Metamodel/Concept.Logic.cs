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
    /// <summary>
    /// CAUTION: There are two further partial class files, one in /Data and one in /Layout
    /// </summary>
    public partial class Concept : INamedElement, IMultiNamedElement
    {
        #region prop

        public Ontology Container
        {
            get { return DataContext; }
        }

        #region IIdentifiable<int>

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
                if (ID == 0) return "Temporary Concept";
                if (DataContext == null) return "Unnamed Concept";
                return DataContext.GetConceptName(ID);
            }
            set
            {
                if (ID == 0) throw new Exception("Cannot set the name of a temporary concept.");
                if (DataContext == null) throw new NullReferenceException("Cannot set the name because the DataContext was not set.");
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
                foreach (string n in DataContext.GetConceptNames(ID))
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
                foreach (string n in DataContext.GetAllConceptNames(ID))
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
                foreach (string n in DataContext.GetPreferredConceptNames(ID))
                    yield return n;
            }
        }

        #endregion
        #region Concept Hierarchy

        public Concept Parent
        {
            get
            {
                if (ID == 0) return null;
                if (DataContext == null) return null;
                if (this == DataContext.RootConcept) return null;
                return DataContext.GetConcept(SuperConceptID);
            }
        }

        public IEnumerable<Concept> Children
        {
            get
            {
                if (ID == 0) return null;
                if (DataContext == null) return null;
                return DataContext.GetChildren(ID);
            }
        }

        public IEnumerable<Concept> Siblings
        {
            get
            {
                if (ID == 0) return null;
                if (DataContext == null) return null;
                if (this == DataContext.RootConcept) return null;
                return Parent.Children;
            }
        }

        #endregion
        #region Properties

        /// <summary>
        /// CAUTION: these are only the properties which are defined for
        /// this concept, the inherited properties are not included.
        /// </summary>
        public List<Property> Properties
        {
            get
            {
                if (ID == 0 || DataContext == null) return new List<Property>();
                return DataContext.GetProperties(ID);
            }
        }

        /// <summary>
        /// Includes inherited properties.
        /// </summary>
        public IEnumerable<Property> AllProperties
        {
            get
            {
                if (ID == 0 || DataContext == null) return new List<Property>();
                return DataContext.GetAllProperties(this);
            }
        }

        #endregion
        #region Instances

        public List<Instance> Instances
        {
            get
            {
                if (ID == 0 || DataContext == null) return new List<Instance>();
                return DataContext.GetInstances(ID);
            }
        }

        #endregion

        #endregion
        #region ctor

        #endregion
        #region impl

        #region lookup

        /// <summary>
        /// looks for a property with the given name. inherited properties are included in the search.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public bool HasProperty(string p)
        {
            if(DataContext == null) throw new NullReferenceException("A DataContext must be available before the properties can be accessed.");

            var q = from row in AllProperties
                    where row.Name == p
                    select row;

            return q.Any();
        }

        /// <summary>
        /// returns the first property with the given name. inherited properties are included in the search.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public Property GetProperty(string p)
        {
            if (DataContext == null) throw new NullReferenceException("A DataContext must be available before the properties can be accessed.");

            var q = from row in AllProperties
                    where row.Name == p
                    select row;

            if (q.Any()) return q.First();
            else return null;
        }

        public IEnumerable<Relation> GetRelations()
        {
            return (from row in DataContext.RelationTable where row.ConceptID1 == ID || row.ConceptID2 == ID select row);
        }

        public IEnumerable<Concept> GetRelatedConcepts()
        {
            List<int> ids = (from row in DataContext.RelationTable where row.ConceptID1 == ID select row.ConceptID2).ToList();
            ids.AddRange((from row in DataContext.RelationTable where row.ConceptID2 == ID select row.ConceptID1).ToList());
            foreach (int id in ids) yield return (from row in DataContext.ConceptTable where row.ID == id select row).FirstOrDefault();
        }

        #endregion
        #region create

        public Concept CreateSubConcept(string name) { return DataContext.CreateConcept(name, this); }

        #endregion

        #endregion
    }
}