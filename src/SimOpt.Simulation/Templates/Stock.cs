using System;
using System.Collections.Generic;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Events;
using SimOpt.Simulation.Processes;

namespace SimOpt.Simulation.Templates
{
    /// <summary>
    /// SIM-127: a stock level, the counterpart of SimPy's <c>Container</c>. It holds an amount
    /// (pieces, kilograms), not entities; a <see cref="Buffer{T}"/> holds entities.
    /// <para>
    /// A take is all-or-nothing: it waits until the whole amount is there and takes nothing before.
    /// A put waits while it would exceed the capacity. Both queues are strictly first come, first
    /// served, so a small request never overtakes a large one that came first. A request is served
    /// at the instant the stock allows it, and its callback runs as a zero-delay event at that
    /// instant (SIM-128), in the order the requests were served.
    /// </para>
    /// <para>
    /// The level is compared exactly; amounts that only add up after rounding (0.1 + 0.2) may wait.
    /// </para>
    /// </summary>
    [Serializable]
    public class Stock : SimpleEntity
    {
        #region cvar

        private readonly double capacity;
        private readonly double initialLevel;
        private double level;
        private readonly LinkedList<Request> takers = new LinkedList<Request>();
        private readonly LinkedList<Request> putters = new LinkedList<Request>();
        private readonly BinaryEvent<Stock, double> levelChangedEvent;

        private readonly struct Request
        {
            public Request(double amount, Action done) { Amount = amount; Done = done; }
            public double Amount { get; }
            public Action Done { get; }
        }

        #endregion
        #region ctor

        /// <param name="model">the model</param>
        /// <param name="initialLevel">the level at the start of every run</param>
        /// <param name="capacity">the most the stock can hold; unlimited by default</param>
        /// <param name="id">the id</param>
        /// <param name="name">the name</param>
        public Stock(IModel model, double initialLevel = 0d, double capacity = double.PositiveInfinity,
                     string id = "", string name = "")
            : base(model, id, name)
        {
            if (double.IsNaN(capacity) || capacity <= 0d)
                throw new ArgumentOutOfRangeException(nameof(capacity), "The capacity must be positive.");
            if (double.IsNaN(initialLevel) || initialLevel < 0d || initialLevel > capacity)
                throw new ArgumentOutOfRangeException(nameof(initialLevel), "The initial level must lie between 0 and the capacity.");

            this.capacity = capacity;
            this.initialLevel = initialLevel;
            level = initialLevel;
            levelChangedEvent = new BinaryEvent<Stock, double>(EntityName + ".LevelChanged");
        }

        #endregion
        #region prop

        public double Level { get { return level; } }

        public double Capacity { get { return capacity; } }

        public double InitialLevel { get { return initialLevel; } }

        public int WaitingTakers { get { return takers.Count; } }

        public int WaitingPutters { get { return putters.Count; } }

        /// <summary>
        /// Raised with the new level whenever the level changes.
        /// </summary>
        public BinaryEvent<Stock, double> LevelChangedEvent { get { return levelChangedEvent; } }

        #endregion
        #region impl

        /// <summary>
        /// Adds the amount, now or, while it would exceed the capacity, once enough has been taken.
        /// </summary>
        /// <param name="amount">a positive amount no larger than the capacity</param>
        /// <param name="onPut">runs at the instant the amount went in; may be null</param>
        public void Put(double amount, Action onPut = null)
        {
            CheckAmount(amount);
            putters.AddLast(new Request(amount, onPut));
            Settle();
        }

        /// <summary>
        /// Removes the whole amount, now or once enough has been put, never part of it.
        /// </summary>
        /// <param name="amount">a positive amount no larger than the capacity</param>
        /// <param name="onTaken">runs at the instant the amount was taken; may be null</param>
        public void Take(double amount, Action onTaken)
        {
            CheckAmount(amount);
            takers.AddLast(new Request(amount, onTaken));
            Settle();
        }

        /// <summary>
        /// SIM-126: for a process, a wait that completes once the whole amount has been taken.
        /// </summary>
        public Wait TakeWait(double amount)
        {
            CheckAmount(amount);
            return Wait.From(done => Take(amount, done));
        }

        /// <summary>
        /// SIM-126: for a process, a wait that completes once the amount has gone in.
        /// </summary>
        public Wait PutWait(double amount)
        {
            CheckAmount(amount);
            return Wait.From(done => Put(amount, done));
        }

        private void CheckAmount(double amount)
        {
            if (double.IsNaN(amount) || amount <= 0d)
                throw new ArgumentOutOfRangeException(nameof(amount), "The amount must be positive.");
            if (amount > capacity)
                throw new ArgumentOutOfRangeException(nameof(amount), "The amount exceeds the capacity, so the request could never be served.");
        }

        /// <summary>
        /// Serves the waiting requests at the head of both queues until neither can go.
        /// </summary>
        private void Settle()
        {
            bool served = true;
            while (served)
            {
                served = false;
                if (putters.Count > 0 && level + putters.First.Value.Amount <= capacity)
                {
                    Request put = putters.First.Value;
                    putters.RemoveFirst();
                    Change(put.Amount);
                    Resume(put.Done);
                    served = true;
                }
                if (takers.Count > 0 && level >= takers.First.Value.Amount)
                {
                    Request take = takers.First.Value;
                    takers.RemoveFirst();
                    Change(-take.Amount);
                    Resume(take.Done);
                    served = true;
                }
            }
        }

        private void Change(double delta)
        {
            level += delta;
            levelChangedEvent.Raise(this, level);
        }

        private void Resume(Action done)
        {
            if (done != null) Model.Schedule(0d, done);
        }

        public override void Reset()
        {
            base.Reset();
            level = initialLevel;
            takers.Clear();
            putters.Clear();
        }

        #endregion
    }
}
