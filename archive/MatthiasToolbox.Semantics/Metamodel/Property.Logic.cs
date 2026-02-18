using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;
using MatthiasToolbox.Basics.Interfaces;

namespace MatthiasToolbox.Semantics.Metamodel
{
    public partial class Property : ILINQTable
    {
        #region evnt

        #region INotifyPropertyChanging

        public event System.ComponentModel.PropertyChangingEventHandler PropertyChanging;

        #endregion

        #endregion
        #region prop

        public bool IsRelationProperty { get { return !IsConceptProperty; } }

        #region IIdentifiable<int>

        public int Identifier
        {
            get { return ID; }
        }

        #endregion
        #region IMultiNamedElement

        public string Name
        {
            get
            {
                if (ID == 0) return "Temporary Property";
                if (DataContext == null) return "Unnamed Property";
                return DataContext.GetName(this);
            }
            set
            {
                if (ID == 0) throw new Exception("Cannot set the name of a temporary concept.");
                if (DataContext == null) throw new NullReferenceException("Cannot set the name because the DataContext was not set.");
                DataContext.SetName(this, value);
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
                foreach (string n in DataContext.GetPropertyNames(ID))
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
                foreach (string n in DataContext.GetAllPropertyNames(ID))
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
                foreach (string n in DataContext.GetPreferredPropertyNames(ID))
                    yield return n;
            }
        }

        #endregion

        #endregion
    }
}