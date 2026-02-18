using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;

namespace MatthiasToolbox.Semantics.Metamodel.Layout
{
    [Table(Name = "tblPropertyLayout")]
    public class PropertyLayout
    {
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
        public int PropertyID { get; set; }
        
        [Column]
        public bool IsBold { get; set; }

        [Column]
        public bool IsItalic { get; set; }

        [Column]
        public string TextColor { get; set; }

        [Column]
        public bool IsVisible { get; set; }

        [Column]
        public bool IsReadonly { get; set; }

        [Column]
        public int Order { get; set; }
    }
}