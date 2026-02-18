using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Simulation.Interfaces;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Events;
using SimOpt.Basics.Datastructures.Geometry;
using SimOpt.Simulation.Enum;
using SimOpt.Mathematics.Stochastics.Interfaces;
using SimOpt.Basics.Exceptions;
using SimOpt.Basics.Interfaces;
using System.Runtime.Serialization;

namespace SimOpt.Simulation.Templates
{
    [Serializable]
    public class Server<TMaterial, TProduct> : Server<TMaterial, TProduct, object>
        where TProduct : new()
    {
        #region cvar

        private Func<List<TMaterial>, TProduct> simpleCreateProduct;

        #endregion
        #region ctor

        public Server() : base() { }

        public Server(IModel model,
                      IDistribution<double> machiningTime,
                      string id = "",
                      string name = "",
                      Func<List<TMaterial>, TProduct> createProduct = null, 
                      IDistribution<double> timeToFailure = null,
                      IDistribution<double> timeToRecover = null,
                      Func<TMaterial, bool> checkMaterialUsable = null,
                      Func<List<TMaterial>, bool> checkMaterialComplete = null, 
                      List<string> states = null,
                      List<Tuple<string, string>> transitions = null,
                      string initialState = null,
                      Point initialPosition = null,
                      IResourceManager manager = null,
                      IEntity currentHolder = null,
                      double autoStartDelay = double.NaN)
            : base(model, id, name, states, transitions, initialState, initialPosition, manager, currentHolder)
        {
            if (createProduct != null) this.simpleCreateProduct = createProduct;
            else this.simpleCreateProduct = DefaultSimpleProductGenerator;
            base.InitializeServer(machiningTime, SimpleProductGeneratorWrapper, timeToFailure, timeToRecover, checkMaterialUsable, checkMaterialComplete, autoStartDelay);
        }

        public Server(IModel model,
                      int seedID,
                      IDistribution<double> machiningTime,
                      string id = "",
                      string name = "",
                      Func<List<TMaterial>, TProduct> createProduct = null, 
                      IDistribution<double> timeToFailure = null,
                      IDistribution<double> timeToRecover = null,
                      Func<TMaterial, bool> checkMaterialUsable = null,
                      Func<List<TMaterial>, bool> checkMaterialComplete = null, 
                      List<string> states = null,
                      List<Tuple<string, string>> transitions = null,
                      string initialState = null,
                      Point initialPosition = null,
                      IResourceManager manager = null,
                      IEntity currentHolder = null,
                      double autoStartDelay = double.NaN)
            : base(model, seedID, id, name, states, transitions, initialState, initialPosition, manager, currentHolder)
        {
            if (createProduct != null) this.simpleCreateProduct = createProduct;
            else this.simpleCreateProduct = DefaultSimpleProductGenerator;
            base.InitializeServer(machiningTime, SimpleProductGeneratorWrapper, timeToFailure, timeToRecover, checkMaterialUsable, checkMaterialComplete, autoStartDelay);
        }

        public Server(IModel model,
                      Func<List<TMaterial>, double> machiningTimeDelegate,
                      string id = "",
                      string name = "",
                      Func<List<TMaterial>, TProduct> createProduct = null,
                      Func<TMaterial, bool> checkMaterialUsable = null,
                      Func<List<TMaterial>, bool> checkMaterialComplete = null,
                      List<string> states = null,
                      List<Tuple<string, string>> transitions = null,
                      string initialState = null,
                      Point initialPosition = null,
                      IResourceManager manager = null,
                      IEntity currentHolder = null,
                      double autoStartDelay = double.NaN)
            : base(model, id, name, states, transitions, initialState, initialPosition, manager, currentHolder)
        {
            if (createProduct != null) this.simpleCreateProduct = createProduct;
            else this.simpleCreateProduct = DefaultSimpleProductGenerator;
            base.InitializeServer(machiningTimeDelegate, SimpleProductGeneratorWrapper, checkMaterialUsable, checkMaterialComplete, autoStartDelay);
        }

        public Server(IModel model,
                      int seedID,
                      Func<List<TMaterial>, double> machiningTimeDelegate,
                      string id = "",
                      string name = "",
                      Func<List<TMaterial>, TProduct> createProduct = null,
                      Func<TMaterial, bool> checkMaterialUsable = null,
                      Func<List<TMaterial>, bool> checkMaterialComplete = null,
                      List<string> states = null,
                      List<Tuple<string, string>> transitions = null,
                      string initialState = null,
                      Point initialPosition = null,
                      IResourceManager manager = null,
                      IEntity currentHolder = null,
                      double autoStartDelay = double.NaN)
            : base(model, seedID, id, name, states, transitions, initialState, initialPosition, manager, currentHolder)
        {
            if (createProduct != null) this.simpleCreateProduct = createProduct;
            else this.simpleCreateProduct = DefaultSimpleProductGenerator;
            base.InitializeServer(machiningTimeDelegate, SimpleProductGeneratorWrapper, checkMaterialUsable, checkMaterialComplete, autoStartDelay);
        }

        #endregion
        #region impl

        private Tuple<TProduct, object> SimpleProductGeneratorWrapper(List<TMaterial> material)
        {
            TProduct result = simpleCreateProduct.Invoke(material);
            return new Tuple<TProduct, object>(result, default(object));
        }

        private TProduct DefaultSimpleProductGenerator(List<TMaterial> material)
        {
            return new TProduct();
        }

        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TMaterial"></typeparam>
    /// <typeparam name="TProduct"></typeparam>
    /// <typeparam name="TData"></typeparam>
    [Serializable]
    public class Server<TMaterial, TProduct, TData> : StateMachineEntity,
                                                      IItemSource<TProduct>,
                                                      IItemSource<TProduct, TData>,
                                                      IResource,
                                                      IPosition<Point>,
                                                      ISeedSource,
                                                      IEntity,
                                                      IItemSink<TMaterial>
        where TProduct : new()
    {
        #region over

        public override void Reset()
        {
            base.Reset();
            currentMaterial.Clear();
            activeMaterial.Clear();
            stop = false;
            working = false;
            damaged = false;
            recovering = false;
            
            if (IsMachiningTimeStochastic)
                nextTimeToFinished = rndMachiningTime.Next();
            else
                nextTimeToFinished = 0;

            if(hasFailure) nextTimeToFailure = rndFailureTime.Next();
            if(hasRecover) nextTimeToRecover = rndRecoverTime.Next();

            if (AutoStart) Model.Schedule(AutoStartDelay, () => Start());
        }

        #endregion
        #region cvar

        private bool hasBuffer = false;
        
        private IItemBuffer<TMaterial> buffer;

        private double nextTimeToFailure;
        private double nextTimeToRecover;
        private double nextTimeToFinished;

        private List<TMaterial> activeMaterial;
        private List<TMaterial> currentMaterial;
        
        private Func<TMaterial, bool> checkMaterialUsable;
        private Func<List<TMaterial>, bool> checkMaterialComplete;
        private Func<List<TMaterial>, Tuple<TProduct, TData>> createProduct;

        private bool stop;
        private bool working;
        private bool damaged;
        private bool recovering;
        private bool autoContinue;
        private bool autoRecover;
        private bool pushAllowed;
        private bool hasFailure;
        private bool hasRecover;
        private bool continueAfterFail;

        private IDistribution<double> machiningTime;
        private IDistribution<double> timeToFailure;
        private IDistribution<double> timeToRecover;
        
        private Random<double> rndMachiningTime;
        private Random<double> rndFailureTime;
        private Random<double> rndRecoverTime;

        private UnaryEvent<IEntity> recoverEvent;
        private UnaryEvent<IEntity> repeaterEvent;
        
        private TernaryEvent<IEntity, TProduct, TData> failureEvent;
        private BinaryEvent<IEntity, TProduct> entityFinishedSimpleEvent;
        private TernaryEvent<IEntity, TProduct, TData> entityFinishedEvent;

        private UnaryEventInstance<IEntity> nextRecoverInstance;
        private UnaryEventInstance<IEntity> nextRepeaterInstance;
        
        private TernaryEventInstance<IEntity, TProduct, TData> nextFailureInstance;
        private BinaryEventInstance<IEntity, TProduct> nextFinishedSimpleInstance;
        private TernaryEventInstance<IEntity, TProduct, TData> nextFinishedInstance;

        #endregion
        #region prop

        /// <summary>
        /// if this is true, the server is either working on a product or being repaired
        /// </summary>
        public bool Busy { get { return (working || recovering); } }

        public bool Working { get { return working; } }
        public bool Damaged { get { return damaged; } }
        public bool Recovering { get { return recovering; } }
        public bool Stopped { get { return stop; } }

        /// <summary>
        /// if this is true the server is neither working nor recovering nor damaged
        /// </summary>
        public bool Idle { get { return (!Busy && !damaged); } }

        public bool AutoStart { get; set; }

        public double AutoStartDelay { get; set; }

        /// <summary>
        /// if this is true (default is false) the server will 
        /// start recovering on its own after a failure
        /// </summary>
        public bool AutoRecover
        {
            get { return autoRecover; }
            set { autoRecover = value; }
        }

        /// <summary>
        /// false per default. if set to true, the server can receive material while damaged or recovering
        /// </summary>
        public bool AllowPushDuringMaintainance { get; set; }

        /// <summary>
        /// false per default
        /// </summary>
        public bool ContinueProductAfterFailure { get; set; }

        /// <summary>
        /// default is false
        /// if set to true the server will start working on the next item
        /// as soon as the previous item is finished (given that the material
        /// is already complete) or as soon as the material is complete.
        /// </summary>
        public bool AutoContinue
        {
            get { return autoContinue; }
            set { autoContinue = value; }
        }

        /// <summary>
        /// default is false
        /// if set to true the server will start working on the next (or current, 
        /// depending on ContinueProductAfterFailure) item afer recovering. Otherwise
        /// you have to restart the server after a failure and recover.
        /// </summary>
        public bool AutoRestart { get; set; }

        public bool PushAllowed
        {
            get { return pushAllowed; }
            set 
            {
                if (Model.IsRunning)
                    throw new InvalidOperationException("PushAllowed cannot be changed anymore when the model is already running.");
                pushAllowed = value; 
            }
        }

        public List<TMaterial> CurrentMaterial
        {
            get { return currentMaterial; }
        }

        /// <summary>
        /// A delegate for calculating the time it takes to process the material.
        /// </summary>
        public Func<List<TMaterial>, double> MachiningTimeFunction { get; set; }

        public bool IsMachiningTimeStochastic { get; private set; }

        public bool IsMachiningTimeFunctional { get { return !IsMachiningTimeStochastic; } }

        /// <summary>
        /// wrapper for EntityFinishedEvent
        /// </summary>
        public BinaryEvent<IEntity, TProduct> EntityCreatedEvent
        {
            get { return entityFinishedSimpleEvent; }
        }

        /// <summary>
        /// wrapper for EntityWithDataFinishedEvent
        /// </summary>
        public TernaryEvent<IEntity, TProduct, TData> EntityWithDataCreatedEvent
        {
            get { return entityFinishedEvent; }
        }

        public BinaryEvent<IEntity, TProduct> EntityFinishedEvent
        {
            get { return entityFinishedSimpleEvent; }
        }

        public TernaryEvent<IEntity, TProduct, TData> EntityWithDataFinishedEvent
        {
            get { return entityFinishedEvent; }
        }

        public TimeSpan MTTF
        {
            get 
            {
                if (!hasFailure) return TimeSpan.MaxValue;
                return timeToFailure.Mean.ToTimeSpan();
            }
        }

        public TimeSpan MTTR
        {
            get
            {
                if (!hasRecover) return TimeSpan.MaxValue;
                return timeToRecover.Mean.ToTimeSpan();
            }
        }

        #endregion
        #region ctor

        public Server() : base() { }

        /// <summary>
        /// CAUTION: creates an uninitialized instance
        /// </summary>
        /// <param name="model"></param>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="states"></param>
        /// <param name="transitions"></param>
        /// <param name="initialState"></param>
        /// <param name="initialPosition"></param>
        /// <param name="manager"></param>
        /// <param name="currentHolder"></param>
        internal Server(IModel model,
                      string id = "",
                      string name = "",
                      List<string> states = null,
                      List<Tuple<string, string>> transitions = null,
                      string initialState = null,
                      Point initialPosition = null,
                      IResourceManager manager = null,
                      IEntity currentHolder = null)
            : base(model, id, name, states, transitions, initialState, initialPosition, manager, currentHolder)
        { }

        /// <summary>
        /// CAUTION: creates an uninitialized instance
        /// </summary>
        /// <param name="model"></param>
        /// <param name="seedID"></param>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="states"></param>
        /// <param name="transitions"></param>
        /// <param name="initialState"></param>
        /// <param name="initialPosition"></param>
        /// <param name="manager"></param>
        /// <param name="currentHolder"></param>
        internal Server(IModel model,
                      int seedID,
                      string id = "",
                      string name = "",
                      List<string> states = null,
                      List<Tuple<string, string>> transitions = null,
                      string initialState = null,
                      Point initialPosition = null,
                      IResourceManager manager = null,
                      IEntity currentHolder = null)
            : base(model, seedID, id, name, states, transitions, initialState, initialPosition, manager, currentHolder)
        { }

        public Server(IModel model,
                      IDistribution<double> machiningTime,
                      string id = "",
                      string name = "",
                      Func<List<TMaterial>, Tuple<TProduct, TData>> createProduct = null,
                      IDistribution<double> timeToFailure = null,
                      IDistribution<double> timeToRecover = null,
                      Func<TMaterial, bool> checkMaterialUsable = null,
                      Func<List<TMaterial>, bool> checkMaterialComplete = null,
                      List<string> states = null,
                      List<Tuple<string, string>> transitions = null,
                      string initialState = null,
                      Point initialPosition = null,
                      IResourceManager manager = null,
                      IEntity currentHolder = null,
                      double autoStartDelay = double.NaN)
            : base(model, id, name, states, transitions, initialState, initialPosition, manager, currentHolder)
        {
            InitializeServer(machiningTime, createProduct, timeToFailure, timeToRecover, checkMaterialUsable, checkMaterialComplete, autoStartDelay);
        }

        public Server(IModel model,
                      int seedID,
                      IDistribution<double> machiningTime,
                      string id = "",
                      string name = "",
                      Func<List<TMaterial>, Tuple<TProduct, TData>> createProduct = null,
                      IDistribution<double> timeToFailure = null,
                      IDistribution<double> timeToRecover = null,
                      Func<TMaterial, bool> checkMaterialUsable = null,
                      Func<List<TMaterial>, bool> checkMaterialComplete = null,
                      List<string> states = null,
                      List<Tuple<string, string>> transitions = null,
                      string initialState = null,
                      Point initialPosition = null,
                      IResourceManager manager = null,
                      IEntity currentHolder = null,
                      double autoStartDelay = double.NaN)
            : base(model, seedID, id, name, states, transitions, initialState, initialPosition, manager, currentHolder)
        {
            InitializeServer(machiningTime, createProduct, timeToFailure, timeToRecover, checkMaterialUsable, checkMaterialComplete, autoStartDelay);
        }

        public Server(IModel model,
                     Func<List<TMaterial>, double> machiningTimeDelegate,
                     string id = "",
                     string name = "",
                     Func<List<TMaterial>, Tuple<TProduct, TData>> createProduct = null,
                     Func<TMaterial, bool> checkMaterialUsable = null,
                     Func<List<TMaterial>, bool> checkMaterialComplete = null,
                     List<string> states = null,
                     List<Tuple<string, string>> transitions = null,
                     string initialState = null,
                     Point initialPosition = null,
                     IResourceManager manager = null,
                     IEntity currentHolder = null, 
                     double autoStartDelay = double.NaN)
            : base(model, id, name, states, transitions, initialState, initialPosition, manager, currentHolder)
        {
            InitializeServer(machiningTimeDelegate, createProduct, checkMaterialUsable, checkMaterialComplete, autoStartDelay);
        }

        public Server(IModel model,
                      int seedID,
                      Func<List<TMaterial>, double> machiningTimeDelegate,
                      string id = "",
                      string name = "",
                      Func<List<TMaterial>, Tuple<TProduct, TData>> createProduct = null,
                      Func<TMaterial, bool> checkMaterialUsable = null,
                      Func<List<TMaterial>, bool> checkMaterialComplete = null,
                      List<string> states = null,
                      List<Tuple<string, string>> transitions = null,
                      string initialState = null,
                      Point initialPosition = null,
                      IResourceManager manager = null,
                      IEntity currentHolder = null,
                      double autoStartDelay = double.NaN)
            : base(model, seedID, id, name, states, transitions, initialState, initialPosition, manager, currentHolder)
        {
            InitializeServer(machiningTimeDelegate, createProduct, checkMaterialUsable, checkMaterialComplete, autoStartDelay);
        }

        #endregion
        #region init

        /// <summary>
        /// CAUTION: the time to recover will be ignored if no time to failure is given
        /// </summary>
        /// <param name="machiningTime"></param>
        /// <param name="timeToFailure"></param>
        /// <param name="timeToRecover"></param>
        public void InitializeServer(IDistribution<double> machiningTime, 
                                     Func<List<TMaterial>, Tuple<TProduct, TData>> createProduct = null, 
                                     IDistribution<double> timeToFailure = null, 
                                     IDistribution<double> timeToRecover = null,
                                     Func<TMaterial, bool> checkMaterialUsable = null,                         
                                     Func<List<TMaterial>, bool> checkMaterialComplete = null,
                                     double autoStartDelay = double.NaN)
        {
            if (!base.IsInitialized) 
                throw new InitializationException("The model initialization has to be finished before the intervals can be configured!");
            if (!double.IsNaN(autoStartDelay) && autoStartDelay >= 0)
            {
                if (Model.IsRunning) throw new ApplicationException("Attempted to initialize server autostarting while the model was already running.");
                AutoStart = true;
                this.AutoStartDelay = autoStartDelay;
            }

            // init product generation 
            if (createProduct == null) this.createProduct = DefaultProductGenerator;
            else this.createProduct = createProduct;
            currentMaterial = new List<TMaterial>();
            activeMaterial = new List<TMaterial>();

            // init material checks
            if (checkMaterialUsable == null) this.checkMaterialUsable = DefaultMaterialUsableCheck;
            else this.checkMaterialUsable = checkMaterialUsable;
            if (checkMaterialComplete == null) this.checkMaterialComplete = DefaultMaterialCompleteCheck;
            else this.checkMaterialComplete = checkMaterialComplete;

            // init distributions
            IsMachiningTimeStochastic = true;
            this.machiningTime = machiningTime;
            rndMachiningTime = new Random<double>(this, machiningTime, Model.Antithetic, Model.NonStochasticMode);
            nextTimeToFinished = rndMachiningTime.Next();
            if (timeToFailure != null)
            {
                hasFailure = true;
                this.timeToFailure = timeToFailure;
                rndFailureTime = new Random<double>(this, timeToFailure, Model.Antithetic, Model.NonStochasticMode);
                nextTimeToFailure = rndFailureTime.Next();
                if (timeToRecover != null)
                {
                    hasRecover = true;
                    this.timeToRecover = timeToRecover;
                    rndRecoverTime = new Random<double>(this, timeToRecover, Model.Antithetic, Model.NonStochasticMode);
                    nextTimeToRecover = rndRecoverTime.Next();
                }
            }

            // init events
            Priority p = new Priority(type: PriorityType.SimWorldBeforeOthers);
            entityFinishedEvent = new TernaryEvent<IEntity, TProduct, TData>(EntityName + ".EntityWithDataFinished");
            entityFinishedSimpleEvent = new BinaryEvent<IEntity, TProduct>(EntityName + ".EntityFinished");
            entityFinishedEvent.Priority = p;
            entityFinishedSimpleEvent.Priority = p;
            entityFinishedEvent.AddHandler(InternalFinishedHandler, new Priority(type: PriorityType.SimWorldBeforeOthers));
            repeaterEvent = new UnaryEvent(EntityName + ".Repeater");
            repeaterEvent.Priority = new Priority(type: PriorityType.LowLevelAfterOthers);
            repeaterEvent.AddHandler(RepeaterHandler);
            failureEvent = new TernaryEvent<IEntity, TProduct, TData>(EntityName + ".Failed");
            recoverEvent = new UnaryEvent(EntityName + ".Recovered");
            failureEvent.AddHandler(InternalFailureHandler, new Priority(type: PriorityType.SimWorldBeforeOthers));
            recoverEvent.AddHandler(InternalRecoverHandler, new Priority(type: PriorityType.SimWorldBeforeOthers));

            // autostart
            if (AutoStart) Model.Schedule(AutoStartDelay, () => Start());
        }

        /// <summary>
        /// CAUTION: the time to recover will be ignored if no time to failure is given
        /// </summary>
        /// <param name="machiningTime"></param>
        /// <param name="timeToFailure"></param>
        /// <param name="timeToRecover"></param>
        public void InitializeServer(Func<List<TMaterial>, double> machiningTimeDelegate,
                                     Func<List<TMaterial>, Tuple<TProduct, TData>> createProduct = null,
                                     Func<TMaterial, bool> checkMaterialUsable = null,
                                     Func<List<TMaterial>, bool> checkMaterialComplete = null,
                                     double autoStartDelay = double.NaN)
        {
            if (!base.IsInitialized)
                throw new InitializationException("The model initialization has to be finished before the intervals can be configured!");
            if (!double.IsNaN(autoStartDelay) && autoStartDelay >= 0)
            {
                if (Model.IsRunning) throw new ApplicationException("Attempted to initialize server autostarting while the model was already running.");
                AutoStart = true;
                this.AutoStartDelay = autoStartDelay;
            }

            // init product generation 
            if (createProduct == null) this.createProduct = DefaultProductGenerator;
            else this.createProduct = createProduct;
            currentMaterial = new List<TMaterial>();
            activeMaterial = new List<TMaterial>();

            // init material checks
            if (checkMaterialUsable == null) this.checkMaterialUsable = DefaultMaterialUsableCheck;
            else this.checkMaterialUsable = checkMaterialUsable;
            if (checkMaterialComplete == null) this.checkMaterialComplete = DefaultMaterialCompleteCheck;
            else this.checkMaterialComplete = checkMaterialComplete;

            // init distributions
            this.MachiningTimeFunction = machiningTimeDelegate;
            IsMachiningTimeStochastic = false;

            // init events
            Priority p = new Priority(type: PriorityType.SimWorldBeforeOthers);
            entityFinishedEvent = new TernaryEvent<IEntity, TProduct, TData>(EntityName + ".EntityWithDataFinished");
            entityFinishedSimpleEvent = new BinaryEvent<IEntity, TProduct>(EntityName + ".EntityFinished");
            entityFinishedEvent.Priority = p;
            entityFinishedSimpleEvent.Priority = p;
            entityFinishedEvent.AddHandler(InternalFinishedHandler, new Priority(type: PriorityType.SimWorldBeforeOthers));
            repeaterEvent = new UnaryEvent(EntityName + ".Repeater");
            repeaterEvent.Priority = new Priority(type: PriorityType.LowLevelAfterOthers);
            repeaterEvent.AddHandler(RepeaterHandler);
            failureEvent = new TernaryEvent<IEntity, TProduct, TData>(EntityName + ".Failed");
            recoverEvent = new UnaryEvent(EntityName + ".Recovered");
            failureEvent.AddHandler(InternalFailureHandler, new Priority(type: PriorityType.SimWorldBeforeOthers));
            recoverEvent.AddHandler(InternalRecoverHandler, new Priority(type: PriorityType.SimWorldBeforeOthers));

            // autostart
            if (AutoStart) Model.Schedule(AutoStartDelay, () => Start());
        }

        #endregion
        #region hand

        private void InternalRecoverHandler(IEntity sender)
        {
            damaged = false;
            recovering = false;
            continueAfterFail = true;
            if (!AutoRestart) return;
            if (ContinueProductAfterFailure) StartWhatever();
            else if (CheckMaterial()) StartWhatever();
        }

        private void InternalFailureHandler(IEntity sender, TProduct product, TData data)
        {
            working = false;
            damaged = true;
            nextTimeToFailure = rndFailureTime.Next();
            if(hasRecover && autoRecover) StartRecovering();
        }

        private void InternalFinishedHandler(IEntity sender, TProduct item, TData data)
        {
            working = false;
            nextFinishedSimpleInstance.EventArgs = item;
            activeMaterial.Clear();

            if (IsMachiningTimeStochastic) 
                nextTimeToFinished = rndMachiningTime.Next();
            else 
                nextTimeToFinished = 0;
        }

        private void RepeaterHandler(IEntity sender) 
        {
            if (stop || damaged || recovering) return;
            if (CheckMaterial()) StartWhatever();
        }

        #endregion
        #region impl

        private bool CheckMaterial()
        {
            if (hasBuffer)
            {
                while (!checkMaterialComplete(currentMaterial))
                {
                    if (!checkMaterialUsable(buffer.Preview())) return false;
                    currentMaterial.Add(buffer.Get());
                }
                return true;
            }
            return checkMaterialComplete(currentMaterial);
        }

        #region ctrl

        /// <summary>
        /// will return false if the material is not usable or - given that 
        /// AllowPushDuringMaintainance is false - a maintainance is underway
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Put(TMaterial item)
        {
            if (!pushAllowed) throw new InvalidOperationException(EntityName + " does not allow push for the material.");
            if (!AllowPushDuringMaintainance && (recovering || damaged)) return false;
            if (!checkMaterialUsable.Invoke(item)) return false;
            currentMaterial.Add(item);
            if(autoContinue && !Busy && !stop && checkMaterialComplete(currentMaterial))
                StartWhatever();
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>false if the server is already working or recovering or damaged</returns>
        public bool Start()
        {
            if (working || recovering || damaged) return false;
            stop = false;
            if (!CheckMaterial()) return false;
            StartWhatever();
            return true;
        }

        /// <summary>
        /// Tell the server to stop working on the current product.
        /// CAUTION: even if AutoContinue is set the server will NOT start 
        /// again automatically as soon as it has all required material!
        /// CAUTION: An exception will be thrown if 
        /// cancelScheduledEvents is set to true but
        /// the next scheduled event instance is in 
        /// the past or present. Only future events
        /// can be cancelled in this model version.
        /// </summary>
        /// <returns>false if the server is not working currently (already stopped, damaged or recovering)</returns>
        public bool Stop(bool cancelScheduledEvents = false)
        {
            if (!working) return false;
            stop = true;
            if (cancelScheduledEvents)
            {
                Model.RemoveEvent(nextFinishedInstance);
                Model.RemoveEvent(nextFinishedSimpleInstance);
                if (autoContinue) Model.RemoveEvent(nextRepeaterInstance);
            }
            return true;
        }

        /// <summary>
        /// Schedules a recovery if a recovery interval was given. 
        /// Use ScheduleRecovery otherwise.
        /// </summary>
        /// <returns>
        /// false if the server is already recovering or not damaged
        /// or if no recovery distribution was given to this server.
        /// </returns>
        public bool Recover()
        {
            if (!hasRecover || !damaged || recovering) return false;
            StartRecovering();
            return true;
        }

        /// <summary>
        /// Schedule the recovery of the server
        /// </summary>
        /// <param name="delay">a timespan greater than zero after which the recovered event will occur</param>
        /// <returns>
        /// false if the server is already recovering or not damaged
        /// or if the server is configured to use a distribution for 
        /// the recovery times. (use Recover in that case)
        /// </returns>
        public bool ScheduleRecovery(double delay)
        {
            if (!damaged || recovering) return false;
            nextTimeToRecover = delay;
            StartRecovering();
            return true;
        }

        #endregion
        #region start

        /// <summary>
        /// decide if the next event is a finished event or a fail event
        /// </summary>
        private void StartWhatever() 
        {
            if (!hasFailure)
            {
                StartWorking();
            }
            else if (nextTimeToFinished < nextTimeToFailure)
            {
                nextTimeToFailure -= nextTimeToFinished;
                StartWorking();
            }
            else
            {
                StartFailing();
            }
            continueAfterFail = false;
        }

        private void StartRecovering() 
        {
            recovering = true;

            nextRecoverInstance = recoverEvent.GetInstance(this);
            Model.AddEvent(nextTimeToRecover, nextRecoverInstance);
        }

        private void StartFailing()
        {
            working = true;

            if (ContinueProductAfterFailure && continueAfterFail)
            {
                nextFailureInstance = failureEvent.GetInstance(this, nextFailureInstance.EventArgs, nextFailureInstance.Data);
            }
            else
            {
                activeMaterial.Clear();
                activeMaterial.AddRange(currentMaterial);
                currentMaterial.Clear();

                nextFailureInstance = failureEvent.GetInstance(this, ReturnProduct);
            }

            Model.AddEvent(nextTimeToFailure, nextFailureInstance);
            
            if (ContinueProductAfterFailure)
                nextTimeToFinished -= nextTimeToFailure;
        }

        private void StartWorking() 
        {
            working = true;

            if (ContinueProductAfterFailure && continueAfterFail)
            {
                nextFinishedInstance = entityFinishedEvent.GetInstance(this, nextFailureInstance.EventArgs, nextFailureInstance.Data);
            }
            else
            {
                activeMaterial.Clear();
                activeMaterial.AddRange(currentMaterial);
                currentMaterial.Clear();
                nextFinishedInstance = entityFinishedEvent.GetInstance(this, ReturnProduct);
            }

            nextFinishedSimpleInstance = entityFinishedSimpleEvent.GetInstance(this, default(TProduct)); // product will be set later
            if (IsMachiningTimeFunctional) nextTimeToFinished = MachiningTimeFunction.Invoke(activeMaterial);

            if (autoContinue)
            {
                nextRepeaterInstance = repeaterEvent.GetInstance(this);
                Model.AddEvent(nextTimeToFinished, nextRepeaterInstance);
            }

            // order sensitive: priorities are the same except for added order
            // but ..withdata.. has to be added first so that the internal
            // handler can set the eventargs for the simple event instance!
            Model.AddEvent(nextTimeToFinished, nextFinishedInstance);
            Model.AddEvent(nextTimeToFinished, nextFinishedSimpleInstance);
        }

        #endregion
        #region clear

        /// <summary>
        /// CAUTION: this will also remove the material that 
        /// is currently being used by the server!
        /// </summary>
        public void ClearAllMaterial()
        {
            activeMaterial.Clear();
            currentMaterial.Clear();
        }

        /// <summary>
        /// CAUTION: this will remove the material that 
        /// is currently being used by the server!
        /// </summary>
        public void ClearActiveMaterial()
        {
            activeMaterial.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        public void ClearCurrentMaterial()
        {
            activeMaterial.Clear();
        }

        #endregion
        #region connections

        /// <summary>
        /// if connected to a buffer, the server will start to try get his material
        /// as soon as start is called and then every time a product has been finished. 
        /// It will first use preview to check the available item for usability. if 
        /// usable, the server will pull the item and repeat the process until the 
        /// material is complete. if the material is not usable or the buffer is emptied 
        /// before the material is complete, the server will remain idle. In that
        /// case you will have to restart the server manually as soon as new material is
        /// available in the buffer, even if AutoRestart is set. AutoRestart only 
        /// concerns the restart after a recovery, not after running out of material.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public bool ConnectTo(IItemBuffer<TMaterial> buffer)
        {
            this.buffer = buffer;
            hasBuffer = true;
            return true;
        }

        public bool IsConnectionAllowed(IItemSource<TMaterial> source)
        {
            return pushAllowed;
        }

        public bool ConnectTo(IItemSource<TMaterial> source)
        {
            if (!pushAllowed)
            {
                this.Log<SIM_WARNING>("Attempt to connect " + source.ToString() + " to " + ToString() + " failed. This server doesn't allow push.", Model);
                return false;
            }
            source.EntityCreatedEvent.AddHandler((sender, entity) => Put(entity), new Priority(type: PriorityType.SimWorldBeforeOthers));
            // TODO: save handlers to be able to remove them on reset, if they have been added dynamically!
            return true;
        }

        /// <summary>
        /// CAUTION: it will be ignored if the sink doesn't accept the newly created entity. 
        /// If you want to be notified about it, use NotifyItemNotAccepted
        /// Caution: the sources will be removed on reset
        /// </summary>
        /// <param name="source"></param>
        public void ConnectTo(params IItemSource<TMaterial>[] sources)
        {
            if (!pushAllowed) return;
            foreach (IItemSource<TMaterial> src in sources)
            {
                src.EntityCreatedEvent.AddHandler((sender, entity) => Put(entity), new Priority(type: PriorityType.SimWorldBeforeOthers));
                // TODO: save handlers to be able to remove them on reset, if they have been added dynamically!
            }
        }

        /// <summary>
        /// CAUTION: it will be ignored if the sink doesn't accept the newly created entity. 
        /// If you want to be notified about it, use NotifyItemNotAccepted
        /// Caution: all sources will be removed on reset
        /// </summary>
        /// <param name="sources"></param>
        public void ConnectTo(IEnumerable<IItemSource<TMaterial>> sources)
        {
            if (!pushAllowed) return;
            foreach (IItemSource<TMaterial> src in sources)
            {
                src.EntityCreatedEvent.AddHandler((sender, entity) => Put(entity), new Priority(type: PriorityType.SimWorldBeforeOthers));
                // TODO: save handlers to be able to remove them on reset, if they have been added dynamically!
            }
        }

        public bool ConnectTo(IItemSink<TProduct> receiver)
        {
            entityFinishedSimpleEvent.AddHandler((sender, entity) => receiver.Put(entity), new Priority(type: PriorityType.SimWorldBeforeOthers));
            // TODO: save handler if dynamic flag is set to be able to remove them on reset, if they have been added dynamically!
            return true;
        }

        #endregion

        #endregion
        #region tool

        private bool DefaultMaterialCompleteCheck(List<TMaterial> material) { return material.Count != 0; }
        
        private bool DefaultMaterialUsableCheck(TMaterial material) { return material != null; }
        
        private Tuple<TProduct, TData> DefaultProductGenerator(List<TMaterial> material) 
        { 
            return new Tuple<TProduct, TData>(new TProduct(), default(TData)); 
        }

        private Tuple<TProduct, TData> ReturnProduct()
        {
            return createProduct.Invoke(activeMaterial);
        }

        #endregion
    }
}
