using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Events;

namespace SimOpt.Simulation.Interfaces
{
    /// <summary>
    /// non generic wrapper
    /// </summary>
    public interface IEntitySource : IEntitySource<IEntity> { }

    /// <summary>
    /// less generic wrapper
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IEntitySource<T> : IItemSource<T>
        where T : IEntity
    {
        // BinaryEvent<IEntity, T> EntityCreatedEvent { get; }
    }

    /// <summary>
    /// interface for simulation objects which produce other simulation objects
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <remarks>alpha</remarks>
    public interface IEntitySource<TEntity, TData> : IItemSource<TEntity, TData> // IEntitySource<TEntity>
        where TEntity : IEntity
        where TData : class
    {
        /// <summary>
        /// The first type parameter is this (the sender / creator), 
        /// TEntity is the created entity, 
        /// TData is additional data if used, otherwise null
        /// </summary>
        // TernaryEvent<IEntity, TEntity, TData> EntityWithDataCreatedEvent { get; }
    }
}
