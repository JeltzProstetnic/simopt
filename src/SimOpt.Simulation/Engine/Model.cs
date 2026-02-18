using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Simulation;
using SimOpt.Simulation.Enum;
using SimOpt.Mathematics.Stochastics;
using SimOpt.Mathematics.Stochastics.RandomSources;
using SimOpt.Simulation.Exceptions;
using SimOpt.Simulation.Events;
using SimOpt.Mathematics.Stochastics.Distributions;
using SimOpt.Simulation.Interfaces;
using SimOpt.Simulation.Tools;
using SimOpt.Basics.Exceptions;
using SimOpt.Simulation.Network;
using System.IO;
using System.Runtime.Serialization;
using SimOpt.Basics.Interfaces;
using SimOpt.Basics.Datastructures.Collections;

namespace SimOpt.Simulation.Engine
{
    /// <summary>
    /// A model for discrete simulation. This implementation manages entities as well as random seeds and numbers.
    /// 
    /// This default model implementation acts as the central seed source for the complete simulation. Further seed sources 
    /// will be initialized using a seed which is provided by the model's central seed generator. This seed generator allows 
    /// the retrieval of indexed seeds to make them independent from the order in which they are retrieved, so that the same 
    /// simulation using the same seed will produce the same results even if the order in which model components are created 
    /// and added to the model changes.
    /// 
    /// This architecture also ensures that all random generators (including dynamically created ones) are set to their antithetic 
    /// mode if the model's antithetic-switch is used. The same is valid for the NonStochasticMode switch. Altogether this allows
    /// a very dynamic approach to discrete simulation, allowing the user to even change the model itself as a part of his simulation 
    /// and still have reproducible results.
    /// 
    /// Finally the default model implementation contains events which can be used to hook into the starting and finishing processes 
    /// of the simulation and a default resource manager for the use in standard resource seizing scenarios.
    /// </summary>
    /// <remarks>rc</remarks>
    [Serializable]
    public class Model : IResettable, IRandom, ISeedSource, IModel
    {
        #region over

        public override string ToString()
        {
            return Name;
        }

        #endregion
        #region cvar

        private static int instanceCounter = 0; // counter for auto-ids

        // main data
        private Dictionary<string, IIdentifiable> items;
        //private Dictionary<string, IEntity> simulationObjects;      // contained objects
        
        private List<IRandom> randomGenerators;                     // associated random distributions
        private SortedDictionary<double, Func<bool>> breakPoints;   // conditional breakpoints   
        internal EventScheduler eventScheduler;                     // event scheduler
        private List<IResettable> resettables;

        // flags
        private bool initialized = false;
        private bool requestStop = false;
        private bool requestPause = false;
        private bool requestInterrupt = false;

        // timing
        private double startTime;
        private double currentTime;
        private double endingTime;
        private double stepSize = 1;
        
        // stats
        private TimeSpan lastRunDuration;
        private DateTime realStartTime;

        // random stuff
        private static int defaultSeed = 0; // not included in serialization
        private bool seedChange = false;
        private int seed;
        private UniformIntegerDistribution seedGenerator;
        private bool nonStochasticMode = false;
        private bool antithetic = false;
        
        // convenience
        private UnaryEvent<Action> actionEvent;

        #endregion
        #region evnt

        private event Action InternalSimulationFinishedEvent;   // event fires after the simulation has finished.
        private event Action InternalSimulationStartedEvent;    // event fires before the simulation has started.
        public event SimulationFinishedHandler SimulationFinished; // event fires before finished logging
        public event Action SimulationTerminating;              // event fires after the finished logging

        #endregion
        #region prop

        #region main

        /// <summary>
        /// a name for the model
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Undefined, Stopped, Paused, Interrupted, TimeElapsed, InBreakPoint, Running
        /// </summary>
        public ExecutionState CurrentState { get; private set; }


        private Dictionary<string, IEntity> _simulationObjects;
        /// <summary>
        /// all simulation objects which are contained in the model
        /// </summary>
        [Obsolete("Use Model.Items instead.")]
        public Dictionary<string, IEntity> SimulationObjects
        {
            get 
            {
                foreach (IIdentifiable item in items.Values)
                {
                    if(item is IEntity)
                        _simulationObjects[item.Identifier] = item as IEntity;
                }
                return _simulationObjects;
            }
            // set { simulationObjects = value; }
        }

        /// <summary>
        /// all identifiable items (e.g. entities) which are contained in the model
        /// </summary>
        public Dictionary<string, IIdentifiable> Items
        {
            get { return items; }
        }

        /// <summary>
        /// indicates if the model was just reset or newly created.
        /// only run and step calls will set this flag to false. 
        /// manual changes to the model are not reflected!
        /// </summary>
        public bool IsReset { get; private set; }

        /// <summary>
        /// The event which is currently being processed (if any) or null.
        /// </summary>
        public IEventInstance CurrentEvent { get { return eventScheduler.CurrentEvent; } }

