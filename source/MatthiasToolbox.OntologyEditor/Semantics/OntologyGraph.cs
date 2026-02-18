using System;
using System.Collections.Generic;
using MatthiasToolbox.Semantics.Engine;
using MatthiasToolbox.Basics.Datastructures.Graph;
using System.Windows;
using MatthiasToolbox.Semantics.Metamodel;

namespace MatthiasToolbox.OntologyEditor.Semantics
{
    public class OntologyGraph : IGraph2D<Point, ConceptVertex, RelationEdge>
    {
        #region over

        // TODO  Presentation - @dwi: implement overrides of adding and removing concepts

        #endregion
        #region cvar

        private Ontology onto;

        #endregion
        #region prop

        public Ontology Ontology { get { return onto; } }

        #region IGraph<ConceptVertex, RelationEdge>

        public IEnumerable<ConceptVertex> Vertices { get; set; }

        public IEnumerable<RelationEdge> Edges { get; set; }

        #endregion

        #endregion
        #region ctor

        public OntologyGraph(string connectionString = "DefaultOntology.sdf") { onto = new Ontology(connectionString, "Unnamed Ontology"); }

        public OntologyGraph(string name, string connectionString = "DefaultOntology.sdf") 
        {
            onto = new Ontology(connectionString, name);
        }

        public OntologyGraph(Ontology ontology)
        {
            onto = ontology;
        }

        #endregion
        #region impl

        public bool AddVertex(ConceptVertex vertex)
        {
            return this.Ontology.AddVertex(vertex);
        }

        public bool AddEdge(RelationEdge edge)
        {
            return this.Ontology.AddEdge(edge);
        }

        public bool Add(object child)
        {
            return this.Ontology.Add(child);
        }

        #endregion
    }
}