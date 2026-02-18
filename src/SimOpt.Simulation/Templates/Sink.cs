using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Simulation;
using SimOpt.Simulation.Interfaces;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Entities;
using SimOpt.Basics.Datastructures.Geometry;
using SimOpt.Simulation.Enum;
using SimOpt.Simulation.Events;

namespace SimOpt.Simulation.Templates
{
    [Serializable]
    public class Sink : Sink<object>
    {
        #region ctor

        public Sink() : base() { }

        public Sink(IModel model, string id = "", string name = "", bool log = false, Point initialPosition = null)
            : base(model, id, name, log, initialPosition)
        { }

        #endregion
    }
    [Serializable]
    public class Sink<T> : SimpleEntity, IItemSink<T>, IEntity
    {
        #region over

        public override void Reset()
        {
            base.Reset();
            count = 0;
        }

        #endregion
        #region cvar

        private bool log;
        private int count = 0;
        private BinaryEvent<Sink<T>, T> itemReceivedEvent;

        #endregion
        #region prop

        public int Count
        {
            get { return count; }
        }

        public bool Logging
        {
            get { return log; }
            set { log = value; }
        }

        /// <summary>
        /// CAUTION: This is an immediate event. Some event schedulers may not process it.
        /// If you call Put in a handler it will probably result in an infinite loop.
        /// </summary>
        public BinaryEvent<Sink<T>, T> ItemReceived { get { return itemReceivedEvent; } }

        #endregion
        #region ctor

        public Sink() : base() 
        {
            itemReceivedEvent = new BinaryEvent<Sink<T>, T>(this.EntityName + ".ItemReceived");
            itemReceivedEvent.Priority = new Priority(type: PriorityType.LowLevelAfterOthers);
        }

        public Sink(IModel model, string id = "", string name = "", bool log = false, Point initialPosition = null) : base(model, id, name, initialPosition)
        {
            this.log = log;

            itemReceivedEvent = new BinaryEvent<Sink<T>, T>(name + ".ItemReceived");
            itemReceivedEvent.Priority = new Priority(type: PriorityType.LowLevelAfterOthers);
        }

        #endregion
        #region impl

        /// <summary>
        /// Note: this will always return true.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Put(T item)
        {
            count += 1;
            if (log) this.Log<SIM_INFO>(EntityName + " received " + item.ToString() + ". Received " + count.ToString() + " items in total.", Model);
#if ImmediateEvents
            Model.AddImmediateEvent(itemReceivedEvent.GetInstance(this, item));
#else
            itemReceivedEvent.Raise(this, item);
#endif
            return true;
        }

        /// <summary>
        /// Note: this will always return true.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="sender"></param>
        /// <returns></returns>
        public bool Put(T item, IEntity sender)
        {
            count += 1;
            if (log) 
                this.Log<SIM_INFO>(EntityName + " received " + item.ToString() + " from " + sender.ToString() + ". Received " + count.ToString() + " items in total.", Model);
#if ImmediateEvents
            Model.AddImmediateEvent(itemReceivedEvent.GetInstance(this, item));
#else
            itemReceivedEvent.Raise(this, item);
#endif
            return true;
        }

        #region connections

        public bool IsConnectionAllowed(IItemSource<T> source)
        {
            return true;
        }

        public bool ConnectTo(IItemSource<T> source)
        {
            source.EntityCreatedEvent.AddHandler((sender, entity) => Put(entity), new Priority(type: PriorityType.SimWorldBeforeOthers));
            // TODO: save handlers to be able to remove them on reset, if they have been added dynamically!
            return true;
        }

        public void ConnectTo(params IItemSource<T>[] sources)
        {
            foreach (IItemSource<T> src in sources)
            {
                src.EntityCreatedEvent.AddHandler((sender, entity) => Put(entity), new Priority(type: PriorityType.SimWorldBeforeOthers));
                // TODO: save handlers to be able to remove them on reset, if they have been added dynamically!
            }
        }

        public void ConnectTo(IEnumerable<IItemSource<T>> sources)
        {
            foreach (IItemSource<T> src in sources)
            {
                src.EntityCreatedEvent.AddHandler((sender, entity) => Put(entity), new Priority(type: PriorityType.SimWorldBeforeOthers));
                // TODO: save handlers to be able to remove them on reset, if they have been added dynamically!
            }
        }

        #endregion

        #endregion
    }
}
