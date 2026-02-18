using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;

using MatthiasToolbox.Presentation.Interfaces;
using MatthiasToolbox.Presentation;
using MatthiasToolbox.Semantics.Engine;
using MatthiasToolbox.OntologyEditor.Semantics;
using MatthiasToolbox.GraphDesigner;
using MatthiasToolbox.GraphDesigner.Events;
using MatthiasToolbox.Semantics.Interfaces;
using MatthiasToolbox.Basics.Datastructures.Graph;
using MatthiasToolbox.Semantics.Metamodel;

namespace Vr.LiBase.Editor
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Ontology onto = new Ontology("");

        public MainWindow()
        {
            InitializeComponent();

            graphControl1.HideDefaultPalettes();

            AddConceptPalette(); // add custom stencils (concept)
            AddRelationPalette(); // add custom stencils (relation)

            graphControl1.Graph = new OntologyGraph();
            graphControl1.OptionsDialogExecuted += new GraphControl.ShowOptionsHandler(graphControl1_OptionsDialogExecuted);

            graphControl1.VertexCreated += graphControl1_VertexCreated;
            // graphControl1.EdgeAdded += ...;

            // graphControl1.NodeChanged += ...;
            // graphControl1.EdgeChanged += ...;

            // graphControl1.NodeRemoved += ...;
            // graphControl1.EdgeRemoved += ...;
        }

        void graphControl1_OptionsDialogExecuted(object sender, OptionsDialogEventArgs e)
        {
            if (e.SelectedVertex != null)
            {
                MessageBox.Show("Options for " + e.SelectedVertex.Name);
            }
            else if (e.SelectedEdge != null)
            {
                if (e.SelectedEdge.IsDirected)
                    MessageBox.Show("Options for " + e.SelectedEdge.Name + "(" + e.SelectedEdge.Vertices[0] + "=>" + e.SelectedEdge.Vertices[1] + ")");
                else
                    MessageBox.Show("Options for " + e.SelectedEdge.Name + "(" + e.SelectedEdge.Vertices[0] + "<=>" + e.SelectedEdge.Vertices[1] + ")");
            }
        }

        private void graphControl1_VertexCreated(object sender, VertexCreationEventArgs e)
        {

        }

        private void AddRelationPalette()
        {
            List<IEdge<IConcept>> relationStencils = new List<IEdge<IConcept>>();

            for (int i = 0; i < 100; i++)
            {
                RelationEdge relation = new RelationEdge();
                relation.Name = String.Format("Test {0}", i);

                relationStencils.Add(relation);
            }

            graphControl1.AddEdgePalette<IConcept>("Relations", relationStencils);
        }

        private void AddConceptPalette()
        {
            List<IVertex2D> termStencils = new List<IVertex2D>();

            for (int i = 0; i < 100; i++)
            {
                ConceptVertex concept = new ConceptVertex(String.Format("Concept {0}", i));
                concept.BackgroundColor = Colors.SkyBlue;

                concept.Definition = string.Format("definition {0}", i);
                termStencils.Add(concept);
            }

            graphControl1.AddVertexPalette<IVertex2D>("Concepts", termStencils);
        }
    }
}