using System.Collections.Generic;

namespace MatthiasToolbox.Semantics.Dictionary
{
    public class English : IEnumerable<string>
    {
        public English() { }

        #region IEnumerable<string>

        public IEnumerator<string> GetEnumerator()
        {
            return new EnglishEnumerator();
        }

        #endregion
        #region IEnumerable

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new EnglishEnumerator();
        }

        #endregion
    }
}