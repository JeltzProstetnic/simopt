using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Basics.Utilities
{
    public class CaseInvariantStringComparer : IEqualityComparer<string>
    {
        private bool trim = false;

        public CaseInvariantStringComparer(bool trim = true)
        {
            this.trim = trim;
        }

        #region IEqualityComparer

        public bool Equals(string x, string y)
        {
            if (trim)
            {
                return x.Trim().ToUpper() == y.Trim().ToUpper();
            }
            else
            {
                return x.ToUpper() == y.ToUpper();
            }
        }

        public int GetHashCode(string obj)
        {
            return obj.GetHashCode();
        }

        #endregion
    }
}
