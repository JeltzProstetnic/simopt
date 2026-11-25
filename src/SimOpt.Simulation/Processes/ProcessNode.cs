using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimOpt.Basics.Utilities;
using SimOpt.Mathematics.Stochastics.Interfaces;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Enum;
using SimOpt.Simulation.Events;
using SimOpt.Simulation.Interfaces;

namespace SimOpt.Simulation.Processes
{
    /// <summary>
    /// SIM-126 schema: the engine side of a topology's <c>process</c> node
    /// (<c>docs/plans/2026-11-25-sim-126-process-schema.md</c>, Q-PROC-SCHEMA). Items pushed into it, as
    /// into a delay, each run through a closed list of <see cref="ProcessStep"/>s, any number of items at
    /// once, and are pushed on after the last step.
    /// <para>
    /// An item leaving frees every unit it still holds, so a model cannot leak a unit by forgetting a
    /// release; <see cref="ReleaseStep"/> frees one earlier. Every random step value is drawn when the
    /// item enters, in step order, each from a stream the node owns for that step and field, seeded from
    /// the node's seed and <c>"step:" + id + "." + field</c>. Inserting a step therefore does not move
    /// the other steps' numbers, and an item consumes the same draws whatever happens to it later.
    /// Each item's steps run as a transient <see cref="Process"/>, which holds no stream of its own.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class ProcessNode : StochasticEntity, IItemSink<SimpleEntity>, IItemSource<SimpleEntity>
    {
        #region cvar

        private readonly ProcessStep[] steps;
        private readonly string[] fieldKeys;              // "<step id>.<field>", in step order
        private readonly Random<double>[] fieldStreams;
        private readonly int[] fieldOfStep;               // the step's duration or amount, or -1
        private readonly int[] patienceOfStep;            // slice 3: the step's patience, or -1
        private readonly Dictionary<string, OutletSource> outlets = new Dictionary<string, OutletSource>();
        private readonly bool seizes;
        private readonly BinaryEvent<IEntity, SimpleEntity> itemReleased;
        private readonly BinaryEvent<ProcessNode, IReadOnlyDictionary<string, double>> itemEntered;
        private int count;
        private Process repeating;

        /// <summary>
        /// Slice 4: cycles of an unfed node in one instant after which it is taken to repeat for ever; a
        /// legitimate model passes this only with an absurdly large stock drained at one instant.
        /// </summary>
        public const int MaxCyclesPerInstant = 100_000;

        #endregion
        #region ctor

        /// <param name="seedID">the node's seed ID, from its id (SIM-62)</param>
        /// <param name="steps">the steps, at least one; a release must free a unit the item holds at that step</param>
        public ProcessNode(IModel model, int seedID, IEnumerable<ProcessStep> steps, string id = "", string name = "")
            : base(model, seedID, id, name)
        {
            this.steps = (steps ?? throw new ArgumentNullException(nameof(steps))).ToArray();
            if (this.steps.Length == 0) throw new ArgumentException("Process '" + EntityName + "' has no steps.", nameof(steps));
            CheckSteps();

            var keys = new List<string>();
            var distributions = new List<IDistribution<double>>();
            fieldOfStep = new int[this.steps.Length];
            patienceOfStep = new int[this.steps.Length];
            int Field(ProcessStep step, string field, IDistribution<double> distribution)
            {
                if (distribution == null) return -1;
                keys.Add(step.Id + "." + field);
                distributions.Add(distribution);
                return keys.Count - 1;
            }
            for (int i = 0; i < this.steps.Length; i++)
            {
                (string field, IDistribution<double> distribution) = RandomField(this.steps[i]);
                fieldOfStep[i] = Field(this.steps[i], field, distribution);
                patienceOfStep[i] = -1;
                if (this.steps[i] is IPatientStep patient && patient.Patience != null)
                {
                    patienceOfStep[i] = Field(this.steps[i], "patience", patient.Patience);
                    if (!outlets.ContainsKey(patient.Outlet))
                        outlets.Add(patient.Outlet, new OutletSource(EntityName + "." + patient.Outlet));
                }
            }
            fieldKeys = keys.ToArray();
            fieldStreams = new Random<double>[fieldKeys.Length];
            for (int f = 0; f < fieldKeys.Length; f++)
            {
                fieldStreams[f] = new Random<double>(this, distributions[f]);
                fieldStreams[f].Reset(FieldSeed(fieldKeys[f]), Model.Antithetic, Model.NonStochasticMode);
            }
            seizes = this.steps.Any(s => s is SeizeStep);

            itemReleased = new BinaryEvent<IEntity, SimpleEntity>(EntityName + ".ItemReleased");
            itemEntered = new BinaryEvent<ProcessNode, IReadOnlyDictionary<string, double>>(EntityName + ".ItemEntered");
        }

        #endregion
        #region prop

        public IReadOnlyList<ProcessStep> Steps { get { return steps; } }

        /// <summary>The items inside, at any step.</summary>
        public int Count { get { return count; } }

        /// <summary>Raised with every item that has run through all the steps, as it is pushed on.</summary>
        public BinaryEvent<IEntity, SimpleEntity> EntityCreatedEvent { get { return itemReleased; } }

        /// <summary>Slice 3: the outlets the steps name, in step order.</summary>
        public IEnumerable<string> Outlets { get { return outlets.Keys; } }

        /// <summary>
        /// Slice 3: the exit of the items that gave up at a step naming <paramref name="name"/>, as an item
        /// source to connect a downstream node to.
        /// </summary>
        public IItemSource<SimpleEntity> Outlet(string name)
        {
            if (name != null && outlets.TryGetValue(name, out OutletSource outlet)) return outlet;
            throw new ArgumentException("Process '" + EntityName + "' has no step with the outlet '" + name + "'.", nameof(name));
        }

        /// <summary>
        /// Raised as an item enters, or an unfed node starts a cycle, with the values drawn for it by
        /// <c>"&lt;step id&gt;.&lt;field&gt;"</c> (<c>"washing.duration"</c>).
        /// </summary>
        public BinaryEvent<ProcessNode, IReadOnlyDictionary<string, double>> ItemEnteredEvent { get { return itemEntered; } }

        #endregion
        #region impl

        /// <summary>Takes the item, draws its step values and starts its steps; never refuses.</summary>
        public bool Put(SimpleEntity item)
        {
            double[] draws = Draw();
            count++;
            Model.StartProcess(p => Run(p, draws, outlet => Leave(item, outlet)));
            return true;
        }

        /// <summary>
        /// Slice 4: makes this node one that nothing feeds: one instance runs its steps over and over from
        /// the start of every run to its end, beside the item flow, with no item. Call it while the model
        /// is stopped, once.
        /// </summary>
        public void Repeat()
        {
            if (repeating != null) throw new InvalidOperationException("Process '" + EntityName + "' already repeats.");
            if (Model.IsRunning) throw new InvalidOperationException("A process node starts repeating while the model is stopped.");
            repeating = Model.StartProcess(Cycles);
        }

        /// <summary>True when the node repeats its steps instead of running items through them.</summary>
        public bool Repeats { get { return repeating != null; } }

        private IEnumerable<Wait> Cycles(Process p)
        {
            double instant = double.NaN;
            int cycles = 0;
            while (true)
            {
                if (p.Now == instant)
                {
                    if (++cycles > MaxCyclesPerInstant)
                        throw new InvalidOperationException("Process '" + EntityName + "' repeated its steps " + MaxCyclesPerInstant +
                            " times at one instant (" + p.Now + "), so it would repeat them there for ever: none of its steps waited. " +
                            "Give it a delay, or a step that waits for something the cycle itself does not undo.");
                }
                else
                {
                    instant = p.Now;
                    cycles = 0;
                }
                foreach (Wait wait in Run(p, Draw(), _ => { })) yield return wait;
            }
        }

        /// <summary>Draws an item's or a cycle's step values, in step order, and announces them.</summary>
        private double[] Draw()
        {
            var draws = new double[fieldStreams.Length];
            for (int f = 0; f < draws.Length; f++) draws[f] = fieldStreams[f].Next();
            itemEntered.Raise(this, new Draws(fieldKeys, draws));
            return draws;
        }

        /// <summary>An item leaves by the normal exit (outlet null) or by the outlet it gave up at.</summary>
        private void Leave(SimpleEntity item, string outlet)
        {
            count--;
            if (outlet == null) itemReleased.Raise(this, item);
            else outlets[outlet].Raise(this, item);
        }

        /// <summary>
        /// Runs the steps once, then frees what is still held and calls <paramref name="done"/> with the
        /// outlet given up at, or null.
        /// </summary>
        private IEnumerable<Wait> Run(Process p, double[] draws, Action<string> done)
        {
            List<Resource> held = seizes ? new List<Resource>(2) : null;
            for (int i = 0; i < steps.Length; i++)
            {
                switch (steps[i])
                {
                    case DelayStep _:
                        yield return p.Timeout(draws[fieldOfStep[i]]);
                        break;
                    case SeizeStep seize:
                    {
                        Wait request = seize.Resource.Request();
                        if (patienceOfStep[i] < 0) yield return request;
                        else
                        {
                            AnyOfWait patient = Wait.AnyOf(request, p.Timeout(draws[patienceOfStep[i]]));
                            yield return patient;
                            if (patient.Winner != request) { Free(held); done(seize.Outlet); yield break; }
                        }
                        held.Add(seize.Resource);
                        break;
                    }
                    case ReleaseStep release:
                        held.Remove(release.Resource);
                        release.Resource.Release();
                        break;
                    case TakeStep take:
                    {
                        double amount = Amount(take, i, draws);
                        if (!(amount > 0d)) break;
                        Wait taken = take.Stock.TakeWait(amount);
                        if (patienceOfStep[i] < 0) yield return taken;
                        else
                        {
                            AnyOfWait patient = Wait.AnyOf(taken, p.Timeout(draws[patienceOfStep[i]]));
                            yield return patient;
                            if (patient.Winner != taken) { Free(held); done(take.Outlet); yield break; }
                        }
                        break;
                    }
                    case PutStep put:
                    {
                        double amount = put.UpToLevel is double level ? level - put.Stock.Level : Amount(put, i, draws);
                        if (amount > 0d) yield return put.Stock.PutWait(amount);
                        break;
                    }
                    case WaitUntilStep until:
                        if (!until.Met(until.Stock.Level))
                            yield return Wait.Next(until.Stock.LevelChangedEvent, (_, level) => until.Met(level));
                        break;
                }
            }
            Free(held);
            done(null);
        }

        private static void Free(List<Resource> held)
        {
            if (held == null) return;
            foreach (Resource unit in held) unit.Release();
        }

        private double Amount(StockStep step, int index, double[] draws) =>
            step.FixedAmount ?? draws[fieldOfStep[index]];

        private static (string Field, IDistribution<double> Distribution) RandomField(ProcessStep step) => step switch
        {
            DelayStep delay => ("duration", delay.Duration),
            StockStep stock when stock.RandomAmount != null => ("amount", stock.RandomAmount),
            _ => (null, null),
        };

        private int FieldSeed(string key) => GetRandomSeedFor(StableHash.Of("step:" + key));

        /// <summary>Step ids are unique, and a release frees a unit the item holds at that step.</summary>
        private void CheckSteps()
        {
            var ids = new HashSet<string>();
            var held = new Dictionary<Resource, int>();
            foreach (ProcessStep step in steps)
            {
                if (step == null) throw new ArgumentException("Process '" + EntityName + "' has a null step.");
                if (!ids.Add(step.Id))
                    throw new ArgumentException("Process '" + EntityName + "' has two steps with the id '" + step.Id + "'.");
                if (step is SeizeStep seize) held[seize.Resource] = held.GetValueOrDefault(seize.Resource) + 1;
                if (step is ReleaseStep release)
                {
                    if (held.GetValueOrDefault(release.Resource) == 0)
                        throw new ArgumentException("Step '" + step.Id + "' of process '" + EntityName + "' releases '" +
                                                    release.Resource.EntityName + "', which the item does not hold at that step.");
                    held[release.Resource]--;
                }
            }
        }

        public override void Reset()
        {
            base.Reset();
            // as Process does for its named streams: seeded from the new seed and the key alone
            for (int f = 0; f < fieldStreams.Length; f++)
                fieldStreams[f].Reset(FieldSeed(fieldKeys[f]), Model.Antithetic, Model.NonStochasticMode);
            count = 0;
        }

        #region connections

        public bool IsConnectionAllowed(IItemSource<SimpleEntity> fromSource) => true;

        public bool ConnectTo(IItemSource<SimpleEntity> source)
        {
            source.EntityCreatedEvent.AddHandler((_, item) => Put(item), new Priority(type: PriorityType.SimWorldBeforeOthers));
            return true;
        }

        public void ConnectTo(params IItemSource<SimpleEntity>[] sources)
        {
            foreach (IItemSource<SimpleEntity> source in sources) ConnectTo(source);
        }

        public void ConnectTo(IEnumerable<IItemSource<SimpleEntity>> sources)
        {
            foreach (IItemSource<SimpleEntity> source in sources) ConnectTo(source);
        }

        public bool ConnectTo(IItemSink<SimpleEntity> sink)
        {
            itemReleased.AddHandler((_, item) => sink.Put(item), new Priority(type: PriorityType.SimWorldBeforeOthers));
            return true;
        }

        #endregion

        /// <summary>Slice 3: an outlet as an item source, so a downstream node connects to it as to any other.</summary>
        [Serializable]
        private sealed class OutletSource : IItemSource<SimpleEntity>
        {
            private readonly BinaryEvent<IEntity, SimpleEntity> released;

            public OutletSource(string name) { released = new BinaryEvent<IEntity, SimpleEntity>(name); }

            public BinaryEvent<IEntity, SimpleEntity> EntityCreatedEvent { get { return released; } }

            public void Raise(IEntity sender, SimpleEntity item) => released.Raise(sender, item);

            public bool ConnectTo(IItemSink<SimpleEntity> sink)
            {
                released.AddHandler((_, item) => sink.Put(item), new Priority(type: PriorityType.SimWorldBeforeOthers));
                return true;
            }
        }

        /// <summary>The values drawn for one item, read by key without a dictionary per item.</summary>
        private sealed class Draws : IReadOnlyDictionary<string, double>
        {
            private readonly string[] keys;
            private readonly double[] values;

            public Draws(string[] keys, double[] values) { this.keys = keys; this.values = values; }

            public double this[string key] => TryGetValue(key, out double value) ? value : throw new KeyNotFoundException(key);
            public IEnumerable<string> Keys => keys;
            public IEnumerable<double> Values => values;
            public int Count => keys.Length;
            public bool ContainsKey(string key) => Array.IndexOf(keys, key) >= 0;

            public bool TryGetValue(string key, out double value)
            {
                int i = Array.IndexOf(keys, key);
                value = i >= 0 ? values[i] : 0d;
                return i >= 0;
            }

            public IEnumerator<KeyValuePair<string, double>> GetEnumerator()
            {
                for (int i = 0; i < keys.Length; i++) yield return new KeyValuePair<string, double>(keys[i], values[i]);
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        #endregion
    }
}
