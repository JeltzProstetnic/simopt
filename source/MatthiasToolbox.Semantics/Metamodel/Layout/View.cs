using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;
using MatthiasToolbox.Basics.Interfaces;
using MatthiasToolbox.Basics.Datastructures.Graph;
using MatthiasToolbox.Semantics.Interfaces;
using MatthiasToolbox.Semantics.Metamodel.Data;
using MatthiasToolbox.Logging;
using System.Windows;
using MatthiasToolbox.Semantics.Metamodel.Layout;

namespace MatthiasToolbox.Semantics.Metamodel.Layout
{
    [Table(Name = "tblViews")]
    public class View : ILINQTable, IGraph<Point>
    {
        #region cvar

        private bool _conceptLayoutChanged = true;
        private Dictionary<Concept, ConceptLayout> _conceptLayouts = new Dictionary<Concept, ConceptLayout>();

        private bool _relationLayoutChanged = true;
        private Dictionary<Relation, RelationLayout> _relationLayouts = new Dictionary<Relation, RelationLayout>();

        private bool _instanceLayoutChanged = true;
        private Dictionary<Instance, InstanceLayout> _instanceLayouts = new Dictionary<Instance, InstanceLayout>();

        private bool _propertyLayoutChanged = true;
        private Dictionary<Property, PropertyLayout> _propertyLayouts = new Dictionary<Property, PropertyLayout>();

        #endregion
        #region data

        /// <summary>
        /// Unique, auto-generated identifier.
        /// </summary>
        [Column(AutoSync = AutoSync.OnInsert,
                IsPrimaryKey = true,
                IsDbGenerated = true)]
        public int ID { get; private set; }

        [Column]
        public string Name { get; set; }

        #endregion
        #region prop

        public Ontology DataContext { get { return this.GetOntology(); } }

        public Dictionary<Concept, ConceptLayout> ConceptLayouts
        {
            get
            {
                if (!_conceptLayoutChanged) return _conceptLayouts;
                else
                {
                    if (ID != 0 && DataContext != null)
                    {
                        var q = from row in DataContext.ConceptLayoutTable
                                where row.ViewID == ID
                                select row;

                        if (q.Any())
                        {
                            _conceptLayoutChanged = false;
                            _conceptLayouts = q.ToDictionary(l => DataContext.GetConcept(l.ConceptID));
                        }
                    }
                    return _conceptLayouts;
                }
            }
        }

        public Dictionary<Instance, InstanceLayout> InstanceLayouts
        {
            get
            {
                if (!_instanceLayoutChanged) return _instanceLayouts;
                else
                {
                    if (ID != 0 && DataContext != null)
                    {
                        var q = from row in DataContext.InstanceLayoutTable
                                where row.ViewID == ID
                                select row;

                        if (q.Any())
                        {
                            _instanceLayoutChanged = false;
                            _instanceLayouts = q.ToDictionary(l => DataContext.GetInstance(l.InstanceID));
                        }
                    }
                    return _instanceLayouts;
                }
            }
        }

        public Dictionary<Relation, RelationLayout> RelationLayouts
        {
            get
            {
                if (!_relationLayoutChanged) return _relationLayouts;
                else
                {
                    if (ID != 0 && DataContext != null)
                    {
                        var q = from row in DataContext.RelationLayoutTable
                                where row.ViewID == ID
                                select row;

                        if (q.Any())
                        {
                            _relationLayoutChanged = false;
                            _relationLayouts = q.ToDictionary(l => DataContext.GetRelation(l.RelationID));
                        }
                    }
                    return _relationLayouts;
                }
            }
        }

        public Dictionary<Property, PropertyLayout> PropertyLayouts
        {
            get
            {
                if (!_propertyLayoutChanged) return _propertyLayouts;
                else
                {
                    if (ID != 0 && DataContext != null)
                    {
                        var q = from row in DataContext.PropertyLayoutTable
                                where row.ViewID == ID
                                select row;

                        if (q.Any())
                        {
                            _propertyLayoutChanged = false;
                            _propertyLayouts = q.ToDictionary(l => DataContext.GetProperty(l.PropertyID));
                        }
                    }
                    return _propertyLayouts;
                }
            }
        }