        #endregion
        #region rand

        /// <summary>
        /// The main random seed on which all other seeds depend.
        /// If possible, set this before adding simulation objects to the model, otherwise
        /// all simulation objects already contained within this model will have their
        /// seed reset. This would cost more processing time than first setting the seed.
        /// Caution: setting this during a run will throw an exception
        /// </summary>
        public int? Seed
        {
            get { return seed; }
            set
            {
                if (CurrentState == ExecutionState.Running) throw new AccessViolationException("The Random Seed cannot be changed while the Simulation is running.");
                if (value == null) throw new ArgumentException("The seed cannot be set to null.");
                Reset((int)value); // private seed var is updated in reset
            }
        }

        /// <summary>
        /// Caution: attempting to set this during a simulation run will result in an exception
        /// </summary>
        public bool Antithetic
        {
            get
            {
                return antithetic;
            }
            set
            {
                if (CurrentState == ExecutionState.Running) throw new AccessViolationException("The Random Seed cannot be changed while the Simulation is running.");
                Reset(value); // private antithetic var is updated in reset
            }
        }

        /// <summary>
        /// Gets or sets a flag, indicating if the random distributions are set to
        /// act stochastically or not. (switching it off will make most distributions
        /// return their mean value perpetuously.) This affects all simulation objects
        /// belonging to this model.
        /// Caution: attempting to set this during a simulation run will result in an exception
        /// </summary>
        public bool NonStochasticMode
        {
            get
            {
                return nonStochasticMode;
            }
            set
            {
                if (CurrentState == ExecutionState.Running) throw new AccessViolationException("The Random Seed cannot be changed while the Simulation is running.");
                Reset(antithetic, value); // private nonStochasticMode variable is updated in reset
            }
        }

        /// <summary>
        /// Source for all object seeds.
        /// </summary>
        public UniformIntegerDistribution SeedGenerator
        {
            get { return seedGenerator; }
            set { seedGenerator = value; }
        }

        /// <summary>
        /// All Random Generators which are managed by the model itself.
        /// </summary>
        public IEnumerable<IRandom> RandomGenerators
        {
            get { return randomGenerators; }
        }

        /// <summary>
        /// This will return true only during reset if the seed was just changed.
        /// </summary>
        public bool SeedChange { get { return seedChange; } }

        #endregion
        #region time
        
        /// <summary>
        /// flag indicating if the simulation is currently running
        /// </summary>
        public bool IsRunning
        {
            get { return CurrentState == ExecutionState.Running; }
        }

        /// <summary>
        /// Indicate if the model instance is busy simulating.
        /// </summary>
        public bool IsPaused { get { return CurrentState == ExecutionState.Paused; } }

        /// <summary>
        /// Indicate if the model instance is busy simulating.
        /// </summary>
        public bool IsInterrupted { get { return CurrentState == ExecutionState.Interrupted; } }

        /// <summary>
        /// Indicate if the model instance is stopped.
        /// </summary>
        public bool IsStopped { get { return CurrentState == ExecutionState.Stopped; } }

        /// <summary>
        /// Indicates if the model is stopped due to a predefined ending time.
        /// </summary>
        public bool IsTimeElapsed { get { return CurrentState == ExecutionState.TimeElapsed; } }
        
        /// <summary>
        /// the simulation time for the model to start with
        /// caution: setting this this will also set the current time to start time
        /// </summary>
        public double StartTime
        {
            get { return startTime; }
            set
            {
                startTime = value;
                currentTime = value;
            }
        }

        /// <summary>
        /// as long as the model is not running, this will contain the same value
        /// as StartTime. during a run it will return the "current" simulation time.
        /// </summary>
        public double CurrentTime
        {
            get { return currentTime; }
        }

        /// <summary>
        /// Converted using extension
        /// </summary>
        public DateTime CurrentDateTime { get { return currentTime.ToDateTime(); } }

        /// <summary>
        /// the step size to be used with Step()
        /// </summary>
        public double StepSize
        {
            get { return stepSize; }
            set { stepSize = value; }
        }

        public bool IsInterruptRequested { get { return requestInterrupt; } }

        #endregion
        #region logg

        /// <summary>
        /// Getter returns the same as LogEvents.
        /// Setter will switch LogStart, LogEvents AND LogFinish
        /// </summary>
        public bool LoggingEnabled
        {
            get
            {
                return LogEvents;
            }
            set
            {
                LogStart = value;
                LogEvents = value;
                LogFinish = value;
            }
        }

        /// <summary>
        /// switch the event logging on or off.
        /// only events with their Log switch set to true will be logged
        /// even if LogEvents is set to true.
        /// </summary>
        public bool LogEvents
        {
            get { return eventScheduler.Logging; }
            set { eventScheduler.Logging = value; }
        }

        /// <summary>
        /// switch finished-logging
        /// if set to true, some run statistics will be logged after the simulation run has finished
        /// </summary>
        public bool LogFinish { get; set; }

