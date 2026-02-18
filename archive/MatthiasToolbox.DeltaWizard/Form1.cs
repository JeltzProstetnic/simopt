using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace MatthiasToolbox.DeltaWizard
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Form1_HelpButtonClicked(object sender, CancelEventArgs e)
        {
            Info i = new Info();
            i.ShowDialog();
            e.Cancel = true;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public int GetState()
        {
            if (radioButton1.Checked) return 1;
            else if (radioButton2.Checked) return 2;
            else if (radioButton3.Checked) return 3;
            else return 4;
        }
        
        public void SetState(int status)
        {
            switch (status)
            {
                case 1:
                    radioButton1.Checked = true;
                    break;
                case 2:
                    radioButton2.Checked = true;
                    break;
                case 3:
                    radioButton3.Checked = true;
                    break;
                case 4:
                    radioButton4.Checked = true;
                    break;
            }
        }
    }
}