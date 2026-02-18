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
using Vr.WarehouseSimulator.Model;
using MatthiasToolbox.Simulation;
using MatthiasToolbox.Logging.Loggers;

namespace Vr.WarehouseSimulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Simulation sim;

        public MainWindow()
        {
            InitializeComponent();
            Simulator.RegisterSimulationLogger(new WPFRichTextBoxLogger(richTextBox1));
        }

        // BUILD
        private void button2_Click(object sender, RoutedEventArgs e)
        {
            sim = new Simulation();

            button1.IsEnabled = true;
        }

        // START
        private void button1_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
