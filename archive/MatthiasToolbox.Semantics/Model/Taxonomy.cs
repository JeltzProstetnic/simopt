using System;
using System.Collections.Generic;
using System.Linq;
using MatthiasToolbox.Semantics.Interfaces;
using MatthiasToolbox.Semantics.Utilities;
using MatthiasToolbox.Basics.Datastructures.Graph;
using System.Windows;

namespace MatthiasToolbox.Semantics.Engine
{
    public class Taxonomy // : ITaxonomy
    {
        // TODO  Semantics - @dwi implement

        #region cvar

        private readonly HierarchyType _hrw;

        #endregion

        #region prop
        #region INamedElement

        public string Name { get; private set; }

        #endregion
        #region IGraph<IHierachricalTerm,IHierarchyRelation<IHierachricalTerm>>

        public IEnumerable<IHierarchicalTerm> Vertices
        {
            get { return Content; }
        }

        /// <summary>
        /// Gets all the parent and child relations.
        /// </summary>
        /// <value>The parent and child relations.</value>
        public IEnumerable<object> Edges
        {
            get {
                throw new NotImplementedException();
            }
        }

        #endregion
        #region ITaxonomy

        public HierarchyType HierarchyType { get { return _hrw; } }

        /// <summary>
        /// The length of the longest hierarchy in this tree. (from the root to the most remote leaf)
        /// </summary>
        public int Depth
        {
            get { return GetDepth(this.Root); }
        }

        /// <summary>
        /// The number of elements on the deepest level.
        /// </summary>
        public int Width { get { return GetNodes(Depth).Count(); } }

        /// <summary>
        /// All elements which have no further children
        /// </summary>
        public IEnumerable<IHierarchicalTerm> LeafNodes
        {
            get
            {
                return Content.Where(hierarchicalTerm => hierarchicalTerm.Children.Count() == 0);
            }
        }

        /// <summary>
        /// Traverse the tree.
        /// 1 ->         R
        /// 2 ->      N1   N2
        /// 3 ->     N3   N4 N5
        /// </summary>
        public IEnumerable<IHierarchicalTerm> NodesLevelOrder
        {
            get { return GetDescendants(this.Root); }
        }

        /// <summary>
        /// Traverse the tree.
        /// 3 ->         R
        /// 2 ->      N1   N2
        /// 1 ->     N3   N4 N5
        /// </summary>
        public IEnumerable<IHierarchicalTerm> NodesInverseLevelOrder
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        #endregion
        #region IContainer<IHierarchicalTerm>

        /// <summary>
        /// All elements within this container.
        /// </summary>
        public IEnumerable<IHierarchicalTerm> Content
        {
            get { return NodesLevelOrder; }
        }

        #endregion
        #region IHierarchy<IHierarchicalTerm>

        /// <summary>
        /// The top level element.
        /// </summary>
        public IHierarchicalTerm Root { get; private set; }

        /// <summary>
        /// All elements of the hierarchy
        /// </summary>
        public IEnumerable<IHierarchicalTerm> Nodes
        {
            get { return Content; }
        }

        #endregion
        #endregion

        #region ctor

        internal Taxonomy()
        {
        }

        public Taxonomy(string name)
            : this()
        {
            this.Name = name;
        }

        ///// <summary>
        ///// Initializes a new instance of the <see cref="Taxonomy"/> class.
        ///// Generates a Wrapper for each concept. And creates a HierarchicalRelation for each Child of relation
        ///// of the same HierarchyType.
        ///// </summary>
        ///// <param name="root">The root concept.</param>
        ///// <param name="hrw">The HierarchyType.</param>
        //public Taxonomy(IConcept root, HierarchyType hrw)
        //    : this(root.Name)
        //{
        //    this._hrw = hrw;
        //    this.Root = GenerateConceptWrapper(root, hrw, null);
        //}

        public Taxonomy(IHierarchicalTerm root, HierarchyType hrw)
            : this(root.Name)
        {
            this.Root = root;
            this._hrw = hrw;
        }

