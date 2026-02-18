using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Semantics.Model;
using MatthiasToolbox.Basics.Interfaces;
using MatthiasToolbox.Basics.Datastructures.Graph;
using MatthiasToolbox.Semantics.Utilities;
using System.Windows;

namespace MatthiasToolbox.Semantics.Interfaces
{
    /// <summary>
    /// TODO  Semantics - derive from IGraph
    /// TODO  Semantics - further traversal modes.
    /// Stepping through the items of a tree, by means of the connections between parents and children, is called 
    /// walking the tree, and the action is a walk of the tree. Often, an operation might be performed when a 
    /// pointer arrives at a particular node. A walk in which each parent node is traversed before its children 
    /// is called a pre-order walk; a walk in which the children are traversed before their respective parents are 
    /// traversed is called a post-order walk; a walk in which a node's left subtree, then the node itself, and then 
    /// finally its right subtree are traversed is a called an in-order traversal. (This last scenario, referring to 
    /// exactly two subtrees, a left subtree and a right subtree, assumes specifically a binary tree.)
    /// </summary>
    public interface ITaxonomy<T> : IHierarchy<T>, INamedElement, IContainer<T>, IGraph<Point>
    {
        #region prop

        T HierarchyType { get; }

        /// <summary>
        /// The length of the longest hierarchy in this tree. (from the root to the most remote leaf)
        /// </summary>
        int Depth { get; }

        /// <summary>
        /// The number of elements on the deepest level.
        /// </summary>
        int Width { get; }

        /// <summary>
        /// All elements which have no further children
        /// </summary>
        IEnumerable<T> LeafNodes { get; }

        /// <summary>
        /// Traverse the tree.
        /// 1 ->         R
        /// 2 ->      N1   N2
        /// 3 ->     N3   N4 N5
        /// </summary>
        IEnumerable<T> NodesLevelOrder { get; }

        /// <summary>
        /// Traverse the tree.
        /// 3 ->         R
        /// 2 ->      N1   N2
        /// 1 ->     N3   N4 N5
        /// </summary>
        IEnumerable<T> NodesInverseLevelOrder { get; }

        #endregion
        #region impl

        /// <summary>
        /// Returns all elements which are on the same level of the tree.
        /// </summary>
        /// <param name="level">A number between 0 (root) and the depth of the tree.</param>
        /// <returns></returns>
        IEnumerable<T> GetNodes(int level);

        #endregion
    }
}
