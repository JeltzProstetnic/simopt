using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq;
using MatthiasToolbox.Basics.Datastructures.Graph;
using MatthiasToolbox.Basics.Datastructures;
using MatthiasToolbox.Logging;
using MatthiasToolbox.Semantics.Metamodel.Data;
using MatthiasToolbox.Semantics.Metamodel.Layout;
using MatthiasToolbox.Semantics.Utilities;
using MatthiasToolbox.Utilities;
using MatthiasToolbox.Basics.Datastructures.Trees;

namespace MatthiasToolbox.Semantics.Metamodel
{
    /// <summary>
    /// method names:
    /// get: you know the id
    /// find: you know the name
    /// search: you know the name approximately
    /// </summary>
    public partial class Ontology : DataContext
    {
        #region impl

        #region get tree

        /// <summary>
        /// Find the root instance for the given "has parent" relation.
        /// </summary>
        /// <param name="relation"></param>
        /// <returns></returns>
        internal Instance FindRoot(Relation relation)
        {
            // retrieve an instance
            var q = from row in InstanceRelationTable
                    where row.RelationID == relation.ID
                    select row.TargetInstanceID;

            if (!q.Any()) // maybe only one instance exists
            {
                Concept rootConcept = relation.Members.Item2;
                if (rootConcept.Instances == null || rootConcept.Instances.Count == 0) return null;
                return rootConcept.Instances[0];
            }

            Instance result = GetInstance(q.First());
            List<Instance> parents = FindInstance(result, relation);

            while (parents != null && parents.Count > 0)
            {
                result = parents.First();
                parents = FindInstance(result, relation);
            }

            return result;
        }

        /// <summary>
        /// retrieve the inheritance hierarchy of all concepts
        /// </summary>
        /// <returns></returns>
        public ITree GetConceptHierarchy() 
        {
            return new ConceptTree(this);
        }

        /// <summary>
        /// retrieve a tree of instances of c which are hierachically related by r
        /// </summary>
        /// <param name="c"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        public ITree GetTree(Concept c, Relation r)
        {
            return new InstanceTree(r);
        }

        #endregion
        #region get object

        public Concept GetConcept(int id)
        {
            var q = from row in ConceptTable
                    where row.ID == id
                    select row;

            if (!q.Any()) return null;
            return q.First();
        }

        public Relation GetRelation(int id)
        {
            var q = from row in RelationTable
                    where row.ID == id
                    select row;

            if (!q.Any()) return null;
            return q.First();
        }

        public Instance GetInstance(int id)
        {
            var q = from row in InstanceTable
                    where row.ID == id
                    select row;

            if (!q.Any()) return null;
            return q.First();
        }

        public Property GetProperty(int id)
        {
            var q = from row in PropertyTable
                    where row.ID == id
                    select row;

            if (!q.Any()) return null;
            return q.First();
        }


        #endregion
        #region get name

        public string GetName(Concept concept)
        {
            return GetConceptName(concept.ID);
        }

        public string GetName(Relation relation) 
        {
            return GetRelationName(relation.ID);
        }

        public string GetName(Property property)
        {
            return GetPropertyName(property.ID);
        }

        public List<Property> GetProperties(Concept concept)
        {
            return GetProperties(concept.ID);
        }

        public T GetPropertyValue<T>(Instance instance, Property property)
        {
            return GetPropertyValue<T>(instance.ID, property.ID);
        }

        #endregion
        #region set name

        public void SetName(Concept c, string value)
        {
            SetConceptName(c.ID, value, GetConceptName(c));
        }

        public void SetName(Relation r, string value)
        {
            SetRelationName(r.ID, value, GetRelationName(r));
        }
        
        public void SetName(Property p, string value)
        {
            SetPropertyName(p.ID, value, GetPropertyName(p));
        }

        #endregion
        #region set value

        public void SetPropertyValue<T>(Instance instance, Property property, T value, bool doSubmit = false)
        {
            SetPropertyValue(instance, property, value.ToString(), doSubmit);
        }

        #endregion
        #region find

