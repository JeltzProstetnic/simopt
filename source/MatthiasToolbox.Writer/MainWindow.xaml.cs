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
using MatthiasToolbox.Presentation;
using MatthiasToolbox.Writer.DataModel;
using MatthiasToolbox.Utilities.Office;
using System.IO;
using MatthiasToolbox.Utilities.COM.Filter;

namespace MatthiasToolbox.Writer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// 
    /// http://msdn.microsoft.com/de-de/library/aa140277(office.10).aspx
    /// 
    /// {\rtf1
    /// Guten Tag!
    /// \par
    /// {\i Dies} ist ein 
    /// formatierter {\b Text}.
    /// \par
    /// Das Ende.
    /// }
    /// </summary>
    public partial class MainWindow : Window
    {
        private Project CurrentProject { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            CurrentProject = Global.ProjectDatabase.ProjectTable.Where(p => p.ID == 1).First();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // WordDocument doc = WordDocument.FromFile("C:\\test.doc");
            // richTextEditor1.Text = Dock.GetText();
            // doc.Close();

            TextReader reader = new FilterReader("C:\\test.doc");
            richTextEditor1.Text = (new FilterReader("C:\\test.doc")).ReadToEnd();
        }
    }
}