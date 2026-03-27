using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Engine;
using SimOpt.Mathematics.Stochastics.Interfaces;
using SimOpt.Basics.Datastructures.Geometry;
using SimOpt.Simulation.Interfaces;

namespace SimOpt.Simulation.Templates
{
    /// <summary>
    /// Non-generic server template for processing <see cref="SimpleEntity"/> instances.
    /// Wraps the generic <see cref="Server{TInput,TOutput}"/> with <see cref="SimpleEntity"/> for both input and output to simplify common use cases.
    /// </summary>
    [Serializable]
    public class SimpleServer : Server<SimpleEntity, SimpleEntity>
    {
        #region ctor

        /// <summary>
        /// Creates an uninitialized instance. <c>Initialize</c> must be called before the server can be used.
        /// </summary>
        public SimpleServer() : base() { }

        /// <summary>
        /// Creates a server with a stochastic machining time distribution, optional failure model, and optional auto-start.
        /// </summary>
        /// <param name="model">The model this server belongs to.</param>
        /// <param name="machiningTime">Distribution used to sample the service duration.</param>
        /// <param name="id">Unique identifier for this entity.</param>
        /// <param name="name">Human-readable name for this entity.</param>
        /// <param name="createProduct">Optional delegate to create the output entity from the input batch.</param>
        /// <param name="timeToFailure">Optional distribution for time between failures.</param>
        /// <param name="timeToRecover">Optional distribution for repair/recovery time after a failure.</param>
        /// <param name="checkMaterialUsable">Optional predicate to validate a single input entity before acceptance.</param>
        /// <param name="checkMaterialComplete">Optional predicate to check if the accumulated input batch is complete.</param>
        /// <param name="states">List of valid state names.</param>
        /// <param name="transitions">List of valid state transitions as (from, to) tuples.</param>
        /// <param name="initialState">The state name to start in.</param>
        /// <param name="initialPosition">The initial spatial position of this entity.</param>
        /// <param name="manager">The resource manager responsible for this entity.</param>
        /// <param name="currentHolder">The entity currently holding this entity.</param>
        /// <param name="autoStartDelay">Delay after model start before the server begins operation; <see cref="double.NaN"/> disables auto-start.</param>
        public SimpleServer(IModel model,
                      IDistribution<double> machiningTime,
                      string id = "",
                      string name = "",
                      Func<List<SimpleEntity>, SimpleEntity> createProduct = null,
                      IDistribution<double> timeToFailure = null,
                      IDistribution<double> timeToRecover = null,
                      Func<SimpleEntity, bool> checkMaterialUsable = null,
                      Func<List<SimpleEntity>, bool> checkMaterialComplete = null,
                      List<string> states = null,
                      List<Tuple<string, string>> transitions = null,
                      string initialState = null,
                      Point initialPosition = null,
                      IResourceManager manager = null,
                      IEntity currentHolder = null,
                      double autoStartDelay = double.NaN)
            : base(model, machiningTime, id, name, createProduct, timeToFailure, timeToRecover, checkMaterialUsable, checkMaterialComplete,
                   states, transitions, initialState, initialPosition, manager, currentHolder, autoStartDelay)
        { }

        /// <summary>
        /// Creates a server with a fixed seed ID, a stochastic machining time distribution, and optional failure model.
        /// </summary>
        /// <param name="model">The model this server belongs to.</param>
        /// <param name="seedID">Index into the model's seed source for reproducible randomness.</param>
        /// <param name="machiningTime">Distribution used to sample the service duration.</param>
        /// <param name="id">Unique identifier for this entity.</param>
        /// <param name="name">Human-readable name for this entity.</param>
        /// <param name="createProduct">Optional delegate to create the output entity from the input batch.</param>
        /// <param name="timeToFailure">Optional distribution for time between failures.</param>
        /// <param name="timeToRecover">Optional distribution for repair/recovery time after a failure.</param>
        /// <param name="checkMaterialUsable">Optional predicate to validate a single input entity before acceptance.</param>
        /// <param name="checkMaterialComplete">Optional predicate to check if the accumulated input batch is complete.</param>
        /// <param name="states">List of valid state names.</param>
        /// <param name="transitions">List of valid state transitions as (from, to) tuples.</param>
        /// <param name="initialState">The state name to start in.</param>
        /// <param name="initialPosition">The initial spatial position of this entity.</param>
        /// <param name="manager">The resource manager responsible for this entity.</param>
        /// <param name="currentHolder">The entity currently holding this entity.</param>
        /// <param name="autoStartDelay">Delay after model start before the server begins operation; <see cref="double.NaN"/> disables auto-start.</param>
        public SimpleServer(IModel model,
                      int seedID,
                      IDistribution<double> machiningTime,
                      string id = "",
                      string name = "",
                      Func<List<SimpleEntity>, SimpleEntity> createProduct = null,
                      IDistribution<double> timeToFailure = null,
                      IDistribution<double> timeToRecover = null,
                      Func<SimpleEntity, bool> checkMaterialUsable = null,
                      Func<List<SimpleEntity>, bool> checkMaterialComplete = null,
                      List<string> states = null,
                      List<Tuple<string, string>> transitions = null,
                      string initialState = null,
                      Point initialPosition = null,
                      IResourceManager manager = null,
                      IEntity currentHolder = null,
                      double autoStartDelay = double.NaN)
            : base(model, seedID, machiningTime, id, name, createProduct, timeToFailure, timeToRecover, checkMaterialUsable, checkMaterialComplete,
                   states, transitions, initialState, initialPosition, manager, currentHolder, autoStartDelay)
        { }

