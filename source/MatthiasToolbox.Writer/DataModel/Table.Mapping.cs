using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;
using MatthiasToolbox.Writer.Enumerations;

namespace MatthiasToolbox.Writer.DataModel
{
    public class Table : Fragment
    {
        public Table() { this.FragmentType = FragmentType.Table; }
    }
}
