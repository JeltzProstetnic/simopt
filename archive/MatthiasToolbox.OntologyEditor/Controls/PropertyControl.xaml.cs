using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using MatthiasToolbox.Presentation.Utilities;
using MatthiasToolbox.Semantics.Interfaces;
using MatthiasToolbox.Semantics.Metamodel;

namespace MatthiasToolbox.OntologyEditor.Controls
{
    /// <summary>
    /// Interaction logic for PropertyControl.xaml
    /// </summary>
    public partial class PropertyControl : UserControl
    {

        private static readonly Type[] _types = {
                                                    typeof (bool),
                                                    typeof (char),
                                                    typeof (string),
                                                    typeof(int),
                                                    typeof(long),
                                                    typeof(float),
                                                    typeof(double),
                                                    typeof(decimal),
                                                    typeof(DateTime),
                                                    typeof(TimeSpan)
                                                };

        public IEnumerable<String> AllowedDataTypes
        {
            get { return _types.Select(t => t.FullName); }
        }

        public PropertyControl()
        {
            InitializeComponent();
        }

        private Concept Concept { get { return DataContext as Concept; } }
        
        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            Concept.DataContext.CreateProperty<string>(txtName.Text, Concept);

            //Reset displayed text
            txtName.Text = "";

            //Refresh the gridview
            dgProperties.Items.Refresh();
        }


    }
}
