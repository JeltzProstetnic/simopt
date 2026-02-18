using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Simulation.Engine;

namespace MatthiasToolbox.Simulation.Events
{
    /// <summary>
    /// simple non generic wrapper for simulation objects.
    /// The EventArgs will be of type object and the sender
    /// will have to be IEntity.
    /// </summary>
    [Serializable]
    public class BinaryEvent : BinaryEvent<object> 
    {
        #region ctor

        /// <summary>
        /// default constructor
        /// </summary>
        /// <param name="name"></param>
        public BinaryEvent(String name) : base(name) { }

        #endregion
    }

    /// <summary>
    /// simple wrapper for simulation objects, sender will
    /// have to be of type IEntity
    /// </summary>
    [Serializable]
    public class BinaryEvent<T> : BinaryEvent<IEntity, T>
    {
        #region ctor

        /// <summary>
        /// default constructor
        /// </summary>
        /// <param name="name"></param>
        public BinaryEvent(String name) : base(name) { }

        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TSender"></typeparam>
    /// <typeparam name="TEventArgs"></typeparam>
    [Serializable]
    public class BinaryEvent<TSender, TEventArgs> : AbstractEvent<Action<TSender, TEventArgs>>
    {
        #region over

        public override int HandlerCount
        {
            get { return Handlers.Count; }
        }

        #endregion
        #region ctor

        /// <summary>
        /// default constructor
        /// </summary>
        /// <param name="name"></param>
        public BinaryEvent(String name) : base(name) { }

        #endregion
        #region impl

        /// <summary>
        /// get an instance with the default priority
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        /// <returns></returns>
        public virtual BinaryEventInstance<TSender, TEventArgs> GetInstance(TSender sender, TEventArgs eventArgs)
        {
            BinaryEventInstance<TSender, TEventArgs> inst = new BinaryEventInstance<TSender, TEventArgs>(this, sender, eventArgs);
            inst.Log = Log;
            return inst;
        }

        /// <summary>
        /// get an instance with the given priority
        /// </summary>
        /// <param name="priority"></param>
        /// <param name="sender"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public virtual BinaryEventInstance<TSender, TEventArgs> GetInstance(Priority priority, TSender sender, TEventArgs eventArgs)
        {
            BinaryEventInstance<TSender, TEventArgs> inst = new BinaryEventInstance<TSender, TEventArgs>(this, sender, eventArgs);
            inst.Priority = priority;
            inst.Log = Log;
            return inst;
        }

        /// <summary>
        /// get an instance with the default priority
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        /// <returns></returns>
        public virtual BinaryEventInstance<TSender, TEventArgs> GetInstance(Func<Tuple<TSender, TEventArgs>> retrieveParamsFunction)
        {
            BinaryEventInstance<TSender, TEventArgs> inst = new BinaryEventInstance<TSender, TEventArgs>(this, retrieveParamsFunction);
            inst.Log = Log;
            return inst;
        }

        /// <summary>
        /// get an instance with the given priority
        /// </summary>
        /// <param name="priority"></param>
        /// <param name="sender"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public virtual BinaryEventInstance<TSender, TEventArgs> GetInstance(Priority priority, Func<Tuple<TSender, TEventArgs>> retrieveParamsFunction)
        {
            BinaryEventInstance<TSender, TEventArgs> inst = new BinaryEventInstance<TSender, TEventArgs>(this, retrieveParamsFunction);
            inst.Priority = priority;
            inst.Log = Log;
            return inst;
        }

        /// <summary>
        /// get an instance with the default priority
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        /// <returns></returns>
        public virtual BinaryEventInstance<TSender, TEventArgs> GetInstance(TSender sender, Func<TEventArgs> retrieveArgsFunction)
        {
            BinaryEventInstance<TSender, TEventArgs> inst = new BinaryEventInstance<TSender, TEventArgs>(this, sender, retrieveArgsFunction);
            inst.Log = Log;
            return inst;
        }

        /// <summary>
        /// get an instance with the given priority
        /// </summary>
        /// <param name="priority"></param>
        /// <param name="sender"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public virtual BinaryEventInstance<TSender, TEventArgs> GetInstance(Priority priority, TSender sender, Func<TEventArgs> retrieveArgsFunction)
        {
            BinaryEventInstance<TSender, TEventArgs> inst = new BinaryEventInstance<TSender, TEventArgs>(this, sender, retrieveArgsFunction);
            inst.Priority = priority;
            inst.Log = Log;
            return inst;
        }

        /// <summary>
        /// raise the event (calling all handlers)
        /// </summary>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        public virtual void Raise(TSender sender, TEventArgs eventArgs)
        {
            foreach (Action<TSender, TEventArgs> handler in Handlers.Values)
            {
                handler.Invoke(sender, eventArgs);
                if (DetachHandlerRequested) ScheduleForRemoval(handler);
            }
            ExecuteRemovalSchedule();
        }

        #endregion
        #region oper

        public static BinaryEvent<TSender, TEventArgs> operator +(BinaryEvent<TSender, TEventArgs> evnt, Action<TSender, TEventArgs> hand)
        {
            evnt.AddHandler(hand);
            return evnt;
        }

        public static BinaryEvent<TSender, TEventArgs> operator -(BinaryEvent<TSender, TEventArgs> evnt, Action<TSender, TEventArgs> hand)
        {
            evnt.RemoveHandler(hand);
            return evnt;
        }

        #endregion
    }
}