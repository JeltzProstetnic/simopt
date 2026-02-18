using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Basics.Interfaces;
using MatthiasToolbox.Basics.Datastructures.Graph;
using System.Windows;

namespace MatthiasToolbox.Semantics.Interfaces
{
    /// <summary>
    /// TODO  Semantics - derive from IGraph
    /// </summary>
    public interface IThesaurus : INamedElement, IContainer<ISemanticTerm>, IGraph<Point>
    {
        /// <summary>
        /// All elements within this container.
        /// </summary>
        IEnumerable<IBinaryRelation<ISemanticTerm>> Relations { get; }

        /// <summary>
        /// Add a new element to this vocabulary.
        /// </summary>
        /// <param name="element">A new instance.</param>
        /// <returns>Success flag</returns>
        bool Add(IBinaryRelation<ISemanticTerm> relation);

        /// <summary>
        /// Delete an element from this container.
        /// </summary>
        /// <param name="element">An existing instance.</param>
        /// <returns>Success flag</returns>
        bool Remove(IBinaryRelation<ISemanticTerm> relation);

        /// <summary>
        /// Retrieve an element by its content or an alias.
        /// </summary>
        /// <param name="word">Name or alias of an existing element.</param>
        /// <returns>May return null if no match is found.</returns>
        IBinaryRelation<ISemanticTerm> GetRelation(string name);
    }
}