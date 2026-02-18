using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using SimOpt.Basics.Interfaces;

namespace SimOpt.Simulation.Engine
{
    /// <summary>
    /// A concrete occurence of an IEvent on the time axis.
    /// 
    /// Event instances have their own priority so that it is possible to enforce ordering not only of different 
    /// events on the same point in time but also the ordering of different instances of the same event. Event
    /// instances also have their own logging flag and a "Time" property which will be set by the event scheduler.
    /// Setting the time of an event instance manually can lead to inconsistencies and errors (setting the time 
    /// for one of the provided event instance implementation will not update the scheduler) and should be avoided. 
    /// Finally there is a "Raise" method which will call the "Raise" method of the event to which the instance belongs.
    /// </summary>
    /// <remarks>beta</remarks>
    public interface IEventInstance : ISerializableSimulation
    {
        /// <summary>
        /// semantic identifier for this instance. not unique.
        /// it is recommended to use OwnerClassName.EventName
        /// </summary>
        String Name { get; set; }

        /// <summary>
        /// the priority for this instance
        /// </summary>
        Priority Priority { get; set; }

        /// <summary>
        /// the point in time at which this event instance is scheduled
        /// CAUTION: setting this manually will not change the time at
        /// which the event will be raised but may result in fatal
        /// inconsistencies!
        /// </summary>
        double Time { get; set; }

        /// <summary>
        /// get the number of handlers assigned to this event instance
        /// </summary>
        int HandlerCount { get; }

        /// <summary>
        /// Get and set log parameter
        /// if set to false, this event will never appear in an event log
        /// </summary>
        bool Log { get; set; }

        /// <summary>
        /// method to raise the event. this will execute all handlers. 
        /// CAUTION: when called manually, this can lead to inconsistencies in
        /// the execution order concerning the events priorities. it is recommended 
        /// to design your simulation in a way which doesn't require instantanous events.
        /// </summary>
        void Raise();
    }
}
