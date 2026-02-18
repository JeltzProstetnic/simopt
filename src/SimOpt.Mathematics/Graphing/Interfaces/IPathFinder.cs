using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Basics.Datastructures.Network;

namespace SimOpt.Mathematics.Graphing.Interfaces
{
    public interface IPathFinder<T>
        where T : IComparable<T>
    {
        bool FindShortestPath(INetwork<T> network, INode<T> fromNode, INode<T> toNode, out IPath<T> result, bool cacheData);
    }
}
