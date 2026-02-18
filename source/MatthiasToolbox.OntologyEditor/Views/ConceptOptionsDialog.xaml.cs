using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MatthiasToolbox.Semantics.Metamodel;
using MatthiasToolbox.Presentation.Utilities;
using System.Windows.Data;
using System.Reflection;
using System.Windows.Controls;

namespace MatthiasToolbox.OntologyEditor.Views
{
    
    /// <summary>
    /// Interaction logic for ConceptOptionsDialog.xaml
    /// </summary>
    public partial class ConceptOptionsDialog : Window
    {
        #region cvar

        private Concept _concept;
        private Ontology _ontology;

        #endregion

        public ConceptOptionsDialog(Ontology ontology, Concept concept)
        {
            _ontology = ontology;
            _concept = concept;
            this.DataContext = _concept;

            InitializeComponent();
        }


        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            bool isValid = this.UpdateAllSources();
            if (!isValid)
                return;

            this.DialogResult = true;
        }

        private void btnCreateInstance_Click(object sender, RoutedEventArgs e)
        {
            Property NameProperty = null;

            if (cbCreateInstanceName.IsChecked == true)
            {

                NameProperty = !_concept.HasProperty("Name") ? _ontology.CreateProperty<string>("Name", _concept) : _concept.GetProperty("Name");
            }

            Instance i = _ontology.CreateInstance(_concept);

            if (cbCreateInstanceName.IsChecked == true) 
                i.Set(NameProperty, txtInstanceName.Text, true);

            
            lbInstances.Items.Refresh();

            txtInstanceName.Text = "";
        }
    }
}
