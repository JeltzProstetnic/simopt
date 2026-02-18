using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimOpt.Basics.Tools
{
    /// <summary>
    /// A wrapper for an enumerator of which the results are to be manipulated. 
    /// The wrapped enumerator must be set in the constructor and cannot be changed later.
    /// A manipulation function can be provided which is provided with the original values during the iteration.
    /// </summary>
    /// <typeparam name="T">The base enumerator return type.</typeparam>
    /// <remarks>final</remarks>
    public class ManipulatedEnumerator<T> : IEnumerator<T>
    {
        #region cvar

        private T current;
        private IEnumerator<T> baseEnumerator;
        
        #endregion
        #region prop

        /// <summary>
        /// Get or set the current filter. The filter must
        /// return true for the item to be filtered.
        /// </summary>
        public Func<T, T> Manipulation { get; set; }

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
        public ManipulatedEnumerator(IEnumerator<T> baseEnumerator, Func<T, T> manipulation = null)
        {
            this.baseEnumerator = baseEnumerator;
            this.Manipulation = manipulation == null ? x => x : manipulation;
        }

        #endregion
        #region impl

        #region IEnumerator<T>

        public T Current
        {
            get { return current; }
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
            get { return current; }
        }

        public bool MoveNext()
        {
            if (!baseEnumerator.MoveNext()) return false;

            current = Manipulation.Invoke(baseEnumerator.Current);

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