using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Simulation.Engine;

namespace MatthiasToolbox.Simulation.Interfaces
{
    public interface IEntityBuffer : IEntityBuffer<IEntity>
    { }

    public interface IEntityBuffer<T> : IItemBuffer<T>, IItemSink<T>
        where T : IEntity
    {
    }
}
