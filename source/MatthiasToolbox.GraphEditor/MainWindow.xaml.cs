using System;
using System.Windows;
using System.Windows.Input;
using MatthiasToolbox.Basics.Datastructures.Network;
using MatthiasToolbox.GraphDesigner.Enumerations;

namespace MatthiasToolbox.GraphEditor
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        bool done = false;

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            if (done) return;
            done = true;
            graphControl1.LoadFromGraphML("c:\\test.graphml");
        }
    }
}
