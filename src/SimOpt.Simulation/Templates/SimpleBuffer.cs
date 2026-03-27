using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Enum;
using SimOpt.Basics.Datastructures.Geometry;
using SimOpt.Simulation.Interfaces;

namespace SimOpt.Simulation.Templates
{
    /// <summary>
    /// Non-generic buffer template for <see cref="SimpleEntity"/> instances.
    /// Wraps the generic <see cref="Buffer{TEntity}"/> with <see cref="SimpleEntity"/> to simplify common use cases.
    /// </summary>
    [Serializable]
    public class SimpleBuffer : Buffer<SimpleEntity>
    {
        #region ctor

        /// <summary>
        /// Creates an uninitialized instance. <c>Initialize</c> must be called before the buffer can be used.
        /// </summary>
        public SimpleBuffer() : base() { }

        /// <summary>
        /// Creates a buffer with a standard queue rule and optional capacity limit.
        /// </summary>
        /// <param name="model">The model this buffer belongs to.</param>
        /// <param name="queueRule">The queue discipline to apply when selecting items.</param>
        /// <param name="id">Unique identifier for this entity.</param>
        /// <param name="name">Human-readable name for this entity.</param>
        /// <param name="maxCapacity">Maximum number of entities the buffer can hold.</param>
        /// <param name="states">List of valid state names.</param>
        /// <param name="transitions">List of valid state transitions as (from, to) tuples.</param>
        /// <param name="initialState">The state name to start in.</param>
        /// <param name="initialPosition">The initial spatial position of this entity.</param>
        /// <param name="manager">The resource manager responsible for this entity.</param>
        /// <param name="currentHolder">The entity currently holding this entity.</param>
        public SimpleBuffer(IModel model,
                     QueueRule queueRule = QueueRule.FIFO,
                     string id = "",
                     string name = "",
                     int maxCapacity = int.MaxValue,
                     List<string> states = null,
                     List<Tuple<string, string>> transitions = null,
                     string initialState = null,
                     Point initialPosition = null,
                     IResourceManager manager = null,
                     IEntity currentHolder = null)
            : base(model, queueRule, id, name, maxCapacity, states, transitions, initialState, initialPosition, manager, currentHolder)
        { }

        /// <summary>
        /// Creates a buffer with a fixed seed ID for reproducible random behavior and a standard queue rule.
        /// </summary>
        /// <param name="model">The model this buffer belongs to.</param>
        /// <param name="seedID">Index into the model's seed source for reproducible randomness.</param>
        /// <param name="queueRule">The queue discipline to apply when selecting items.</param>
        /// <param name="id">Unique identifier for this entity.</param>
        /// <param name="name">Human-readable name for this entity.</param>
        /// <param name="maxCapacity">Maximum number of entities the buffer can hold.</param>
        /// <param name="states">List of valid state names.</param>
        /// <param name="transitions">List of valid state transitions as (from, to) tuples.</param>
        /// <param name="initialState">The state name to start in.</param>
        /// <param name="initialPosition">The initial spatial position of this entity.</param>
        /// <param name="manager">The resource manager responsible for this entity.</param>
        /// <param name="currentHolder">The entity currently holding this entity.</param>
        public SimpleBuffer(IModel model,
                     int seedID,
                     QueueRule queueRule = QueueRule.FIFO,
                     string id = "",
                     string name = "",
                     int maxCapacity = int.MaxValue,
                     List<string> states = null,
                     List<Tuple<string, string>> transitions = null,
                     string initialState = null,
                     Point initialPosition = null,
                     IResourceManager manager = null,
                     IEntity currentHolder = null)
            : base(model, seedID, queueRule, id, name, maxCapacity, states, transitions, initialState, initialPosition, manager, currentHolder)
        { }

        /// <summary>
        /// Creates a buffer with a custom item selector function that controls dequeue order.
        /// </summary>
        /// <param name="model">The model this buffer belongs to.</param>
        /// <param name="itemSelector">A delegate that selects the next entity to dequeue.</param>
        /// <param name="id">Unique identifier for this entity.</param>
        /// <param name="name">Human-readable name for this entity.</param>
        /// <param name="maxCapacity">Maximum number of entities the buffer can hold.</param>
        /// <param name="states">List of valid state names.</param>
        /// <param name="transitions">List of valid state transitions as (from, to) tuples.</param>
        /// <param name="initialState">The state name to start in.</param>
        /// <param name="initialPosition">The initial spatial position of this entity.</param>
        /// <param name="manager">The resource manager responsible for this entity.</param>
        /// <param name="currentHolder">The entity currently holding this entity.</param>
        public SimpleBuffer(IModel model,
                     Func<SimpleEntity> itemSelector,
                     string id = "",
                     string name = "",
                     int maxCapacity = int.MaxValue,
                     List<string> states = null,
                     List<Tuple<string, string>> transitions = null,
                     string initialState = null,
                     Point initialPosition = null,
                     IResourceManager manager = null,
                     IEntity currentHolder = null)
            : base(model, itemSelector, id, name, maxCapacity, states, transitions, initialState, initialPosition, manager, currentHolder)
        { }

        /// <summary>
        /// Creates a buffer with a custom item selector and a fixed seed ID for reproducible randomness.
        /// </summary>
        /// <param name="model">The model this buffer belongs to.</param>
        /// <param name="itemSelector">A delegate that selects the next entity to dequeue.</param>
        /// <param name="seedID">Index into the model's seed source for reproducible randomness.</param>
        /// <param name="id">Unique identifier for this entity.</param>
        /// <param name="name">Human-readable name for this entity.</param>
        /// <param name="maxCapacity">Maximum number of entities the buffer can hold.</param>
        /// <param name="states">List of valid state names.</param>
        /// <param name="transitions">List of valid state transitions as (from, to) tuples.</param>
        /// <param name="initialState">The state name to start in.</param>
        /// <param name="initialPosition">The initial spatial position of this entity.</param>
        /// <param name="manager">The resource manager responsible for this entity.</param>
        /// <param name="currentHolder">The entity currently holding this entity.</param>
        public SimpleBuffer(IModel model,
                     Func<SimpleEntity> itemSelector,
                     int seedID,
                     string id = "",
                     string name = "",
                     int maxCapacity = int.MaxValue,
                     List<string> states = null,
                     List<Tuple<string, string>> transitions = null,
                     string initialState = null,
                     Point initialPosition = null,
                     IResourceManager manager = null,
                     IEntity currentHolder = null)
            : base(model, itemSelector, seedID, id, name, maxCapacity, states, transitions, initialState, initialPosition, manager, currentHolder)
        { }

        #endregion
    }
}
