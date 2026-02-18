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
    public class UnaryEventInstance : UnaryEventInstance<IEntity> 
    {
        #region ctor

        /// <summary>
        /// Constructor using original event and handler parameters
        /// </summary>
        /// <param name="innerEvent"> original instance of event </param>
        /// <param name="param1"> handler parameter - Tsource </param>
        /// <param name="param2"> handler parameter - Tparam </param>
        internal UnaryEventInstance(UnaryEvent innerEvent, IEntity data)
            : base(innerEvent, data)
        { }

        /// <summary>
        /// Constructor using original event and handler parameters
        /// </summary>
        /// <param name="innerEvent"> original instance of event </param>
        /// <param name="param1"> handler parameter - Tsource </param>
        /// <param name="param2"> handler parameter - Tparam </param>
        internal UnaryEventInstance(UnaryEvent innerEvent, Func<IEntity> retrieveData)
            : base(innerEvent, retrieveData)
        { }

        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    [Serializable]
    public class UnaryEventInstance<TData> : AbstractEventInstance<Action<TData>>, ISerializableSimulation
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
            innerEvent.Raise(retrieveParamsFunction.Invoke());
            base.Raise();
        }

        #endregion
        #region cvar

        private UnaryEvent<TData> innerEvent;
        private TData data;
        private Func<TData> retrieveParamsFunction;

        #endregion
        #region prop

        /// <summary>
        /// Get inner event
        /// </summary>
        public UnaryEvent<TData> InnerEvent 
        { 
            get { return innerEvent; } 
        }

        /// <summary>
        /// Get event data - this may be null until the instance is
        /// processed because the args may not be retrieved before raise.
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
        /// <param name="param1"> handler parameter - Tsource </param>
        /// <param name="param2"> handler parameter - Tparam </param>
        internal UnaryEventInstance(UnaryEvent<TData> innerEvent, TData data)
            : base(innerEvent.Name, innerEvent.Priority)
        {
            this.innerEvent = innerEvent;
            this.data = data;
            this.retrieveParamsFunction = RetrieveParams;
        }

        /// <summary>
        /// Constructor using original event and handler parameters
        /// </summary>
        /// <param name="innerEvent"> original instance of event </param>
        /// <param name="param1"> handler parameter - Tsource </param>
        /// <param name="param2"> handler parameter - Tparam </param>
        internal UnaryEventInstance(UnaryEvent<TData> innerEvent, Func<TData> retrieveData)
            : base(innerEvent.Name, innerEvent.Priority)
        {
            this.innerEvent = innerEvent;
            this.retrieveParamsFunction = retrieveData;
        }

        #endregion
        #region impl

        private TData RetrieveParams()
        {
            return data;
        }

        #region ISerializableGrubi

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue("data", data);
            info.AddValue("inner", innerEvent);

            // private Func<TData> retrieveParamsFunction;
        }

        #endregion
        #endregion
    }
}
