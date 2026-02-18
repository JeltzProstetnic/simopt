using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using MatthiasToolbox.OntologyEditor.Semantics;
using MatthiasToolbox.Semantics.Metamodel;
using MatthiasToolbox.GraphDesigner.Interfaces;
using MatthiasToolbox.Semantics.Metamodel.Layout;

namespace MatthiasToolbox.OntologyEditor.Views
{
    /// <summary>
    /// Interaction logic for RelationTemplateDialog.xaml
    /// </summary>
    public partial class RelationTemplateDialog : Window
    {
        #region cvar

        private View _view;

        #endregion
        #region prop

        public ObservableCollection<RelationTemplate> RelationTemplates { get; set; }
        public IPalette Palette { get; set; }

        #endregion
        #region ctor

        public RelationTemplateDialog(View view, IPalette palette)
        {
            this._view = view;
            this.Title = "Palette Options: " + palette.Title;
            this.Palette = palette;
            this.RelationTemplates = new ObservableCollection<RelationTemplate>(palette.ItemsSource.OfType<RelationTemplate>());

            InitializeComponent();
        }

        #endregion

        #region impl

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            Palette.UpdateItems(RelationTemplates);

            this.DialogResult = true;
        }

        private void btnTemplateAdd_Click(object sender, RoutedEventArgs e)
        {
            RelationTemplate template = new RelationTemplate();
            //template.ViewContext = _view; //TODO  OntologyEditor - relationtemplate view ?
            template.Name = "New Relation";
            template.ForegroundColor = Colors.Black;
            template.BackgroundColor = Colors.Gray;

            RelationTemplates.Add(template);
        }

        private void btnTemplateRemove_Click(object sender, RoutedEventArgs e)
        {
            RelationTemplates.Remove(lbTemplates.SelectedItem as RelationTemplate);
        }

        #endregion

    }
}
