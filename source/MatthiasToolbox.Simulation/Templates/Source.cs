using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Simulation.Engine;
using MatthiasToolbox.Simulation.Entities;
using MatthiasToolbox.Simulation.Interfaces;
using MatthiasToolbox.Basics.Datastructures.Geometry;
using MatthiasToolbox.Simulation.Events;
using MatthiasToolbox.Mathematics.Stochastics.Interfaces;
using MatthiasToolbox.Basics.Exceptions;
using MatthiasToolbox.Simulation.Enum;
using MatthiasToolbox.Basics.Interfaces;

namespace MatthiasToolbox.Simulation.Templates
{
    /// <summary>
    /// non generic wrapper
    /// </summary>
    [Serializable]
    public class Source : Source<object> 
    {
        #region ctor

        /// <summary>
        /// Creates an uninitialized instance. Initialize AND 
        /// InitializeSource will have to be called before the
        /// source can be used.
        /// </summary>
        public Source() : base() { }

        /// <summary>
        /// CAUTION: The starttime of the model must be initialized 
        /// if autostart is used.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="interval"></param>
        /// <param name="autoStartDelay"></param>
        /// <param name="generator"></param>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="states"></param>
        /// <param name="transitions"></param>
        /// <param name="initialState"></param>
        /// <param name="initialPosition"></param>
        /// <param name="manager"></param>
        /// <param name="currentHolder"></param>
        public Source(IModel model,
                      IDistribution<double> interval,
                      Func<object> generator,
                      double autoStartDelay = double.NaN,
                      Priority entityCreationEventPriority = null,
                      string id = "",
                      string name = "",
                      List<string> states = null,
                      List<Tuple<string, string>> transitions = null,
                      string initialState = null,
                      Point initialPosition = null,
                      IResourceManager manager = null,
                      IEntity currentHolder = null)
            : base(model, interval, generator, autoStartDelay, entityCreationEventPriority, id, name, states, transitions, initialState, initialPosition, manager, currentHolder)
        { }

        

        /// <summary>
        /// CAUTION: The starttime of the model must be initialized 
        /// if autostart is used.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="seedID"></param>
        /// <param name="interval"></param>
        /// <param name="autoStartDelay"></param>
        /// <param name="generator"></param>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="states"></param>
        /// <param name="transitions"></param>
        /// <param name="initialState"></param>
        /// <param name="initialPosition"></param>
        /// <param name="manager"></param>
        /// <param name="currentHolder"></param>
        public Source(IModel model,
                      int seedID,
                      IDistribution<double> interval,
                      Func<object> generator,
                      double autoStartDelay = double.NaN,
                      Priority entityCreationEventPriority = null,
                      string id = "",
                      string name = "",
                      List<string> states = null,
                      List<Tuple<string, string>> transitions = null,
                      string initialState = null,
                      Point initialPosition = null,
                      IResourceManager manager = null,
                      IEntity currentHolder = null)
            : base(model, seedID, interval, generator, autoStartDelay, entityCreationEventPriority, id, name, states, transitions, initialState, initialPosition, manager, currentHolder)
        { }

        /// <summary>
        /// CAUTION: The starttime of the model must be initialized 
        /// if autostart is used.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="interval"></param>
        /// <param name="autoStartDelay"></param>
        /// <param name="generator"></param>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="states"></param>
        /// <param name="transitions"></param>
        /// <param name="initialState"></param>
        /// <param name="initialPosition"></param>
        /// <param name="manager"></param>
        /// <param name="currentHolder"></param>
        public Source(IModel model,
                      IDistribution<double> interval,
                      double autoStartDelay = double.NaN,
                      Priority entityCreationEventPriority = null,
                      string id = "",
                      string name = "",
                      List<string> states = null,
                      List<Tuple<string, string>> transitions = null,
                      string initialState = null,
                      Point initialPosition = null,
                      IResourceManager manager = null,
                      IEntity currentHolder = null)
            : base(model, interval, autoStartDelay, entityCreationEventPriority, id, name, states, transitions, initialState, initialPosition, manager, currentHolder)
        { }



