using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Simulation.Engine;

namespace SimOpt.Simulation.Events
{
    /// <summary>
    /// simple non generic wrapper for simulation objects
    /// </summary>
    [Serializable]
    public class TernaryEvent : TernaryEvent<IEntity, object, object> 
    {
        #region ctor

        /// <summary>
        /// default constructor
        /// </summary>
        /// <param name="name"></param>
        public TernaryEvent(String name) : base(name) { }

        #endregion
    }

    /// <summary>
    /// simple non generic wrapper for simulation objects
    /// </summary>
    [Serializable]
    public class TernaryEvent<TEventArgs, TData> : TernaryEvent<IEntity, TEventArgs, TData>
    {
        #region ctor

        /// <summary>
        /// default constructor
        /// </summary>
        /// <param name="name"></param>
        public TernaryEvent(String name) : base(name) { }

        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TSender"></typeparam>
    /// <typeparam name="TEventArgs"></typeparam>
    /// <typeparam name="TData"></typeparam>
    [Serializable]
    public class TernaryEvent<TSender, TEventArgs, TData> 
        : AbstractEvent<Action<TSender, TEventArgs, TData>>
    {
        #region over

        public override int HandlerCount
        {
            get { return Handlers.Count; }
        }

        #endregion
        #region cvar

        // protected BinaryEvent<TSender, TEventArgs> castEvent;

        #endregion
        #region ctor

        /// <summary>
        /// default constructor
        /// </summary>
        /// <param name="name"></param>
        public TernaryEvent(String name) : base(name) 
        {
            // castEvent = new BinaryEvent<TSender, TEventArgs>(name);
        }

        #endregion
        #region impl

        /// <summary>
        /// get an instance with the default priority
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        /// <returns></returns>
        public TernaryEventInstance<TSender, TEventArgs, TData> GetInstance(TSender sender, TEventArgs eventArgs, TData data = default(TData))
        {
            TernaryEventInstance<TSender, TEventArgs, TData> inst = new TernaryEventInstance<TSender, TEventArgs, TData>(this, sender, eventArgs, data);
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
        public TernaryEventInstance<TSender, TEventArgs, TData> GetInstance(Priority priority, TSender sender, TEventArgs eventArgs, TData data = default(TData))
        {
            TernaryEventInstance<TSender, TEventArgs, TData> inst = new TernaryEventInstance<TSender, TEventArgs, TData>(this, sender, eventArgs, data);
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
        public TernaryEventInstance<TSender, TEventArgs, TData> GetInstance(TSender sender, TEventArgs eventArgs, Func<TData> retrieveDataFunction)
        {
            TernaryEventInstance<TSender, TEventArgs, TData> inst = new TernaryEventInstance<TSender, TEventArgs, TData>(this, sender, eventArgs, retrieveDataFunction);
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
        public TernaryEventInstance<TSender, TEventArgs, TData> GetInstance(Priority priority, TSender sender, TEventArgs eventArgs, Func<TData> retrieveDataFunction)
        {
            TernaryEventInstance<TSender, TEventArgs, TData> inst = new TernaryEventInstance<TSender, TEventArgs, TData>(this, sender, eventArgs, retrieveDataFunction);
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
        public TernaryEventInstance<TSender, TEventArgs, TData> GetInstance(TSender sender, Func<Tuple<TEventArgs, TData>> retrieveEventArgsFunction)
        {
            TernaryEventInstance<TSender, TEventArgs, TData> inst = new TernaryEventInstance<TSender, TEventArgs, TData>(this, sender, retrieveEventArgsFunction);
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
        public TernaryEventInstance<TSender, TEventArgs, TData> GetInstance(Priority priority, TSender sender, Func<Tuple<TEventArgs, TData>> retrieveEventArgsFunction)
        {
            TernaryEventInstance<TSender, TEventArgs, TData> inst = new TernaryEventInstance<TSender, TEventArgs, TData>(this, sender, retrieveEventArgsFunction);
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
        public TernaryEventInstance<TSender, TEventArgs, TData> GetInstance(Func<Tuple<TSender, TEventArgs, TData>> retrieveParamsFunction)
        {
            TernaryEventInstance<TSender, TEventArgs, TData> inst = new TernaryEventInstance<TSender, TEventArgs, TData>(this, retrieveParamsFunction);
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
        public TernaryEventInstance<TSender, TEventArgs, TData> GetInstance(Priority priority, Func<Tuple<TSender, TEventArgs, TData>> retrieveParamsFunction)
        {
            TernaryEventInstance<TSender, TEventArgs, TData> inst = new TernaryEventInstance<TSender, TEventArgs, TData>(this, retrieveParamsFunction);
            inst.Priority = priority;
            inst.Log = Log;
            return inst;
        }

        /// <summary>
        /// raise the event (calling all handlers)
        /// </summary>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        public void Raise(TSender sender, TEventArgs eventArgs, TData data = default(TData))
        {
            foreach (Action<TSender, TEventArgs, TData> handler in Handlers.Values)
            {
                handler.Invoke(sender, eventArgs, data);
                if (DetachHandlerRequested) ScheduleForRemoval(handler);
            }
            ExecuteRemovalSchedule();
        }

        #endregion
        #region oper

        public static TernaryEvent<TSender, TEventArgs, TData> operator +(TernaryEvent<TSender, TEventArgs, TData> evnt, Action<TSender, TEventArgs, TData> hand)
        {
            evnt.AddHandler(hand);
            return evnt;
        }

        public static TernaryEvent<TSender, TEventArgs, TData> operator -(TernaryEvent<TSender, TEventArgs, TData> evnt, Action<TSender, TEventArgs, TData> hand)
        {
            evnt.RemoveHandler(hand);
            return evnt;
        }

        #endregion
        #region cast

        //public static explicit operator BinaryEvent<TSender, TEventArgs>(TernaryEvent<TSender, TEventArgs, TData> evnt)
        //{
        //    return evnt.castEvent;
        //}

        #endregion
    }
}
