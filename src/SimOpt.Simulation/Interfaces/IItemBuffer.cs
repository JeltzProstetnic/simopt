using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimOpt.Simulation.Interfaces
{
    public interface IItemBuffer : IItemBuffer<object>
    { }

    public interface IItemBuffer<T> : IItemSink<T>
    {
        int Count { get; }
        bool IsFull { get; }
        // bool Put(T item);
        T Get();
        T Preview();
    }
}
