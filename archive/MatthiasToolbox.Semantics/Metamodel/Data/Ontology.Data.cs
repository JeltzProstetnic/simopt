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
using System.Globalization;

namespace MatthiasToolbox.Semantics.Metamodel
{
    /// <summary>
    /// you have the ID -> get
    /// you have the Name -> lookup
    /// you have part of the Name -> find
    /// </summary>
    public partial class Ontology : DataContext
    {
        #region over

        public override void SubmitChanges(ConflictMode failureMode)
        {
            base.SubmitChanges(failureMode);

            if (_ontologyChangePending) _ontologyChanged = true;
            if (_conceptChangePending) _conceptChanged = true;
            if (_instanceChangePending) _instanceChanged = true;
            if (_instanceNumberChangePending) _instanceNumberChanged = true;

            _ontologyChangePending = false;
            _conceptChangePending = false;
            _instanceChangePending = false;
            _instanceNumberChangePending = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (base.Connection != null)
            {
                if (base.Connection.State != System.Data.ConnectionState.Closed)
                {
                    base.Connection.Close();
                    base.Connection.Dispose();
                }
            }
            base.Dispose(disposing);
        }
        
        #endregion
        #region cvar

        private Dictionary<Type, Func<string, object>> _stringParsers = new Dictionary<Type, Func<string, object>>();

        #region caches

        private Cache<int, string> _instanceCache = new Cache<int, string>(0, false);
        private Cache<int, string> _conceptCache = new Cache<int, string>(0, false);
        private Cache<int, string> _propertyCache = new Cache<int, string>(0, false);
        private Cache<int, string> _relationCache = new Cache<int, string>(0, false);

        #endregion
        #region connection

        private string connectionString;
        private bool isConnected;

        #endregion
        #region change flags

        private bool _ontologyChangePending = false; // added or removed concepts and relations
        private bool _conceptChangePending = false;
        private bool _instanceChangePending = false; // modified instances
        private bool _instanceNumberChangePending = false; // adding or removing instances
        private bool _ontologyChanged = false;
        private bool _conceptChanged = false;
        private bool _instanceChanged = false;
        private bool _instanceNumberChanged = false;

        #endregion

        #endregion
        #region prop

        public string Name { get; set; }

        public bool IsInitialized { get; private set; }
        public string ConnectionString { get { return connectionString; } }
        public bool IsConnected { get { return isConnected; } }

        public int ActiveLanguageID { get; set; }

        public Concept RootConcept { get; private set; }

        #region tables

        public readonly Table<View> ViewTable;

        public readonly Table<Concept> ConceptTable;
        public readonly Table<ConceptName> ConceptNamesTable;
        public readonly Table<ConceptLayout> ConceptLayoutTable;

        public readonly Table<Instance> InstanceTable;
        public readonly Table<InstanceData> InstanceDataTable;
        public readonly Table<InstanceLayout> InstanceLayoutTable;
        public readonly Table<InstanceRelation> InstanceRelationTable;

        public readonly Table<Relation> RelationTable;
        public readonly Table<RelationName> RelationNamesTable;
        public readonly Table<RelationLayout> RelationLayoutTable;
        
        public readonly Table<Property> PropertyTable;
        public readonly Table<PropertyName> PropertyNamesTable;
        public readonly Table<PropertyLayout> PropertyLayoutTable;
        
        #endregion

        #endregion
        #region ctor

        public Ontology(string connectionString, string name)
            : base(connectionString)
        {
            this.Name = name;
            this.connectionString = connectionString;
            this.isConnected = true;

            ActiveLanguageID = 1;

            this.Log<INFO>("Connected to " + base.Connection.DataSource);

            _stringParsers[typeof(bool)] = s => bool.Parse(s);
            _stringParsers[typeof(char)] = s => char.Parse(s);
            _stringParsers[typeof(string)] = s => s;
            _stringParsers[typeof(int)] = s => int.Parse(s);
            _stringParsers[typeof(long)] = s => long.Parse(s);
            _stringParsers[typeof(float)] = s => float.Parse(s);
            _stringParsers[typeof(double)] = s => double.Parse(s);
            _stringParsers[typeof(decimal)] = s => decimal.Parse(s);
            _stringParsers[typeof(DateTime)] = s => DateTime.Parse(s, CultureInfo.CreateSpecificCulture("de-DE"));
            _stringParsers[typeof(TimeSpan)] = s => TimeSpan.Parse(s);
        }

        #endregion
        #region init

