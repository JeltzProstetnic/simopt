using System;
using System.Collections.Generic;
using SimOpt.Simulation.Interfaces;
using SimOpt.Simulation.Events;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Entities;
using SimOpt.Basics.Datastructures.Geometry;
using SimOpt.Mathematics.Stochastics.Interfaces;
using SimOpt.Basics.Exceptions;
using SimOpt.Simulation.Enum;

namespace SimOpt.Simulation.Templates
{
    [Serializable]
    public class Delay : Delay<object> 
    {
        #region ctor

        public Delay() { }

        public Delay(IModel model,
                     IDistribution<double> interval,
                     string id = "",
                     string name = "",
                     object initialItem = null,
                     List<string> states = null,
                     List<Tuple<string, string>> transitions = null,
                     string initialState = null,
                     Point initialPosition = null,
                     IResourceManager manager = null,
                     IEntity currentHolder = null)
            : base(model, interval, id, name, initialItem, states, transitions, initialState, initialPosition, manager, currentHolder)
        { }

        public Delay(IModel model,
                     IDistribution<double> interval,
                     int seedID,
                     string id = "",
                     string name = "",
                     object initialItem = null,
                     List<string> states = null,
                     List<Tuple<string, string>> transitions = null,
                     string initialState = null,
                     Point initialPosition = null,
                     IResourceManager manager = null,
                     IEntity currentHolder = null)
            : base(model, interval, seedID, id, name, initialItem, states, transitions, initialState, initialPosition, manager, currentHolder)
        { }

        #endregion
    }
    [Serializable]
    public class Delay<T> : StateMachineEntity, IItemSource<T, double>, IItemSink<T>
    {
        #region over

        public override void Reset()
        {
            base.Reset();
            hasItem = !initialItem.Equals(default(T));
            currentItem = initialItem;
        }

        #endregion
        #region virt

        public virtual void OnQueueInitialized()
        {
            // if (AutoStart) Start(autoStartDelay);
        }

        #endregion
        #region cvar

        private bool hasItem;
        private T initialItem;
        private T currentItem;

        private BinaryEvent<IEntity, T> itemReleasedSimple;
        private TernaryEvent<IEntity, T, double> itemReleased;
        private BinaryEventInstance<IEntity, T> nextItemReleasedSimpleInstance;
        private TernaryEventInstance<IEntity, T, double> nextItemReleasedInstance;

        private bool intervalInitialized;
        private Random<double> randomIntervalGenerator;
        private IDistribution<double> randomDistribution;

        #endregion
        #region prop

        public BinaryEvent<IEntity, T> ItemReleasedSimple
        {
            get { return itemReleasedSimple; }
        }

        public TernaryEvent<IEntity, T, double> ItemReleased
        {
            get { return itemReleased; }
        }

        /// <summary>
        /// this is only a wrapper for the ItemReleased event
        /// </summary>
        public TernaryEvent<IEntity, T, double> EntityWithDataCreatedEvent
        {
            get { return ItemReleased; }
        }

        /// <summary>
        /// this is only a wrapper for the ItemReleased event
        /// </summary>
        public BinaryEvent<IEntity, T> EntityCreatedEvent
        {
            get { return ItemReleasedSimple; }
        }

        public Random<double> RandomIntervalGenerator { get { return randomIntervalGenerator; } }

        public IDistribution<double> RandomDistribution { get { return randomDistribution; } }

        public bool LogRelease { get; set; }

        public bool LogReceive { get; set; }

        public bool LogReject { get; set; }

        #endregion
        #region ctor

        public Delay() { }

        public Delay(IModel model,
                     IDistribution<double> interval, 
                     string id = "", 
                     string name = "", 
                     T initialItem = default(T), 
                     List<string> states = null, 
                     List<Tuple<string, string>> transitions = null, 
                     string initialState = null, 
                     Point initialPosition = null, 
                     IResourceManager manager = null, 
                     IEntity currentHolder = null)
            : base(model, id, name, states, transitions, initialState, initialPosition, manager, currentHolder)
        {
            InitializeDelay(initialItem, interval);
        }

