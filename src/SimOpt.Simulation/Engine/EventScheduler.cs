using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Basics.Exceptions;
using SimOpt.Simulation;
using System.Runtime.Serialization;

using SimOpt.Simulation.Enum;
using SimOpt.Simulation.Tools;
using SimOpt.Basics.Datastructures.Collections;

namespace SimOpt.Simulation.Engine
{
    /// <summary>
    /// A class to process events ordered by time and priority.
    ///
    /// Core of the simulation engine is a mechanism to manage events on a timeline. This mechanism is manifested in this class.
    /// This event scheduler is only an example. Other event schedulers may be implemented without restrictions for different models
    /// or event scheduling concepts (e. g. for distributed or hybrid scenarios). The event scheduler not only holds references to
    /// the events but is also responsible for the invocation of the logic which may be associated with these events.
    ///
    /// The schedule is a binary min-heap of entries ordered by time, then by the event instance's priority (type, number, and
    /// the order in which it was added), so events at one point in time are raised in priority order and ties in the order they
    /// were added (SIM-129; it replaced a sorted dictionary per point in time inside a sorted dictionary of times, with the same
    /// order). An entry copies the priority's values when the event is added, so a priority object shared between events or
    /// changed later cannot disturb the schedule (SIM-05).
    ///
    /// A logging flag can be used to generally turn event logging off or to turn it on for events which have their logging flag set to
    /// true. Furthermore, as soon as events are available, the scheduler keeps track of the time and instance of the next scheduled
    /// event according to simulation time and to the most recently processed event instance if events were already processed. Internally
    /// the scheduler also holds a reference to the model as a private class variable. The event scheduler cannot be instantiated without
    /// a model because it is dependent on the model's simulation time. In distributed scenarios a new event scheduler has to be implemented
    /// which synchronizes the different model parts. In this case the model reference will have to be the reference to the common model
    /// which manages the distributed sub-models.
    ///
    /// The event scheduler allows to add events to the schedule using the Add or the AddImmediate method. An event added at the
    /// current point in simulation time runs in that instant, among the pending events by priority (SIM-128). Adding events
    /// to a past point in time may even result in an infinite loop and subsequently a deadlock of the engine. Adding events through the
    /// model class prevents such errors. The remove method allows removal of event instances from the timeline. Events that have already
    /// been processed are removed automatically.
    ///
    /// The above adding and removing methods are normally used exclusively by the model. They have been kept public nevertheless for allowing
    /// remote models to add events in case of distributed models or simulations.
    ///
    /// The core of the event scheduler is the "ProcessNextPointInTime" method. It is also triggered by the model normally and raises
    /// the events of the earliest scheduled point in time in order of their priority. Handlers may add events at that point in time,
    /// which are raised in the same instant (SIM-128).
    ///
    /// Finally there is a reset method to be able to re-use the same event scheduler for multiple simulation runs.
    /// </summary>
    /// <remarks>rc</remarks>
    [Serializable]
    public class EventScheduler
    {
        #region over

        /// <summary>
        /// Returns <code>model.EntityName + ".EventScheduler"</code> if a model is available.
        /// </summary>
        /// <returns><code>model.EntityName + ".EventScheduler"</code> if a model is available.</returns>
        public override string ToString()
        {
            if (model != null) return model.Name + ".EventScheduler";
            else return "EventScheduler, currently unbound.";
        }

        #endregion
        #region cvar

        /// <summary>One scheduled occurrence of an event instance.</summary>
        [Serializable]
        private sealed class Entry
        {
            public double Time;
            public int Type;            // (int)PriorityType
            public double Number;       // PriorityNumber
            public int Order;           // AddedOrder, unique, so the order is total
            public IEventInstance Event;
            public int Index = -1;      // position in the heap, -1 while not scheduled
        }

        private IModel model;

