using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MatthiasToolbox.Semantics.Metamodel;

namespace Vr.LiBase.Client
{
    public partial class _Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // url argumente = Page.RouteData.Values
        }

        public string GetString(string name)
        {
            return "Suche";
        }

        protected void SearchButton_Click(object sender, EventArgs e)
        {
            BulletedList1.Items.Clear();

            String searchString = SearchTermTextfield.Text;
            if (String.IsNullOrWhiteSpace(searchString))
                return; //TODO: LiBase.Client - SearchButton: message enter text

            //search the concept
            List<Concept> cresults = new List<Concept>();
            foreach (string word in searchString.Split(' '))
            {
                cresults.AddRange(SiteMaster.onto.FindConcept(word));
            }
            //search the instances
            List<Instance> iresults = new List<Instance>();
            foreach (Concept c in cresults)
            {
                iresults.AddRange(c.Instances);
            }
            foreach (Instance instance in iresults)
            {
                StringBuilder sb = new StringBuilder(instance.Name);
                sb.Append("; ");
                //sb.Append(instance.Title);
                //sb.Append("; ");
                foreach (Property property in instance.Properties)
                {
                    sb.Append(property.Name);
                    sb.Append(", ");
                    sb.Append(instance.Get<String>(property));
                    sb.Append("; ");

                }
                BulletedList1.Items.Add(sb.ToString());
            }
        }
    }
}
