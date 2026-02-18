using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Basics.Utilities
{
    public class MultiEnumerable<T> : IEnumerable<List<T>>
    {
        private MultiEnumerator<T> enumerator;

        public MultiEnumerable(params IEnumerable<T>[] sources)
        {
            enumerator = new MultiEnumerator<T>(sources);
        }

        public MultiEnumerable(bool enumerateUntilAllEmpty, params IEnumerable<T>[] sources)
        {
            enumerator = new MultiEnumerator<T>(sources);
            enumerator.EnumerateUntilAllEmpty = enumerateUntilAllEmpty;
        }

        public MultiEnumerable(IEnumerable<IEnumerable<T>> sources, bool enumerateUntilAllEmpty = true)
        {
            enumerator = new MultiEnumerator<T>(sources);
            enumerator.EnumerateUntilAllEmpty = enumerateUntilAllEmpty;
        }

        #region IEnumerable<List<T>> Member

        public IEnumerator<List<T>> GetEnumerator()
        {
            return enumerator;
        }

        #endregion

        #region IEnumerable Member

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return enumerator;
        }

        #endregion
    }
}
