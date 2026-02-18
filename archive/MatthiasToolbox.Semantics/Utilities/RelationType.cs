using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Basics.Interfaces;

namespace MatthiasToolbox.Semantics.Utilities
{
    public class RelationType : INamedElement
    {
        public string Name
        {
            get; private set;
        }

        public RelationType(string name)
        {
            this.Name = name;
        }

        public static bool operator ==(RelationType a, RelationType b)
        {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(a, b)) return true;

            // If one is null, but not both, return false.
            if (((object)a == null) || ((object)b == null)) return false;

            // Return true if the fields match:
            return a.Name == b.Name;
        }

        public static bool operator !=(RelationType a, RelationType b)
        {
            return !(a == b);
        }

        // override object.Equals
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;
            else
                return (obj as RelationType).Name.Equals(this.Name);
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}