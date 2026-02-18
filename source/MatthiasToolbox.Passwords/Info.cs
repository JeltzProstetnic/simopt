using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace MatthiasToolbox.Passwords
{
    public partial class Info : Form
    {
        public Info()
        {
            InitializeComponent();
        }

        private void linkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.FileName = "http://www.bluelogic.at";
            p.Start();
        }
    }
}