        /// <summary>
        /// switch start-logging
        /// if set to true, the start of a simulation run will be logged
        /// </summary>
        public bool LogStart { get; set; }

        #endregion
        #region stat

        /// <summary>
        /// real world duration of the last run
        /// </summary>
        public TimeSpan LastRunDuration
        {
            get { return lastRunDuration; }
        }

        /// <summary>
        /// number of events processed since the last reset
        /// </summary>
        public int EventCounter { get { return eventScheduler.EventCounter; } }

        /// <summary>
        /// number of handlers processed since the last reset
        /// </summary>
        public int HandlerCounter { get { return eventScheduler.HandlerCounter; } }

        #endregion
        #region tool

        /// <summary>
        /// the default resource manager for resource objects 
        /// which do not set it explicitely
        /// </summary>
        public IResourceManager DefaultResourceManager { get; set; }

        /// <summary>
        /// Fires at start-time, before any other simulation events occur.
        /// NOTE: this event is not counted to the statistics and does not
        /// appear in event logging
        /// The second parameter contains the next event which is scheduled
        /// to occure next or null if no events are scheduled yet.
        /// </summary>
        public BinaryEvent<IModel, IEventInstance> SimulationStartedEvent { get; private set; }

        /// <summary>
        /// Fires just before the finished-logging. This will be the last
        /// code being executed by the model after the simulation
        /// is finished.
        /// NOTE: handlers may add further events but those will not be simulated.
        /// NOTE: this event is not counted to the statistics and does not
        /// appear in event logging
        /// The second parameter contains the last event instance which was processed.
        /// </summary>
        public BinaryEvent<IModel, IEventInstance> SimulationFinishedEvent { get; private set; }

        #endregion

        #endregion
        #region ctor

        /// <summary>
        /// Creates a new model. The start time may be changed later, but be aware that this 
        /// may result in already scheduled events to suddenly be in the past, which will
        /// lead to undefined and inconsistent behavior!
        /// Caution: the seed will be set to the defaultSeed value and the start time to zero!
        /// CAUTION: You must initialize the DefaultResourceManager manually!
        /// </summary>
        public Model() : this("Model" + instanceCounter++.ToString(), defaultSeed, 0d) { }

        /// <summary>
        /// Creates a new model. The start time may be changed later, but be aware that this 
        /// may result in already scheduled events to suddenly be in the past, which will
        /// lead to undefined and inconsistent behavior!
        /// CAUTION: You must initialize the DefaultResourceManager manually!
        /// </summary>
        /// <param name="name"></param>
        /// <param name="seed"></param>
        /// <param name="startTime"></param>
        public Model(String name, int seed, DateTime startTime, bool antithetic = false, bool nonStochasticMode = false) 
            : this(name, seed, startTime.ToDouble(), antithetic, nonStochasticMode) { }

        /// <summary>
        /// Creates a new model. The start time may be changed later, but be aware that this 
        /// may result in already scheduled events to suddenly be in the past, which will
        /// lead to undefined and inconsistent behavior!
        /// CAUTION: You must initialize the DefaultResourceManager manually!
        /// </summary>
        /// <param name="name"></param>
        /// <param name="seed"></param>
        /// <param name="startTime"></param>
        public Model(String name, int seed, double startTime, bool antithetic = false, bool nonStochasticMode = false) 
        {
            // init primitive cvars
			this.Name = name;
            this.seed = seed;
            this.antithetic = antithetic;
            this.nonStochasticMode = nonStochasticMode;
			this.StartTime = startTime; // also sets currentTime

			// create object cvars
            seedGenerator = new UniformIntegerDistribution(seed, 0, int.MaxValue, this.antithetic);
            eventScheduler = new EventScheduler(this);
            randomGenerators = new List<IRandom>();
			items = new Dictionary<string, IIdentifiable>();
            breakPoints = new SortedDictionary<double,Func<bool>>();
            actionEvent = new UnaryEvent<Action>(Name + ".ActionEvent");
            resettables = new List<IResettable>();

            // initialize
            actionEvent.AddHandler(InternalActionEventHandler);
            InternalSimulationFinishedEvent += OnSimulationTerminated;
            InternalSimulationStartedEvent += OnSimulationStarted;
            SimulationTerminating += OnTerminating;
            SimulationStartedEvent = new BinaryEvent<IModel, IEventInstance>(Name + ".Started");
            SimulationFinishedEvent = new BinaryEvent<IModel, IEventInstance>(Name + ".Finished");

            DefaultResourceManager = new ResourceManager();
            RegisterResourceManager(DefaultResourceManager);
            
            IsReset = true;
            initialized = true;
            CurrentState = ExecutionState.Stopped;
		}

        #endregion
        #region hand

        /// <summary>
        /// a handler for simple action events
        /// </summary>
        /// <param name="a"></param>
        private void InternalActionEventHandler(Action a)
        {
            a.Invoke();
        }

