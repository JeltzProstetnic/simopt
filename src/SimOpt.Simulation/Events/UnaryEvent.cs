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
    public class UnaryEvent : UnaryEvent<IEntity> 
    {
        #region ctor

        /// <summary>
        /// default constructor
        /// </summary>
        /// <param name="name"></param>
        public UnaryEvent(String name) : base(name) { }

        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    [Serializable]
    public class UnaryEvent<TData> : AbstractEvent<Action<TData>>
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
        public UnaryEvent(String name) : base(name) { }

        #endregion
        #region impl

        /// <summary>
        /// get an instance with the default priority
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public UnaryEventInstance<TData> GetInstance(TData data)
        {
            UnaryEventInstance<TData> inst = new UnaryEventInstance<TData>(this, data);
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
        public UnaryEventInstance<TData> GetInstance(Priority priority, TData data)
        {
            UnaryEventInstance<TData> inst = new UnaryEventInstance<TData>(this, data);
            inst.Priority = priority;
            inst.Log = Log;
            return inst;
        }

        /// <summary>
        /// get an instance with the default priority
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public UnaryEventInstance<TData> GetInstance(Func<TData> retrieveData)
        {
            UnaryEventInstance<TData> inst = new UnaryEventInstance<TData>(this, retrieveData);
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
        public UnaryEventInstance<TData> GetInstance(Priority priority, Func<TData> retrieveData)
        {
            UnaryEventInstance<TData> inst = new UnaryEventInstance<TData>(this, retrieveData);
            inst.Priority = priority;
            inst.Log = Log;
            return inst;
        }

        /// <summary>
        /// raise the event (calling all handlers)
        /// </summary>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        public void Raise(TData data)
        {
            foreach (Action<TData> handler in Handlers.Values)
            {
                handler.Invoke(data);
                if (DetachHandlerRequested) ScheduleForRemoval(handler);
            }
            ExecuteRemovalSchedule();
        }

        #endregion
        #region oper

        public static UnaryEvent<TData> operator +(UnaryEvent<TData> evnt, Action<TData> hand)
        {
            evnt.AddHandler(hand);
            return evnt;
        }

        public static UnaryEvent<TData> operator -(UnaryEvent<TData> evnt, Action<TData> hand)
        {
            evnt.RemoveHandler(hand);
            return evnt;
        }

        #endregion
    }
}
