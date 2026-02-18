using MatthiasToolbox.GraphDesigner.Factory;
using MatthiasToolbox.GraphDesigner.Interfaces;
using MatthiasToolbox.Basics.Datastructures.Graph;
using MatthiasToolbox.OntologyEditor.Controls;
using MatthiasToolbox.Semantics.Metamodel;
using System.Windows;
using MatthiasToolbox.Semantics.Metamodel.Layout;

namespace MatthiasToolbox.OntologyEditor.Semantics
{
    public class SemanticNodeFactory : DefaultNodeFactory, INodeFactory
    {
        Ontology _ontology;
        View _view;

        public override IVertex<Point> CreateVertex(IVertex<Point> template, Point position, Size? size)
        {
            ConceptTemplate ct = template as ConceptTemplate;
            
            Concept newConcept = _ontology.CreateConcept(ct.Name, ct.BaseConcept);
            newConcept.ViewContext = _view;

            ConceptLayout l = _view.CreateConceptLayout(newConcept);
            l.BackgroundColor = ct.BackgroundColor.ToString();
            l.ForegroundColor = ct.ForegroundColor.ToString();
            l.X = position.X;
            l.Y = position.Y;

            if (size.HasValue)
            {
                l.Width = size.Value.Width;
                l.Height = size.Value.Height;
            }

            _ontology.SubmitChanges();

            return newConcept;
        }

        public override UIElement CreateUIElement(IVertex<Point> vertex, IVertex<Point> template)
        {
            ConceptControl nc = new ConceptControl(vertex as Concept);


            return nc;
        }

        public SemanticNodeFactory(Ontology ontology, View view) 
        {
            this._ontology = ontology;
            this._view = view;
        }
    }
}
