using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Semantics.Interfaces
{
    /// <summary>
    /// An entity which is part of a hierarchy in which adding and removing children is supported.
    /// </summary>
    /// <typeparam name="T">The commmon base type of all elements of the hierarchy.</typeparam>
    /// <remarks>FINAL</remarks>
    public interface IHierarchyElement<T> : MatthiasToolbox.Basics.Interfaces.IHierarchyElement<T>
    {
        /// <summary>
        /// <see cref="Children"/>
        /// </summary>
        IEnumerable<T> Hypoonyms { get; }

        /// <summary>
        /// <see cref="Parent"/>
        /// </summary>
        T Hypernym { get; }

        /// <summary>
        /// The children of this item's parent excluding this instance.
        /// </summary>
        /// <value>The siblings.</value>
        IEnumerable<T> Siblings { get; }

        /// <summary>
        /// Removes the child element.
        /// </summary>
        /// <param name="element">The child element to remove.</param>
        /// <returns>Returns false if the element is not a child of this instance.</returns>
        bool RemoveChild(T element);

        /// <summary>
        /// Add a child element.
        /// </summary>
        /// <param name="element">The child element to add.</param>
        /// <returns>success flag</returns>
        bool AddChild(T element);
    }
}