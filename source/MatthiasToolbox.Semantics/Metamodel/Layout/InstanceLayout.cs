using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;

namespace MatthiasToolbox.Semantics.Metamodel.Layout
{
    [Table(Name = "tblInstanceLayout")]
    public class InstanceLayout
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
        public int InstanceID { get; set; }
        
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

        public InstanceLayout() { }

        public InstanceLayout(View view, Instance instance) 
        {
            this.ViewID = view.ID;
            this.InstanceID = instance.ID;
            IsVisible = true;
            Width = 150;
            Height = 150;
            Opacity = 1;
            BackgroundColor = "#FFFFFF";
            ForegroundColor = "#111111";
        }

        #endregion
    }
}