        #endregion

        ///// <summary>
        ///// Generates the concept wrapper.
        ///// </summary>
        ///// <param name="root">The root.</param>
        ///// <param name="hierarchyType">Type of the hierarchy.</param>
        ///// <param name="parentTerm">The parent term.</param>
        ///// <returns></returns>
        //private IHierarchicalTerm GenerateConceptWrapper(IConcept root, HierarchyType hierarchyType, IHierarchicalTerm parentTerm)
        //{
        //    IHierarchicalTerm hierarchicalTerm = new HierarchicalConceptWrapper(root, hierarchyType, parentTerm);
        //    foreach (IBinaryRelation<IConcept> binaryRelation in root.GetRelations(hierarchyType.ParentOfRelation))
        //    {
        //        IHierarchicalTerm child = GenerateConceptWrapper(binaryRelation.RelatedEntity, hierarchyType, hierarchicalTerm);
        //        if (!hierarchicalTerm.AddChild(child))
        //            child.Parent = null;
        //    }
        //    return hierarchicalTerm;
        //}

        /// <summary>
        /// Checks if the Edge is valid.
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        private bool IsValidEdge(IHierarchicalTerm v1, IHierarchicalTerm v2)
        {
            if (v1.Equals(v2))
                return false;

            if (!Vertices.Contains(v1) || !Vertices.Contains(v2))
                return false;

            foreach (IHierarchyRelation<IHierarchicalTerm> edge in Edges)
            {
                if (!edge.Vertex1.Equals(v1) && !edge.Vertex1.Equals(v2))
                    continue;
                if (!edge.Vertex2.Equals(v1) && !edge.Vertex2.Equals(v2))
                    continue;
            }

            return false;
        }

        /// <summary>
        /// Rekursive calculation of the depth.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private int GetDepth(IHierarchicalTerm node)
        {
            if (node == null) return 0;
            int max = 0;
            foreach (IHierarchicalTerm hierarchicalTerm in node.Children)
            {
                int depth = GetDepth(hierarchicalTerm);
                if (depth > max)
                    max = depth;
            }
            return max;
        }

        /// <summary>
        /// Returns all elements which are on the same level of the tree.
        /// </summary>
        /// <param name="node">The current node.</param>
        /// <param name="currentLevel">The current level.</param>
        /// <param name="searchedLevel">A number between 0 (root) and the depth of the tree.</param>
        /// <returns></returns>
        private IEnumerable<IHierarchicalTerm> GetNodes(IHierarchicalTerm node, int currentLevel, int searchedLevel)
        {
            if (currentLevel == searchedLevel)
                yield return node;
            else if(currentLevel < searchedLevel)
            {
                currentLevel++;
                foreach (IHierarchicalTerm hierarchicalTerm in node.Children)
                {
                    GetNodes(hierarchicalTerm, currentLevel, searchedLevel);
                }
            }
        }

        #region IHierarchy<IHierachricalTerm>

        /// <summary>
        /// Return the given elements parent, grandparent and so on (the last element will always be the root).
        /// </summary>
        /// <param name="term"></param>
        /// <returns></returns>
        public IEnumerable<IHierarchicalTerm> GetAncestors(IHierarchicalTerm term)
        {
            IHierarchicalTerm currentTerm = term;
            while ((currentTerm = currentTerm.Hyperonym) != null)
                yield return currentTerm;
        }

        /// <summary>
        /// Return the subtree starting at the given node.
        /// </summary>
        /// <param name="term"></param>
        /// <returns></returns>
        public IHierarchy<IHierarchicalTerm> GetSubTree(IHierarchicalTerm term)
        {
            Taxonomy subTree = new Taxonomy(term, this._hrw);

            return subTree;
        }

