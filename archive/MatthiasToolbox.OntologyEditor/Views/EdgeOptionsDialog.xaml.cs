using System;
using System.Reflection;
using System.Windows;
using MatthiasToolbox.GraphDesigner.Enumerations;
using MatthiasToolbox.Presentation.Utilities;

namespace MatthiasToolbox.OntologyEditor.Views
{
    /// <summary>
    /// Interaction logic for EdgeOptionsDialog.xaml
    /// </summary>
    public partial class EdgeOptionsDialog : Window
    {
        public EdgeOptionsDialog()
        {
            InitializeComponent();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            bool isValid = this.UpdateAllSources();
            if (!isValid)
                return;

            this.DialogResult = true;
        }
    }
}