        /// <summary>
        /// internal simulation finished event handler
        /// </summary>
        internal virtual void OnSimulationStarted()
        {
            SimulationStartedEvent.GetInstance(this, eventScheduler.NextScheduledEvent).Raise();
        }

        /// <summary>
        /// internal simulation finished event handler
        /// </summary>
        internal virtual void OnSimulationTerminated()
        {
            SimulationFinishedEvent.GetInstance(this, eventScheduler.LastProcessedEvent).Raise();
            OnSimulationFinished(new SimulationEventArgs(this));
            if (LogFinish) WriteFinishedLog();
            SimulationTerminating.Invoke();
        }

        /// <summary>
        /// noop
        /// </summary>
        internal virtual void OnTerminating() { }

        #endregion
        #region init

        /// <summary>
        /// initializes seed and seed source
        /// </summary>
        /// <param name="seed"></param>
        /// <param name="startTime"></param>
        public void Initialize(int seed = 0, double startTime = 0)
        {
            this.seed = seed;
            seedGenerator = new UniformIntegerDistribution(seed, 0, int.MaxValue, antithetic);
            initialized = true;
        }

        #endregion
        #region impl

        #region main

        /// <summary>
        /// add entities, random generators and 
        /// </summary>
        /// <param name="modelState"></param>
        /// <returns></returns>
        public bool Load(ModelState modelState)
        {
            modelState.Model = this;

            // main
            instanceCounter = modelState.InstanceCounter;
            this.Name = modelState.ModelName;
            this.seed = modelState.Seed;
            this.antithetic = modelState.IsAntithetic;
            this.nonStochasticMode = modelState.IsNonStochasticMode;

            // flags
            this.initialized = modelState.IsInitialized;
            this.requestStop = modelState.IsRequestStop;
            this.requestPause = modelState.IsRequestPause;
            this.requestInterrupt = modelState.IsRequestInterrupt;
            this.LogStart = modelState.IsLogStart;
            this.LogFinish = modelState.IsLogFinish;
            this.IsReset = modelState.IsReset;
            this.seedChange = modelState.IsSeedChange;

            // timing
            this.startTime = modelState.StartTime;
            this.currentTime = modelState.CurrentTime;
            this.endingTime = modelState.EndingTime;
            this.stepSize = modelState.StepSize;

            // stats
            this.lastRunDuration = modelState.LastRunDuration;
            this.realStartTime = modelState.RealStartTime;

            // rnds

            modelState.IsModelLoaded = true;

            // entities
            foreach (IIdentifiable item in items.Values)
            {
                if (item is ISaveable) (item as ISaveable).Load(modelState);
            }

            modelState.IsEntitiesLoaded = true;
            
            // TODO: resmgrs & networks

            // evnts & handlers
            eventScheduler.Load(modelState);

            // set execution state and return
            this.CurrentState = modelState.CurrentState;
            return true;
        }

        public ModelState Save()
        {
            if (CurrentState == ExecutionState.Running) throw new ApplicationException("Cannot save model while running. Pause or stop the simulation first.");

            ModelState result = new ModelState(Name);

            result.Model = this;

            result.InstanceCounter = instanceCounter;
            result.Seed = seed;
            result.IsAntithetic = antithetic;
            result.IsNonStochasticMode = nonStochasticMode;
            result.StartTime = startTime;
            result.CurrentTime = currentTime;
            result.EndingTime = endingTime;
            result.StepSize = stepSize;
            result.IsSeedChange = seedChange;
            result.IsReset = IsReset;
            result.IsLogFinish = LogFinish;
            result.IsLogStart = LogStart;
            result.IsInitialized = initialized;
            result.CurrentState = CurrentState;
            result.IsRequestStop = requestStop;
            result.IsRequestPause = requestPause;
            result.IsRequestInterrupt = requestInterrupt;
            result.LastRunDuration = lastRunDuration;
            result.RealStartTime = realStartTime;
            result.SeedGenerator = seedGenerator;

            eventScheduler.Save(result);

            foreach (IIdentifiable item in items.Values)
            {
                if (item is ISaveable) (item as ISaveable).Save(result);
            }
            
            // private List<IRandom> randomGenerators;                     // associated random distributions - refill from entity deserializer
            // private List<IResettable> resettables; = resource managers & task machines & networks


            // TODO: user restore his own handlers?
            // BinaryEvent<IModel, IEventInstance> SimulationStartedEvent
            // BinaryEvent<IModel, IEventInstance> SimulationFinishedEvent
            // private UnaryEvent<Action> actionEvent;
            // private SortedDictionary<double, Func<bool>> breakPoints;   // conditional breakpoints   

            result.AddValue("DefaultResourceManager", DefaultResourceManager);

            return result;
        }

        #endregion
        #region ctrl

        #region run / step

        #region public