        public List<Concept> FindConcept(string name)
        {
            List<Concept> result = new List<Concept>();

            var q = from row in ConceptNamesTable
                    where row.LanguageID == ActiveLanguageID && 
                        row.Value.ToUpper() == name.ToUpper()
                    select row;

            foreach (ConceptName n in q)
                result.Add(GetConcept(n.ConceptID));

            return result;
        }

        public List<Instance> FindInstance(Property property, string value)
        {
            List<Instance> result = new List<Instance>();

            var q = from row in InstanceDataTable
                    where row.PropertyID == property.ID &&
                          row.LiteralValue == value
                    select row.InstanceID;

            if (!q.Any()) return result;

            foreach (int i in q.Distinct())
                result.Add(GetInstance(i));

            return result;
        }

        public List<Instance> FindInstance(Concept c, Property property, string value)
        {
            List<Instance> result = new List<Instance>();

            var q = from row in InstanceDataTable
                    where row.PropertyID == property.ID &&
                            row.LiteralValue == value
                    select row.InstanceID;

            if (!q.Any()) return result;

            foreach (int i in q.Distinct())
            {
                Instance ii = GetInstance(i);
                if (ii.ConceptID == c.ID) result.Add(ii);
            }

            return result;
        }

        public List<Instance> FindInstance(Concept c, Property property, string value, int maxDistance)
        {
            List<Instance> result = new List<Instance>();

            var q = from row in InstanceDataTable
                    where row.PropertyID == property.ID
                    select row.InstanceID;

            if (!q.Any()) return result;

            foreach (int i in q.Distinct())
            {
                Instance ii = GetInstance(i);
                if (ii.ConceptID == c.ID &&
                    ii.Get<string>(property).DistanceTo(value) <= maxDistance)
                    result.Add(ii);
            }

            return result;
        }

        /// <summary>
        /// Find instances to which the relation points. (Related instances)
        /// </summary>
        /// <param name="source"></param>
        /// <param name="relation"></param>
        /// <returns></returns>
        public List<Instance> FindInstance(Instance source, Relation relation)
        {
            List<Instance> result = new List<Instance>();

            var q = from row in InstanceRelationTable
                    where row.InstanceID == source.ID &&
                            row.RelationID == relation.ID
                    select row.TargetInstanceID;

            if (!q.Any()) return result;

            foreach (int i in q.Distinct())
            {
                Instance ii = GetInstance(i);
                // if (ii.ConceptID == c.ID) 
                result.Add(ii);
            }

            return result;
        }

        /// <summary>
        /// Find instances which have the relation to the given target. (Reflexively related instances)
        /// </summary>
        /// <param name="target"></param>
        /// <param name="relation"></param>
        /// <returns></returns>
        public List<Instance> FindInstanceReflexive(Instance target, Relation relation)
        {
            List<Instance> result = new List<Instance>();

            var q = from row in InstanceRelationTable
                    where row.TargetInstanceID == target.ID &&
                            row.RelationID == relation.ID
                    select row.InstanceID;

            if (!q.Any()) return result;

            foreach (int i in q.Distinct())
            {
                Instance ii = GetInstance(i);
                // if (ii.ConceptID == c.ID) 
                result.Add(ii);
            }

            return result;
        }

        /// <summary>
        /// Compares using Levenshtein distance
        /// </summary>
        /// <param name="c"></param>
        /// <param name="property"></param>
        /// <param name="value"></param>
        /// <param name="maxNumberOfResults"></param>
        /// <returns></returns>
        public List<Instance> FindSimilarInstances(Concept c, Property property, string value, int maxDistance = 3, int maxNumberOfResults = 10)
        {
            int counter = 0;
            Dictionary<int, int> candidates = new Dictionary<int, int>();
            List<Instance> results = new List<Instance>();

            var q = from row in InstanceDataTable
                    where row.PropertyID == property.ID
                    select row;

            if (!q.Any()) return results;

            foreach (InstanceData data in q)
                candidates[data.InstanceID] = data.LiteralValue.DistanceTo(value);

            foreach (KeyValuePair<int, int> kvp in candidates.OrderBy(o => o.Value))
            {
                Instance i = GetInstance(kvp.Key);
                if (i.ConceptID == c.ID)
                {
                    counter += 1;
                    if (counter > maxNumberOfResults) break;
                    if (kvp.Value <= maxDistance) results.Add(i);
                }
            }

            return results;
        }

