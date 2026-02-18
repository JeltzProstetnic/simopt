using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Basics.Datastructures.Trees;
using MatthiasToolbox.Semantics.Metamodel;
using MatthiasToolbox.Basics.Interfaces;

namespace MatthiasToolbox.Semantics.Utilities
{
    public class ConceptTreeItem : ITreeItem, INamedElement, IIdentifiable
    {
        #region cvar

        private Concept _concept;

        #endregion
        #region prop

        public Concept Concept { get { return _concept; } }

        #region IIdentifiable

        public string Identifier
        {
            get { return _concept.Identifier.ToString(); }
        }

        #endregion
        #region INamedElement

        public string Name { get { return _concept.Name; } }

        #endregion
        #region ITreeItem

        public ITreeItem Parent
        {
            get
            {
                if (_concept.Parent == null) return null;
                return ConceptTreeItem.From(_concept.Parent);
            }
            set
            {
                // TODO: implement to allow prune & graft
                throw new NotImplementedException();
            }
        }

        public List<ITreeItem> Children
        {
            get
            {
                List<Concept> result = Concept.Children.ToList();
                if (result == null || result.Count == 0) return null;
                return result.ConvertAll<ITreeItem>(c => ConceptTreeItem.From(c));
            }
            set
            {
                // TODO: implement to allow prune & graft
                throw new NotImplementedException();
            }
        }

        #endregion

        #endregion
        #region ctor

        private ConceptTreeItem(Concept concept)
        {
            this._concept = concept;
        }

        #endregion
        #region impl

        #region static factory

        public static ConceptTreeItem From(Concept concept)
        {
            return new ConceptTreeItem(concept);
        }

        #endregion

        #endregion
    }
}