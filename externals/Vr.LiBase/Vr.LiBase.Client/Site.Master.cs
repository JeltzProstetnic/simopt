using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MatthiasToolbox.Semantics.Metamodel;

namespace Vr.LiBase.Client
{
    public partial class SiteMaster : System.Web.UI.MasterPage
    {
        public static Ontology onto = new Ontology("DefaultOntology.sdf", "Default Ontology");

        protected void Page_Load(object sender, EventArgs e)
        {
            onto.Initialize();


            //Test code to fill ontology

            //Relation TopicRelation = SiteMaster.onto.CreateRelation("TopicRelation", SiteMaster.onto.RootConcept, 1, 2, SiteMaster.onto.RootConcept, true);
            //Property TitleProperty = SiteMaster.onto.CreateProperty<string>("Title", SiteMaster.onto.RootConcept);
            //Property TextProperty = SiteMaster.onto.CreateProperty<string>("Text", SiteMaster.onto.RootConcept);

            //Concept CGrundsaule = SiteMaster.onto.CreateConcept("Grundsäule", SiteMaster.onto.RootConcept);
            //Concept CBegehung = SiteMaster.onto.CreateConcept("Begehung", SiteMaster.onto.RootConcept);

            //Instance GrundSaule = SiteMaster.onto.CreateInstance(CGrundsaule);
            //GrundSaule.DisplayPropertyID = TitleProperty.ID;
            //GrundSaule.Set(TextProperty, "Grundsäule ....");

            //var CBegehungen = SiteMaster.onto.FindConcept("Begehung");
            //foreach (Concept CBegehung in CBegehungen)
            //{
            //    var TitleProperty = SiteMaster.onto.FindProperty(SiteMaster.onto.RootConcept, "Title").First();
            //    var TextProperty = SiteMaster.onto.FindProperty(SiteMaster.onto.RootConcept, "Text").First();

            //    Instance Begehung = SiteMaster.onto.CreateInstance(CBegehung);
            //    Begehung.DisplayPropertyID = TitleProperty.ID;
            //    Begehung.Set(TextProperty, "Begehung ..."); 
            //}
            //Instance Begehung = SiteMaster.onto.CreateInstance(CBegehung);
            //Begehung.DisplayPropertyID = TitleProperty.ID;
            //Begehung.Set(TextProperty, "Begehung ...");

            //SiteMaster.onto.CreateRelation(TopicRelation, GrundSaule, Begehung);
        }
    }
}
