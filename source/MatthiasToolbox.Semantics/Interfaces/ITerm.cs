using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Basics.Datastructures.Graph;
using MatthiasToolbox.Basics.Interfaces;
using System.Windows;

namespace MatthiasToolbox.Semantics.Interfaces
{
    public interface ITerm : INamedElement, ISynSetElement<ITerm>, IMultiNamedElement, IDefinedElement
    {
        /// <summary>
        /// Retrurns true if this is the preferred / main term in this synset.
        /// </summary>
        bool IsPreferredTerm { get; }
    }
}