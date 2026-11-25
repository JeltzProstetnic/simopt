using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Simulation.Engine;
using System.Runtime.Serialization;
using SimOpt.Basics.Interfaces;

namespace SimOpt.Simulation.Events
{
    /// <summary>
    /// simple non generic wrapper for simulation objects
    /// </summary>
    [Serializable]
    public class TernaryEventInstance : TernaryEventInstance<IEntity, object, object> 
    {
        #region ctor

        /// <summary>
        /// Constructor using original event and handler parameters
        /// </summary>
        /// <param name="innerEvent"> original instance of event </param>
        internal TernaryEventInstance(TernaryEvent innerEvent, IEntity sender, object eventArgs, object data = null)
            : base(innerEvent, sender, eventArgs, data)
        { }

        /// <summary>
        /// Constructor using original event and handler parameters
        /// </summary>
        /// <param name="innerEvent"> original instance of event </param>
        internal TernaryEventInstance(TernaryEvent innerEvent, IEntity sender, object eventArgs, Func<object> retrieveDataFunction)
            : base(innerEvent, sender, eventArgs, retrieveDataFunction)
        { }

        /// <summary>
        /// Constructor using original event and handler parameters
        /// </summary>
        /// <param name="innerEvent"> original instance of event </param>
        internal TernaryEventInstance(TernaryEvent innerEvent, IEntity sender, Func<Tuple<object, object>> retrieveEventArgsFunction)
            : base(innerEvent, sender, retrieveEventArgsFunction)
        { }

        /// <summary>
        /// Constructor using original event and handler parameters
        /// </summary>
        /// <param name="innerEvent"> original instance of event </param>
        internal TernaryEventInstance(TernaryEvent innerEvent, Func<Tuple<IEntity, object, object>> retrieveParamsFunction)
            : base(innerEvent, retrieveParamsFunction)
        { }

        #endregion
    }

    /// <summary>
    /// simple non generic wrapper for simulation objects
    /// </summary>
    [Serializable]
    public class TernaryEventInstance<TEventArgs, TData> : TernaryEventInstance<IEntity, TEventArgs, TData>
    {
        #region ctor

        /// <summary>
        /// Constructor using original event and handler parameters
        /// </summary>
        /// <param name="innerEvent"> original instance of event </param>
        internal TernaryEventInstance(TernaryEvent<TEventArgs, TData> innerEvent, IEntity sender, TEventArgs eventArgs, TData data = default(TData))
            : base(innerEvent, sender, eventArgs, data)
        { }

        /// <summary>
        /// Constructor using original event and handler parameters
        /// </summary>
        /// <param name="innerEvent"> original instance of event </param>
        internal TernaryEventInstance(TernaryEvent<TEventArgs, TData> innerEvent, IEntity sender, TEventArgs eventArgs, Func<TData> retrieveDataFunction)
            : base(innerEvent, sender, eventArgs, retrieveDataFunction)
        { }

        /// <summary>
        /// Constructor using original event and handler parameters
        /// </summary>
        /// <param name="innerEvent"> original instance of event </param>
        internal TernaryEventInstance(TernaryEvent<TEventArgs, TData> innerEvent, IEntity sender, Func<Tuple<TEventArgs, TData>> retrieveEventArgsFunction)
            : base(innerEvent, sender, retrieveEventArgsFunction)
        { }

