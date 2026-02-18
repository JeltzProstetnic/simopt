using System;
using System.Collections.Generic;
using System.Linq;
using MatthiasToolbox.Semantics.Interfaces;
using MatthiasToolbox.Basics.Datastructures.Graph;
using System.Windows;

namespace MatthiasToolbox.Semantics.Model
{
    public class Thesaurus : IThesaurus
    {
        // TODO  Semantics - implement thesaurus - not so urgent

        #region cvar

        private readonly Dictionary<string, ISemanticTerm> _vertices;
        private readonly List<IBinaryRelation<ISemanticTerm>> _edges;

        #endregion

        #region ctor

        public Thesaurus()
        {
            _vertices = new Dictionary<string, ISemanticTerm>();
            _edges = new List<IBinaryRelation<ISemanticTerm>>();
        }

        #endregion

        /// <summary>
        /// Gets the related edges of the term.
        /// </summary>
        /// <param name="term">The term.</param>
        /// <returns></returns>
        public IEnumerable<ISemanticTerm> GetRelatedEdges(ISemanticTerm term)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Checks if the Edge is valid.
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        private bool IsValidEdge(ISemanticTerm v1, ISemanticTerm v2)
        {
            if (v1.Equals(v2))
                return false;

            if (!_vertices.ContainsKey(v1.Name) || !_vertices.ContainsKey(v2.Name))
                return false;

            foreach (IBinaryRelation<ISemanticTerm> edge in Edges)
            {
                if (!edge.Vertex1.Equals(v1) && !edge.Vertex1.Equals(v2))
                    continue;
                if (!edge.Vertex2.Equals(v1) && !edge.Vertex2.Equals(v2))
                    continue;
            }

            return false;
        }

        #region IThesaurus Member

        public IEnumerable<IBinaryRelation<ISemanticTerm>> Relations
        {
            get { return _edges; }
        }

        public bool Add(IBinaryRelation<ISemanticTerm> relation)
        {
            if (IsValidEdge(relation.Owner, relation.RelatedEntity))
            {
                _edges.Add(relation);
                return true;
            }

            return false;
        }

        public bool Remove(IBinaryRelation<ISemanticTerm> relation)
        {
            throw new NotImplementedException();
        }

        public IBinaryRelation<ISemanticTerm> GetRelation(string name)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region INamedElement Member

        public string Name { get; set; }

        #endregion

        #region IContainer<ISemanticTerm> Member

        /// <summary>
        /// All elements within this container.
        /// </summary>
        public IEnumerable<ISemanticTerm> Content { get { return _vertices.Values; } }

        public bool Add(ISemanticTerm element)
        {
            if (_vertices.ContainsKey(element.Name))
            {
                _vertices.Add(element.Name, element);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes the specified element and it's subtree.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns></returns>
        public bool Remove(ISemanticTerm element)
        {
            if (_vertices.ContainsKey(element.Name))
            {
                _vertices.Remove(element.Name);

                //remove parent/child relations
                var relationsToRemove = _edges.Where(relation => relation.Members.Contains(element)).ToList();
                foreach (var hierarchyRelation in relationsToRemove)
                {
                    _edges.Remove(hierarchyRelation);
                }

                return true;
            }
            return false;
        }

        public ISemanticTerm GetElement(string word)
        {
            return _vertices[word];
        }

        public IEnumerable<ISemanticTerm> FindElement(string word, bool searchDefinitions)
        {
            if (!searchDefinitions)
            {

                foreach (ISemanticTerm term in Content)
                {
                    if (term.Name.ToLower().Contains(word))

                        yield return term;
                }
            }
            else
            {
                foreach (ISemanticTerm term in Content)
                    if (term.Name.ToLower().Contains(word) && term.Definition.ToLower().Contains(word))
                        yield return term;
            }
        }

        #endregion

        #region IGraph<IConcept,ISemanticRelation> Member

        public IEnumerable<IVertex<Point>> Vertices
        {
            get { return _vertices.Values; }
        }

        public IEnumerable<IEdge<Point>> Edges
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

        #endregion
    }
}