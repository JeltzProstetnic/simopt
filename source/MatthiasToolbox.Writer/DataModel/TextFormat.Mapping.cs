using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;

namespace MatthiasToolbox.Writer.DataModel
{
    [Table(Name = "tblTextFormats")]
    public class TextFormat
    {
        public TextFormat() { }

        [Column(AutoSync = AutoSync.OnInsert,
                IsPrimaryKey = true,
                IsDbGenerated = true)]
        public int ID { get; private set; }

        [Column]
        public string Name { get; set; }

        [Column]
        public string Code { get; set; }
    }
}