        /// <summary>
        /// Constructor using original event and handler parameters
        /// </summary>
        /// <param name="innerEvent"> original instance of event </param>
        internal TernaryEventInstance(TernaryEvent<TEventArgs, TData> innerEvent, Func<Tuple<IEntity, TEventArgs, TData>> retrieveParamsFunction)
            : base(innerEvent, retrieveParamsFunction)
        { }

        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TSender"></typeparam>
    /// <typeparam name="TEventArgs"></typeparam>
    /// <typeparam name="TData"></typeparam>
    [Serializable]
    public class TernaryEventInstance<TSender, TEventArgs, TData> 
        : AbstractEventInstance<Action<TSender, TEventArgs, TData>>, ISerializableSimulation
    {
        #region over

        /// <summary>
        /// number of attached handlers
        /// </summary>
        public override int HandlerCount
        {
            get { return InnerEvent.Handlers.Count; }
        }

        /// <summary>
        /// Overrided raise method to execute raise method of inner event
        /// </summary>
        public override void Raise()
        {
            // SIM-129: the values go to the event directly; a tuple is only unpacked when the
            // caller supplied a function returning one
            switch (mode)
            {
                case DeferredParams:
                    Tuple<TSender, TEventArgs, TData> result = retrieveParamsFunction.Invoke();
                    innerEvent.Raise(result.Item1, result.Item2, result.Item3);
                    break;
                case DeferredArgsAndData:
                    Tuple<TEventArgs, TData> argsAndData = retrieveEventArgsFunction.Invoke();
                    innerEvent.Raise(sender, argsAndData.Item1, argsAndData.Item2);
                    break;
                case DeferredData:
                    innerEvent.Raise(sender, eventArgs, retrieveDataFunction.Invoke());
                    break;
                default:
                    innerEvent.Raise(sender, eventArgs, data);
                    break;
            }
            base.Raise();
        }

        #endregion
        #region cvar

        private TernaryEvent<TSender, TEventArgs, TData> innerEvent;
        private TSender sender;
        private TEventArgs eventArgs;
        private TData data;
        private Func<Tuple<TSender, TEventArgs, TData>> retrieveParamsFunction;
        private Func<Tuple<TEventArgs, TData>> retrieveEventArgsFunction;
        private Func<TData> retrieveDataFunction;
        private byte mode;  // which constructor built it
        private const byte Values = 0, DeferredData = 1, DeferredArgsAndData = 2, DeferredParams = 3;

        #endregion
        #region prop

        /// <summary>
        /// Get inner event
        /// </summary>
        public TernaryEvent<TSender, TEventArgs, TData> InnerEvent
        {
            get { return innerEvent; }
        }

        /// <summary>
        /// Get parameter of handler
        /// </summary>
        public TSender Sender
        {
            get { return sender; }
        }

        /// <summary>
        /// Get parameter of handler
        /// </summary>
        public TEventArgs EventArgs
        {
            get { return eventArgs; }
        }

        /// <summary>
        /// Get the additional data
        /// </summary>
        public TData Data
        {
            get { return data; }
        }

        #endregion
        #region ctor

        /// <summary>
        /// Constructor using original event and handler parameters
        /// </summary>
        /// <param name="innerEvent"> original instance of event </param>
        internal TernaryEventInstance(TernaryEvent<TSender, TEventArgs, TData> innerEvent, TSender sender, TEventArgs eventArgs, TData data = default(TData))
            : base(innerEvent.Name, innerEvent.Priority)
        {
            this.innerEvent = innerEvent;
            this.sender = sender;
            this.eventArgs = eventArgs;
            this.data = data;
        }

        /// <summary>
        /// Constructor using original event and handler parameters
        /// </summary>
        /// <param name="innerEvent"> original instance of event </param>
        internal TernaryEventInstance(TernaryEvent<TSender, TEventArgs, TData> innerEvent, TSender sender, TEventArgs eventArgs, Func<TData> retrieveDataFunction)
            : base(innerEvent.Name, innerEvent.Priority)
        {
            this.innerEvent = innerEvent;
            this.sender = sender;
            this.eventArgs = eventArgs;
            this.retrieveDataFunction = retrieveDataFunction;
            this.mode = DeferredData;
        }

        /// <summary>
        /// Constructor using original event and handler parameters
        /// </summary>
        /// <param name="innerEvent"> original instance of event </param>
        internal TernaryEventInstance(TernaryEvent<TSender, TEventArgs, TData> innerEvent, TSender sender, Func<Tuple<TEventArgs, TData>> retrieveEventArgsFunction)
            : base(innerEvent.Name, innerEvent.Priority)
        {
            this.innerEvent = innerEvent;
            this.sender = sender;
            this.retrieveEventArgsFunction = retrieveEventArgsFunction;
            this.mode = DeferredArgsAndData;
        }

        /// <summary>
        /// Constructor using original event and handler parameters
        /// </summary>
        /// <param name="innerEvent"> original instance of event </param>
        internal TernaryEventInstance(TernaryEvent<TSender, TEventArgs, TData> innerEvent, Func<Tuple<TSender, TEventArgs, TData>> retrieveParamsFunction)
            : base(innerEvent.Name, innerEvent.Priority)
        {
            this.innerEvent = innerEvent;
            this.retrieveParamsFunction = retrieveParamsFunction;
            this.mode = DeferredParams;
        }

        #endregion
        #region impl

        #region ISerializableGrubi

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue("sender", sender);
            info.AddValue("args", eventArgs);
            info.AddValue("inner", innerEvent);
            info.AddValue("data", data);

            //private Func<Tuple<TSender, TEventArgs, TData>> retrieveParamsFunction;
            //private Func<Tuple<TEventArgs, TData>> retrieveEventArgsFunction;
            //private Func<TData> retrieveDataFunction;
        }

        #endregion
        #endregion
    }
}