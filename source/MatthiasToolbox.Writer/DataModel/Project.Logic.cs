using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;
using System.Data.Linq;

namespace MatthiasToolbox.Writer.DataModel
{
    public partial class Project
    {
        public List<Container> RootContainers
        {
            get
            {
                if (Containers == null) return null;
                IEnumerable<Container> results = Containers.Where(c => c.ParentID == 0);
                if (results.Any()) return results.ToList();
                else return null;
            }
        }
    }
}