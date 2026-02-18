using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimOpt.Basics.Interfaces
{
    /// <summary>
    /// An entity which is part of a hierarchy.
    /// </summary>
    /// <typeparam name="T">The commmon base type of all elements of the hierarchy.</typeparam>
    /// <remarks>FINAL</remarks>
    public interface IHierarchyElement<T>
    {
        /// <summary>
        /// Gets or sets the parent item.
        /// </summary>
        /// <value>The parent item.</value>
        T Parent { get; set; }

        /// <summary>
        /// The children of this item.
        /// </summary>
        IEnumerable<T> Children { get; }
    }
}