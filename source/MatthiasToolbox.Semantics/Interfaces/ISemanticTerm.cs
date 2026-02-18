using System.Collections.Generic;
using MatthiasToolbox.Basics.Interfaces;
using MatthiasToolbox.Basics.Datastructures.Graph;
using System.Windows;

namespace MatthiasToolbox.Semantics.Interfaces
{
    public interface ISemanticTerm :
        IVertex<Point>,
        INamedElement,
        IMultiNamedElement,
        IDefinedElement,
        IInvertible<ISemanticTerm>,
        ISynSetElement<ISemanticTerm>, 
        ICompositionElement<ISemanticTerm>,
        IBinaryRelationPartner<ISemanticTerm>,
        IMultiHierarchyElement<ISemanticTerm>
    {
        #region prop

        /// <summary>
        /// Retrurns true if this is the preferred / main term in this synset.
        /// </summary>
        bool IsPreferredTerm { get; }

        /// <summary>
        /// Each listed troponym denotes a particular way to do this entry’s referent.
        /// </summary>
        IEnumerable<IConcept> Troponyms { get; }

        ///// <summary>
        ///// Each listed coordinate term shares a hypernym with this entry.
        ///// </summary>
        //IEnumerable<ISemanticTerm> CoordinateTerms { get; }
        
        // TODO  Semantics - coordinate terms == siblings == Kohyponyms ?

        #endregion
    }
}