        /// <summary>
        /// Creates a server with a delegate-based machining time that can depend on the current input batch.
        /// </summary>
        /// <param name="model">The model this server belongs to.</param>
        /// <param name="machiningTimeDelegate">A delegate returning the service duration based on the current input entities.</param>
        /// <param name="id">Unique identifier for this entity.</param>
        /// <param name="name">Human-readable name for this entity.</param>
        /// <param name="createProduct">Optional delegate to create the output entity from the input batch.</param>
        /// <param name="checkMaterialUsable">Optional predicate to validate a single input entity before acceptance.</param>
        /// <param name="checkMaterialComplete">Optional predicate to check if the accumulated input batch is complete.</param>
        /// <param name="states">List of valid state names.</param>
        /// <param name="transitions">List of valid state transitions as (from, to) tuples.</param>
        /// <param name="initialState">The state name to start in.</param>
        /// <param name="initialPosition">The initial spatial position of this entity.</param>
        /// <param name="manager">The resource manager responsible for this entity.</param>
        /// <param name="currentHolder">The entity currently holding this entity.</param>
        /// <param name="autoStartDelay">Delay after model start before the server begins operation; <see cref="double.NaN"/> disables auto-start.</param>
        public SimpleServer(IModel model,
                      Func<List<SimpleEntity>, double> machiningTimeDelegate,
                      string id = "",
                      string name = "",
                      Func<List<SimpleEntity>, SimpleEntity> createProduct = null,
                      Func<SimpleEntity, bool> checkMaterialUsable = null,
                      Func<List<SimpleEntity>, bool> checkMaterialComplete = null,
                      List<string> states = null,
                      List<Tuple<string, string>> transitions = null,
                      string initialState = null,
                      Point initialPosition = null,
                      IResourceManager manager = null,
                      IEntity currentHolder = null,
                      double autoStartDelay = double.NaN)
            : base(model, machiningTimeDelegate, id, name, createProduct, checkMaterialUsable, checkMaterialComplete, states, transitions,
                   initialState, initialPosition, manager, currentHolder, autoStartDelay)
        { }

        /// <summary>
        /// Creates a server with a fixed seed ID and a delegate-based machining time.
        /// </summary>
        /// <param name="model">The model this server belongs to.</param>
        /// <param name="seedID">Index into the model's seed source for reproducible randomness.</param>
        /// <param name="machiningTimeDelegate">A delegate returning the service duration based on the current input entities.</param>
        /// <param name="id">Unique identifier for this entity.</param>
        /// <param name="name">Human-readable name for this entity.</param>
        /// <param name="createProduct">Optional delegate to create the output entity from the input batch.</param>
        /// <param name="checkMaterialUsable">Optional predicate to validate a single input entity before acceptance.</param>
        /// <param name="checkMaterialComplete">Optional predicate to check if the accumulated input batch is complete.</param>
        /// <param name="states">List of valid state names.</param>
        /// <param name="transitions">List of valid state transitions as (from, to) tuples.</param>
        /// <param name="initialState">The state name to start in.</param>
        /// <param name="initialPosition">The initial spatial position of this entity.</param>
        /// <param name="manager">The resource manager responsible for this entity.</param>
        /// <param name="currentHolder">The entity currently holding this entity.</param>
        /// <param name="autoStartDelay">Delay after model start before the server begins operation; <see cref="double.NaN"/> disables auto-start.</param>
        public SimpleServer(IModel model,
                      int seedID,
                      Func<List<SimpleEntity>, double> machiningTimeDelegate,
                      string id = "",
                      string name = "",
                      Func<List<SimpleEntity>, SimpleEntity> createProduct = null,
                      Func<SimpleEntity, bool> checkMaterialUsable = null,
                      Func<List<SimpleEntity>, bool> checkMaterialComplete = null,
                      List<string> states = null,
                      List<Tuple<string, string>> transitions = null,
                      string initialState = null,
                      Point initialPosition = null,
                      IResourceManager manager = null,
                      IEntity currentHolder = null,
                      double autoStartDelay = double.NaN)
            : base(model, seedID, machiningTimeDelegate, id, name, createProduct, checkMaterialUsable, checkMaterialComplete, states, transitions,
                   initialState, initialPosition, manager, currentHolder, autoStartDelay)
        { }

        #endregion
    }
}
