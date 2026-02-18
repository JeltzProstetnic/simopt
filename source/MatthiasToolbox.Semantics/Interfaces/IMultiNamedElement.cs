using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Basics.Interfaces;

namespace MatthiasToolbox.Semantics.Interfaces
{
    public interface IMultiNamedElement : INamedElement
    {
        /// <summary>
        /// Synonyms / aliases for this item including the preferred name.
        /// </summary>
        IEnumerable<string> Names { get; }
    }
}