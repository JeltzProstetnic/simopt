using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;
using MatthiasToolbox.Writer.Enumerations;

namespace MatthiasToolbox.Writer.DataModel
{
    [Table(Name = "tblFragments")]
    [InheritanceMapping(Code = FragmentType.TextBlock, Type = typeof(TextBlock), IsDefault = true)]
    [InheritanceMapping(Code = FragmentType.Image, Type = typeof(Image))]
    [InheritanceMapping(Code = FragmentType.Table, Type = typeof(Table))]
    public abstract class Fragment
    {
        [Column(AutoSync = AutoSync.OnInsert,
                IsPrimaryKey = true,
                IsDbGenerated = true)]
        public int ID { get; private set; }

        [Column]
        public int ProjectID { get; set; }

        [Column(IsDiscriminator = true)]
        public FragmentType FragmentType { get; set; }
    }
}