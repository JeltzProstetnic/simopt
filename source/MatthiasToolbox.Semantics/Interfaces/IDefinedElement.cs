using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Semantics.Interfaces
{
    public interface IDefinedElement
    {
        /// <summary>
        /// A formal, unique definition of this term.
        /// </summary>
        string Definition { get; }
    }
}