        /// <summary>
        /// Gets the descendants.
        /// </summary>
        /// <param name="term">The term.</param>
        /// <returns></returns>
        private IEnumerable<IHierarchicalTerm> GetDescendants(IHierarchicalTerm term)
        {
            Queue<IHierarchicalTerm> queue = new Queue<IHierarchicalTerm>();
            queue.Enqueue(term);

            while (queue.Count > 0)
            {
                IHierarchicalTerm current = queue.Dequeue();
                foreach (var child in current.Hyponyms)
                    queue.Enqueue(child);
                yield return current;
            }
        }

        #endregion

        #region IContainer<IHierachricalTerm>

        /// <summary>
        /// Add a new element to this vocabulary.
        /// </summary>
        /// <param name="element">A new instance.</param>
        /// <returns>Success flag</returns>
        public bool Add(IHierarchicalTerm element)
        {
            if (Root != null)
                return false;

            Root = element;

            return true;
        }

        /// <summary>
        /// Delete an element from this container.
        /// </summary>
        /// <param name="element">An existing instance.</param>
        /// <returns>Success flag</returns>
        public bool Remove(IHierarchicalTerm element)
        {
            element.Hyperonym.RemoveChild(element);
            //TODO  Semantics - Delete the element or just remove from the taxonomy?
            //if (_vertices.ContainsKey(element.Name))
            //{
            //    _vertices.Remove(element.Name);

            //    //remove parent/child relations
            //    var relationsToRemove = _edges.Where(relation => relation.Members.Contains(element)).ToList();
            //    foreach (var hierarchyRelation in relationsToRemove)
            //    {
            //        _edges.Remove(hierarchyRelation);
            //    }

            //    return true;
            //}
            return false;
        }

        /// <summary>
        /// Retrieve an element by it's content or an alias.
        /// </summary>
        /// <param name="word">Name or alias of an existing element.</param>
        /// <returns>May return null if no match is found.</returns>
        public IHierarchicalTerm GetElement(string word)
        {
            return (from term in this.Content where term.Name.Equals(word) select term).FirstOrDefault();
        }

        /// <summary>
        /// Find elements which contain the given search string in their name or definition.
        /// </summary>
        /// <param name="word">Name or alias of an existing element.</param>
        /// <param name="searchDefinitions">If set to true, elements which have the given 
        /// search string in their definition will be found. Default is false.</param>
        /// <returns>May return null if no match is found.</returns>
        public IEnumerable<IHierarchicalTerm> FindElement(string word, bool searchDefinitions)
        {
            if (!searchDefinitions)
            {

                foreach (IHierarchicalTerm term in Content)
                {
                    if (term.Name.ToLower().Contains(word))

                        yield return term;
                }
            }
            else
            {
                foreach (IHierarchicalTerm term in Content)
                    if (term.Name.ToLower().Contains(word) && term.Definition.ToLower().Contains(word))
                        yield return term;
            }
        }

        #endregion

        #region ITaxonomy

        /// <summary>
        /// Returns all elements which are on the same level of the tree.
        /// </summary>
        /// <param name="level">A number between 0 (root) and the depth of the tree.</param>
        /// <returns></returns>
        public IEnumerable<IHierarchicalTerm> GetNodes(int level)
        {
            if (level < 0 && level > this.Depth)
                return null;

            return GetNodes(this.Root, 0, level);
        }

        #endregion

        IEnumerable<IVertex<Point>> IGraph<Point>.Vertices
        {
            get { return Content; }
        }

        IEnumerable<IEdge<Point>> IGraph<Point>.Edges
        {
            get { throw new NotImplementedException(); }
        }

        public bool AddVertex(IVertex<Point> vertex)
        {
            throw new NotImplementedException();
        }

        public bool AddEdge(IEdge<Point> edge)
        {
            throw new NotImplementedException();
        }

        public bool RemoveVertex(IVertex<Point> vertex)
        {
            throw new NotImplementedException();
        }

        public bool RemoveEdge(IEdge<Point> edge)
        {
            throw new NotImplementedException();
        }
    }
}
