using System.Windows;
using System.Windows.Controls;
using MatthiasToolbox.Presentation.Utilities;

namespace MatthiasToolbox.OntologyEditor.Controls
{
    /// <summary>
    /// Relation 
    /// </summary>
    public partial class RelationTemplateControl : UserControl
    {
        public RelationTemplateControl()
        {
            InitializeComponent();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            this.UpdateAllSources();
        }

        private void btnAddProperty_Click(object sender, RoutedEventArgs e)
        {
            //TODO  OntologyEditor - AddProperty
        }
    }
}