        /// <summary>
        /// CAUTION: The starttime of the model must be initialized 
        /// if autostart is used.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="seedID"></param>
        /// <param name="interval"></param>
        /// <param name="autoStartDelay"></param>
        /// <param name="generator"></param>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="states"></param>
        /// <param name="transitions"></param>
        /// <param name="initialState"></param>
        /// <param name="initialPosition"></param>
        /// <param name="manager"></param>
        /// <param name="currentHolder"></param>
        public Source(IModel model,
                      int seedID,
                      IDistribution<double> interval,
                      double autoStartDelay = double.NaN,
                      Priority entityCreationEventPriority = null,
                      string id = "",
                      string name = "",
                      List<string> states = null,
                      List<Tuple<string, string>> transitions = null,
                      string initialState = null,
                      Point initialPosition = null,
                      IResourceManager manager = null,
                      IEntity currentHolder = null)
            : base(model, seedID, interval, autoStartDelay, entityCreationEventPriority, id, name, states, transitions, initialState, initialPosition, manager, currentHolder)
        { }

        #endregion
    }

    /// <summary>
    /// less generic wrapper
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable] 
    public class Source<T> : Source<T, object>
        where T : new()
    {
        #region ctor

        /// <summary>
        /// Creates an uninitialized instance. Initialize AND 
        /// InitializeSource will have to be called before the
        /// source can be used.
        /// </summary>
        public Source() : base() { }

        /// <summary>
        /// CAUTION: The starttime of the model must be initialized 
        /// if autostart is used.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="interval"></param>
        /// <param name="autoStartDelay"></param>
        /// <param name="generator"></param>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="states"></param>
        /// <param name="transitions"></param>
        /// <param name="initialState"></param>
        /// <param name="initialPosition"></param>
        /// <param name="manager"></param>
        /// <param name="currentHolder"></param>
        public Source(IModel model,
                      IDistribution<double> interval,
                      double autoStartDelay = double.NaN,
                      Priority entityCreationEventPriority = null,
                      string id = "",
                      string name = "",
                      List<string> states = null,
                      List<Tuple<string, string>> transitions = null,
                      string initialState = null,
                      Point initialPosition = null,
                      IResourceManager manager = null,
                      IEntity currentHolder = null)
            : base(model, interval, autoStartDelay, null, entityCreationEventPriority, id, name, states, transitions, initialState, initialPosition, manager, currentHolder)
        { 
            
        }

        /// <summary>
        /// CAUTION: The starttime of the model must be initialized 
        /// if autostart is used.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="seedID"></param>
        /// <param name="interval"></param>
        /// <param name="autoStartDelay"></param>
        /// <param name="generator"></param>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="states"></param>
        /// <param name="transitions"></param>
        /// <param name="initialState"></param>
        /// <param name="initialPosition"></param>
        /// <param name="manager"></param>
        /// <param name="currentHolder"></param>
        public Source(IModel model,
                      int seedID,
                      IDistribution<double> interval,
                      double autoStartDelay = double.NaN,
                      Priority entityCreationEventPriority = null,
                      string id = "",
                      string name = "",
                      List<string> states = null,
                      List<Tuple<string, string>> transitions = null,
                      string initialState = null,
                      Point initialPosition = null,
                      IResourceManager manager = null,
                      IEntity currentHolder = null)
            : base(model, seedID, interval, autoStartDelay, null, entityCreationEventPriority, id, name, states, transitions, initialState, initialPosition, manager, currentHolder)
        { }

        /// <summary>
        /// CAUTION: The starttime of the model must be initialized 
        /// if autostart is used.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="interval"></param>
        /// <param name="autoStartDelay"></param>
        /// <param name="generator"></param>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="states"></param>
        /// <param name="transitions"></param>
        /// <param name="initialState"></param>
        /// <param name="initialPosition"></param>
        /// <param name="manager"></param>
        /// <param name="currentHolder"></param>
        public Source(IModel model,
                      IDistribution<double> interval,
                      Func<T> generator,
                      double autoStartDelay = double.NaN,
                      Priority entityCreationEventPriority = null,
                      string id = "",
                      string name = "",
                      List<string> states = null,
                      List<Tuple<string, string>> transitions = null,
                      string initialState = null,
                      Point initialPosition = null,
                      IResourceManager manager = null,
                      IEntity currentHolder = null)
            : base(model, interval, autoStartDelay, null, entityCreationEventPriority, id, name, states, transitions, initialState, initialPosition, manager, currentHolder)
        {
            this.customGenerator = generator;
            this.Generator = CustomGenerator;
        }

        /// <summary>
        /// CAUTION: The starttime of the model must be initialized 
        /// if autostart is used.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="seedID"></param>
        /// <param name="interval"></param>
        /// <param name="autoStartDelay"></param>
        /// <param name="generator"></param>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="states"></param>
        /// <param name="transitions"></param>
        /// <param name="initialState"></param>
        /// <param name="initialPosition"></param>
        /// <param name="manager"></param>
        /// <param name="currentHolder"></param>
        public Source(IModel model,
                      int seedID,
                      IDistribution<double> interval,
                      Func<T> generator,
                      double autoStartDelay = double.NaN,
                      Priority entityCreationEventPriority = null,
                      string id = "",
                      string name = "",
                      List<string> states = null,
                      List<Tuple<string, string>> transitions = null,
                      string initialState = null,
                      Point initialPosition = null,
                      IResourceManager manager = null,
                      IEntity currentHolder = null)
            : base(model, seedID, interval, autoStartDelay, null, entityCreationEventPriority, id, name, states, transitions, initialState, initialPosition, manager, currentHolder)
        {
            this.customGenerator = generator;
            this.Generator = CustomGenerator;
        }

        #endregion

        // This design prevents lambda problems on serialization
        private Func<T> customGenerator;

        private Tuple<T, object> CustomGenerator()
        {
            return Tuple.Create<T, object>(customGenerator.Invoke(), null);
        }


    }

    /// <summary>
    /// a simulation object which creates other simulation objects
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TData"></typeparam>
    [Serializable]
    public class Source<TEntity, TData> : StateMachineEntity, 
                                          IResource,
                                          IPosition<Point>, 
                                          ISeedSource, 
                                          IEntity, 
                                          IItemSource<TEntity>, 
                                          IItemSource<TEntity, TData>
        where TEntity : new()
    {
        #region over

        public override void Reset()
        {
            base.Reset();
            if (AutoStart) Start(autoStartDelay);
            else running = false;
        }

        #endregion
        #region virt

        public virtual void OnSourceInitialized() 
        {
            if (AutoStart) Start(autoStartDelay);
        }

        #endregion
        #region cvar

        private bool autoStart;
        private bool running = false;
        private bool intervalInitialized;
        private Random<double> randomIntervalGenerator;
        private IDistribution<double> randomDistribution;
        private Func<TEntity> simpleGenerator;
        private double autoStartDelay = double.NaN;
        private UnaryEvent repeaterEvent;
        private BinaryEvent<IEntity, TEntity> entityCreatedEvent;
        private TernaryEvent<IEntity, TEntity, TData> entityWithDataCreatedEvent;
        private UnaryEventInstance<IEntity> nextScheduledRepeaterInstance;
        private BinaryEventInstance<IEntity, TEntity> nextScheduledEventInstance;
        private TernaryEventInstance<IEntity, TEntity, TData> nextScheduledEventWithDataInstance;
        private Func<TEntity> customGenerator;

        #endregion
        #region prop

        /// <summary>
        /// Indicate if the source is currently active
        /// </summary>
        public bool Running { get { return running; } }

        /// <summary>
        /// this event will fire whenever an entity is created
        /// </summary>
        public BinaryEvent<IEntity, TEntity> EntityCreatedEvent
        {
            get { return entityCreatedEvent; }
            // get { return (BinaryEvent<IEntity, TEntity>)entityWithDataCreatedEvent; }
        }

        /// <summary>
        /// this event will fire whenever an entity is created
        /// </summary>
        public TernaryEvent<IEntity, TEntity, TData> EntityWithDataCreatedEvent
        {
            get { return entityWithDataCreatedEvent; }
        }

        public bool AutoStart { get { return autoStart; } }

        public double AutoStartDelay { get { return autoStartDelay; } }

        public Random<double> RandomIntervalGenerator { get { return randomIntervalGenerator; } }

        public IDistribution<double> RandomDistribution { get { return randomDistribution; } }

        
        public Func<TEntity> SimpleGenerator
        {
            get { return simpleGenerator; }
            set 
            {
                simpleGenerator = value;
                Generator = SimpleGeneratorInvoker;
            }
        }

        /// <summary>
        /// The method which will be used to create instances of TEntity
        /// </summary>
        public Func<Tuple<TEntity, TData>> Generator { get; set; }

        #endregion
        #region ctor

        // TODO: default source states!

        /// <summary>
        /// Creates an uninitialized instance. Initialize AND 
        /// InitializeSource will have to be called before the
        /// source can be used.
        /// </summary>
        public Source() : base() { }

        /// <summary>
        /// CAUTION: The starttime of the model must be initialized 
        /// if autostart is used.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="interval"></param>
        /// <param name="autoStartDelay"></param>
        /// <param name="generator"></param>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="states"></param>
        /// <param name="transitions"></param>
        /// <param name="initialState"></param>
        /// <param name="initialPosition"></param>
        /// <param name="manager"></param>
        /// <param name="currentHolder"></param>
        public Source(IModel model,
                      IDistribution<double> interval, 
                      double autoStartDelay = double.NaN, 
                      Func<Tuple<TEntity, TData>> generator = null,
                      Priority entityCreationEventPriority = null,
                      string id = "",
                      string name = "",
                      List<string> states = null,
                      List<Tuple<string, string>> transitions = null,
                      string initialState = null,
                      Point initialPosition = null,
                      IResourceManager manager = null,
                      IEntity currentHolder = null)
            : base(model, id, name, states, transitions, initialState, initialPosition, manager, currentHolder)
        {
            InitializeSource(interval, autoStartDelay, generator, entityCreationEventPriority);
        }

        /// <summary>
        /// CAUTION: The starttime of the model must be initialized 
        /// if autostart is used.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="seedID"></param>
        /// <param name="interval"></param>
        /// <param name="autoStartDelay"></param>
        /// <param name="generator"></param>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="states"></param>
        /// <param name="transitions"></param>
        /// <param name="initialState"></param>
        /// <param name="initialPosition"></param>
        /// <param name="manager"></param>
        /// <param name="currentHolder"></param>
        public Source(IModel model,
                      int seedID,
                      IDistribution<double> interval, 
                      double autoStartDelay = double.NaN, 
                      Func<Tuple<TEntity, TData>> generator = null,
                      Priority entityCreationEventPriority = null,
                      string id = "",
                      string name = "",
                      List<string> states = null,
                      List<Tuple<string, string>> transitions = null,
                      string initialState = null,
                      Point initialPosition = null,
                      IResourceManager manager = null,
                      IEntity currentHolder = null)
            : base(model, seedID, id, name, states, transitions, initialState, initialPosition, manager, currentHolder)
        {
            InitializeSource(interval, autoStartDelay, generator, entityCreationEventPriority);
        }

        #endregion
        #region init

        /// <summary>
        /// CAUTION: The starttime of the model must be initialized 
        /// if autostart is used.
        /// </summary>
        /// <param name="interval"></param>
        /// <param name="autoStartDelay"></param>
        /// <param name="generator"></param>
        public void InitializeSource(IDistribution<double> interval, 
                                     double autoStartDelay = double.NaN, 
                                     Func<Tuple<TEntity, TData>> generator = null, 
                                     Priority entityCreationEventPriority = null)
        {
            if (!base.IsInitialized) throw new InitializationException("The model initialization has to be finished before the source interval can be configured!");
            if (intervalInitialized) throw new InitializationException("This instance was already initialized!");
            if (!double.IsNaN(autoStartDelay) && autoStartDelay >= 0) 
            {
                if (Model.IsRunning) throw new ApplicationException("Attempted to initialize source autostarting while the model was already running.");
                autoStart = true;
                this.autoStartDelay = autoStartDelay;
            }

            // set entity generator
            if (generator != null)
            {
                this.Generator = generator;
            }
            else
            {
                this.Generator = DefaultGenerator;
            }

            // create events
            entityWithDataCreatedEvent = new TernaryEvent<IEntity, TEntity, TData>(this.EntityName + ".EntityWithDataCreatedEvent");
            entityCreatedEvent = new BinaryEvent<IEntity, TEntity>(this.EntityName + ".EntityCreatedEvent");
            repeaterEvent = new UnaryEvent(this.EntityName + ".RepeaterEvent");

            // set event priorities
            repeaterEvent.Priority = new Priority(type: PriorityType.LowLevelAfterOthers);
            if (entityCreationEventPriority != null)
            {
                entityCreatedEvent.Priority = entityCreationEventPriority;
                entityWithDataCreatedEvent.Priority = entityCreationEventPriority;
            }
            else
            {
                Priority p = new Priority(type: PriorityType.SimWorldBeforeOthers);
                entityCreatedEvent.Priority = p;
                entityWithDataCreatedEvent.Priority = p;
            }

            // add handlers
            entityWithDataCreatedEvent.AddHandler(InternalEntitiyCreationHandler, new Priority(type: PriorityType.SimWorldBeforeOthers));
            repeaterEvent.AddHandler(RepeaterHandler);

            // initialize
            this.randomDistribution = interval;
            this.randomIntervalGenerator = new Random<double>(this, interval, Model.Antithetic, Model.NonStochasticMode);
            intervalInitialized = true;
            OnSourceInitialized();
        }

        #endregion
        #region hand

        private void InternalEntitiyCreationHandler(IEntity source, TEntity entity, TData data)
        {
            nextScheduledEventInstance.EventArgs = entity;
        }

        private void RepeaterHandler(IEntity sender)
        {
            if (!running) return;
            Start(randomIntervalGenerator.Next());
        }

        #endregion
        #region impl

        /// <summary>
        /// CAUTION: if the delay is zero the event will
        /// be scheduled with LowLevelAfterOthers priority!
        /// </summary>
        /// <param name="delay"></param>
        public void Start(TimeSpan delay) { Start(delay.ToDouble()); }

        /// <summary>
        /// CAUTION: if the delay is zero the event will
        /// be scheduled with LowLevelAfterOthers priority!
        /// </summary>
        /// <param name="delay"></param>
        public void Start(double delay = 0) 
        {
            running = true;

            nextScheduledRepeaterInstance = repeaterEvent.GetInstance(this);
            nextScheduledEventInstance = entityCreatedEvent.GetInstance(this, default(TEntity)); // entity parameter will be set later
            nextScheduledEventWithDataInstance = entityWithDataCreatedEvent.GetInstance(this, Generator);

            if (delay == 0)
            {
                // order sensitive: priorities are the same except for added order
                // but ..withdata.. has to be added first so that the internal
                // handler can set the eventargs for the simple event instance!
#if ImmediateEvents
                Model.AddImmediateEvent(nextScheduledEventWithDataInstance);
                Model.AddImmediateEvent(nextScheduledEventInstance);
                Model.AddImmediateEvent(nextScheduledRepeaterInstance);
#else
                nextScheduledEventWithDataInstance.Raise();
                nextScheduledEventInstance.Raise();
                nextScheduledRepeaterInstance.Raise();
#endif
            }
            else
            {
                // order sensitive: priorities are the same except for added order
                // but ..withdata.. has to be added first so that the internal
                // handler can set the eventargs for the simple event instance!
                Model.AddEvent(delay, nextScheduledEventWithDataInstance);
                Model.AddEvent(delay, nextScheduledEventInstance);
                Model.AddEvent(delay, nextScheduledRepeaterInstance);
            }
        }

        /// <summary>
        /// CAUTION: An exception will be thrown if 
        /// cancelScheduledEvents is set to true but
        /// the next scheduled event instance is in 
        /// the past or present. Only future events
        /// can be cancelled in this model version.
        /// </summary>
        /// <param name="cancelScheduledEvents"></param>
        public void Stop(bool cancelScheduledEvents = false) 
        { 
            running = false;
            if (cancelScheduledEvents)
            {
                Model.RemoveEvent(nextScheduledEventInstance);
                Model.RemoveEvent(nextScheduledEventWithDataInstance);
                Model.RemoveEvent(nextScheduledRepeaterInstance);
            }
        }

        /// <summary>
        /// CAUTION: it will be ignored if the sink doesn't accept the newly created entity.
        /// If you want to be notified this must be done by the receiver.
        /// </summary>
        /// <param name="receiver"></param>
        public bool ConnectTo(IItemSink<TEntity> receiver) 
        {
            entityCreatedEvent.AddHandler((sender, entity) => receiver.Put(entity), new Priority(type: PriorityType.SimWorldBeforeOthers));
            // TODO: save handler if dynamic flag is set to be able to remove them on reset, if they have been added dynamically!
            return true;
        }

        #endregion
        #region tool


        private Tuple<TEntity, TData> DefaultGenerator() 
        {
            return new Tuple<TEntity, TData>(new TEntity(), default(TData));
        }

        private Tuple<TEntity, TData> SimpleGeneratorInvoker()
        {
            return new Tuple<TEntity, TData>(simpleGenerator(), default(TData));
        }

        #endregion
    }
}
