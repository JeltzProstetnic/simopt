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
using System.Windows.Shapes;

namespace MatthiasToolbox.Money
{
    /// <summary>
    /// Interaction logic for Info.xaml
    /// </summary>
    public partial class Info : Window
    {
        public Info()
        {
            InitializeComponent();
            double n = SystemParameters.PrimaryScreenHeight / 3;
            Width = Math.Min(n, 535);
            Height = Width;

            image1.Height = Height;
            image1.Width = Width;
            viewBox1.Height = Height;
            viewBox1.Width = Width;
            canvas1.Height = Height;
            canvas1.Width = Width;                
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Close();
        }
    }
}
