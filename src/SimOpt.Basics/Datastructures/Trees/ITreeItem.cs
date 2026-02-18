using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Basics.Interfaces;

namespace SimOpt.Basics.Datastructures.Trees
{
    public interface ITreeItem : IIdentifiable
    {
        /// <summary>
        /// return null for root
        /// </summary>
        ITreeItem Parent { get; set; }

        /// <summary>
        /// return null or empty list for leaf nodes
        /// </summary>
        List<ITreeItem> Children { get; set; }
    }
}