using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Exceptions;
using SimOpt.Simulation.Interfaces;
using System.Runtime.Serialization;
using System.Reflection;
using SimOpt.Basics.Utilities;
using SimOpt.Basics.Interfaces;
using SimOpt.Simulation.Tools;

namespace SimOpt.Simulation.Events
{
    //TODO grubi add ISerilaizable
    [Serializable]
    public abstract class AbstractEvent<THandler> : IEvent<THandler>, IPriorityContainer//, ISerializableGrubi
        where THandler : class
    {
        #region cvar

        private String name;
        private Priority priority;
        private int orderCounter = 0;

        private SortedDictionary<Priority, THandler> handlers;
        private Dictionary<THandler, Priority> handlerLookup;
        private List<HandlerInfo> deserializedHandlers;

        private List<THandler> toDetach = new List<THandler>();

        #endregion
        #region prop

        /// <summary>
        /// Get and set name
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        /// <summary>
        /// Get and set priority
        /// </summary>
        public Priority Priority
        {
            get { return priority; }
            set { priority = value; }
        }

        /// <summary>
        /// Get and set list of handlers
        /// </summary>
        public SortedDictionary<Priority, THandler> Handlers
        {
            get { return handlers; }
            internal set { handlers = value; }
        }

        /// <summary>
        /// number of attached handlers
        /// </summary>
        public abstract int HandlerCount { get; }

        /// <summary>
        /// Get and set log parameter
        /// </summary>
        public bool Log { get; set; }

        /// <summary>
        /// Check this in your raise method and enqueue 
        /// </summary>
        protected bool DetachHandlerRequested { get; set; }

        #endregion
        #region ctor

        protected AbstractEvent(SerializationInfo info, StreamingContext context)
        {
            deserializedHandlers = (List<HandlerInfo>)info.GetValue("handlers", typeof(List<HandlerInfo>));
            Name = info.GetString("name");
            Priority = (Priority)info.GetValue("Priority", typeof(Priority));
            orderCounter = info.GetInt32("orderCounter");
            Log = info.GetBoolean("Log");
        }

        #region public

        /// <summary>
        /// creates an event with default priority
        /// </summary>
        public AbstractEvent() : this(new Priority()) { }

        /// <summary>
        /// creates an event with the given priority
        /// </summary>
        /// <param name="priority"></param>
        public AbstractEvent(Priority priority)
        {
            this.priority = priority;
            handlers = new SortedDictionary<Priority, THandler>();
            handlerLookup = new Dictionary<THandler, Priority>();
        }

        /// <summary>
        /// creates an event with default priority
        /// </summary>
        /// <param name="name"></param>
        public AbstractEvent(String name) : this(name, new Priority()) { }

        /// <summary>
        /// default constructor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="priority"></param>
        public AbstractEvent(String name, Priority priority)
        {
            this.priority = priority;
            handlers = new SortedDictionary<Priority, THandler>();
            handlerLookup = new Dictionary<THandler, Priority>();
            this.name = name;
        }

        #endregion

        #endregion
        #region impl

        /// <summary>
        /// throws an exception if the same handler is added twice
        /// </summary>
        /// <param name="handler"></param>
        public virtual void AddHandler(THandler handler)
        {
            AddHandler(handler, new Priority());
        }

        /// <summary>
        /// throws an exception if the same handler is added twice
        /// </summary>
        /// <param name="handler"></param>
        public virtual void AddHandler(THandler handler, Priority priority)
        {
            if (handlerLookup.ContainsKey(handler))
                throw new HandlerAlreadyAddedException(handler, Name);
            priority.AddedOrder = orderCounter++;
            handlerLookup[handler] = priority;
            handlers[priority] = handler;
        }

        /// <summary>
        /// remove the given handler
        /// </summary>
        /// <param name="handler"></param>
        /// <returns>false if the handler was not attached to this event</returns>
        public virtual bool RemoveHandler(THandler handler)
        {
            if (handlerLookup.ContainsKey(handler))
            {
                handlers.Remove(handlerLookup[handler]);
                handlerLookup.Remove(handler);
                return true;
            }
            return false;
        }

        /// <summary>
        /// This can be used to remove the currently executing handler after it is finished.
        /// </summary>
        public void DetachCurrentHandler() { DetachHandlerRequested = true; }

        /// <summary>
        /// Use this to indicate the handler to be removed.
        /// </summary>
        /// <param name="handler"></param>
        protected void ScheduleForRemoval(THandler handler)
        {
            toDetach.Add(handler); 
            DetachHandlerRequested = false;
        }

        /// <summary>
        /// Call this after the handler loop to detach handlers which have been scheduled for removal.
        /// </summary>
        protected void ExecuteRemovalSchedule()
        {
            foreach (THandler h in toDetach)
            {
                RemoveHandler(h);
            }
            toDetach.Clear();
        }

        /// <summary>
        /// Removes all handlers from this event.
        /// </summary>
        public virtual void ClearHandlers()
        {
            handlers.Clear();
            handlerLookup.Clear();
        }

        #region IComparable

        /// <summary>
        /// default comparer
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(IEvent<THandler> other)
        {
            return this.priority.CompareTo(other.Priority);
        }

        #endregion

        #endregion

        internal void RestoreHandlers(Model model)
        {
            foreach (HandlerInfo hi in deserializedHandlers)
            {
                IIdentifiable target = model.GetItem(hi.ItemID);
                MethodInfo methodInfo = target.GetType().GetMethod(hi.MethodName);
                THandler handler = (THandler)((object)DelegateFactory.CreateOpenDelegate(methodInfo));
                handlers[hi.Priority] = handler;
                handlerLookup[handler] = hi.Priority;
                if (hi.IsToDetatch) toDetach.Add(handler);
            }
        }

        #region ISerializableGrubi Member

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("name", name);
            info.AddValue("Priority", priority);
            info.AddValue("orderCounter", orderCounter);
            info.AddValue("Log", Log);

            List<HandlerInfo> serializableHandlers = new List<HandlerInfo>();

            foreach(KeyValuePair<Priority, THandler> kvp in handlers) 
            {
                Delegate methodDelegate = (Delegate)((object)kvp.Value);
                MethodInfo methodInfo = methodDelegate.Method;
                IEntity instance = (IEntity)methodDelegate.Target;
                // Type sourceClassType = methodInfo.DeclaringType;
                HandlerInfo hi = new HandlerInfo(kvp.Key, methodInfo.Name, instance.Identifier);
                if (toDetach.Contains(kvp.Value)) hi.IsToDetatch = true;
                serializableHandlers.Add(hi);
            }

            info.AddValue("handlers", serializableHandlers);
        }

        #endregion
    }
}