        public IEnumerable<Concept> Concepts { get { return ConceptLayouts.Keys; } }

        public IEnumerable<Instance> Instances { get { return InstanceLayouts.Keys; } }

        public IEnumerable<Relation> Relations { get { return RelationLayouts.Keys; } }

        public IEnumerable<Property> Properties { get { return PropertyLayouts.Keys; } }

        #region IIdentifiable<int>

        public int Identifier
        {
            get { return ID; }
        }

        #endregion

        #endregion
        #region ctor

        public View() { }

        public View(string name) { this.Name = name; }

        #endregion
        #region impl

        public void Invalidate()
        {
            _conceptLayoutChanged = true;
            _relationLayoutChanged = true;
            _instanceLayoutChanged = true;
            _propertyLayoutChanged = true;
        }

        #region create
        
        public ConceptLayout CreateConceptLayout(IVertex<Point> vertex)
        {
            ConceptLayout l = new ConceptLayout(this, vertex as Concept);
            DataContext.ConceptLayoutTable.InsertOnSubmit(l);
            ConceptLayouts[vertex as Concept] = l;
            return l;
        }

        public ConceptLayout CreateConceptLayout(Concept vertex)
        {
            ConceptLayout l = new ConceptLayout(this, vertex);
            DataContext.ConceptLayoutTable.InsertOnSubmit(l);
            ConceptLayouts[vertex] = l;
            return l;
        }

        public RelationLayout CreateRelationLayout(IEdge<Point> edge)
        {
            RelationLayout l = new RelationLayout(this, edge as Relation);
            DataContext.RelationLayoutTable.InsertOnSubmit(l);
            RelationLayouts[edge as Relation] = l;
            return l;
        }

        public RelationLayout CreateRelationLayout(Relation edge)
        {
            RelationLayout l = new RelationLayout(this, edge);
            DataContext.RelationLayoutTable.InsertOnSubmit(l);
            RelationLayouts[edge] = l;
            return l;
        }

        #endregion
        #region IGraph

        public IEnumerable<IVertex<Point>> Vertices
        {
            get
            {
                foreach (Concept c in Concepts) yield return c;
                foreach (Instance i in Instances) yield return i;
            }
        }

        public IEnumerable<IEdge<Point>> Edges
        {
            get
            {
                foreach (Relation r in Relations) yield return r;
            }
        }

        public bool AddVertex(IVertex<Point> vertex)
        {
            if (vertex is Concept)
            {
                //ConceptLayout l = new ConceptLayout(this, vertex as Concept);
                //DataContext.ConceptLayoutTable.InsertOnSubmit(l);
                //ConceptLayouts[vertex as Concept] = l;
                //DataContext.SubmitChanges();
                return true;
            }
            else if (vertex is Instance)
            {
                InstanceLayout l = new InstanceLayout(this, vertex as Instance);
                DataContext.InstanceLayoutTable.InsertOnSubmit(l);
                InstanceLayouts[vertex as Instance] = l;
                DataContext.SubmitChanges();
                return true;
            }
            else
            {
                this.Log<WARN>("An unknown type of graph element was added to the view: " + vertex.Name);
                return false;
            }
        }

        public bool AddEdge(IEdge<Point> edge)
        {
            if (edge is Relation)
            {
                //RelationLayout l = new RelationLayout(this, edge as Relation);
                //DataContext.RelationLayoutTable.InsertOnSubmit(l);
                //RelationLayouts[edge as Relation] = l;
                //DataContext.SubmitChanges();
                return true;
            }
            else
            {
                this.Log<WARN>("An unknown type of graph element was added to the view: " + edge.Name);
                return false;
            }
        }

        public bool RemoveVertex(IVertex<Point> vertex)
        {
            throw new NotImplementedException();
        }

        public bool RemoveEdge(IEdge<Point> edge)
        {
            throw new NotImplementedException();
        }

        #endregion
        #region INotifyPropertyChanging

        public event System.ComponentModel.PropertyChangingEventHandler PropertyChanging;

        #endregion

        #endregion
    }
}