using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Basics.Datastructures.Trees;
using MatthiasToolbox.Semantics.Metamodel;

namespace MatthiasToolbox.Semantics.Utilities
{
    public class ConceptTree : ITree
    {
        private Concept _root;

        public ConceptTree(Ontology o)
        {
            this._root = o.RootConcept;
        }

        #region ITree

        public ITreeItem Root
        {
            get { return ConceptTreeItem.From(_root); }
            set 
            {
                if (value is ConceptTreeItem)
                    _root = (value as ConceptTreeItem).Concept;
                else
                    throw new ArgumentException("The Root of a concept tree must be a ConceptTreeItem.", "value");
            }
        }

        #endregion
    }
}
