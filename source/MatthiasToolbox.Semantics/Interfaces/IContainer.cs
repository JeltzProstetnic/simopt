using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Semantics.Interfaces
{
    public interface IContainer<T>
    {
        /// <summary>
        /// All elements within this container.
        /// </summary>
        IEnumerable<T> Content { get; }

        /// <summary>
        /// Add a new element to this vocabulary.
        /// </summary>
        /// <param name="element">A new instance.</param>
        /// <returns>Success flag</returns>
        bool Add(T element);

        /// <summary>
        /// Delete an element from this container.
        /// </summary>
        /// <param name="element">An existing instance.</param>
        /// <returns>Success flag</returns>
        bool Remove(T element);

        /// <summary>
        /// Retrieve an element by it's content or an alias.
        /// </summary>
        /// <param name="word">Name or alias of an existing element.</param>
        /// <returns>May return null if no match is found.</returns>
        T GetElement(string word);

        /// <summary>
        /// Find elements which contain the given search string in their name or definition.
        /// </summary>
        /// <param name="word">Name or alias of an existing element.</param>
        /// <param name="searchDefinitions">If set to true, elements which have the given 
        /// search string in their definition will be found. Default is false.</param>
        /// <returns>May return null if no match is found.</returns>
        IEnumerable<T> FindElement(string word, bool searchDefinitions);
    }
}