        /// <summary>
        /// Initialize this instance.
        /// </summary>
        /// <returns>success flag</returns>
        public bool Initialize(bool resetDB = false, bool readUncommitted = false, Action<Ontology> onCreate = null)
        {
            #region checks

            if (IsInitialized)
            {
#if DEBUG
                this.Log<WARN>("The OntologyDatabase was already initialized.");
#endif
                return true;
            }

            try
            {
                bool b = DatabaseExists();
            }
            catch (Exception e)
            {
                this.Log<FATAL>("Error opening DB connection: ", e);
                return false;
            }

            try
            {
                if (resetDB && DatabaseExists()) DeleteDatabase();
            }
            catch (Exception e)
            {
                this.Log<ERROR>("Error resetting the database: ", e);
                return false;
            }

            try
            {
                if (!DatabaseExists())
                {
                    DoCreateDatabase();
                    if (onCreate != null) onCreate.Invoke(this);
                }
            }
            catch (Exception e)
            {
                this.Log<FATAL>("Error creating a new database: ", e);
                return false;
            }

            try
            {
                SubmitChanges();
            }
            catch (Exception e)
            {
                this.Log<FATAL>("Error while trying to submit data: ", e);
                return false;
            }

            #endregion

            if (readUncommitted) ExecuteCommand("SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;");

            IsInitialized = true;

            this.Log<INFO>("OntologyDatabase ready.");

            CreateLookups();

            this.Log<INFO>("Lookup dictionaries created.");

            return true;
        }

        private void DoCreateDatabase()
        {
            CreateDatabase();
            CreateDefaultData();
        }

        private void CreateDefaultData()
        {
            ViewTable.InsertOnSubmit(new View("Default View"));
            RootConcept = CreateConcept("Thing", 1);
        }

        private void CreateLookups() 
        {
            if (RootConcept == null)
            {
                RootConcept = GetConcept(1);
            }

            foreach (Concept concept in ConceptTable)
            {
                _conceptCache.Put<Concept>(concept.Name, concept);
                foreach (Property property in concept.Properties)
                {
                    _conceptCache.Put<Property>(concept.ID, property.Name, property);
                }
            }

            foreach (Relation relation in RelationTable)
            {
                _relationCache.Put<Relation>(relation.Name, relation);
            }
        }

        #endregion
        #region impl

        #region add

        private bool AddRelation(Relation relation, bool doSubmit = false)
        {
            if (relation.ID != 0) return false;
            RelationTable.InsertOnSubmit(relation);

            if (doSubmit)
            {
                SubmitChanges();
                _ontologyChanged = true;
            }
            else
                _ontologyChangePending = true;

            return true;
        }

        private bool AddConcept(Concept concept, bool doSubmit = false)
        {
            if (concept.ID != 0) return false;
            ConceptTable.InsertOnSubmit(concept);

            if (doSubmit)
            {
                SubmitChanges();
                _ontologyChanged = true;
            }
            else
                _ontologyChangePending = true;

            return true;
        }

        private bool AddProperty(Property property, bool doSubmit = false)
        {
            if (property.ID != 0) return false;
            PropertyTable.InsertOnSubmit(property);

            if (doSubmit)
            {
                SubmitChanges();
                _conceptChanged = true;
            }
            else
                _conceptChangePending = true;

            return true;
        }

        private bool AddInstance(Instance instance, bool doSubmit = false)
        {
            if (instance.ID != 0) return false;
            InstanceTable.InsertOnSubmit(instance);

            if (doSubmit)
            {
                SubmitChanges();
                _instanceNumberChanged = true;
            }
            else
            {
                _instanceNumberChangePending = true;
            }

            return true;
        }

        #endregion
        #region get

        #region concepts

        internal IEnumerable<Concept> GetChildren(int ID)
        {
            throw new NotImplementedException(); // TODO: retrieve children
        }

        internal IEnumerable<string> GetPreferredConceptNames(int ID)
        {
            var q = from row in ConceptNamesTable
                    where row.ConceptID == ID && row.IsPreferredName
                    select row;

            foreach (ConceptName n in q)
            {
                yield return n.Value;
            }
        }

        internal IEnumerable<string> GetAllConceptNames(int ID)
        {
            var q = from row in ConceptNamesTable
                    where row.ConceptID == ID
                    select row;

            foreach (ConceptName n in q)
            {
                yield return n.Value;
            }
        }

        internal IEnumerable<string> GetConceptNames(int ID)
        {
            var q = from row in ConceptNamesTable
                    where row.ConceptID == ID && row.LanguageID == ActiveLanguageID
                    select row;

            foreach (ConceptName n in q)
            {
                yield return n.Value;
            }
        }