        public Delay(IModel model,
                     IDistribution<double> interval, 
                     int seedID,
                     string id = "",
                     string name = "",
                     T initialItem = default(T), 
                     List<string> states = null,
                     List<Tuple<string, string>> transitions = null,
                     string initialState = null,
                     Point initialPosition = null,
                     IResourceManager manager = null,
                     IEntity currentHolder = null)
            : base(model, seedID, id, name, states, transitions, initialState, initialPosition, manager, currentHolder)
        {
            InitializeDelay(initialItem, interval);
        }

        #endregion
        #region init

        public void InitializeDelay(T initialItem, IDistribution<double> interval) 
        {
            if (!base.IsInitialized) throw new InitializationException("The model initialization has to be finished before the source interval can be configured!");
            if (intervalInitialized) throw new InitializationException("This instance was already initialized!");

            this.initialItem = initialItem;
            if (!object.Equals(initialItem, default(T)))
            {
                currentItem = initialItem;
                hasItem = true;
            }
            
            // TODO: default states

            itemReleased = new TernaryEvent<IEntity, T, double>(EntityName + ".ItemReleased");
            itemReleasedSimple = new BinaryEvent<IEntity, T>(EntityName + ".ItemReleasedSimple");
            itemReleased.AddHandler(InternalReleasedItemHandler, new Priority(type: PriorityType.LowLevelBeforeOthers));

            // initialize
            this.randomDistribution = interval;
            this.randomIntervalGenerator = new Random<double>(this, interval, Model.Antithetic, Model.NonStochasticMode);
            intervalInitialized = true;

            // setup initial item
            if (hasItem) // TODO: test this.
            {
                double delay = randomIntervalGenerator.Next();

                nextItemReleasedSimpleInstance = itemReleasedSimple.GetInstance(this, currentItem);
                nextItemReleasedInstance = itemReleased.GetInstance(this, currentItem, delay);

                Model.AddEvent(delay, nextItemReleasedInstance);
                Model.AddEvent(delay, nextItemReleasedSimpleInstance);
            }

            OnQueueInitialized();
        }

        #endregion
        #region hand

        private void InternalReleasedItemHandler(IEntity sender, T item, double time)
        {
            hasItem = false;
            currentItem = default(T);
            if (LogRelease)
                this.Log<SIM_INFO>(item.ToString() + " was released after a delay of " + time.ToTimeSpan().ToString() + ".", Model);
        }

        #endregion
        #region impl

        public bool Put(T item)
        {
            if (hasItem)
            {
                if (LogReject)
                    this.Log<SIM_INFO>(item.ToString() + " was rejected because " + currentItem.ToString() + " is currently in " + EntityName + ".", Model);
                return false;
            }
            hasItem = true;
            currentItem = item;

            double delay = randomIntervalGenerator.Next();
            
            nextItemReleasedSimpleInstance = itemReleasedSimple.GetInstance(this, item);
            nextItemReleasedInstance = itemReleased.GetInstance(this, item, delay);

            Model.AddEvent(delay, nextItemReleasedInstance);
            Model.AddEvent(delay, nextItemReleasedSimpleInstance);

            if (LogReceive)
                this.Log<SIM_INFO>(item.ToString() + " was received and will be delayed for " + delay.ToTimeSpan().ToString() + ".", Model);

            return true;
        }

        #region connections

        public bool IsConnectionAllowed(IItemSource<T> source)
        {
            return true;
        }

        public bool ConnectTo(IItemSource<T> source)
        {
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
        public void ConnectTo(params IItemSource<T>[] sources)
        {
            foreach (IItemSource<T> src in sources)
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
        public void ConnectTo(IEnumerable<IItemSource<T>> sources)
        {
            foreach (IItemSource<T> src in sources)
            {
                src.EntityCreatedEvent.AddHandler((sender, entity) => Put(entity), new Priority(type: PriorityType.SimWorldBeforeOthers));
                // TODO: save handlers to be able to remove them on reset, if they have been added dynamically!
            }
        }

        public bool ConnectTo(IItemSink<T> receiver)
        {
            itemReleasedSimple.AddHandler((sender, entity) => receiver.Put(entity), new Priority(type: PriorityType.SimWorldBeforeOthers));
            // TODO: save handler if dynamic flag is set to be able to remove them on reset, if they have been added dynamically!
            return true;
        }

        #endregion

        #endregion
    }
}