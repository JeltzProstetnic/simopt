using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;
using MatthiasToolbox.Writer.Enumerations;

namespace MatthiasToolbox.Writer.DataModel
{
    public class Image : Fragment
    {
        public Image() { this.FragmentType = FragmentType.Image; }
    }
}
