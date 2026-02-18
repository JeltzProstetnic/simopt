using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimOpt.Basics.Tools
{
    /// <summary>
    /// A wrapper for an enumerator to be filtered. The wrapped enumerator 
    /// must be set in the constructor and cannot be changed later. If a filter
    /// function is provided, only items for which the filter returns false will be iterated.
    /// </summary>
    /// <typeparam name="T">The base enumerator return type.</typeparam>
    /// <remarks>final</remarks>
    public class FilteredEnumerator<T> : IEnumerator<T>
    {
        #region cvar

        private IEnumerator<T> baseEnumerator;
        
        #endregion
        #region prop

        /// <summary>
        /// Get or set the current filter. The filter must
        /// return true for the item to be filtered.
        /// </summary>
        public Func<T, bool> Filter { get; set; }

        /// <summary>
        /// The enumerator on which this is based.
        /// </summary>
        public IEnumerator<T> BaseEnumerator
        {
            get { return baseEnumerator; }
        }

        #endregion
        #region ctor

        /// <summary>
        /// If you do not provide a filter the filter will be 
        /// set to always return false and thereby filter nothing.
        /// </summary>
        /// <param name="baseEnumerator">The IEnumerator to wrap.</param>
        /// <param name="filter">A filter function.</param>
        public FilteredEnumerator(IEnumerator<T> baseEnumerator, Func<T, bool> filter = null)
        {
            this.baseEnumerator = baseEnumerator;
            this.Filter = filter == null ? x => false : filter;
        }

        #endregion
        #region impl

        #region IEnumerator<T>

        public T Current
        {
            get { return baseEnumerator.Current; }
        }

        #endregion
        #region IDisposable

        public void Dispose()
        {
            baseEnumerator.Dispose();
        }

        #endregion
        #region IEnumerator

        object System.Collections.IEnumerator.Current
        {
            get { return baseEnumerator.Current; }
        }

        public bool MoveNext()
        {
            if (!baseEnumerator.MoveNext()) return false;

            while (Filter.Invoke(baseEnumerator.Current)) // item is filtered?
            {
                if (!baseEnumerator.MoveNext()) return false;
            }

            return true;
        }

        public void Reset()
        {
            baseEnumerator.Reset();
        }

        #endregion

        #endregion
    }
}