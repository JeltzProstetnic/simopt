using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Semantics.Interfaces
{
    /// <summary>
    /// An element which is part of other elements or consists of parts of the same type or a common base type.
    /// </summary>
    /// <typeparam name="T">The common base type of all containers and parts.</typeparam>
    public interface ICompositionElement<T>
    {
        /// <summary>
        /// Each listed meronym denotes part of this entry’s referent.
        /// </summary>
        IEnumerable<T> Meronyms { get; }
        /// <summary>
        /// <see cref="Meronyms"/>
        /// </summary>
        IEnumerable<T> Parts { get; }

        /// <summary>
        /// Each listed holomym has this entry’s referent as a part of itself; this entry’s referent is part of that of each listed holonym.
        /// </summary>
        IEnumerable<T> Holonyms { get; }
        /// <summary>
        /// <see cref="Holonyms"/>
        /// </summary>
        IEnumerable<T> Containers { get; }

        /// <summary>
        /// TODO  Semantics - comment
        /// </summary>
        /// <param name="part"></param>
        /// <returns></returns>
        bool AddPart(T part);

        /// <summary>
        /// TODO  Semantics - comment
        /// </summary>
        /// <param name="part"></param>
        /// <returns></returns>
        bool RemovePart(T part);
    }
}