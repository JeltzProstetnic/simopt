using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Simulation.Interfaces
{
    public interface IItemSink : IItemSink<object>
    { }

    public interface IItemSink<T>
    {
        bool Put(T item);
        bool ConnectTo(IItemSource<T> source);
        void ConnectTo(params IItemSource<T>[] sources);
        void ConnectTo(IEnumerable<IItemSource<T>> sources);
        bool IsConnectionAllowed(IItemSource<T> fromSource);
    }
}
