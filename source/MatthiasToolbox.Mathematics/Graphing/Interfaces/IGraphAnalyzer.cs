using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Basics.Datastructures.Network;

namespace MatthiasToolbox.Mathematics.Graphing.Interfaces
{
    public interface IGraphAnalyzer<T>
        where T : IComparable<T>
    {
        void AnalyzeGraph(INetwork<T> network);
    }
}