        /// <summary>
        /// run from starttime until no more events are in the eventlist
        /// or the given ending time is reached.
        /// </summary>
        public void Start()
        {
            Run();
        }

        /// <summary>
        /// run from starttime until no more events are in the eventlist
        /// or the given ending time is reached.
        /// </summary>
        /// <param name="stopTime">a point in time at ("after") which the simulation will stop automatically</param>
        public void Run(DateTime stopTime) { Run(stopTime.ToDouble()); }

        /// <summary>
        /// run from starttime until no more events are in the eventlist
        /// or the given ending time is reached.
        /// the default value for the ending time will most probably not be
        /// reached before your current computer is crumbled to dust.
        /// </summary>
        /// <param name="stopTime">a point in time at ("after") which the simulation will stop automatically</param>
        public void Run(double stopTime = double.MaxValue)
        {
            DoRun(stopTime);
        }

        /// <summary>
        /// starts and proceeds the simulation up to startTime + stepSize.
        /// default step size is 1 and default time unit is ticks, so if 
        /// nothing else was defined, the simulation will be VERY slow!
        /// CAUTION: do not forget to call Stop() after the last step!
        /// </summary>
        public void Step()
        {
            Run(currentTime + stepSize);
        }

        /// <summary>
        /// starts and proceeds the simulation up to startTime + stepDuration.
        /// </summary>
        /// <param name="stepDuration"></param>
        public void Step(double stepDuration)
        {
            Run(currentTime + stepDuration);
        }

        /// <summary>
        /// starts and proceeds the simulation up to startTime + stepDuration.
        /// </summary>
        /// <param name="stepDuration"></param>
        public void Step(TimeSpan stepDuration)
        {
            Run(currentTime + stepDuration.ToDouble());
        }

        #endregion
        #region private

        private void DoRun(double targetTime) 
        {
            if (targetTime < currentTime) throw new CausalityException("Cannot step into the past. Time travel is forbidden.");

            this.endingTime = targetTime;

            PrepareRun();

            double breakTime;
            bool breakPoint = false;
            if (breakPoints.Count > 0) breakTime = breakPoints.Keys.First();
            else breakTime = double.MaxValue;

            while (eventScheduler.EventfulMomentsCount > 0
                && !requestStop
                && !requestPause
                && !requestInterrupt
                && eventScheduler.TimeOfNextScheduledEvent <= targetTime)
            {
                if (eventScheduler.TimeOfNextScheduledEvent > breakTime)
                {
                    bool doBreak = breakPoints[breakTime].Invoke();
                    breakPoints.Remove(breakTime);
                    if (breakPoints.Count > 0) breakTime = breakPoints.Keys.First();
                    else breakTime = double.MaxValue;

                    if (doBreak)
                    {
                        currentTime = breakTime;
                        breakPoint = true;
                        break;
                    }
                }
                currentTime = eventScheduler.TimeOfNextScheduledEvent;
                eventScheduler.ProcessNextPointInTime();
            }

            if (!breakPoint && !requestPause && !requestInterrupt && !requestStop && targetTime != double.MaxValue)
                currentTime = targetTime; // jump (set CurrentTime) to target time

            if (requestPause)
            {
                CurrentState = ExecutionState.Paused;
                requestPause = false;
            }
            else if (requestInterrupt)
            {
                CurrentState = ExecutionState.Interrupted;
                requestInterrupt = false;
            }
            else if (eventScheduler.EventfulMomentsCount == 0 || requestStop)
            {
                CurrentState = ExecutionState.Stopped;
                requestStop = false;
                lastRunDuration = DateTime.Now.Subtract(realStartTime);
                InternalSimulationFinishedEvent.Invoke();
            }
            else if (breakPoint)
            {
                CurrentState = ExecutionState.InBreakPoint;
            }
            else
            {
                CurrentState = ExecutionState.TimeElapsed;
                lastRunDuration = DateTime.Now.Subtract(realStartTime);
                InternalSimulationFinishedEvent.Invoke();
            }
        }

        #endregion

        #endregion
        #region stop / pause / interrupt

        /// <summary>
        /// Stop the simulation. If the current state is "Interrupted", 
        /// the simulation will finish the current point in time before changeing it's state to "Stopped".
        /// If the model is in any other state, nothing will happen and the current state will not be changed.
        /// </summary>
        public void Stop()
        {
            // TODO  Simulation - Replace this mess with a switch
            if (CurrentState == ExecutionState.Interrupted)
            {
                requestStop = true;
                Continue();
            } 
            else if (CurrentState == ExecutionState.Paused)
            {
                CurrentState = ExecutionState.Stopped;
                // TODO  Simulation - What about the lastRunDuration?
                InternalSimulationFinishedEvent.Invoke();
            }
            else if (CurrentState == ExecutionState.Running)
            {
                requestStop = true;
            }
            else if (CurrentState == ExecutionState.TimeElapsed)
            {
                CurrentState = ExecutionState.Stopped;
            }
            else if (CurrentState == ExecutionState.InBreakPoint) 
            {
                CurrentState = ExecutionState.Stopped;
                // TODO  Simulation - What about the lastRunDuration?
                InternalSimulationFinishedEvent.Invoke();
            }
        }

