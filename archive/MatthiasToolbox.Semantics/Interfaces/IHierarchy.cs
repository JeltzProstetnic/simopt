using System.Collections.Generic;

namespace MatthiasToolbox.Semantics.Interfaces
{
    /// <summary>
    /// TODO  Semantics - Prune & Graft
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IHierarchy<T>
    {
        /// <summary>
        /// The top level element. In a multi-inheritance hierarchy 
        /// this may not be unique and therefore return null.
        /// </summary>
        T Root { get; }

        /// <summary>
        /// All elements of the hierarchy
        /// </summary>
        IEnumerable<T> Nodes { get; }

        /// <summary>
        /// Return the given elements parent, grandparent and 
        /// so on (the last element will always be the root).
        /// </summary>
        /// <param name="term"></param>
        /// <returns></returns>
        IEnumerable<T> GetAncestors(T item);

        /// <summary>
        /// Return the subtree starting at the given node.
        /// </summary>
        /// <param name="term"></param>
        /// <returns></returns>
        IHierarchy<T> GetSubTree(T item);
    }
}