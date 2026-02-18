using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Simulation.Engine;
using MatthiasToolbox.Simulation.Events;

namespace MatthiasToolbox.Simulation.Interfaces
{
    /// <summary>
    /// non generic wrapper
    /// </summary>
    public interface IItemSource : IItemSource<object> { }

    /// <summary>
    /// less generic wrapper
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IItemSource<T>
    {
        BinaryEvent<IEntity, T> EntityCreatedEvent { get; }
        bool ConnectTo(IItemSink<T> sink);
    }

    /// <summary>
    /// interface for simulation objects which produce other simulation objects
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <remarks>alpha</remarks>
    public interface IItemSource<TEntity, TData> : IItemSource<TEntity>
    {
        /// <summary>
        /// The first type parameter is this (the sender / creator), 
        /// TEntity is the created entity, 
        /// TData is additional data if used, otherwise null
        /// </summary>
        TernaryEvent<IEntity, TEntity, TData> EntityWithDataCreatedEvent { get; }
    }
}