        /// <summary>
        /// Suspend simulation run. The current point in time will be finished first.
        /// If the model is not running, nothing will happen and the current state will not be changed.
        /// </summary>
        public void Pause()
        {
            switch (CurrentState)
            {
                case ExecutionState.Running:
                    requestPause = true;
                break;
                case ExecutionState.Interrupted:
                    requestPause = true;
                    Continue();
                break;
                case ExecutionState.Stopped:
                return;
                default:
                    CurrentState = ExecutionState.Paused;
                break;
            }
        }

        /// <summary>
        /// Suspend simulation run. The current point in time may not be finished.
        /// </summary>
        public void Interrupt()
        {
            if (CurrentState != ExecutionState.Running)
                this.Log<SIM_WARNING>("Interrupt was called but the model was not running.", this);
            else requestInterrupt = true;
        }

        #endregion
        #region other

        /// <summary>
        /// continue after pause
        /// </summary>
        public void Continue()
        {
            if (CurrentState != ExecutionState.Paused && CurrentState != ExecutionState.Interrupted) 
                throw new ApplicationException("Continue was called but the simulation was not paused.");
            Run(endingTime);
        }

        /// <summary>
        /// <see cref="Continue"/>
        /// </summary>
        public void Resume() { Continue(); }

        #endregion

        #endregion
        #region dbug

        /// <summary>
        /// pauses model execution when the given point in time is reached.
        /// this will occure before the events of the given point in time
        /// are processed so as to allow their inspection.
        /// </summary>
        /// <param name="pointInTime">
        /// a point in simulation time at which to pause the model execution
        /// </param>
        /// <remarks>
        /// CAUTION: if a breakpoint at the given point in time already exists it will be overwritten
        /// </remarks>
        public void AddBreakPoint(double pointInTime) 
        {
            AddConditionalBreakPoint(pointInTime, () => true);
        }

        /// <summary>
        /// pauses model execution when the given point in time is reached.
        /// this will occure before the events of the given point in time
        /// are processed so as to allow their inspection.
        /// </summary>
        /// <param name="pointInTime">
        /// a point in simulation time at which to pause the model execution
        /// </param>
        /// <param name="condition">
        /// the model will only be paused if the given condition function returns true.
        /// </param>
        /// <remarks>
        /// CAUTION: if a breakpoint at the given point in time already exists it will be overwritten
        /// </remarks>
        public void AddConditionalBreakPoint(double pointInTime, Func<bool> condition) 
        {
            breakPoints[pointInTime] = condition;
        }

        #endregion
        #region evnt

        /// <summary>
        /// Schedule an event at currenttime + timespan.
        /// Caution: an exception will be thrown if the
        /// specified point in time is less or equal now.
        /// </summary>
        /// <param name="timeSpan"></param>
        /// <param name="evnt"></param>
        public void AddEvent(double timeSpan, IEventInstance evnt) 
        {
            AddEventAt(currentTime + timeSpan, evnt);
        }

        /// <summary>
        /// Schedule an event at the specified point in time.
        /// Caution: an exception will be thrown if the
        /// specified point in time is less or equal now.
        /// </summary>
        /// <param name="pointInTime"></param>
        /// <param name="evnt"></param>
        public void AddEventAt(double pointInTime, IEventInstance evnt) 
        {
#if DEBUG
            if (evnt == null)
                throw new Exception("Attempt to add a null-pointer event instance at " + pointInTime.ToDateTime().ToString() + ".");
#else
            if (evnt == null) return;
#endif

            if (double.IsNaN(pointInTime) || double.IsInfinity(pointInTime)) 
                throw new Exception("Attempt to add the event instance <" + evnt.Name + "> at " + pointInTime.ToString() + ".");

            if (CurrentState == ExecutionState.Running)
            {
                if (pointInTime <= currentTime) throw new CausalityException("You may not change the past or present. Events are "
                + "results of physical processes, which always take time. If you really still feel you have to "
                + "do this, use the IEventInstance.Raise() method instead. This will call all handlers \"now\".");
            }
            else
            {
                if (pointInTime < currentTime) throw new CausalityException("You may not change the past. Time travel is forbidden!");
            }
            eventScheduler.Add(pointInTime, evnt);
        }

        /// <summary>
        /// schedule an action at currenttime + delay
        /// </summary>
        /// <param name="delay"></param>
        /// <param name="a"></param>
        public void Schedule(TimeSpan delay, Action a)
        {
            AddEvent(delay.ToDouble(), actionEvent.GetInstance(a));
        }

