using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MatthiasToolbox.Semantics.Metamodel;
using MatthiasToolbox.Semantics.Utilities;

namespace Vr.LiBase.Client
{
    public partial class Submit : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            CheckBoxList1.Items.Clear();

            Concept n = SiteMaster.onto.CreateConcept(TextBox1.Text, SiteMaster.onto.RootConcept);
            //n.Set(SiteMaster.onto.DefinitionProperty, TextBox2.Text);
            foreach (string word in TextBox2.Text.Split(' '))
            {
                List<Concept> results = SiteMaster.onto.FindConcept(word);
                foreach (Concept c in results)
                    CheckBoxList1.Items.Add(c.Name);
            }
        }
    }
}