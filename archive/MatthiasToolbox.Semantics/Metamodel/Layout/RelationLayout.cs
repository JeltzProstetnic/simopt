using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;

namespace MatthiasToolbox.Semantics.Metamodel.Layout
{
    [Table(Name = "tblRelationLayout")]
    public class RelationLayout
    {
        #region data

        /// <summary>
        /// Unique, auto-generated identifier.
        /// </summary>
        [Column(AutoSync = AutoSync.OnInsert,
                IsPrimaryKey = true,
                IsDbGenerated = true)]
        public int ID { get; private set; }

        [Column]
        public int ViewID { get; set; }

        [Column]
        public int RelationID { get; set; }
        
        [Column]
        public double Thickness { get; set; }

        [Column]
        public double Opacity { get; set; }

        [Column]
        public string LineColor { get; set; }

        [Column]
        public string TextColor { get; set; }

        [Column]
        public bool IsVisible { get; set; }

        [Column]
        public bool IsReadonly { get; set; }

        [Column]
        public int StartCaps { get; set; }

        [Column]
        public int EndCaps { get; set; }

        [Column]
        public int LineType { get; set; }

        [Column]
        public string PathData { get; set; }

        #endregion
        #region ctor

        public RelationLayout() { }

        public RelationLayout(View view, Relation relation) 
        {
            this.ViewID = view.ID;
            this.RelationID = relation.ID;
            IsVisible = true;
            Thickness = 3;
            Opacity = 1;
            LineColor = "#000000";
            TextColor = "#111111";
        }

        #endregion
    }
}