        /// <summary>
        /// schedule an action at currenttime + delay
        /// </summary>
        /// <param name="delay"></param>
        /// <param name="priority"></param>
        /// <param name="a"></param>
        public void Schedule(TimeSpan delay, Priority priority, Action a)
        {
            AddEvent(delay.ToDouble(), actionEvent.GetInstance(priority, a));
        }

        /// <summary>
        /// schedule an action at currenttime + delay
        /// </summary>
        /// <param name="delay"></param>
        /// <param name="a"></param>
        public void Schedule(double delay, Action a)
        {
            AddEvent(delay, actionEvent.GetInstance(a));
        }

        /// <summary>
        /// schedule an action at currenttime + delay
        /// </summary>
        /// <param name="delay"></param>
        /// <param name="priority"></param>
        /// <param name="a"></param>
        public void Schedule(double delay, Priority priority, Action a)
        {
            AddEvent(delay, actionEvent.GetInstance(priority, a));
        }

        /// <summary>
        /// will remove the given event from the event schedule.
        /// if the event was already processed or is scheduled to 
        /// be processed at the current point in time, nothing will happen.
        /// </summary>
        /// <param name="evnt"></param>
        public void TryRemoveEvent(IEventInstance evnt)
        {
            if (evnt.Time != double.NaN) return;
            else if (evnt.Time.CompareTo(currentTime) <= 0) return;
            eventScheduler.Remove(evnt);
        }

        /// <summary>
        /// will remove the given event from the event schedule.
        /// if the event is scheduled to be processed at the current 
        /// point in time, an exception will be thrown.
        /// </summary>
        /// <param name="evnt"></param>
        public void RemoveEvent(IEventInstance evnt)
        {
            eventScheduler.Remove(evnt);
        }

#if ImmediateEvents
        /// <summary>
        /// Schedule an event to be raised at the current
        /// point in time. This will happen after all other
        /// events from the current time have been processed.
        /// Caution: the priority will be set to LowLevelAfterOthers. 
        /// The priority number will be kept and respected, though.
        /// </summary>
        /// <param name="evnt"></param>
        public void AddImmediateEvent(IEventInstance evnt)
        {
            evnt.Priority.PriorityType = PriorityType.LowLevelAfterOthers;
            eventScheduler.AddImmediate(evnt);
        }
#endif

        #endregion
        #region sobj

        /// <summary>
        /// sets the entities <code>Model</code> property to <code>this</code>
        /// stores the entities reference in a dictionary by its <code>ID</code> property
        /// will throw an exception if the object was already added earlier
        /// </summary>
        /// <param name="entity">the entity to permanently associate with this model</param>
        public void AddEntity(IEntity obj)
        {
            AddIdentifiable(obj);
            if(obj.Model == null) obj.Model = this;
        }

        /// <summary>
        /// removes the given object if found
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>false if the object was not contained in this model</returns>
        public bool RemoveEntity(IEntity obj)
        {
            return items.Remove(obj.Identifier);
        }

        /// <summary>
        /// check if a simulation object is contained within this model
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool HasEntity(string id) // TODO: rename to HasItem
        {
            return items.ContainsKey(id);
        }

        /// <summary>
        /// retrieve a simulation object.
        /// if the id cannot be found or the associated object is not of the given type an exception will be thrown
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public T GetEntity<T>(string id)
        {
            return (T)items[id];
        }

        /// <summary>
        /// returns all objects of the given type (as type param) from the
        /// previously added simulation objects
        /// </summary>
        /// <returns></returns>
        public IEnumerable<T> FindEntities<T>()
        {
            foreach (IIdentifiable obj in items.Values)
            {
                if (obj is T) yield return (T)obj;
            }
        }

        #endregion
        #region rand

        /// <summary>
        /// Retrieve a seed by ID. A given ID will always result in the same seed as long as the model seed is not changed.
        /// </summary>
        /// <param name="seedID"></param>
        /// <returns></returns>
        public int GetRandomSeedFor(int seedID)
        {
            return ((new System.Random(seedID ^ seed)).Next());
        }

        public void AddRandomGenerator(IRandom generator)
        {
            randomGenerators.Add(generator);
        }

        #endregion
        #region misc

        public void AddIdentifiable(IIdentifiable item)
        {
            if (items.ContainsKey(item.Identifier))
            {
                string warning = "An object with the given id (" + item.Identifier + ") was already added. ";
                if (item is IEntity)
                {
                    warning += (item as IEntity).EntityName + " will overwrite " + items[item.Identifier].ToString() + ".";
                }
                this.Log<SIM_WARNING>(warning, this);
            }
            items.Add(item.Identifier, item);
        }

        public IEntity GetEntity(string id)
        {
            return items[id] as IEntity;
        }

        public IIdentifiable GetItem(string id)
        {
            return items[id];
        }

        public void RegisterNetwork(ISimulationNetwork net)
        {
            if (!resettables.Contains(net)) resettables.Add(net);
        }

