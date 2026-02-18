using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Basics.Interfaces;
using MatthiasToolbox.Basics.Datastructures.Graph;
using System.Windows;

namespace MatthiasToolbox.Semantics.Interfaces
{
    public interface IOntology : INamedElement, IContainer<IConcept>, IGraph<Point>
    {
        IEnumerable<IBinaryRelation<IConcept>> Entities { get; } // same as Content
        IEnumerable<IBinaryRelation<IConcept>> Relations { get; }
        IEnumerable<IRule> Rules { get; }

        /// <summary>
        /// Add a new element to this vocabulary.
        /// </summary>
        /// <param name="element">A new instance.</param>
        /// <returns>Success flag</returns>
        bool Add(IBinaryRelation<IConcept> relation);

        /// <summary>
        /// Delete an element from this container.
        /// </summary>
        /// <param name="element">An existing instance.</param>
        /// <returns>Success flag</returns>
        bool Remove(IBinaryRelation<IConcept> relation);

        /// <summary>
        /// Retrieve an element by its content or an alias.
        /// </summary>
        /// <param name="word">Name or alias of an existing element.</param>
        /// <returns>May return null if no match is found.</returns>
        IBinaryRelation<IConcept> GetRelation(string name);

        /// <summary>
        /// Add a new element to this vocabulary.
        /// </summary>
        /// <param name="element">A new instance.</param>
        /// <returns>Success flag</returns>
        bool Add(IRule rule);

        /// <summary>
        /// Delete an element from this container.
        /// </summary>
        /// <param name="element">An existing instance.</param>
        /// <returns>Success flag</returns>
        bool Remove(IRule rule);

        /// <summary>
        /// Retrieve an element by its content or an alias.
        /// </summary>
        /// <param name="word">Name or alias of an existing element.</param>
        /// <returns>May return null if no match is found.</returns>
        IRule GetRule(string name);
    }
}