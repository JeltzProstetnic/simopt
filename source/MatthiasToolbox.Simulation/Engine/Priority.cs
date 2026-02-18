using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Simulation.Enum;
using System.Runtime.Serialization;
using MatthiasToolbox.Basics.Interfaces;

namespace MatthiasToolbox.Simulation.Engine
{
    /// <summary>
    /// A three staged priority object for priorization of events, event instances and handlers.
    /// 
    /// All priorities are of the type "Priority" in the Engine namespace. A priority consists of a value (double precision number), a priority 
    /// type (enumeration) and an order number (integer). The priority type is used to discern the classes of priorities defined in the 
    /// "PriorityType" enumeration:
    /// 
    /// <list type="bullet">
    /// <item>LowLevelBeforeOthers – On rare occasions this priority class is used internally for events, event instances or handlers which must 
    /// precede all others. Using this outside the engine is discouraged.</item>
    /// <item>SimWorldBeforeOthers – This is intended for events, event instances and handlers which are related to the "simulation world" or
    /// "environment" and should be raised / handled prior to the rest.</item>
    /// <item>User – This is the default priority. It is recommended to use this by default and document all other cases clearly.</item>
    /// <item>SimWorldAfterOthers – This priority class is intended for special events, event instances or handlers (related to the 
    /// "simulation world" as above) which are to be raised / handeled after the above.</item>
    /// <item>LowLevelAfterOthers – Like LowLevelBeforeOthers this is used only on a few events, event instances or handlers in the 
    /// engine code and it should be avoided in user code.</item>
    /// </list>
    /// 
    /// Events, event instances and handlers which are in the same priority class are ordered by their priority value which should be seen as the 
    /// main priorization system. Note though that the engine cannot handle priority values of "Double.NaN" (for Not a Number) and the use of 
    /// Double.Infinity (either PositiveInfinity or NegativeInfinity) is strongly discouraged. By default the priority value will be set to zero.
    /// 
    /// For events, event instances and handlers with the same priority class and the same priority value there is the order number which keeps track 
    /// of when an event instance was added to the timeline or when a handler was attached to an event so that those with equal priorities are 
    /// processed in a stable ordering as well.
    /// 
    /// Summarizing this one can say that event priorities are the default priorities for the respective event instances and handlers 
    /// are ordered and raised primarily by the event instance priority and secondarily by the handler priority.
    /// </summary>
    /// <remarks>final</remarks>
    [Serializable]
    public class Priority : IComparable<Priority>, ICloneable, ISerializableSimulation // TODO: change to struct? !!!
    {
        #region over

        /// <summary>
        /// returns a string representation of the priority in the format
        /// [PriorityType;PriorityNumber;AddedOrder]
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "[" + PriorityType.ToString() + ";" + PriorityNumber.ToString() + ";" + AddedOrder.ToString() + "]";
        }

        #endregion
        #region stat

        /// <summary>
        /// Return a new Priority with the default values. This is 
        /// equivalent to using <code>new Priority()</code>.
        /// </summary>
        /// <returns>A new Priority with the default values. This is 
        /// equivalent to using <code>new Priority()</code>.</returns>
        public static Priority Default() { return new Priority(); }

        #endregion
        #region prop

        /// <summary>
		/// type of the priority
		/// </summary>
		public PriorityType PriorityType { get; set; }
		
		/// <summary>
		/// the actual priority number
		/// </summary>
		public double PriorityNumber { get; set; }
		
		/// <summary>
		/// indexer to avoid duplicate priorities so
		/// that the priority can be used as unambiguous
		/// sorting criterion.
		/// </summary>
		internal int AddedOrder { get; set; }
		
		#endregion
        #region ctor

        /// <summary>
        /// default constructor
        /// </summary>
        private Priority(double priority, PriorityType type, int addedOrder)
        {
            PriorityType = type;
            PriorityNumber = priority;
            AddedOrder = addedOrder;
        }

        /// <summary>
		/// default constructor
		/// </summary>
		public Priority(double priority = 0, PriorityType type = PriorityType.User) 
        {
            PriorityType = type;
            PriorityNumber = priority;
        }

        #endregion
        #region impl

        #region IComparable<Priority>

        /// <summary>
		/// Comparer tho priorities.
		/// </summary>
		/// <param name="other">the object to compare with this instance</param>
		/// <returns>a number smaller than zero if the other instance is 
        /// of lesser priority, a number greater then zero in the 
        /// opposite case and zero for equal priorities (which should
        /// never occur though!)</returns>
		public int CompareTo(Priority other) {
            if (PriorityType.CompareTo(other.PriorityType) == 0)
            {
                if (PriorityNumber.CompareTo(other.PriorityNumber) == 0)
                    return AddedOrder.CompareTo(other.AddedOrder);
                return PriorityNumber.CompareTo(other.PriorityNumber);
            }
            return PriorityType.CompareTo(other.PriorityType);
        }

        #endregion
        #region ICloneable

        /// <summary>
        /// creates a fully equivalent clone (including the added order!)
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            return new Priority(PriorityNumber, PriorityType, AddedOrder);
        }

        #endregion
        #region ISerializableGrubi

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("PriorityType", PriorityType);
            info.AddValue("PriorityNumber", PriorityNumber);
            info.AddValue("AddedOrder", AddedOrder);
        }

        #endregion

        #endregion
    }
}