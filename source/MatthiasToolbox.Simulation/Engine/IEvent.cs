using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Simulation.Engine
{
    /// <summary>
    /// Interface for simulation events.
    /// 
    /// The interface proscribes a name. This does not necessarily have to be unique but it is recommended to only use unique names 
    /// of the form "EntityClassName.EventName" (e. g. "Machine1.StateChanged"). Furthermore it proscribes a <see cref="Priority"/>
    /// and a boolean logging flag "Log". The logging flag allows the modeller to set logging on or off not only on model level 
    /// but also more fine grained for events. This is helpful in debugging for it is common to have certain frequently recurring 
    /// events which would be distracting during a search for minor irregularities in other, less frequent events. 
    /// 
    /// Furthermore a getter for the number of currently attached handlers "HandlerCount" is included in the interface. This is mainly 
    /// used for runtime statistics which are often helpful as a first indication for reproducibility problems.
    /// 
    /// Finally the interface proscribes methods to attach and detach handlers. "void AddHandler(THandler handler)" and 
    /// "void AddHandler(THandler handler, Priority priority)" are used to attach handlers. If no priority is given it must be set to
    /// the according default priority. "bool RemoveHandler(THandler handler)" is used to detach handlers by reference. Implementors 
    /// should return false if the given handler was not attached to the event and true if the handler was successfully detached.
    /// 
    /// Due to the fact that no unique identifiers are proscribed the implementor has to keep references to handlers which he wishes 
    /// to detach and optionally re-attach dynamically. It is of course possible to provide implementations including a unique ID and 
    /// some kind of lookup to retrieve events by their ID. But it should be noted that all event related issues are extremely performance 
    /// critical. Dynamically detaching event handlers is not a very common scenario for discrete simulation models and the effort of 
    /// keeping references to those detachable handlers is minimal. Therefore cost and benefit of implementing indexing and lookup for 
    /// events should be carefully weighed against each other.
    /// </summary>
    /// <remarks>final</remarks>
    public interface IEvent<THandler> : IComparable<IEvent<THandler>>
        where THandler : class
    {
        /// <summary>
        /// semantic identifier (not unique!)
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// the default priority for all instances of the event
        /// </summary>
        Priority Priority { get; set; }

        /// <summary>
        /// if set to false, this event will never appear in an event log
        /// </summary>
        bool Log { get; set; }

        /// <summary>
        /// get the number of handlers assigned to this event instance
        /// </summary>
        int HandlerCount { get; }

        /// <summary>
        /// caution: if two handlers are added with the same priority, they will be called in the order they were added
        /// </summary>
        /// <param name="priority"></param>
        /// <param name="handler"></param>
        void AddHandler(THandler handler, Priority priority);

        /// <summary>
        /// default priority will be used. all events added by this method will be called in the order they were added
        /// </summary>
        /// <param name="handler"></param>
        void AddHandler(THandler handler);

        /// <summary>
        /// remove a handler
        /// </summary>
        /// <param name="handler"></param>
        /// <returns>success flag</returns>
        bool RemoveHandler(THandler handler);

        /// <summary>
        /// Removes all handlers from this event.
        /// </summary>
        void ClearHandlers();
    }
}
