using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Semantics.Metamodel;
using MatthiasToolbox.Basics.Datastructures.Trees;
using MatthiasToolbox.Basics.Interfaces;

namespace MatthiasToolbox.Semantics.Utilities
{
    public class InstanceTreeItem : ITreeItem, INamedElement, IIdentifiable
    {
        #region cvar

        private Instance _instance;
        private Relation _relation;

        #endregion
        #region prop

        public Instance Instance { get { return _instance; } }
        public Relation Relation { get { return _relation; } }
        
        #region IIdentifiable

        public string Identifier
        {
            get { return _instance.Identifier.ToString(); }
        }

        #endregion
        #region INamedElement

        public string Name { get { return _instance.Name; } }

        #endregion
        #region ITreeItem

        public ITreeItem Parent
        {
            get
            {
                List<Instance> tmp = _instance.GetRelatedItems(_relation);
                if (tmp == null || tmp.Count == 0) return null;
                return InstanceTreeItem.From(tmp[0], _relation);
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
                List<Instance> tmp = _instance.GetReflexivelyRelatedItems(_relation);
                if (tmp == null || tmp.Count == 0) return null;
                return tmp.ConvertAll<ITreeItem>(i => InstanceTreeItem.From(i, _relation));
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

        private InstanceTreeItem(Instance instance, Relation relation)
        {
            this._instance = instance;
            this._relation = relation;
        }

        #endregion
        #region impl

        #region static factory

        public static InstanceTreeItem From(Instance instance, Relation relation)
        {
            return new InstanceTreeItem(instance, relation);
        }

        #endregion

        #endregion
    }
}