        public void RegisterTaskMachine(TaskMachine tm)
        {
            if (!resettables.Contains(tm)) resettables.Add(tm);
        }

        public void RegisterResourceManager(IResourceManager mgr)
        {
            if (!resettables.Contains(mgr)) resettables.Add(mgr);
        }

        #endregion

        #endregion
        #region rset

        /// <summary>
        /// Resets all managed objects and random generators and updates the setting variables.
        /// </summary>
        public void Reset()
        {
            Reset(seed);
        }

        /// <summary>
        /// Resets all managed objects and random generators and updates the setting variables.
        /// </summary>
        /// <param name="seed"></param>
        public void Reset(int seed) 
        {
            Reset(seed, antithetic, nonStochasticMode);
        }

        /// <summary>
        /// Resets all managed objects and random generators and updates the setting variables.
        /// </summary>
        /// <param name="seed"></param>
        /// <param name="antithetic"></param>
        /// <param name="nonStochasticMode"></param>
        public void Reset(int seed, bool antithetic = false, bool nonStochasticMode = false)
        {
            // update private vars; Caution: the wrapping properties rely on this!
            seedChange = this.seed != seed;
            this.seed = seed;
            this.antithetic = antithetic;
            this.nonStochasticMode = nonStochasticMode;

            // reset time, event scheduler, seed source and default resource manager
            currentTime = startTime;
            eventScheduler.Reset();
            seedGenerator.Reset(seed, antithetic);
            // DefaultResourceManager.Reset();

            // reset random generators
            foreach (IRandom rnd in randomGenerators)
                rnd.Reset(seed, antithetic, nonStochasticMode);

            // reset objects
            foreach (IIdentifiable obj in items.Values)
                if(obj is IEntity) (obj as IEntity).Reset();

            // reset other resettable objects
            foreach (IResettable obj in resettables)
                obj.Reset();

            seedChange = false;
            IsReset = true;

            CurrentState = ExecutionState.Stopped;
        }

        /// <summary>
        /// Resets all managed objects and random generators and updates the setting variables.
        /// </summary>
        /// <param name="antithetic"></param>
        /// <param name="nonStochasticMode"></param>
        public void Reset(bool antithetic = false, bool nonStochasticMode = false)
        {
            Reset(seed, antithetic, nonStochasticMode);
        }

        #endregion
        #region util

        private void PrepareStep() { PrepareRun(); }
        private void PrepareRun()
        {
            if (CurrentState == ExecutionState.Running)
            {
                this.Log<SIM_ERROR>("Unable to start, the model is already running.", this);
                throw new ApplicationException("Unable to start, the model is already running.");
            }
            else if (CurrentState == ExecutionState.Stopped)
            {
                if (!initialized)
                {
                    this.Log<SIM_ERROR>("Cannot simulate - the model is not initialized.", this);
                    throw new InitializationException("Cannot simulate - the model is not initialized.");
                }

                if (eventScheduler.EventfulMomentsCount == 0)
                {
                    this.Log<SIM_ERROR>("Cannot simulate - no events are scheduled. If this happens on a consecutive run, check your reset functions.", this);
                    throw new ApplicationException("Cannot simulate - no events are scheduled. If this happens on a consecutive run, check your reset functions.");
                }

                eventScheduler.ResetEventCounter();
                if (LogStart) this.Log<SIM_INFO>("Simulation started.", this);
                IsReset = false;
                CurrentState = ExecutionState.Running;
                InternalSimulationStartedEvent.Invoke();
                realStartTime = DateTime.Now;
            }
            else CurrentState = ExecutionState.Running;
        }

        /// <summary>
        /// log finished stats
        /// </summary>
        private void WriteFinishedLog()
        {
            String eEndTime;
            if (eventScheduler.LastProcessedEvent.Time >= DateTime.MaxValue.ToDouble()) eEndTime = DateTime.MaxValue.ToString();
            else eEndTime = eventScheduler.LastProcessedEvent.Time.ToDateTime().ToString();

            this.Log<SIM_INFO>("Simulation end. Last event (" + eventScheduler.LastProcessedEvent.Name + ") processed at " + 
                eEndTime + ".", this);
            this.Log<SIM_INFO>(eventScheduler.EventCounter.ToString() + " events processed in " +
                lastRunDuration.ToString() + " at " +
                (Math.Round(eventScheduler.EventCounter / lastRunDuration.TotalSeconds, 1)).ToString() +
                " events per second.", this);
            this.Log<SIM_INFO>(eventScheduler.HandlerCounter.ToString() + " handlers processed in " +
                lastRunDuration.ToString() + " at " +
                (Math.Round(eventScheduler.HandlerCounter / lastRunDuration.TotalSeconds, 1)).ToString() +
                " handlers per second.", this);
        }

        protected virtual void OnSimulationFinished(SimulationEventArgs e)
        {
            if (SimulationFinished != null) SimulationFinished.Invoke(this, e);
        }

        #endregion
    }
}