        /// <summary>
        /// Compares using Edit Script distance
        /// </summary>
        /// <param name="c"></param>
        /// <param name="property"></param>
        /// <param name="value"></param>
        /// <param name="maxNumberOfResults"></param>
        /// <returns></returns>
        public List<Instance> FindSimilarTexts(Concept c, Property property, string value, int maxNumberOfResults = 10)
        {
            int counter = 0;
            Dictionary<int, int> candidates = new Dictionary<int, int>();
            List<Instance> results = new List<Instance>();

            var q = from row in InstanceDataTable
                    where row.PropertyID == property.ID
                    select row;

            if (!q.Any()) return results;

            foreach (InstanceData data in q)
                candidates[data.InstanceID] = data.LiteralValue.DistanceTo(value, true);

            foreach (KeyValuePair<int, int> kvp in candidates.OrderBy(o => o.Value))
            {
                Instance i = GetInstance(kvp.Key);
                if (i.ConceptID == c.ID)
                {
                    counter += 1;
                    if (counter > maxNumberOfResults) break;
                    results.Add(i);
                }
            }

            return results;
        }

        public List<Property> FindProperty(Concept concept, string name)
        {
            return (from row in concept.Properties
                   where row.Name.ToUpper() == name.ToUpper()
                   select row).ToList();
        }

        public List<Relation> FindRelation(string name)
        {
            List<Relation> result = new List<Relation>();

            var q = from row in RelationNamesTable
                    where row.LanguageID == ActiveLanguageID && 
                        row.Value.ToUpper() == name.ToUpper()
                    select row;

            foreach (RelationName n in q)
                result.Add(GetRelation(n.RelationID));

            return result;
        }

        #endregion
        #region create

        public Concept CreateConcept(string name, Concept superConcept = null, bool doSubmit = true)
        {
            return CreateConcept(name, superConcept == null ? RootConcept.ID : superConcept.ID, doSubmit);
        }

        public Property CreateProperty<T>(string name, Concept container, bool doSubmit = true) 
        {
            return CreateProperty<T>(name, container.ID, doSubmit);
        }

        /// <summary>
        /// Create the actual relation (not a relation instance)
        /// </summary>
        /// <param name="name"></param>
        /// <param name="concept1"></param>
        /// <param name="cardinality"></param>
        /// <param name="concept2"></param>
        /// <param name="isDirected"></param>
        /// <param name="doSubmit"></param>
        /// <returns></returns>
        public Relation CreateRelation(string name, Concept concept1, Cardinality cardinality, Concept concept2, bool isDirected = true, bool doSubmit = true)
        {
            return CreateRelation(name, concept1.ID, cardinality, concept2.ID, isDirected, doSubmit);
        }

        /// <summary>
        /// Create a relation instance
        /// </summary>
        /// <param name="relation"></param>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="doSubmit"></param>
        /// <returns></returns>
        public bool CreateRelation(Relation relation, Instance source, Instance target, bool doSubmit = true)
        {
            return CreateRelation(relation.ID, source.ID, target.ID, doSubmit);
        }

        public Instance CreateInstance(Concept concept, bool doSubmit = true, int displayPropertyID = -1)
        {
            return CreateInstance(concept.ID, doSubmit, displayPropertyID);
        }

        public ConceptLayout CreateLayout(View view, Concept concept, System.Windows.Media.Color backColor, System.Windows.Media.Color foreColor, double x, double y, double width, double height, bool doSubmit = true)
        {
            ConceptLayout l = view.CreateConceptLayout(concept);
            l.BackgroundColor = backColor.ToString();
            l.ForegroundColor = foreColor.ToString();
            l.X = x;
            l.Y = y;
            l.Width = width;
            l.Height = height;
            if(doSubmit) SubmitChanges();
            return l;
        }

        #endregion

        #endregion
    }
}