using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Semantics.Utilities
{
    public class HierarchyType : RelationType
    {
        public RelationType ParentOfRelation { get; set; }
        public RelationType ChildOfRelation { get; set; }

        internal HierarchyType(string name) : base(name)
        {
        }

        // TODO  Semantics - add ctors which include the relation types
    }
}