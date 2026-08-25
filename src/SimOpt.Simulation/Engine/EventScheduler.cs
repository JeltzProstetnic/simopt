using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Basics.Exceptions;
using SimOpt.Simulation;
using System.Runtime.Serialization;

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
    /// One of the central properties (getter in Java) is the EventList, which is a sorted dictionary structure containing a 
    /// sorted dictionary structure. The outer key is the timestamp. The inner dictionary contains all events which are to occure
    /// at the point in time as specified by the key. The inner key is the priority of the events, which are the value members of the 
    /// inner dictionary.
    /// 
    /// A logging flag can be used to generally turn event logging off or to turn it on for events which have their logging flag set to
    /// true. Furthermore, as soon as events are available, the scheduler keeps track of the time and instance of the next scheduled 
    /// event according to simulation time and to the most recently processed event instance if events were already processed. Internally 
    /// the scheduler also holds a reference to the model as a private class variable. The event scheduler cannot be instantiated without
    /// a model because it is dependent on the model's simulation time. In distributed scenarios a new event scheduler has to be implemented 
    /// which synchronizes the different model parts. In this case the model reference will have to be the reference to the common model 
    /// which manages the distributed sub-models.
    /// 
    /// The event scheduler allows to add events to the schedule using the Add or the AddImmediate method. Add does allow to add events 
    /// to the current point in simulation time but this is not according to intended use and the behaviour is undefined. Adding events
    /// to a past point in time may even result in an infinite loop and subsequently a deadlock of the engine. Adding events through the
    /// model class prevents such errors. The remove method allows removal of event instances from the timeline. Events that have already 
    /// been processed are removed automatically.
    /// 
    /// The above adding and removing methods are normally used exclusively by the model. They have been kept public nevertheless for allowing
    /// remote models to add events in case of distributed models or simulations.
    /// 
    /// The core of the event scheduler is the "ProcessNextPointInTime" method. It is also triggered by the model normally and retrieves 
    /// the events for the current simulation time as a first step. These are then raised in order of their priority. Handlers may add 
    /// immediate events during this process which are raised subsequently. The adding of immediate events during immediate event processing
    /// is not supported. Ideally immediate events should be avoided in general but this is only the author's opinion and still a matter of
    /// intense discussion amongst experts.
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

        private IModel model;
        
        // EventList := SortedDictionary<PointInTime, EventsAtThisPointInTime> 
        //  where EventsAtThisPointInTime := SortedDictionary<Priority, EventInstance>
        private SortedDictionary<double, SortedDictionary<Priority, IEventInstance>> eventList;
        
#if ImmediateEvents
        private SortedDictionary<Priority, IEventInstance> immediateList;
        private bool processingImmediate;
#endif
        private SortedDictionary<Priority, IEventInstance> tmpEventsAtTheTime;
        private SortedDictionary<Priority, IEventInstance> tmpEventsAtTheTimeAdd;
        private SortedDictionary<Priority, IEventInstance> tmpEventsAtTheTimeRemove;
        private List<IEventInstance> tmpHandledEvents;
        private Dictionary<IEventInstance, Priority> eventKeyMap;
        private KeyValuePair<double, SortedDictionary<Priority, IEventInstance>> tmpPointInTime;
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
        /// </summary>
        public int EventfulMomentsCount { get { return eventList.Count; } }
        
        /// <summary>
        /// enable or disable logging
        /// </summary>
        public bool Logging
        {
            get { return logging; }
            set { logging = value; }
        }

        /// <summary>
        /// the time of the next scheduled event(s)
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
                // SIM-58: test the list, not the sentinel — a stale or differently-signalled
                // "empty" used to reach the indexer and throw KeyNotFoundException.
                if (eventList.Count == 0) return null;
                if (!eventList.TryGetValue(timeOfNextScheduledEvent, out var eventsAtTheTime)) return null;
                if (eventsAtTheTime.Count == 0) return null;
                return eventsAtTheTime.First().Value;
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
            eventList = new SortedDictionary<double, SortedDictionary<Priority, IEventInstance>>();
#if ImmediateEvents
            immediateList = new SortedDictionary<Priority, IEventInstance>();
