using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Interfaces;
using SimOpt.Simulation.Engine;
using SimOpt.Basics.Datastructures.Geometry;
using System.Collections;
using SimOpt.Logging;
using SimOpt.Mathematics.Stochastics.Distributions;
using SimOpt.Simulation.Enum;
using SimOpt.Simulation.Events;
using SimOpt.Basics.Interfaces;

// TODO: ████ buffer refactoring

namespace SimOpt.Simulation.Templates
{
    [Serializable]
    public class Buffer : Buffer<object>
    {
        #region ctor

        public Buffer() : base() { }

        public Buffer(IModel model,
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

        public Buffer(IModel model,
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

        public Buffer(IModel model,
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

        public Buffer(IModel model,
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
    [Serializable]
    public class Buffer<T> : StateMachineEntity, 
                            IResource,
                            IPosition<Point>, 
                            ISeedSource, 
                            IEntity,
                            IEnumerable, 
                            IEnumerable<T>,
                            IItemBuffer<T>,
                            IElectiveBuffer<T, int>,
                            IElectiveBuffer<T, string>
    {
        #region over

        public override void Reset()
        {
            base.Reset();
            this.maxCapacity = initialMaxCapacity;
            this.itemSelector = initialItemSelector;
            nextID = 0;
            itemsByID.Clear();
            entityIDs.Clear();
            itemsByEntityID.Clear();
            itemsByPriority.Clear();
            itemPriorityMap.Clear();
            sources.Clear();
        }

        #endregion
        #region cvar

        private int nextID = 0;
        private int maxCapacity;
        private int initialMaxCapacity;
        private Dictionary<int, T> itemsByID;
        private Dictionary<int, string> entityIDs;
        private Dictionary<string, T> itemsByEntityID;
        private SortedDictionary<Priority, T> itemsByPriority;
        private Dictionary<T, Priority> itemPriorityMap;
        private Func<T> initialItemSelector;
        private Func<T> itemSelector;
        private Random<double> rnd;
        private List<IItemSource<T>> sources;
        private Action<T> notifyItemNotAccepted;
        private BinaryEvent<IEntity, T> itemReceivedEvent;
        private UnaryEvent<IEntity> bufferFullEvent;
        private UnaryEvent<IEntity> bufferEmptyEvent;

        #endregion
        #region prop

        public T this[int id] { get { return itemsByID[id]; } }

        public IEnumerator<T> GetEnumerator()
        {
            return itemPriorityMap.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return itemPriorityMap.Keys.GetEnumerator();
        }

        public int Count { get { return itemPriorityMap.Count; } }

        public int MaxCapacity {
            get { return maxCapacity; }
            set {
                if (maxCapacity < Count) throw new InvalidOperationException(
                    "The maximal capacity of this queue may not be set to a number smaller than the current number of contained items.");
                maxCapacity = value;
            }
        }

        public bool IsFull { get { return itemPriorityMap.Count == maxCapacity; } }

        public bool IsEmpty { get { return itemPriorityMap.Count == 0; } }

        public Action<T> NotifyItemNotAccepted
        {
            get { return notifyItemNotAccepted; }
            set { notifyItemNotAccepted = value; }
        }

        /// <summary>
        /// CAUTION: This is an immediate event. Some event schedulers may not process it.
        /// If you call Put in a handler it will probably result in an infinite loop.
        /// </summary>
        public BinaryEvent<IEntity, T> ItemReceivedEvent { get { return itemReceivedEvent; } }
        
        /// <summary>
        /// CAUTION: This is an immediate event. Some event schedulers may not process it.
        /// If you call Put in a handler it will probably result in an infinite loop.
        /// </summary>
        public UnaryEvent<IEntity> BufferFullEvent { get { return bufferFullEvent; } }

        /// <summary>
        /// CAUTION: This is an immediate event. Some event schedulers may not process it.
        /// CAUTION: This event will fire before(!) the last item is actually passed on to the receiver!
        /// If you call Get in a handler it will probably result in an infinite loop.
        /// </summary>
        public UnaryEvent<IEntity> BufferEmptyEvent { get { return bufferEmptyEvent; } }

        public Func<T> ItemSelector { get { return itemSelector; } set { itemSelector = value; } }

        public Func<T> InitialItemSelector { get { return initialItemSelector; } }

        public bool LogPut { get; set; }
        public bool LogGet { get; set; }
        public bool LogReject { get; set; }

        #endregion
        #region ctor

        // TODO: states!

        public Buffer() : base() { Initialize(null); }

        public Buffer(IModel model,
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
            : base(model, id, name, states, transitions, initialState, initialPosition, manager, currentHolder) 
        {
            Initialize(GetItemSelector(queueRule), maxCapacity);
        }

        public Buffer(IModel model,
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
            : base(model, seedID, id, name, states, transitions, initialState, initialPosition, manager, currentHolder) 
        {
            Initialize(GetItemSelector(queueRule), maxCapacity);
        }

        public Buffer(IModel model,
                     Func<T> itemSelector, 
                     string id = "",
                     string name = "",
                     int maxCapacity = int.MaxValue, 
                     List<string> states = null,
                     List<Tuple<string, string>> transitions = null,
                     string initialState = null,
                     Point initialPosition = null,
                     IResourceManager manager = null,
                     IEntity currentHolder = null)
            : base(model, id, name, states, transitions, initialState, initialPosition, manager, currentHolder)
        {
            Initialize(itemSelector, maxCapacity);
        }

        public Buffer(IModel model,
                     Func<T> itemSelector, 
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
            : base(model, seedID, id, name, states, transitions, initialState, initialPosition, manager, currentHolder)
        {
            Initialize(itemSelector, maxCapacity);
        }

        #endregion
        #region init

        private void Initialize(Func<T> itemSelector, int maxCapacity = int.MaxValue)
        {
            this.maxCapacity = maxCapacity;
            initialMaxCapacity = maxCapacity;
            this.itemSelector = itemSelector;
            initialItemSelector = itemSelector;
            nextID = 0;
            itemsByID = new Dictionary<int, T>();
            entityIDs = new Dictionary<int, string>();
            itemsByEntityID = new Dictionary<string, T>();
            itemsByPriority = new SortedDictionary<Priority, T>();
            itemPriorityMap = new Dictionary<T, Priority>();
            rnd = new Random<double>(this.Model, new UniformDoubleDistribution(0d, 1d), Model.Antithetic, Model.NonStochasticMode);
            sources = new List<IItemSource<T>>();
            notifyItemNotAccepted = Noop;
            bufferFullEvent = new UnaryEvent<IEntity>(EntityName + ".Filled");
            bufferFullEvent.Priority = new Priority(type: PriorityType.LowLevelAfterOthers);
            bufferEmptyEvent = new UnaryEvent<IEntity>(EntityName + ".Emptied");
            bufferEmptyEvent.Priority = new Priority(type: PriorityType.LowLevelAfterOthers);
            itemReceivedEvent = new BinaryEvent<IEntity, T>(EntityName + ".ItemReceived");
            itemReceivedEvent.Priority = new Priority(type: PriorityType.LowLevelAfterOthers);
        }

        #endregion
        #region impl

        #region put

        /// <summary>
        /// If the item is no IPriorityContainer, the default priority will be assumed.
        /// Caution: if the item is a IPriorityContainer the added order of the priority
        /// will be overwritten, so do not enqueue simulation events or other objects, 
        /// which use the added order of their priority!
        /// </summary>
        /// <param name="item"></param>
        public bool Put(T item)
        {
            return Put(item, nextID);
        }

        /// <summary>
        /// Caution: the priorities added order will be overwritten
        /// </summary>
        /// <param name="item"></param>
        /// <param name="priority"></param>
        public bool Put(T item, Priority priority)
        {
            return Put(item, nextID, priority);
        }
        
        /// <summary>
        /// CAUTION: if you manually set the integer id and are using FIFO or LIFO, 
        /// you will break the order if your id's are not incrementing each time!
        /// </summary>
        /// <param name="item"></param>
        /// <param name="id"></param>
        public bool Put(T item, int id)
        {
            string sid;
            if (item is IIdentifiable) sid = (item as IIdentifiable).Identifier;
            else sid = id.ToString();
            return Put(item, id, sid);
        }

        /// <summary>
        /// </summary>
        /// <param name="item"></param>
        /// <param name="id"></param>
        public bool Put(T item, string id)
        {
            return Put(item, nextID, id);
        }

         /// <summary>
        /// CAUTION: if you manually set the integer id and are using FIFO or LIFO, 
        /// you will break the order if your id's are not incrementing each time!
        /// </summary>
        /// <param name="item"></param>
        /// <param name="id"></param>
        public bool Put(T item, int id, string sid)
        {
            Priority priority;
            if (item is IPriorityContainer)
                priority = ((IPriorityContainer)item).Priority;
            else
                priority = new Priority();
            return Put(item, id, sid, priority);
        }

        /// <summary>
        /// CAUTION: if you manually set the integer id and are using FIFO or LIFO, 
        /// you will break the order if your id's are not incrementing each time!
        /// </summary>
        /// <param name="item"></param>
        /// <param name="id"></param>
        /// <param name="priority"></param>
        public bool Put(T item, int id, Priority priority)
        {
            string sid;
            if (item is IIdentifiable) sid = (item as IIdentifiable).Identifier;
            else sid = id.ToString();
            return Put(item, id, sid, priority);
        }

        public bool Put(T item, string id, Priority priority)
        {
            return Put(item, nextID, id, priority);
        }

        /// <summary>
        /// CAUTION: if you manually set the integer id and are using FIFO or LIFO, 
        /// you will break the order if your id's are not incrementing each time!
        /// </summary>
        /// <param name="item"></param>
        /// <param name="id"></param>
        /// <param name="sid"></param>
        /// <param name="priority"></param>
        public bool Put(T item, int id, string sid, Priority priority)
        {
            if (item == null)
            {
                if (LogReject) this.Log<SIM_ERROR>(EntityName + ".Put was called but the item was null.");
                return false;
            }
            if (IsFull) // already full
            {
                LogItemNotAcceptedMsg(item);
                notifyItemNotAccepted.Invoke(item);
                return false;
            }
            priority.AddedOrder = id;
            itemsByID[id] = item;
            entityIDs[id] = sid;
            itemsByEntityID[sid] = item;
            itemsByPriority[priority] = item;
            itemPriorityMap[item] = priority;
            nextID = id + 1;
            LogPutMsg(item);

#if ImmediateEvents
            Model.AddImmediateEvent(itemReceivedEvent.GetInstance(this, item));
            if (IsFull) Model.AddImmediateEvent(bufferFullEvent.GetInstance(this));
#else
            itemReceivedEvent.Raise(this, item);
            if (IsFull) bufferFullEvent.Raise(this);
#endif

            return true;
        }

        #endregion
        #region get

        /// <summary>
        /// retrieve an item from this queue using the current item selector
        /// </summary>
        /// <returns></returns>
        public T Get() 
        {
            if (Count > 0)
            {
                T result = itemSelector.Invoke();
                Priority priority = itemPriorityMap[result];
                int id = priority.AddedOrder;
                itemPriorityMap.Remove(result);
                itemsByID.Remove(id);
                itemsByEntityID.Remove(entityIDs[id]);
                entityIDs.Remove(id);
                itemsByPriority.Remove(priority);
                LogGetMsg(result);

#if ImmediateEvents
                if (IsEmpty) Model.AddImmediateEvent(bufferEmptyEvent.GetInstance(this));
#else
                if (IsEmpty) bufferEmptyEvent.Raise(this);
#endif

                return result;
            }
            else
            {
                this.Log<SIM_WARNING>("Get was called but the queue doesn't contain anything.");
                return default(T);
            }
        }

        public T Get(int id)
        {
            if (Count > 0)
            {
                T result = itemsByID[id];
                Priority priority = itemPriorityMap[result];
                itemPriorityMap.Remove(result);
                itemsByID.Remove(id);
                itemsByEntityID.Remove(entityIDs[id]);
                entityIDs.Remove(id);
                itemsByPriority.Remove(priority);
                LogGetMsg(result);

#if ImmediateEvents
                if (IsEmpty) Model.AddImmediateEvent(bufferEmptyEvent.GetInstance(this));
#else
                if (IsEmpty) bufferEmptyEvent.Raise(this);
#endif

                return result;
            }
            else
            {
                this.Log<SIM_WARNING>("Get was called but the queue doesn't contain anything.");
                return default(T);
            }
        }

        public T Get(string id)
        {
            if (Count > 0)
            {
                T result = itemsByEntityID[id];
                Priority priority = itemPriorityMap[result];
                int iid = priority.AddedOrder;
                itemPriorityMap.Remove(result);
                itemsByID.Remove(iid);
                itemsByEntityID.Remove(id);
                entityIDs.Remove(iid);
                itemsByPriority.Remove(priority);
                LogGetMsg(result);

#if ImmediateEvents
                if (IsEmpty) Model.AddImmediateEvent(bufferEmptyEvent.GetInstance(this));
#else
                if (IsEmpty) bufferEmptyEvent.Raise(this);
#endif

                return result;
            }
            else
            {
                this.Log<SIM_WARNING>("Get was called but the queue doesn't contain anything.");
                return default(T);
            }
        }

        #endregion
        #region preview

        /// <summary>
        /// return an item from this queue using the current 
        /// item selector without removing it from the queue
        /// </summary>
        /// <returns></returns>
        public T Preview()
        {
            if (Count > 0)
            {
                return itemSelector.Invoke();
            }
            else
            {
                this.Log<SIM_WARNING>("Preview was called but the queue doesn't contain anything.");
                return default(T);
            }
        }

        public T Preview(int id)
        {
            if (Count > 0)
            {
                return itemsByID[id];
            }
            else
            {
                this.Log<SIM_WARNING>("Preview was called but the queue doesn't contain anything.");
                return default(T);
            }
        }

        public T Preview(string id)
        {
            if (Count > 0)
            {
                return itemsByEntityID[id];
            }
            else
            {
                this.Log<SIM_WARNING>("Preview was called but the queue doesn't contain anything.");
                return default(T);
            }
        }

        #endregion
        #region connections

        public bool IsConnectionAllowed(IItemSource<T> source)
        {
            return true;
        }

        public bool ConnectTo(IItemSource<T> source)
        {
            this.sources.Add(source);
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
                this.sources.Add(src);
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
                this.sources.Add(src);
                src.EntityCreatedEvent.AddHandler((sender, entity) => Put(entity), new Priority(type: PriorityType.SimWorldBeforeOthers));
                // TODO: save handlers to be able to remove them on reset, if they have been added dynamically!
            }
        }

        #endregion

        #endregion
        #region tool

        private void Noop(T ignore) { }

        private Func<T> GetItemSelector(QueueRule rule) 
        {
            switch (rule)
            {
                case QueueRule.FIFO:
                    return FifoSelector;
                case QueueRule.LIFO:
                    return LifoSelector;
                case QueueRule.Priority:
                    return PrioritySelector;
                case QueueRule.Random:
                    return RandomSelector;
                case QueueRule.Indexed:
                    return null;
                default:
                    this.Log<SIM_ERROR>("An unknown QueueRuleEnumeration value was encountered.");
                    return null;
            }
        }

        #region logging

        private void LogItemNotAcceptedMsg(T item)
        {
            if(LogReject)
                this.Log<SIM_INFO>(item.ToString() + " was rejected. " + EntityName + " is full.");
        }

        private void LogPutMsg(T item) 
        {
            if (LogPut)
            {
                if(maxCapacity != int.MaxValue)
                    this.Log<SIM_INFO>(item.ToString() + " was stored into " + EntityName + ". " + (maxCapacity - Count).ToString() + " storage spaces left.");
                else
                    this.Log<SIM_INFO>(item.ToString() + " was stored into " + EntityName + ". " + Count.ToString() + " items stored.");
            }
        }

        private void LogGetMsg(T item) 
        {
            if (LogGet)
                this.Log<SIM_INFO>(item.ToString() + " was retrieved from " + EntityName + ". " + Count.ToString() + " items remaining.");
        }

        #endregion
        #region itemselectors

        private T FifoSelector() 
        {
            return itemsByID[(from item in itemsByID select item.Key).Min()];
        }

        private T LifoSelector()
        {
            return itemsByID[(from item in itemsByID select item.Key).Max()];
        }

        private T PrioritySelector()
        {
            return itemsByPriority.First().Value;
        }

        private T RandomSelector() 
        {
            int n = (int)(rnd.Next() * (double)Count);
            return itemsByID.Values.ElementAt(n);
        }

        #endregion

        #endregion
    }
}