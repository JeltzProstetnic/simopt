using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MatthiasToolbox.OntologyEditor.Views;

namespace MatthiasToolbox.OntologyEditor.Controls
{
    /// <summary>
    /// Interaction logic for SimpleColorChooser.xaml
    /// </summary>
    public partial class SimpleColorChooser : UserControl
    {
        public SimpleColorChooser()
        {
            InitializeComponent();

            this.DataContextChanged += new DependencyPropertyChangedEventHandler(SimpleColorChooser_DataContextChanged);
        }

        void SimpleColorChooser_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(e.NewValue is Color))
                return;
            Color color = (Color) e.NewValue;
            PropertyInfo info = PropertyInfoColorConverter.ColorToPropertyInfo(color);

            if (info != null)
                cboColors.SelectedItem = info;
        }

        private void cboColors_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(cboColors.SelectedItem == null)
                return;

            var color = PropertyInfoColorConverter.PropertyInfoToColor((PropertyInfo) cboColors.SelectedItem);

            this.DataContext = color;
        }
    }
}
