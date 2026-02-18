using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Simulation.Engine;

namespace MatthiasToolbox.Simulation.Interfaces
{
    public interface IEntitySink : IItemSink<IEntity>
    { }

    public interface IEntitySink<T> : IItemSink<T>
        where T : IEntity
    { }
}
