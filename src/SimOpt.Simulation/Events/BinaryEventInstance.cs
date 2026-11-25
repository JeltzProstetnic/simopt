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
    public class BinaryEventInstance : BinaryEventInstance<object> 
    {
        #region ctor

        /// <summary>
        /// Constructor using original event and handler parameters
        /// </summary>
        /// <param name="innerEvent"> original instance of event </param>
        internal BinaryEventInstance(BinaryEvent innerEvent, IEntity sender, object eventArgs)
            : base(innerEvent, sender, eventArgs)
        { }

        /// <summary>
        /// Constructor using original event and handler parameters
        /// </summary>
        /// <param name="innerEvent"> original instance of event </param>
        internal BinaryEventInstance(BinaryEvent innerEvent, Func<Tuple<IEntity, object>> retrieveParamsFunction)
            : base(innerEvent, retrieveParamsFunction)
        { }

        /// <summary>
        /// Constructor using original event and handler parameters
        /// </summary>
        /// <param name="innerEvent"> original instance of event </param>
        internal BinaryEventInstance(BinaryEvent innerEvent, IEntity sender, Func<object> retrieveParam)
            : base(innerEvent, sender, retrieveParam)
        { }

        #endregion
    }

    /// <summary>
    /// simple non generic wrapper for simulation objects
    /// </summary>
    [Serializable]
    public class BinaryEventInstance<T> : BinaryEventInstance<IEntity, T>
    {
        #region ctor

        /// <summary>
        /// Constructor using original event and handler parameters
        /// </summary>
        /// <param name="innerEvent"> original instance of event </param>
        internal BinaryEventInstance(BinaryEvent<T> innerEvent, IEntity sender, T eventArgs)
            : base(innerEvent, sender, eventArgs)
        { }

        /// <summary>
        /// Constructor using original event and handler parameters
        /// </summary>
        /// <param name="innerEvent"> original instance of event </param>
        internal BinaryEventInstance(BinaryEvent<T> innerEvent, Func<Tuple<IEntity, T>> retrieveParamsFunction)
            : base(innerEvent, retrieveParamsFunction)
        { }

        /// <summary>
        /// Constructor using original event and handler parameters
        /// </summary>
        /// <param name="innerEvent"> original instance of event </param>
        internal BinaryEventInstance(BinaryEvent<T> innerEvent, IEntity sender, Func<T> retrieveParam)
            : base(innerEvent, sender, retrieveParam)
        { }

        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TSender"></typeparam>
    /// <typeparam name="TEventArgs"></typeparam>
    [Serializable]
    public class BinaryEventInstance<TSender, TEventArgs> : AbstractEventInstance<Action<TSender, TEventArgs>>, ISerializableSimulation
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
            if (mode == DeferredParams)
            {
                Tuple<TSender, TEventArgs> para = retrieveParamsFunction.Invoke();
                innerEvent.Raise(para.Item1, para.Item2);
            }
            else if (mode == DeferredArgs) innerEvent.Raise(sender, retrieveEventArgsFunction.Invoke());
            else innerEvent.Raise(sender, eventArgs);
            base.Raise();
        }

        #endregion
        #region cvar

        private BinaryEvent<TSender, TEventArgs> innerEvent;
        private TSender sender;
        private TEventArgs eventArgs;
        private Func<Tuple<TSender, TEventArgs>> retrieveParamsFunction;
        private Func<TEventArgs> retrieveEventArgsFunction;
        private byte mode;  // which constructor built it: Values, DeferredArgs or DeferredParams
        private const byte Values = 0, DeferredArgs = 1, DeferredParams = 2;

        #endregion
        #region prop

        /// <summary>
        /// Get inner event
        /// </summary>
        public BinaryEvent<TSender, TEventArgs> InnerEvent
        {
            get { return innerEvent; }
        }

        /// <summary>
        /// Get sender - this may be null until the instance is
        /// processed because the args may not be retrieved before raise.
        /// </summary>
        public TSender Sender
        {
            get { return sender; }
        }

        /// <summary>
        /// Get arguments - these may be null until the instance is
        /// processed because the args may not be retrieved before raise.
        /// </summary>
        public TEventArgs EventArgs
        {
            get { return eventArgs; }
            internal set 
            {
                if (HasBeenRaised) throw new InvalidOperationException("The event args cannot be changed after the event has already been raised.");
                eventArgs = value; 
            }
        }

        #endregion
        #region ctor

        /// <summary>
        /// Constructor using original event and handler parameters
        /// </summary>
        /// <param name="innerEvent"> original instance of event </param>
        internal BinaryEventInstance(BinaryEvent<TSender, TEventArgs> innerEvent, TSender sender, TEventArgs eventArgs)
            : base(innerEvent.Name, innerEvent.Priority)
        {
            this.innerEvent = innerEvent;
            this.sender = sender;
            this.eventArgs = eventArgs;
        }

        /// <summary>
        /// Constructor using original event and handler parameters
        /// to be retrieved on raise
        /// </summary>
        /// <param name="innerEvent"> original instance of event </param>
        internal BinaryEventInstance(BinaryEvent<TSender, TEventArgs> innerEvent, Func<Tuple<TSender, TEventArgs>> retrieveParamsFunction)
            : base(innerEvent.Name, innerEvent.Priority)
        {
            this.innerEvent = innerEvent;
            this.retrieveParamsFunction = retrieveParamsFunction;
            this.mode = DeferredParams;
        }

        /// <summary>
        /// Constructor using original event and handler parameters
        /// to be retrieved on raise
        /// </summary>
        /// <param name="innerEvent"> original instance of event </param>
        internal BinaryEventInstance(BinaryEvent<TSender, TEventArgs> innerEvent, TSender sender, Func<TEventArgs> retrieveArgsFunction)
            : base(innerEvent.Name, innerEvent.Priority)
        {
            this.innerEvent = innerEvent;
            this.sender = sender;
            this.retrieveEventArgsFunction = retrieveArgsFunction;
            this.mode = DeferredArgs;
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

            //private Func<Tuple<TSender, TEventArgs>> retrieveParamsFunction;
            //private Func<TEventArgs> retrieveEventArgsFunction;
        }

        #endregion

        #endregion
    }
}
