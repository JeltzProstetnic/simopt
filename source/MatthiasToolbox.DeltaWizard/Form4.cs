using System;
using System.Windows.Forms;

namespace MatthiasToolbox.DeltaWizard
{
    public partial class Form4 : Form
    {
        public Form4()
        {
            InitializeComponent();
        }
        
        public void setText(String text)
        {
            textBox1.Text = text;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}