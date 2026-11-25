using System;
using SimOpt.Mathematics.Stochastics.Interfaces;
using SimOpt.Simulation.Templates;

namespace SimOpt.Simulation.Processes
{
    /// <summary>
    /// SIM-126 schema: one step of a <see cref="ProcessNode"/>'s closed list. The steps are the words a
    /// topology document may use; there is no loop, condition or variable among them (Q-PROC-SCHEMA).
    /// </summary>
    [Serializable]
    public abstract class ProcessStep
    {
        protected ProcessStep(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A step has an id.", nameof(id));
            Id = id;
        }

        /// <summary>Names the step in its node; its random streams are keyed to it.</summary>
        public string Id { get; }

        /// <summary>
        /// Slice 3: patience and its outlet come together or not at all; an outlet without patience
        /// would never be used, and patience without one would leave the item nowhere to go.
        /// </summary>
        protected static void CheckPatience(IDistribution<double> patience, string outlet)
        {
            if (patience != null && string.IsNullOrWhiteSpace(outlet))
                throw new ArgumentException("A step with patience names the outlet an item that gives up leaves by.", nameof(outlet));
            if (patience == null && !string.IsNullOrWhiteSpace(outlet))
                throw new ArgumentException("A step with an outlet has a patience after which an item gives up.", nameof(patience));
        }
    }

    /// <summary>A step an item may give up waiting at: after its patience it leaves by its outlet.</summary>
    public interface IPatientStep
    {
        /// <summary>How long an item waits at the step before it gives up, drawn on entry; null when it waits for ever.</summary>
        IDistribution<double> Patience { get; }

        /// <summary>The outlet an item that gave up leaves by; null without patience.</summary>
        string Outlet { get; }
    }

    /// <summary>Holds the item for a duration drawn when the item enters the node.</summary>
    [Serializable]
    public sealed class DelayStep : ProcessStep
    {
        public DelayStep(string id, IDistribution<double> duration) : base(id)
        {
            Duration = duration ?? throw new ArgumentNullException(nameof(duration));
        }

        /// <summary>Configured, not initialised: the node makes a stream of it.</summary>
        public IDistribution<double> Duration { get; }
    }

    /// <summary>
    /// Waits for a unit of a resource, first come, first served, and holds it; with patience, for at
    /// most that long.
    /// </summary>
    [Serializable]
    public sealed class SeizeStep : ProcessStep, IPatientStep
    {
        public SeizeStep(string id, Resource resource, IDistribution<double> patience = null, string outlet = null) : base(id)
        {
            Resource = resource ?? throw new ArgumentNullException(nameof(resource));
            CheckPatience(patience, outlet);
            Patience = patience;
            Outlet = patience == null ? null : outlet;
        }

        public Resource Resource { get; }
        public IDistribution<double> Patience { get; }
        public string Outlet { get; }
    }

    /// <summary>Frees one unit of a resource the item holds, before the item leaves the node.</summary>
    [Serializable]
    public sealed class ReleaseStep : ProcessStep
    {
        public ReleaseStep(string id, Resource resource) : base(id)
        {
            Resource = resource ?? throw new ArgumentNullException(nameof(resource));
        }

        public Resource Resource { get; }
    }

    /// <summary>A step that moves an amount into or out of a stock, fixed or drawn on entry.</summary>
    [Serializable]
    public abstract class StockStep : ProcessStep
    {
        protected StockStep(string id, Stock stock, double amount) : base(id)
        {
            if (double.IsNaN(amount) || amount <= 0d || double.IsInfinity(amount))
                throw new ArgumentOutOfRangeException(nameof(amount), "The amount must be a positive number.");
            Stock = stock ?? throw new ArgumentNullException(nameof(stock));
            FixedAmount = amount;
        }

        protected StockStep(string id, Stock stock, IDistribution<double> amount) : base(id)
        {
            Stock = stock ?? throw new ArgumentNullException(nameof(stock));
            RandomAmount = amount ?? throw new ArgumentNullException(nameof(amount));
        }

        public Stock Stock { get; }

        /// <summary>The amount, when it is fixed.</summary>
        public double? FixedAmount { get; }

        /// <summary>
        /// The amount's distribution, when it is drawn; configured, not initialised. A draw of 0 or less
        /// moves nothing.
        /// </summary>
        public IDistribution<double> RandomAmount { get; }
    }

    /// <summary>
    /// Takes an amount out of a stock, waiting until the whole amount is there; with patience, for at
    /// most that long.
    /// </summary>
    [Serializable]
    public sealed class TakeStep : StockStep, IPatientStep
    {
        public TakeStep(string id, Stock stock, double amount, IDistribution<double> patience = null, string outlet = null)
            : base(id, stock, amount)
        {
            CheckPatience(patience, outlet);
            Patience = patience;
            Outlet = patience == null ? null : outlet;
        }

        public TakeStep(string id, Stock stock, IDistribution<double> amount, IDistribution<double> patience = null, string outlet = null)
            : base(id, stock, amount)
        {
            CheckPatience(patience, outlet);
            Patience = patience;
            Outlet = patience == null ? null : outlet;
        }

        public IDistribution<double> Patience { get; }
        public string Outlet { get; }
    }

    /// <summary>Puts an amount into a stock, waiting while it would overfill it.</summary>
    [Serializable]
    public sealed class PutStep : StockStep
    {
        public PutStep(string id, Stock stock, double amount) : base(id, stock, amount) { }
        public PutStep(string id, Stock stock, IDistribution<double> amount) : base(id, stock, amount) { }
    }
}