        // SIM-129: a binary min-heap. Entries leaving it go back to the pool, so a run that has warmed
        // up allocates nothing here; the sorted dictionaries it replaced allocated about 415 bytes per event.
        private Entry[] heap = new Entry[16];
        private int count;
        private readonly Stack<Entry> pool = new Stack<Entry>();
        // the entry of each event's latest schedule, for identity-based removal (SIM-05)
        private readonly Dictionary<IEventInstance, Entry> eventKeyMap = new Dictionary<IEventInstance, Entry>();

#if ImmediateEvents
        private SortedDictionary<Priority, IEventInstance> immediateList = new SortedDictionary<Priority, IEventInstance>();
        private bool processingImmediate;
#endif
        // SIM-58 (DES-3): the class used two different "no next event" sentinels — double.MaxValue
        // here and at the NextScheduledEvent guard, double.PositiveInfinity after processing.
        // NextScheduledEvent only tested for MaxValue, so once the other sentinel was in force it
        // indexed the event list at positive infinity and threw. One sentinel now, and emptiness
        // is tested directly rather than by comparing against a magic value.
        private double timeOfNextScheduledEvent = NoNextEvent;

        /// <summary>Sentinel time meaning "the schedule is empty".</summary>
        private const double NoNextEvent = double.PositiveInfinity;
        private bool logging = true;
        private int eventCounter = 0;
        private int handlerCounter = 0;
        private int orderCounter = 0;
        private double now;
        private bool processing;

        #endregion
        #region prop

        /// <summary>
        /// Returns the number of points in time in which events are currently scheduled.
        /// It is counted when asked; use <see cref="HasEvents"/> to ask whether anything is scheduled.
        /// </summary>
        public int EventfulMomentsCount
        {
            get
            {
                if (count == 0) return 0;
                var times = new HashSet<double>();
                for (int i = 0; i < count; i++) times.Add(heap[i].Time);
                return times.Count;
            }
        }

        /// <summary>
        /// True while at least one event is scheduled.
        /// </summary>
        public bool HasEvents { get { return count > 0; } }

        /// <summary>
        /// enable or disable logging
        /// </summary>
        public bool Logging
        {
            get { return logging; }
            set { logging = value; }
        }

        /// <summary>
        /// the time of the next scheduled event(s); while a point in time is being processed, that point in time
        /// </summary>
        public double TimeOfNextScheduledEvent
        {
            get { return timeOfNextScheduledEvent; }
        }

        /// <summary>
        /// contains number of processed events since first start / last reset
        /// </summary>
        public int EventCounter
        {
            get
            {
                return eventCounter;
            }
        }

        /// <summary>
        /// contains number of processed handlers since first start / last reset
        /// </summary>
        public int HandlerCounter
        {
            get { return handlerCounter; }
        }

        /// <summary>
        /// The most recently processed event.
        /// </summary>
        public IEventInstance LastProcessedEvent { get; private set; }

        /// <summary>
        /// The event which is currently being processed or null.
        /// </summary>
        public IEventInstance CurrentEvent { get; private set; }

        /// <summary>
        /// the next event on the schedule
        /// </summary>
        public IEventInstance NextScheduledEvent
        {
            get
            {
                // SIM-58: test the schedule, not the sentinel — a stale or differently-signalled
                // "empty" used to reach the indexer and throw KeyNotFoundException.
                if (count == 0) return null;
                if (heap[0].Time != timeOfNextScheduledEvent) return null;
                return heap[0].Event;
            }
        }

        /// <summary>
        /// Returns <see langword="true"/> while the scheduler is actively processing events at a point in time.
        /// </summary>
        public bool IsProcessingEvents { get { return processing; } }

#if ImmediateEvents
        public bool IsProcessingImmediateEvents { get { return processingImmediate; } }
#endif

        #endregion
        #region ctor

        /// <summary>
        /// default constructor
        /// </summary>
        public EventScheduler(IModel model)
        {
            this.model = model;
        }

        #endregion
        #region impl

        #region add / remove