        internal string GetConceptName(int conceptID)
        {
            string result;

            if (!_conceptCache.TryGet<string>(conceptID, "Name", out result))
            {
                var q = from row in ConceptNamesTable
                        where row.ConceptID == conceptID && row.LanguageID == ActiveLanguageID // TODO: && row.IsPreferredName
                        select row.Value;

                if (!q.Any()) return "Concept " + conceptID.ToString(); // skip caching

                result = q.First();
                _conceptCache.Put(conceptID, "Name", result);
            }

            return result;
        }

        internal ConceptName GetConceptName(Concept concept)
        {
            var q = from row in ConceptNamesTable
                    where row.ConceptID == concept.ID && row.LanguageID == ActiveLanguageID // TODO: && row.IsPreferredName
                    select row;

            if (!q.Any()) return null;

            return q.First();
        }

        #endregion
        #region relations

        internal RelationName GetRelationName(Relation relation)
        {
            var q = from row in RelationNamesTable
                    where row.RelationID == relation.ID && row.LanguageID == ActiveLanguageID
                    select row;

            if (!q.Any()) return null;

            return q.First();
        }

        internal IEnumerable<string> GetRelationNames(int ID)
        {
            var q = from row in RelationNamesTable
                    where row.RelationID == ID && row.LanguageID == ActiveLanguageID
                    select row;

            foreach (RelationName n in q)
                yield return n.Value;
        }

        internal IEnumerable<string> GetAllRelationNames(int ID)
        {
            var q = from row in RelationNamesTable
                    where row.RelationID == ID
                    select row;

            foreach (RelationName n in q)
                yield return n.Value;
        }

        internal IEnumerable<string> GetPreferredRelationNames(int ID)
        {
            var q = from row in RelationNamesTable
                    where row.RelationID == ID && row.IsPreferredName
                    select row;

            foreach (RelationName n in q)
                yield return n.Value;
        }

        internal string GetRelationName(int relationID)
        {
            string result;

            if (!_relationCache.TryGet<string>(relationID, "Name", out result))
            {
                var q = from row in RelationNamesTable
                        where row.RelationID == relationID && row.LanguageID == ActiveLanguageID // TODO: && row.IsPreferredName
                        select row.Value;

                if (!q.Any()) return "Relation " + relationID.ToString(); // skip caching

                result = q.First();
                _relationCache.Put(relationID, "Name", result);
            }

            return result;
        }

        #endregion
        #region instances

        internal List<Instance> GetInstances(int conceptID)
        {
            List<Instance> result;

            if (!_conceptCache.TryGet<List<Instance>>(conceptID, "_Instances_", out result))
            {
                var q = from row in InstanceTable
                        where row.ConceptID == conceptID
                        select row;

                if (!q.Any()) return new List<Instance>(); // skip caching

                result = q.ToList();
                _conceptCache.Put(conceptID, "_Instances_", result);
            }

            return result;
        }

        #endregion
        #region properties

        #region get name(s)

        internal string GetPropertyName(int propertyID)
        {
            string result;

            if (!_propertyCache.TryGet<string>(propertyID, "Name", out result))
            {
                var q = from row in PropertyNamesTable
                        where row.PropertyID == propertyID && row.LanguageID == ActiveLanguageID// TODO: && row.IsPreferredName
                        select row.Value;

                if (!q.Any()) return "Property " + propertyID.ToString(); // skip caching

                result = q.First();
                _propertyCache.Put(propertyID, "Name", result);
            }

            return result;
        }

        internal PropertyName GetPropertyName(Property property)
        {
            var q = from row in PropertyNamesTable
                    where row.PropertyID == property.ID && row.LanguageID == ActiveLanguageID// TODO: && row.IsPreferredName
                    select row;

            if (!q.Any()) return null;

            return q.First();
        }

        internal IEnumerable<string> GetPropertyNames(int ID)
        {
            var q = from row in PropertyNamesTable
                    where row.PropertyID == ID && row.LanguageID == ActiveLanguageID
                    select row;

            foreach (PropertyName n in q)
                yield return n.Value;
        }

        internal IEnumerable<string> GetAllPropertyNames(int ID)
        {
            var q = from row in PropertyNamesTable
                    where row.PropertyID == ID
                    select row;

            foreach (PropertyName n in q)
                yield return n.Value;
        }

        internal IEnumerable<string> GetPreferredPropertyNames(int ID)
        {
            var q = from row in PropertyNamesTable
                    where row.PropertyID == ID && row.IsPreferredName
                    select row;

            foreach (PropertyName n in q)
                yield return n.Value;
        }

