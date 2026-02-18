using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Semantics.Interfaces
{
    public interface IView<TModel, TVertex, TEdge>
    {
        TModel Model { get; }

        IEnumerable<TVertex> VisibleNodes { get; }

        IEnumerable<TEdge> VisibleEdges { get; }
    }
}
