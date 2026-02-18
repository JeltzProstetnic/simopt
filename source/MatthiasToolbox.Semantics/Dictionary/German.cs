using System.Collections.Generic;

namespace MatthiasToolbox.Semantics.Dictionary
{
    public class German : IEnumerable<string>
    {
        public German() { }

        #region IEnumerable<string>

        public IEnumerator<string> GetEnumerator()
        {
            return new GermanEnumerator();
        }

        #endregion
        #region IEnumerable

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new GermanEnumerator();
        }

        #endregion
    }
}