#endif
            tmpHandledEvents = new List<IEventInstance>();
            eventKeyMap = new Dictionary<IEventInstance, Priority>();
        }

        #endregion
        #region impl

        #region add / remove

        /// <summary>
        /// Adds an event to the internal event list at the given time.
        /// CAUTION: You must not provide events for a current or past
        /// simulation time. This will result in undefined behaviour.
        /// </summary>
        /// <param name="time">point in time of occurence (absolute)</param>
        /// <param name="evnt">the event to be scheduled</param>
        public void Add(double time, IEventInstance evnt)
        {
            evnt.Time = time;

            // create entry if first event at that time
            if (!eventList.TryGetValue(time, out tmpEventsAtTheTimeAdd))
            {
                tmpEventsAtTheTimeAdd = new SortedDictionary<Priority, IEventInstance>();
                eventList.Add(time, tmpEventsAtTheTimeAdd);
            }

            // clone priority as key to prevent shared-reference corruption (SIM-05)
            var storedKey = (Priority)evnt.Priority.Clone();
            storedKey.AddedOrder = orderCounter++;
            evnt.Priority.AddedOrder = storedKey.AddedOrder;
            tmpEventsAtTheTimeAdd[storedKey] = evnt;
            eventKeyMap[evnt] = storedKey;

            // (re)calculate next point in time with events
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
            // use reverse index for identity-based removal (SIM-05)
            if (!eventKeyMap.TryGetValue(evnt, out var storedKey)) return;
            if (!eventList.TryGetValue(evnt.Time, out tmpEventsAtTheTimeRemove)) return;
            tmpEventsAtTheTimeRemove.Remove(storedKey);
            eventKeyMap.Remove(evnt);
            if (tmpEventsAtTheTimeRemove.Count == 0) eventList.Remove(evnt.Time);

            // calculate next point in time with events
            // SIM-58: removing the last event used to leave the previous finite time in place, so
            // external consumers (visualization, logging) asked for events at a time that no
            // longer had any.
            if (eventList.Count > 0)
                timeOfNextScheduledEvent = eventList.First<KeyValuePair<double, SortedDictionary<Priority, IEventInstance>>>().Key;
            else
                timeOfNextScheduledEvent = NoNextEvent;
        }

        #endregion
        #region process

        /// <summary>
        /// process the events at the next available point in time
        /// </summary>
        public void ProcessNextPointInTime()
        {
            tmpPointInTime = eventList.First<KeyValuePair<double, SortedDictionary<Priority, IEventInstance>>>();
            now = tmpPointInTime.Key;
            tmpEventsAtTheTime = tmpPointInTime.Value;
            processing = true;
            foreach (IEventInstance evnt in tmpEventsAtTheTime.Values)
            {
                if (logging && evnt.Log) this.Log<EVENT>("Priority = " + evnt.Priority.ToString() + " Event = " + evnt.Name, model);
                CurrentEvent = evnt;
                evnt.Raise();
                CurrentEvent = null;
                LastProcessedEvent = evnt;
                eventCounter += 1;
                handlerCounter += evnt.HandlerCount;
                tmpHandledEvents.Add(evnt);
                if (model.IsInterruptRequested) break;
            }
            processing = false;
            if (model.IsInterruptRequested)
            {
                // remove only handled events from inner dict and reverse index
                foreach (IEventInstance ei in tmpHandledEvents)
                {
                    if (eventKeyMap.TryGetValue(ei, out var key))
                        tmpEventsAtTheTime.Remove(key);
                    eventKeyMap.Remove(ei);
                }
                if (tmpEventsAtTheTime.Count == 0) eventList.Remove(now);
            }
            else
            {
                // all events at this time were processed — clean up reverse index
                foreach (var evnt in tmpEventsAtTheTime.Values) eventKeyMap.Remove(evnt);
                eventList.Remove(now);
            }

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
            if (eventList.Any())
                timeOfNextScheduledEvent = eventList.First<KeyValuePair<double, SortedDictionary<Priority, IEventInstance>>>().Key;
            else
                timeOfNextScheduledEvent = NoNextEvent;

            tmpHandledEvents.Clear();
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
            eventList.Clear();
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

            state.AddValue("eventList", eventList);
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

            state.AddValue("eventList", eventList);
        }

        #endregion

        #endregion
    }
}