        /// <summary>
        /// Adds an event to the internal event list at the given time.
        /// CAUTION: You must not provide events for a past simulation time.
        /// An event at the current time joins the pending events of this instant (SIM-128).
        /// </summary>
        /// <param name="time">point in time of occurence (absolute)</param>
        /// <param name="evnt">the event to be scheduled</param>
        public void Add(double time, IEventInstance evnt)
        {
            evnt.Time = time;

            // the entry copies the priority's values (SIM-05) and gets the next added order
            Priority priority = evnt.Priority;
            Entry entry = pool.Count > 0 ? pool.Pop() : new Entry();
            entry.Time = time;
            entry.Type = (int)priority.PriorityType;
            entry.Number = priority.PriorityNumber;
            entry.Order = orderCounter++;
            entry.Event = evnt;
            priority.AddedOrder = entry.Order;
            eventKeyMap[evnt] = entry;
            Push(entry);

            // (re)calculate next point in time with events; while an instant is processed it stays that instant
            timeOfNextScheduledEvent = Math.Min(time, timeOfNextScheduledEvent);
        }

#if ImmediateEvents
        /// <summary>
        /// Add an event to immediately "after the current point in time".
        /// The time index will be the same as for all currently processing
        /// events, but the execution will be afterwards.
        /// </summary>
        /// <param name="evnt"></param>
        public void AddImmediate(IEventInstance evnt)
        {
            if (!processing)
            {
                if (processingImmediate)
                    throw new InfiniteRecursionException("Congratulations, you have somehow managed to cause the scheduling of an immediate event during the processing of immediate events...!!! Good luck finding that error! Hint: this may have happened due to a state transition.");
                else
                    throw new ApplicationException("Attempt to schedule an immediate event outside of event handling code. Hint: the model must be running to be able to schedule immediate events.");
            }
            evnt.Time = now;
            evnt.Priority.AddedOrder = orderCounter++;
            immediateList[evnt.Priority] = evnt;
        }
#endif

        /// <summary>
        /// removes the given event from the internal event list
        /// will do nothing if the event instance doesn't exist or was already processed
        /// </summary>
        /// <param name="evnt"></param>
        public void Remove(IEventInstance evnt)
        {
            // use reverse index for identity-based removal (SIM-05); an instance scheduled twice
            // loses its latest schedule
            if (!eventKeyMap.TryGetValue(evnt, out Entry entry)) return;
            eventKeyMap.Remove(evnt);
            if (entry.Index >= 0)
            {
                RemoveAt(entry.Index);
                Release(entry);
            }

            // calculate next point in time with events
            // SIM-58: removing the last event used to leave the previous finite time in place, so
            // external consumers (visualization, logging) asked for events at a time that no
            // longer had any. While an instant is processed it stays that instant.
            if (!processing) timeOfNextScheduledEvent = count > 0 ? heap[0].Time : NoNextEvent;
        }

        #endregion
        #region process

        /// <summary>
        /// process the events at the next available point in time
        /// </summary>
        public void ProcessNextPointInTime()
        {
            if (count == 0) throw new InvalidOperationException("No events are scheduled.");
            now = heap[0].Time;
            processing = true;
            // SIM-128: an event a handler schedules at the current time enters the heap among this
            // instant's pending events, in priority order (ties in the order added), and never runs
            // before the event that scheduled it, because that one has already left the heap.
            while (count > 0 && heap[0].Time == now)
            {
                Entry entry = heap[0];
                RemoveAt(0);
                IEventInstance evnt = entry.Event;
                if (logging && evnt.Log) this.Log<EVENT>("Priority = " + evnt.Priority.ToString() + " Event = " + evnt.Name, model);
                CurrentEvent = evnt;
                evnt.Raise();
                CurrentEvent = null;
                LastProcessedEvent = evnt;
                eventCounter += 1;
                handlerCounter += evnt.HandlerCount;
                eventKeyMap.Remove(evnt);
                Release(entry);
                // on an interrupt the events not yet handled stay scheduled at this time
                if (model.IsInterruptRequested) break;
            }
            processing = false;

#if ImmediateEvents
            processingImmediate = true;
            foreach (IEventInstance evnt in immediateList.Values)
            {
                if (logging && evnt.Log) this.Log<EVENT>("Priority = " + evnt.Priority.ToString() + " Event = " + evnt.Name + " (IMMEDIATE EVENT)", model);
                CurrentEvent = evnt;
                evnt.Raise();
                CurrentEvent = null;
                LastProcessedEvent = evnt;
                eventCounter += 1;
                handlerCounter += evnt.HandlerCount;
            }
            processingImmediate = false;
            immediateList.Clear();
#endif

            // calculate next point in time with events
            timeOfNextScheduledEvent = count > 0 ? heap[0].Time : NoNextEvent;
        }

        #endregion
        #region heap

        // time first, then priority type, priority number and added order, as Priority.CompareTo orders them
        private static bool Before(Entry a, Entry b)
        {
            int c = a.Time.CompareTo(b.Time);
            if (c != 0) return c < 0;
            if (a.Type != b.Type) return a.Type < b.Type;
            c = a.Number.CompareTo(b.Number);
            if (c != 0) return c < 0;
            return a.Order < b.Order;
        }

