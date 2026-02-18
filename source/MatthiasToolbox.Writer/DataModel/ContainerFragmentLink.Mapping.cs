using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;

namespace MatthiasToolbox.Writer.DataModel
{
    [Table(Name = "tblContainerFragmentLinks")]
    public class ContainerFragmentLink
    {
        [Column]
        public int ContainerID { get; set; }

        [Column]
        public int FragmentID { get; set; }
    }
}
