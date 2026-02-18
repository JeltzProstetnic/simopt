using System;
using MatthiasToolbox.GraphDesigner.DiagramDesigner;
using MatthiasToolbox.GraphDesigner.Enumerations;
using MatthiasToolbox.GraphDesigner.Factory;
using MatthiasToolbox.GraphDesigner.Interfaces;
using MatthiasToolbox.Basics.Datastructures.Graph;
using MatthiasToolbox.OntologyEditor.Converters;
using MatthiasToolbox.Semantics.Metamodel;
using System.Windows;
using MatthiasToolbox.Semantics.Metamodel.Layout;

namespace MatthiasToolbox.OntologyEditor.Semantics
{
    public class SemanticEdgeFactory : DefaultEdgeFactory, IEdgeFactory
    {
        Ontology _ontology;
        View _view;
        #region over

        public override IEdge<Point> CreateEdge(IEdge<Point> template, IVertex<Point> source, IVertex<Point> target)
        {
            RelationTemplate rt = template as RelationTemplate;
            
            Relation newRelation = _ontology.CreateRelation(rt.Name, source as Concept, rt.Cardinality, target as Concept, rt.IsDirected);
            newRelation.ViewContext = _view;

            RelationLayout l = _view.CreateRelationLayout(newRelation);
            l.LineColor = rt.BackgroundColor.ToString();
            l.TextColor = rt.ForegroundColor.ToString();

            _ontology.SubmitChanges();

            return newRelation;
        }

        protected override void UpdateConnectionFromEdge(Connection connection, IEdge<Point> edge, PathRouting routing)
        {
            base.UpdateConnectionFromEdge(connection, edge, routing);

            Relation relation = edge as Relation;
            connection.SourceArrowSymbol = (ArrowSymbol)Enum.ToObject(typeof(ArrowSymbol), relation.StartCaps);
            connection.SinkArrowSymbol = (ArrowSymbol)Enum.ToObject(typeof(ArrowSymbol), relation.EndCaps);

            connection.UpdateConnectionInfo();
        }

        #endregion
        #region cvar

        public SemanticEdgeFactory(Ontology ontology, View view)
        {
            this._ontology = ontology;
            this._view = view;
        }

        #endregion
    }
}