        #endregion
        #region get props from concept

        /// <summary>
        /// returns only direct properties
        /// </summary>
        /// <param name="conceptID"></param>
        /// <returns></returns>
        internal List<Property> GetProperties(int conceptID) { return GetDirectProperties(conceptID); }

        /// <summary>
        /// returns only direct properties
        /// </summary>
        /// <param name="conceptID"></param>
        /// <returns></returns>
        internal List<Property> GetDirectProperties(int conceptID)
        {
            List<Property> result;

            if (!_conceptCache.TryGet<List<Property>>(conceptID, "_Properties_", out result))
            {
                var q = from row in PropertyTable
                        where row.IsConceptProperty && row.ConceptOrRelationID == conceptID
                        select row;

                if (!q.Any()) return new List<Property>(); // skip caching

                result = q.ToList();
                _conceptCache.Put(conceptID, "_Properties_", result);
            }

            return result;
        }

        /// <summary>
        /// includes inherited properties
        /// </summary>
        /// <param name="conceptID"></param>
        /// <returns></returns>
        internal IEnumerable<Property> GetAllProperties(Concept c)
        {
            foreach (Property p in GetDirectProperties(c.ID))
            {
                yield return p;
            }

            if (c == RootConcept) yield break;

            foreach (Property p in GetAllProperties(c.Parent))
            {
                yield return p;
            }
        }

        #endregion
        #region values

        internal T GetPropertyValue<T>(int instanceID, int propertyID)
        {
            T result;
            string resultString;

            if (!_instanceCache.TryGet<T>(instanceID, "Property " + propertyID.ToString(), out result))
            {
                var q = from row in InstanceDataTable
                        where row.InstanceID == instanceID &&
                            row.PropertyID == propertyID &&
                            row.LanguageID == ActiveLanguageID
                        select row.LiteralValue;

                if (!q.Any()) return default(T); // skip caching

                resultString = q.First();

                result = (T)(_stringParsers[typeof(T)].Invoke(resultString));

                _instanceCache.Put(instanceID, "Property " + propertyID.ToString(), result);
            }

            return result;
        }

        #endregion

        #endregion

        #endregion
        #region set

        internal void SetConceptName(int conceptID, string value, ConceptName n)
        {
            if (n != null) n.Value = value;
            else
            {
                n = new ConceptName();
                n.LanguageID = ActiveLanguageID;
                n.ConceptID = conceptID;
                n.Value = value;
                ConceptNamesTable.InsertOnSubmit(n);
            }

            SubmitChanges();
            
            _conceptCache.Put(conceptID, "Name", value);
        }

        internal void SetRelationName(int relationID, string value, RelationName n)
        {
            if (n != null) n.Value = value;
            else
            {
                n = new RelationName();
                n.LanguageID = ActiveLanguageID;
                n.RelationID = relationID;
                n.Value = value;
                RelationNamesTable.InsertOnSubmit(n);
            }

            SubmitChanges();

            _relationCache.Put(relationID, "Name", value);
        }

        internal void SetPropertyName(int propertyID, string value, PropertyName n)
        {
            if (n != null) n.Value = value;
            else
            {
                n = new PropertyName();
                n.LanguageID = ActiveLanguageID;
                n.PropertyID = propertyID;
                n.Value = value;
                PropertyNamesTable.InsertOnSubmit(n);
            }

            SubmitChanges();

            _propertyCache.Put(propertyID, "Name", value);
        }

        internal void SetPropertyValue(Instance instance, Property property, string value, bool doSubmit = false)
        {
            InstanceData data = new InstanceData();
            data.InstanceID = instance.ID;
            data.LanguageID = ActiveLanguageID;
            data.PropertyID = property.ID;
            data.LiteralValue = value;

            InstanceDataTable.InsertOnSubmit(data);

            if (doSubmit) 
            {
                SubmitChanges();
                _instanceCache.Put(data.ID, "Property " + property.ID.ToString(), value);
            }
        }

        internal void SetPropertyValue(int instanceID, int propertyID, string value, bool doSubmit = false)
        {
            InstanceData data = new InstanceData();
            data.InstanceID = instanceID;
            data.LanguageID = ActiveLanguageID;
            data.PropertyID = propertyID;
            data.LiteralValue = value;

            InstanceDataTable.InsertOnSubmit(data);

            if (doSubmit)
            {
                SubmitChanges();
                _instanceCache.Put(data.ID, "Property " + propertyID.ToString(), value);
            }
        }

