using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Semantics.Interfaces
{
    /// <summary>
    /// TODO  Semantics - comment
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IInvertible<T>
    {
        /// <summary>
        /// The element which denotes the exact opposite of this instance.
        /// </summary>
        T Antonym { get; }
    }
}