        private void Push(Entry entry)
        {
            if (count == heap.Length) Array.Resize(ref heap, heap.Length * 2);
            heap[count] = entry;
            SiftUp(count++);
        }

        private void RemoveAt(int i)
        {
            heap[i].Index = -1;
            int last = --count;
            Entry moved = heap[last];
            heap[last] = null;
            if (i == last) return;
            heap[i] = moved;
            if (i > 0 && Before(moved, heap[(i - 1) >> 1])) SiftUp(i);
            else SiftDown(i);
        }

        private void SiftUp(int i)
        {
            Entry entry = heap[i];
            while (i > 0)
            {
                int parent = (i - 1) >> 1;
                Entry p = heap[parent];
                if (!Before(entry, p)) break;
                heap[i] = p;
                p.Index = i;
                i = parent;
            }
            heap[i] = entry;
            entry.Index = i;
        }

        private void SiftDown(int i)
        {
            Entry entry = heap[i];
            int half = count >> 1;
            while (i < half)
            {
                int child = 2 * i + 1;
                Entry c = heap[child];
                int right = child + 1;
                if (right < count && Before(heap[right], c))
                {
                    child = right;
                    c = heap[right];
                }
                if (!Before(c, entry)) break;
                heap[i] = c;
                c.Index = i;
                i = child;
            }
            heap[i] = entry;
            entry.Index = i;
        }

        private void Release(Entry entry)
        {
            entry.Event = null;
            entry.Index = -1;
            pool.Push(entry);
        }

        #endregion
        #region reset

        /// <summary>
        /// reset event counter and handler counter statistics
        /// </summary>
        public void ResetEventCounter()
        {
            eventCounter = 0;
            handlerCounter = 0;
        }

        /// <summary>
        /// reset the whole thing
        /// </summary>
        internal void Reset()
        {
            processing = false;
            orderCounter = 0;
            for (int i = 0; i < count; i++)
            {
                Release(heap[i]);
                heap[i] = null;
            }
            count = 0;
            eventKeyMap.Clear();
            // SIM-58: Reset cleared the calendar but left the cached next-event time behind, so a
            // freshly reset model still advertised the previous run's schedule.
            timeOfNextScheduledEvent = NoNextEvent;
#if ImmediateEvents
            processingImmediate = false;
            immediateList.Clear();
#endif
            ResetEventCounter();
        }

        #endregion
        #region persistence

        internal void Load(ModelState state)
        {
#if ImmediateEvents

#endif
            timeOfNextScheduledEvent = state.TimeOfNextScheduledEvent;
            logging = state.EventSchedulerLogging;
            eventCounter = state.EventCounter;
            handlerCounter = state.HandlerCounter;
            orderCounter = state.OrderCounter;
            now = state.EventSchedulerNow;
            processing = state.EventSchedulerProcessing;

            state.AddValue("eventList", EventListSnapshot());
        }

        internal void Save(ModelState state)
        {
#if ImmediateEvents
            info.AddValue("immediateList", immediateList);
            info.AddValue("processingImmediate", processingImmediate);
#endif
            state.TimeOfNextScheduledEvent = timeOfNextScheduledEvent;
            state.EventSchedulerLogging = logging;
            state.EventCounter = eventCounter;
            state.HandlerCounter = handlerCounter;
            state.OrderCounter = orderCounter;
            state.EventSchedulerNow = now;
            state.EventSchedulerProcessing = processing;

            state.AddValue("eventList", EventListSnapshot());
        }

        /// <summary>
        /// The schedule in the shape it was kept and persisted in before SIM-129: point in time to priority to event.
        /// </summary>
        private SortedDictionary<double, SortedDictionary<Priority, IEventInstance>> EventListSnapshot()
        {
            var list = new SortedDictionary<double, SortedDictionary<Priority, IEventInstance>>();
            for (int i = 0; i < count; i++)
            {
                Entry e = heap[i];
                if (!list.TryGetValue(e.Time, out var atTime))
                {
                    atTime = new SortedDictionary<Priority, IEventInstance>();
                    list.Add(e.Time, atTime);
                }
                atTime.Add(new Priority(e.Number, (PriorityType)e.Type) { AddedOrder = e.Order }, e.Event);
            }
            return list;
        }

        #endregion

        #endregion
    }
}