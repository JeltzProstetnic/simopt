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
using MatthiasToolbox.Semantics.Metamodel;

namespace MatthiasToolbox.OntoEdit
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.DataContext = new Ontology("DefaultOntology.sdf", "TestOntology");

            InitializeComponent();

            Logging.Logger.Add(new Logging.Loggers.ConsoleLogger());
        }

        #region impl

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ucOntologyControl.Close();
        }

        #endregion
    }
}
