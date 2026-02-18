using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Simulation.Events;

namespace MatthiasToolbox.Simulation.Tools
{
    [Serializable]
    public class BinaryCompoundEvent<TSender, TEventArgs> : BinaryEvent<TSender, TEventArgs>
    {
        #region cvar

        private List<BinaryEvent<TSender, TEventArgs>> events;

        #endregion
        #region over

        /// <summary>
        /// returns the sum of handlers of all contained events
        /// </summary>
        public override int HandlerCount
        {
            get
            {
                int result = 0;
                foreach (BinaryEvent<TSender, TEventArgs> e in events)
                    result += e.HandlerCount;
                return result;
            }
        }

        /// <summary>
        /// Adds the handler to all contained events.
        /// </summary>
        /// <param name="handler"></param>
        public override void AddHandler(Action<TSender, TEventArgs> handler)
        {
            foreach(BinaryEvent<TSender, TEventArgs> e in events)
                e.AddHandler(handler);
        }

        /// <summary>
        /// Adds the handler to all contained events.
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="priority"></param>
        public override void AddHandler(Action<TSender, TEventArgs> handler, Engine.Priority priority)
        {
            foreach (BinaryEvent<TSender, TEventArgs> e in events)
                e.AddHandler(handler, priority);
        }

        /// <summary>
        /// Clears all handlers from all contained events
        /// </summary>
        public override void ClearHandlers()
        {
            foreach (BinaryEvent<TSender, TEventArgs> e in events) 
                e.ClearHandlers();
        }

        /// <summary>
        /// Removes the handler from all contained events.
        /// </summary>
        /// <param name="handler"></param>
        /// <returns>true only if all handlers were successfully removed</returns>
        public override bool RemoveHandler(Action<TSender, TEventArgs> handler)
        {
            bool result = true;
            foreach (BinaryEvent<TSender, TEventArgs> e in events)
            {
                if(!e.RemoveHandler(handler)) result = false;
            }
            return result;
        }

        /// <summary>
        /// not allowed, will throw an exception
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        public override void Raise(TSender sender, TEventArgs eventArgs)
        {
            throw new InvalidOperationException("Someone scheduled a compound event...");
        }

        #region GetInstance Methods

        public override BinaryEventInstance<TSender, TEventArgs> GetInstance(Engine.Priority priority, Func<Tuple<TSender, TEventArgs>> retrieveParamsFunction)
        {
            throw new InvalidOperationException("The compound event cannot be used to schedule instances.");
        }

        public override BinaryEventInstance<TSender, TEventArgs> GetInstance(Engine.Priority priority, TSender sender, Func<TEventArgs> retrieveArgsFunction)
        {
            throw new InvalidOperationException("The compound event cannot be used to schedule instances.");
        }

        public override BinaryEventInstance<TSender, TEventArgs> GetInstance(Engine.Priority priority, TSender sender, TEventArgs eventArgs)
        {
            throw new InvalidOperationException("The compound event cannot be used to schedule instances.");
        }

        public override BinaryEventInstance<TSender, TEventArgs> GetInstance(Func<Tuple<TSender, TEventArgs>> retrieveParamsFunction)
        {
            throw new InvalidOperationException("The compound event cannot be used to schedule instances.");
        }

        public override BinaryEventInstance<TSender, TEventArgs> GetInstance(TSender sender, Func<TEventArgs> retrieveArgsFunction)
        {
            throw new InvalidOperationException("The compound event cannot be used to schedule instances.");
        }

        public override BinaryEventInstance<TSender, TEventArgs> GetInstance(TSender sender, TEventArgs eventArgs)
        {
            throw new InvalidOperationException("The compound event cannot be used to schedule instances.");
        }

        #endregion

        #endregion
        #region ctor

        public BinaryCompoundEvent(string name, params BinaryEvent<TSender, TEventArgs>[] args) : base(name)
        {
            if(args.Length < 1) throw new ArgumentNullException("args","You must provide at least one event.");
            events = args.ToList();
        }

        #endregion
    }
}
