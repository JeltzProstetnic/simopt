using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;
using System.Windows;

namespace MatthiasToolbox.Semantics.Metamodel.Layout
{
    [Table(Name = "tblConceptLayout")]
    public class ConceptLayout
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
        public int ConceptID { get; set; }
        
        [Column]
        public double X { get; set; }

        [Column]
        public double Y { get; set; }

        [Column]
        public double Width { get; set; }

        [Column]
        public double Height { get; set; }

        [Column]
        public double Opacity { get; set; }

        [Column]
        public string BackgroundColor { get; set; }

        [Column]
        public string ForegroundColor { get; set; }

        [Column]
        public bool IsVisible { get; set; }

        [Column]
        public bool IsReadonly { get; set; }

        #endregion
        #region ctor

        public ConceptLayout() { }
        
        public ConceptLayout(View view, Concept concept) 
        {
            ViewID = view.ID;
            ConceptID = concept.ID;
            IsVisible = true;
            Width = 250;
            Height = 200;
            Opacity = 1;
            X = concept.Position.X;
            Y = concept.Position.Y;
            BackgroundColor = "#FFFFFF";
            ForegroundColor = "#000000";
        }

        #endregion
    }
}