using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Basics.Utilities
{
    /// <summary>
    /// This enumerator enumerates simultaneously over multiple IEnumerables. It can be 
    /// configured to either stop as soon as the first source has reached the end or
    /// to continue until all sources are depleted. In the latter case the resulting
    /// list will contain default(T) for all sources which are already depleted.
    /// </summary>
    /// <typeparam name="T">
    /// The type of items of the source IEnumerables.
    /// </typeparam>
    public class MultiEnumerator<T> : IEnumerator<List<T>>
    {
        private List<T> current;
        private IEnumerable<IEnumerable<T>> source;
        private List<IEnumerator<T>> sources;

        public bool EnumerateUntilAllEmpty { get; set; }

        public MultiEnumerator(bool enumerateUntilAllEmpty, params IEnumerable<T>[] source)
        {
            sources = new List<IEnumerator<T>>();
            this.source = source;
            this.EnumerateUntilAllEmpty = enumerateUntilAllEmpty;
            foreach (IEnumerable<T> e in source)
                this.sources.Add(e.GetEnumerator());
        }

        public MultiEnumerator(params IEnumerable<T>[] source)
        {
            sources = new List<IEnumerator<T>>();
            this.source = source;
            foreach (IEnumerable<T> e in source)
                this.sources.Add(e.GetEnumerator());
        }

        public MultiEnumerator(IEnumerable<IEnumerable<T>> source, bool enumerateUntilAllEmpty = true)
        {
            sources = new List<IEnumerator<T>>();
            this.source = source;
            this.EnumerateUntilAllEmpty = enumerateUntilAllEmpty;
            foreach (IEnumerable<T> e in source)
                this.sources.Add(e.GetEnumerator());
        }

        #region IEnumerator<List<T>> Member

        public List<T> Current
        {
            get { return current; }
        }

        #endregion

        #region IDisposable Member

        public void Dispose()
        {
            foreach (IEnumerator<T> e in sources)
                e.Dispose();
        }

        #endregion

        #region IEnumerator Member

        object System.Collections.IEnumerator.Current
        {
            get { return current; }
        }

        public bool MoveNext()
        {
            bool resultOr = false;
            List<T> tmp = new List<T>();

            foreach (IEnumerator<T> e in sources)
            {
                bool value = e.MoveNext();
                if (!EnumerateUntilAllEmpty && !value) return false;
                resultOr = resultOr || value;
                tmp.Add(value ? e.Current : default(T));
            }

            if (!resultOr) return false;
            current = tmp;
            return true;
        }

        public void Reset()
        {
            current = null;
            foreach (IEnumerator<T> e in sources)
                e.Reset();
        }

        #endregion
    }
}
