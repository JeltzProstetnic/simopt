using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Basics.Datastructures.Trees;
using MatthiasToolbox.Semantics.Metamodel;

namespace MatthiasToolbox.Semantics.Utilities
{
    public class InstanceTree : ITree
    {
        private Instance _root;
        private Relation _relation;

        public InstanceTree(Relation relation)
        {
            this._relation = relation;
            this._root = relation.FindRoot();
        }

        #region ITree

        public ITreeItem Root
        {
            get { return InstanceTreeItem.From(_root, _relation); }
            set 
            {
                if (value is InstanceTreeItem)
                    _root = (value as InstanceTreeItem).Instance;
                else
                    throw new ArgumentException("The Root of an instance tree must be an InstanceTreeItem.", "value");
            }
        }

        #endregion
    }
}
