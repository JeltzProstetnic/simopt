using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Simulation.Interfaces;
using System.IO;
using MatthiasToolbox.Simulation.Enum;
using MatthiasToolbox.Simulation.Tools;

namespace MatthiasToolbox.Simulation.Engine
{
    /// <summary>
    /// Interface for a simulation model.
    /// 
    /// The model is responsible for the management of most model components and controls the event scheduler. Therefore it contains
    /// methods to add and remove events and entities. It also keeps track of the current simulation time and allows starting, pausing
    /// and stopping simulation experiments. The default implementation additionally allows stepping through a simulation in freely 
    /// definable time steps and simulation based conventional and conditional breakpoints. These help in debugging or analyzing simulations
    /// by allowing to pause the model execution at a certain simulation time without entering actual break mode as in normal debugging.
    /// 
    /// The model interface has been kept as small as possible to minimize the effort required to implement a model class and to maximize 
    /// the designers freedom in doing so. Further interface members may be implemented as needed but this should be considered carefully 
    /// in every case. In most cases it will be easier to extend or modify the model class to meet the requirements. Nevertheless further 
    /// growth of the interface will be unavoidable whenever a part of the engine is extended which is not directly referencing the 
    /// default implementation.
    /// </summary>
    /// <remarks>rc</remarks>
    public interface IModel : IResettable, IRandom, ISeedSource
    {
        #region evnt

        /// <summary>
        /// Invoke this when a simulation run was finished.
        /// </summary>
        event SimulationFinishedHandler SimulationFinished;

        #endregion
        #region prop

        /// <summary>
        /// The unique name of the model instance.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Indicate if the model instance is busy simulating.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Indicate if the model instance is busy simulating.
        /// </summary>
        bool IsPaused { get; }

        /// <summary>
        /// Indicate if the model instance is busy simulating.
        /// </summary>
        bool IsInterrupted { get; }

        /// <summary>
        /// Indicate if the model instance is stopped.
        /// </summary>
        bool IsStopped { get; }

        /// <summary>
        /// Indicate if a predefined ending time has been reached.
        /// </summary>
        bool IsTimeElapsed { get; }

        /// <summary>
        /// The current state of the model.
        /// </summary>
        ExecutionState CurrentState { get; }

        /// <summary>
        /// the simulation time for the model to start with
        /// </summary>
        double StartTime { get; set; }

        /// <summary>
        /// during a run return the "current" simulation time.
        /// </summary>
        double CurrentTime { get; }

        /// <summary>
        /// Return true only during reset if the seed was just changed.
        /// </summary>
        bool SeedChange { get; }

        /// <summary>
        /// a default resource manager
        /// </summary>
        IResourceManager DefaultResourceManager { get; set; }

        /// <summary>
        /// gets or sets the state of the logging flag
        /// </summary>
        bool LoggingEnabled { get; set; }

        /// <summary>
        /// The event which is currently being processed (if any) or null.
        /// </summary>
        IEventInstance CurrentEvent { get; }

        /// <summary>
        /// Has a pause been requested from the model?
        /// </summary>
        bool IsInterruptRequested { get; }

        #endregion
        #region impl

        #region main

        ModelState Save();
        bool Load(ModelState state);

        #endregion
        #region ctrl

        /// <summary>
        /// start the simulation
        /// </summary>
        void Start();

        /// <summary>
        /// stop the simulation
        /// </summary>
        void Stop();

        /// <summary>
        /// Suspend simulation run. The current point in time will be finished first.
        /// </summary>
        void Pause();

        /// <summary>
        /// Suspend simulation run. The current point in time may not be finished.
        /// </summary>
        void Interrupt();

        #endregion
        #region sobj

        /// <summary>
        /// Associate the given entity with this model.
        /// </summary>
        /// <param name="obj"></param>
        void AddEntity(IEntity obj);

        /// <summary>
        /// Check if a simulation object is contained in the model
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        bool HasEntity(string id);

        /// <summary>
        /// retrieve a simulation object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        T GetEntity<T>(string id);

        /// <summary>
        /// returns all objects of the given type
        /// </summary>
        /// <returns></returns>
        IEnumerable<T> FindEntities<T>();

        #endregion
        #region evnt

        /// <summary>
        /// schedule an action at currenttime + delay
        /// </summary>
        /// <param name="delay"></param>
        /// <param name="a"></param>
        void Schedule(double delay, Action a);

        /// <summary>
        /// Schedule an event at currenttime + timespan.
        /// Caution: an exception will be thrown if the
        /// specified point in time is less or equal now.
        /// </summary>
        /// <param name="timeSpan"></param>
        /// <param name="evnt"></param>
        void AddEvent(double timeSpan, IEventInstance evnt);

        /// <summary>
        /// Schedule an event at the specified point in time.
        /// Caution: an exception should be thrown if the
        /// specified point in time is less or equal now.
        /// </summary>
        /// <param name="pointInTime"></param>
        /// <param name="evnt"></param>
        void AddEventAt(double pointInTime, IEventInstance evnt);

#if ImmediateEvents
        /// <summary>
        /// Schedule an event to be raised at the current
        /// point in time. This should happen after all other
        /// events from the current time have been processed.
        /// Caution: the priority should be set to LowLevelAfterOthers. 
        /// The priority number should be kept and respected, though.
        /// </summary>
        /// <param name="evnt"></param>
        void AddImmediateEvent(IEventInstance evnt);
#endif

        /// <summary>
        /// Remove an event which was already scheduled.
        /// </summary>
        /// <param name="evnt"></param>
        void RemoveEvent(IEventInstance evnt);

        #endregion

        #endregion
    }
}