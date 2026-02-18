using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Basics.Datastructures.Trees
{
    public interface ITree<T>
    {
        T Root { get; }
    }

    public interface ITree : ITree<ITreeItem>
    {

    }
}