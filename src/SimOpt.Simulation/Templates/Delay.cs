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

            // SIM-58 (DES-1): initialItem is null whenever T is a reference type and no initial
            // item was supplied — the ordinary case — and Reset runs on every evaluation.
            // SIM-132: Model.Reset() clears the event scheduler before resetting entities, so the
            // items in flight are dropped with their release events; only the initial item is back.
            count = object.Equals(initialItem, default(T)) ? 0 : 1;
            refusedCount = 0;

            // SIM-58 (DES-2): InitializeDelay schedules the initial item's release, but Reset used
            // to restore the item without re-scheduling it. The item then sat here forever and
            // blocked every Put, so evaluation #2 of the same model behaved differently from
            // evaluation #1 — silently, which is what makes it dangerous to an optimizer.
            // Re-scheduling from here lands in the freshly cleared calendar.
            if (count == 1 && intervalInitialized)
                ScheduleRelease(initialItem);
        }

        #endregion
        #region virt

        public virtual void OnQueueInitialized()
        {
            // if (AutoStart) Start(autoStartDelay);
        }

        #endregion
        #region cvar

        private T initialItem;

        // SIM-132: the items inside, each on its own timer, against how many the delay may hold.
        private int count;
        private int capacity = 1;
        private int refusedCount;
        private Action<T> notifyItemNotAccepted = Noop;

        private BinaryEvent<IEntity, T> itemReleasedSimple;
        private TernaryEvent<IEntity, T, double> itemReleased;

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

        /// <summary>
        /// SIM-132: how many items the delay holds at once, each on its own timer. The default, 1, is
        /// the old behaviour; int.MaxValue sets no limit. An item put while the delay is full is
        /// refused: Put returns false, RefusedCount goes up and NotifyItemNotAccepted is called,
        /// because a source wired with ConnectTo ignores the return value.
        /// </summary>
        public int Capacity
        {
            get { return capacity; }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(value), value,
                    "A delay holds at least one item.");
                if (value < count) throw new InvalidOperationException(
                    EntityName + " holds " + count + " items, so its capacity cannot be set to " + value + ".");
                capacity = value;
            }
        }

        /// <summary>The items inside, each waiting for its own release.</summary>
        public int Count { get { return count; } }

        public bool IsFull { get { return count >= capacity; } }

        /// <summary>SIM-132: the items refused because the delay was full, since the last reset.</summary>
        public int RefusedCount { get { return refusedCount; } }

        /// <summary>Called with every item the delay refuses because it is full.</summary>
        public Action<T> NotifyItemNotAccepted
        {
            get { return notifyItemNotAccepted; }
            set { notifyItemNotAccepted = value ?? Noop; }
        }

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
            count = object.Equals(initialItem, default(T)) ? 0 : 1;

            // TODO: default states

            itemReleased = new TernaryEvent<IEntity, T, double>(EntityName + ".ItemReleased");
            itemReleasedSimple = new BinaryEvent<IEntity, T>(EntityName + ".ItemReleasedSimple");
            itemReleased.AddHandler(InternalReleasedItemHandler, new Priority(type: PriorityType.LowLevelBeforeOthers));

            // initialize
            this.randomDistribution = interval;
            this.randomIntervalGenerator = new Random<double>(this, interval, Model.Antithetic, Model.NonStochasticMode);
            intervalInitialized = true;

            // setup initial item (tested in DelayTests, SIM-132)
            if (count == 1)
                ScheduleRelease(initialItem);

            OnQueueInitialized();
        }

        #endregion
        #region hand

        private void InternalReleasedItemHandler(IEntity sender, T item, double time)
        {
            count--;
            if (LogRelease)
                this.Log<SIM_INFO>(item.ToString() + " was released after a delay of " + time.ToTimeSpan().ToString() + ".", Model);
        }

        #endregion
        #region impl

        public bool Put(T item)
        {
            if (count >= capacity)
            {
                // SIM-132: a source wired with ConnectTo ignores the false, so the refusal is
                // counted and reported here, or the item would vanish without a trace.
                refusedCount++;
                if (LogReject)
                    this.Log<SIM_INFO>(item.ToString() + " was rejected because " + EntityName + " is full (" + count + " of " + capacity + ").", Model);
                notifyItemNotAccepted.Invoke(item);
                return false;
            }
            count++;

            double delay = ScheduleRelease(item);

            if (LogReceive)
                this.Log<SIM_INFO>(item.ToString() + " was received and will be delayed for " + delay.ToTimeSpan().ToString() + ".", Model);

            return true;
        }

        /// <summary>Draws the item's own delay and schedules its release; returns the delay.</summary>
        private double ScheduleRelease(T item)
        {
            double delay = randomIntervalGenerator.Next();

            Model.AddEvent(delay, itemReleased.GetInstance(this, item, delay));
            Model.AddEvent(delay, itemReleasedSimple.GetInstance(this, item));

            return delay;
        }

        private static void Noop(T ignore) { }

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