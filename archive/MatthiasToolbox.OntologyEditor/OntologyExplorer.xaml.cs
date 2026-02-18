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
using System.Windows.Shapes;
using MatthiasToolbox.Semantics.Metamodel;
using MatthiasToolbox.Semantics.Utilities;

namespace MatthiasToolbox.OntologyEditor
{
    /// <summary>
    /// Interaction logic for OntologyExplorer.xaml
    /// </summary>
    public partial class OntologyExplorer : Window
    {
        private Ontology onto;
        private Property TextProperty;

        public OntologyExplorer()
        {
            InitializeComponent();

            onto = new Ontology("TestOntology.sdf", "Default Ontology");
            onto.Initialize(true);

            Relation TopicRelation = onto.CreateRelation("TopicRelation", onto.RootConcept, Cardinality.AnyToAny, onto.RootConcept, true);
            Property TitleProperty = onto.CreateProperty<string>("Title", onto.RootConcept);
            TextProperty = onto.CreateProperty<string>("Text", onto.RootConcept);

            Concept CGrundsaule = onto.CreateConcept("Grundsäule", onto.RootConcept);
            Concept CBegehung = onto.CreateConcept("Begehung", onto.RootConcept);

            Instance GrundSaule = onto.CreateInstance(CGrundsaule);
            GrundSaule.DisplayPropertyID = TitleProperty.ID;
            GrundSaule.Set(TextProperty, "Grundsäule Blabla blabla bla.");

            Instance Begehung = onto.CreateInstance(CBegehung);
            GrundSaule.DisplayPropertyID = TitleProperty.ID;
            GrundSaule.Set(TextProperty, "Begehung Blabla blabla bla.");

            onto.CreateRelation(TopicRelation, GrundSaule, Begehung);
        }

        private void buttonSearch_Click(object sender, RoutedEventArgs e)
        {
            string SuchText = textBoxSearch.Text;

            listBoxResults.Items.Clear();

            List<Concept> cresults = new List<Concept>();
            foreach (string word in SuchText.Split(' '))
            {
                cresults.AddRange(onto.FindConcept(word));
            }

            List<Instance> iresults = new List<Instance>();
            foreach (Concept c in cresults)
            {
                iresults.AddRange(c.Instances);
            }

            foreach (Instance i in iresults)
                listBoxResults.Items.Add(i.Get<string>(TextProperty));
                // doesn't work: listBoxResults.Items.Add(i.Get<string>("Text"));
        }

        private void buttonSubmit_Click(object sender, RoutedEventArgs e)
        {
            dataGridAnnotation.Items.Clear();
            textBoxTitle.Text = "";
            textBoxText.Clear();
        }

        private void buttonAnnotate_Click(object sender, RoutedEventArgs e)
        {
            string SuchText = textBoxText.Text;

            List<Concept> cresults = new List<Concept>();
            foreach (string word in SuchText.Split(' '))
            {
                cresults.AddRange(onto.FindConcept(word));
            }

            // List<Tuple<bool, Concept>> anno = new List<Tuple<bool,Concept>>();

            foreach (Concept c in cresults)
            {
                // anno.Add(new Tuple<bool,Concept>(true, c));
                dataGridAnnotation.Items.Add(c.Name);
            }

            // dataGridAnnotation.Columns.Clear();
            // dataGridAnnotation.DataContext = anno;
        }
    }
}
