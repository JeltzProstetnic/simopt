using System;
using System.Collections.Generic;
using System.Linq;
using SimOpt.Mathematics.Stochastics.Distributions;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Enum;
using SimOpt.Simulation.Interfaces;

namespace SimOpt.Simulation.Templates
{
    /// <summary>How a <see cref="Router{T}"/> picks the target for an item.</summary>
    public enum RoutingPolicy
    {
        /// <summary>Each target with probability weight / sum of weights, drawn from the router's own stream.</summary>
        Probabilistic,

        /// <summary>The target whose length is smallest; a tie goes to the target added first.</summary>
        ShortestQueue,
    }

    /// <summary>
    /// SIM-65 step 4 (D4): passes each item it is given straight on to one of its targets. The draws of
    /// the probabilistic policy come from the router's own stream, keyed to its seed ID and reset with
    /// the model, so routing is a function of (model seed, router id). <see cref="SimpleRejectServer"/>'s
    /// routing is not (SIM-99), which is why this is a new template. A target that refuses the item makes
    /// the router refuse it: Put returns false, RefusedCount goes up and NotifyItemNotAccepted is called,
    /// because a source wired with ConnectTo ignores the return value (SIM-132).
    /// </summary>
    [Serializable]
    public class Router<T> : StochasticEntity, IItemSink<T>
    {
        #region cvar

        private readonly List<IItemSink<T>> targets = new List<IItemSink<T>>();
        private readonly List<double> weights = new List<double>();
        private readonly List<Func<int>> lengths = new List<Func<int>>();
        private readonly List<int> routed = new List<int>();
        private double totalWeight;
        private int refusedCount;
        private Action<T> notifyItemNotAccepted = Noop;
        private readonly Random<double> uniform;

        #endregion
        #region ctor

        public Router(IModel model, RoutingPolicy policy, int seedID, string id = "", string name = "")
            : base(model, seedID, id, name)
        {
            Policy = policy;
            uniform = new Random<double>(this, new UniformDoubleDistribution(0d, 1d), Model.Antithetic, Model.NonStochasticMode);
        }

        #endregion
        #region prop

        public RoutingPolicy Policy { get; }

        /// <summary>The targets, in the order they were added.</summary>
        public IReadOnlyList<IItemSink<T>> Targets { get { return targets; } }

        /// <summary>The items passed on to each target since the last reset, in target order.</summary>
        public IReadOnlyList<int> Routed { get { return routed; } }

        /// <summary>The items refused because the chosen target refused them, since the last reset.</summary>
        public int RefusedCount { get { return refusedCount; } }

        /// <summary>Called with every item the router refuses.</summary>
        public Action<T> NotifyItemNotAccepted
        {
            get { return notifyItemNotAccepted; }
            set { notifyItemNotAccepted = value ?? Noop; }
        }

        #endregion
        #region init

        /// <summary>
        /// Adds a target. <paramref name="weight"/> is its share under the probabilistic policy, a
        /// positive finite number; <paramref name="length"/> is the target's length, which the
        /// shortest-queue policy needs for every target. What counts is the caller's choice: the MCP
        /// builder counts the items waiting in a queue plus those in service at the station it feeds.
        /// </summary>
        public void AddTarget(IItemSink<T> target, double weight = 1d, Func<int> length = null)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (!(weight > 0d) || double.IsInfinity(weight))
                throw new ArgumentOutOfRangeException(nameof(weight), weight, "A routing weight is a positive, finite number.");
            if (Policy == RoutingPolicy.ShortestQueue && length == null)
                throw new ArgumentException(EntityName + " routes to the shortest queue, so it needs each target's queue length.", nameof(length));

            targets.Add(target);
            weights.Add(weight);
            lengths.Add(length);
            routed.Add(0);
            totalWeight += weight;
        }

        #endregion
        #region over

        public override void Reset()
        {
            base.Reset();
            refusedCount = 0;
            for (int i = 0; i < routed.Count; i++) routed[i] = 0;
        }

        #endregion
        #region impl

        public bool Put(T item)
        {
            if (targets.Count == 0) throw new InvalidOperationException(EntityName + " has no target to route to.");

            int chosen = Policy == RoutingPolicy.Probabilistic ? Draw() : Shortest();
            if (targets[chosen].Put(item))
            {
                routed[chosen]++;
                return true;
            }
            refusedCount++;
            notifyItemNotAccepted.Invoke(item);
            return false;
        }

        private int Draw()
        {
            double u = uniform.Next() * totalWeight;
            double cumulative = 0d;
            for (int i = 0; i < weights.Count; i++)
            {
                cumulative += weights[i];
                if (u < cumulative) return i;
            }
            return weights.Count - 1;   // u reached the total through rounding
        }

        private int Shortest()
        {
            int best = 0;
            int bestLength = lengths[0]();
            for (int i = 1; i < lengths.Count; i++)
            {
                int length = lengths[i]();
                if (length < bestLength)
                {
                    best = i;
                    bestLength = length;
                }
            }
            return best;
        }

        #region connections

        public bool IsConnectionAllowed(IItemSource<T> fromSource)
        {
            return true;
        }

        public bool ConnectTo(IItemSource<T> source)
        {
            source.EntityCreatedEvent.AddHandler((sender, item) => Put(item), new Priority(type: PriorityType.SimWorldBeforeOthers));
            return true;
        }

        public void ConnectTo(params IItemSource<T>[] sources)
        {
            foreach (IItemSource<T> source in sources) ConnectTo(source);
        }

        public void ConnectTo(IEnumerable<IItemSource<T>> sources)
        {
            foreach (IItemSource<T> source in sources) ConnectTo(source);
        }

        #endregion

        private static void Noop(T ignore) { }

        #endregion
    }

    [Serializable]
    public class SimpleRouter : Router<SimpleEntity>
    {
        public SimpleRouter(IModel model, RoutingPolicy policy, int seedID, string id = "", string name = "")
            : base(model, policy, seedID, id, name)
        { }
    }
}
