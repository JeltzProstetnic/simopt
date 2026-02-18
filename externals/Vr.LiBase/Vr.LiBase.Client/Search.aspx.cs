using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Vr.LiBase.Client
{
    public partial class Search : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        public string GetString(string name)
        {
            return "Suche";

            // GridView1.DataSource = var x;
            // GridView1.DataBind();
        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            BulletedList1.Items.Clear();

            BulletedList1.Items.Add("1");
            BulletedList1.Items.Add("2");
            BulletedList1.Items.Add("3");
        }
    }
}