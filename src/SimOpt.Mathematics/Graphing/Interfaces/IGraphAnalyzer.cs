using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Basics.Datastructures.Network;

namespace SimOpt.Mathematics.Graphing.Interfaces
{
    public interface IGraphAnalyzer<T>
        where T : IComparable<T>
    {
        void AnalyzeGraph(INetwork<T> network);
    }
}
