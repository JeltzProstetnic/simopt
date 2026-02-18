using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Semantics.Interfaces
{
    public interface ISynSetElement<T>
    {
        /// <summary>
        /// All synonyms / aliases for this term.
        /// Each listed synonym denotes the same as this entry.
        /// </summary>
        IEnumerable<T> Synonyms { get; }
    }
}