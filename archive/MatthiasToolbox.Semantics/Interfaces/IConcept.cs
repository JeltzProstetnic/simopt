using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Basics.Datastructures.Graph;
using MatthiasToolbox.Semantics.Utilities;
using MatthiasToolbox.Basics.Interfaces;
using System.Windows;

namespace MatthiasToolbox.Semantics.Interfaces
{
    public interface IConcept :
        IVertex<Point>,
        INamedElement,
        IMultiNamedElement,
        IDefinedElement,
        ISynSetElement<IConcept>,
        IBinaryRelationPartner<IConcept>,
        IMultiHierarchyElement<IConcept>,
        ICompositionElement<IConcept>,
        IInvertible<IConcept>
    {
        /// <summary>
        /// The ontology in which this concept is contained.
        /// </summary>
        IOntology Container { get; }

        /// <summary>
        /// Rules which apply to this concept.
        /// </summary>
        IEnumerable<IRule> Rules { get; }

        IConcept SuperConcept { get; }
        
        IEnumerable<IConcept> SubConcepts { get; }

        ///// <summary>
        ///// Gets the rule.
        ///// </summary>
        ///// <param name="name">The name of the rule.</param>
        ///// <returns></returns>
        //IRule GetRule(string name);

        ///// <summary>
        ///// Gets the related concept.
        ///// </summary>
        ///// <param name="relationName">Name of the relation.</param>
        ///// <returns></returns>
        //IConcept GetRelatedConcept(string relationName);

        /// <summary>
        /// Removes the relation. // TODO  Semantics - remove vs. delete relation
        /// </summary>
        /// <param name="relation">The relation to remove.</param>
        /// <returns></returns>
        bool RemoveRelation(IBinaryRelation<IConcept> relation);
    }
}