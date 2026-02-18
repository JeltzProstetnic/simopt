using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Basics.Interfaces;

namespace MatthiasToolbox.Semantics.Interfaces
{
    /// <summary>
    /// Extension of <code>IHierarchyElement&lt;T&gt;</code> for multi-inheritance.
    /// </summary>
    /// <typeparam name="T">The commmon base type of all elements of the hierarchy.</typeparam>
    /// <remarks>FINAL</remarks>
    public interface IMultiHierarchyElement<T> : MatthiasToolbox.Semantics.Interfaces.IHierarchyElement<T>
    {
        /// <summary>
        /// Gets the parent items.
        /// Each listed hypernym is superordinate to this entry; This entry’s referent is a kind of that denoted by listed hypernym.
        /// transitive, irreflexive, asymmetric
        /// </summary>
        /// <value>The parent items.</value>
        IEnumerable<T> Parents { get; }
        /// <summary>
        /// <see cref="Parents"/>
        /// </summary>
        IEnumerable<T> Hyperonyms { get; }

        /// <summary>
        /// Gets the parent concept of the given HierarchyType.
        /// </summary>
        /// <param name="hrw">The HierarchyType.</param>
        /// <returns></returns>
        T GetParent(INamedElement hierarchyType);
    }
}