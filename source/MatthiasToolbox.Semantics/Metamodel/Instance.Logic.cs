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
    public partial class Instance : ILINQTable, ISemanticNode
    {
        #region prop

        public int Identifier
        {
            get { return ID; }
        }

        public string Name
        {
            get
            {
                if (DataContext == null)
                    throw new NullReferenceException("Datacontext not available.");
                else if (DisplayPropertyID == 0)
                    throw new NullReferenceException("Display property not set.");
                else if (DisplayPropertyID == -1)
                    return "";

                return DataContext.GetPropertyValue<string>(ID, DisplayPropertyID);
                // return Get<string>("Name");
            }
            set
            {
                if (DataContext == null)
                    throw new NullReferenceException("Datacontext not available.");
                else if (DisplayPropertyID <= 0)
                    throw new NullReferenceException("Display property not set.");

                DataContext.SetPropertyValue(ID, DisplayPropertyID, value, true);
                // Set<string>("Name", value, true);
            }
        }

        public Concept Concept
        {
            get { return DataContext.GetConcept(ConceptID); }
        }

        public List<Property> Properties
        {
            get { return Concept.Properties; }
        }

        #endregion
        #region impl

        public bool HasValue(Relation relation, Instance targetInstance)
        {
            if (DataContext == null) throw new NullReferenceException("A DataContext must be available before the properties can be accessed.");

            var q = from row in DataContext.InstanceRelationTable
                    where row.RelationID == relation.ID && row.InstanceID == ID && row.TargetInstanceID == targetInstance.ID
                    select row;

            return q.Any();
        }

        /// <summary>
        /// looks for a property with the given name. inherited properties are included in the search.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public bool HasProperty(string p)
        {
            return Concept.HasProperty(p);
        }

        #region get related

        public List<Instance> GetRelatedItems(Relation r)
        {
            return DataContext.FindInstance(this, r);
        }

        public List<Instance> GetReflexivelyRelatedItems(Relation r)
        {
            return DataContext.FindInstanceReflexive(this, r);
        }

        #endregion
        #region get property

        public T Get<T>(string propertyName)
        {
            Property p = DataContext.LookupProperty(Concept, propertyName);
            if (p == null) return default(T);
            else return DataContext.GetPropertyValue<T>(ID, p.ID);
        }

        public T Get<T>(int propertyID)
        {
            return DataContext.GetPropertyValue<T>(ID, propertyID);
        }

        public T Get<T>(Property property)
        {
            return DataContext.GetPropertyValue<T>(ID, property.ID);
        }

        #endregion
        #region set property

        public void Set<T>(string propertyName, T value, bool doSubmit = false)
        {
            DataContext.SetPropertyValue<T>(this, DataContext.LookupProperty(Concept, propertyName), value, doSubmit);
        }

        public void Set<T>(int propertyID, T value, bool doSubmit = false)
        {
            DataContext.SetPropertyValue<T>(this, DataContext.GetProperty(propertyID), value, doSubmit);
        }

        public void Set<T>(Property property, T value, bool doSubmit = false)
        {
            DataContext.SetPropertyValue<T>(this, property, value, doSubmit);
        }

        #endregion

        #endregion
    }
}