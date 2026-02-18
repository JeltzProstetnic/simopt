using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MatthiasToolbox.Logging;
using MatthiasToolbox.Logging.Loggers;
using MatthiasToolbox.Semantics.Metamodel;

namespace MatthiasToolbox.ImageLibrary
{
    public partial class Form1 : Form
    {
        public Ontology Ontology { get; set; }

        public Form1()
        {
            InitializeComponent();

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
