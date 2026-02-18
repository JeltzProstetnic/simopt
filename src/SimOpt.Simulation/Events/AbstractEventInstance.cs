using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Interfaces;
using System.Runtime.Serialization;
using SimOpt.Basics.Interfaces;

namespace SimOpt.Simulation.Events
{
    /// <summary>
    /// The class "AbstractEventInstance" implements most of the interface in a straight forward fashion except for the "Raise" logic. 
    /// This cannot be implemented in the abstract class because it is dependent on the concrete handler type which is a generic 
    /// parameter of "AbstractEventInstance".
    /// 
    /// Concrete implementations are provided for the three event implementations as described above. These implement raise by 
    /// retrieving the event arguments and passing them on to the event's raise method to which they keep a reference. The event 
    /// reference is also exposed as a property "InnerEvent". 
    /// 
    /// If the event arguments were passed directly to the instance they can be changed via the "EventArgs" property up to the 
    /// moment when the event is finally raised. After that the setter will throw an exception.
    /// </summary>
    /// <typeparam name="THandler">A delegate type to handle the event instances.</typeparam>
    [Serializable]
    public abstract class AbstractEventInstance<THandler> : IEventInstance, IPriorityContainer, ISerializableSimulation
    {
        #region cvar

        private String name;
        private double time = double.NaN;
        private Priority priority;

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
            set
            {
                // CAUTION: the following clone is necessary 
                // because the priority will be used as a 
                // dictionary key in the event scheduler!
                priority = value.Clone() as Priority;
            }
        }

        /// <summary>
        /// get and set time
        /// caution: changing this manually will NOT change the time of occurence
        /// </summary>
        public double Time
        {
            get { return time; }
            set { time = value; }
        }

        /// <summary>
        /// Indicate if the event instance has been raised yet
        /// </summary>
        public bool HasBeenRaised { get; set; }

        /// <summary>
        /// number of attached handlers
        /// </summary>
        public abstract int HandlerCount { get; }

        /// <summary>
        /// Get and set log parameter
        /// </summary>
        public bool Log { get; set; }

        #endregion
        #region ctor

        /// <summary>
        /// creates an event instance with the default priority
        /// </summary>
        /// <param name="name"></param>
        public AbstractEventInstance(String name) : this(name, new Priority()) { }

        /// <summary>
        /// creates an event instance with the given priority
        /// </summary>
        /// <param name="name"></param>
        /// <param name="priority"></param>
        public AbstractEventInstance(String name, Priority priority)
        {
            // CAUTION: the following clone is necessary 
            // because the priority will be used as a 
            // dictionary key in the event scheduler!
            this.priority = priority.Clone() as Priority; 
            this.name = name;
        }

        #endregion
        #region impl

        /// <summary>
        /// raise the event instance (calling all handlers)
        /// call base at the end of your override, this here
        /// sets HasBeenRaised to true!
        /// </summary>
        public virtual void Raise() { HasBeenRaised = true; }

        #region IComparable

        /// <summary>
        /// default comparer
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(IEventInstance other)
        {
            return this.priority.CompareTo(other.Priority);
        }

        #endregion
        #region ISerializableGrubi

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("name", name);
            info.AddValue("time", time);
            info.AddValue("priority", priority);
            info.AddValue("HasBeenRaised", HasBeenRaised);
            info.AddValue("Log", Log);
        }

        #endregion

        #endregion
    }
}