        #endregion
        #region ctrl

        public void Close()
        {
            Connection.Close();
        }

        #endregion
        #region create

        private Concept CreateConcept(string name, int superConceptID = -1, bool doSubmit = true)
        {
            int superID = superConceptID > 0 ? superConceptID : RootConcept.ID;

            Concept result = new Concept();
            result.SuperConceptID = superID;
            AddConcept(result, true);

            ConceptName cname = new ConceptName();
            cname.ConceptID = result.ID;
            cname.LanguageID = ActiveLanguageID;
            cname.Value = name;
            ConceptNamesTable.InsertOnSubmit(cname);

            if(doSubmit) SubmitChanges();

            return result;
        }

        private Property CreateProperty<T>(string name, int conceptID, bool doSubmit = true)
        {
            Property result = new Property();
            result.ConceptOrRelationID = conceptID;
            result.IsConceptProperty = true;
            result.DataType = typeof(T).FullName;
            AddProperty(result, true);

            PropertyName pname = new PropertyName();
            pname.LanguageID = ActiveLanguageID;
            pname.PropertyID = result.ID;
            pname.Value = name;

            PropertyNamesTable.InsertOnSubmit(pname);
            if (doSubmit) SubmitChanges();

            _conceptCache.Put<Property>(conceptID, name, result);

            //update the properties cache for the container if it exists
            List<Property> properties;
            if (_conceptCache.TryGet<List<Property>>(conceptID, "_Properties_", out properties))
            {
                properties.Add(result);
            }

            return result;
        }
        
        /// <summary>
        /// Create the actual relation (not a relation instance)
        /// </summary>
        /// <param name="name"></param>
        /// <param name="conceptID1"></param>
        /// <param name="cardinality"></param>
        /// <param name="conceptID2"></param>
        /// <param name="isDirected"></param>
        /// <param name="doSubmit"></param>
        /// <returns></returns>
        private Relation CreateRelation(string name, int conceptID1, Cardinality cardinality, int conceptID2, bool isDirected = true, bool doSubmit = true)
        {
            Relation result = new Relation();
            result.ConceptID1 = conceptID1;
            result.ConceptID2 = conceptID2;
            result.Cardinality = cardinality;
            result.IsDirected = isDirected;
            AddRelation(result, true);

            RelationName rname = new RelationName();
            rname.RelationID = result.ID;
            rname.LanguageID = ActiveLanguageID;
            rname.Value = name;
            RelationNamesTable.InsertOnSubmit(rname);

            SubmitChanges();

            return result;
        }

        /// <summary>
        /// Create a relation instance
        /// </summary>
        /// <param name="relationID"></param>
        /// <param name="sourceID"></param>
        /// <param name="targetID"></param>
        /// <param name="doSubmitChanges"></param>
        /// <returns></returns>
        private bool CreateRelation(int relationID, int sourceID, int targetID, bool doSubmitChanges = false)
        {
            // TODO: check cardinalities (multiple entries allowed?)
            InstanceRelation r = new InstanceRelation();
            r.RelationID = relationID;
            r.InstanceID = sourceID;
            r.TargetInstanceID = targetID;

            InstanceRelationTable.InsertOnSubmit(r);
            if (doSubmitChanges) SubmitChanges();
          
            return true;
        }

        private Instance CreateInstance(int conceptID, bool doSubmit = true, int displayPropertyID = -1)
        {
            Instance result = new Instance();
            result.ConceptID = conceptID;
            result.DisplayPropertyID = displayPropertyID;

            AddInstance(result, doSubmit);

            List<Instance> instanceList;
            if(_conceptCache.TryGet<List<Instance>>(conceptID, "_Instances_", out instanceList))
            {
                instanceList.Add(result);
            } 
            else
            {
                _conceptCache.Put(conceptID, "_Instances_", new List<Instance> { result });
            }

            return result;
        }
        
        #endregion
        #region lookup

        public Concept LookupConcept(string name)
        {
            return _conceptCache.Get<Concept>(name, RootConcept);
        }

        public Relation LookupRelation(string name)
        {
            return _relationCache.Get<Relation>(name, (Relation)null);
        }

        public Property LookupProperty(Concept concept, string name)
        {
            Property result = null;
            Concept parent = concept;
            while ((result = _conceptCache.Get<Property>(parent.ID, name, (Property)null)) == null)
            {
                if (parent.ID == 1) return result;
                parent = GetConcept(parent.SuperConceptID);
            }
            return result;
        }

        #endregion

        #endregion
    }
}