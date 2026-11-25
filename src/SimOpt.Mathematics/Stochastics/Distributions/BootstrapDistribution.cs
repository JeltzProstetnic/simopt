using SimOpt.Mathematics.Stochastics.Interfaces;
using SimOpt.Mathematics.Stochastics.RandomSources;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimOpt.Mathematics.Stochastics.Distributions
{
    /// <summary>
    /// SIM-118 item 1: a bootstrap of observed values. Every draw picks one of the observations with
    /// equal chance and puts it back, so a value observed twice is drawn twice as often, and no value
    /// that was not observed is ever drawn (unlike <see cref="HistogramDoubleDistribution"/>, which
    /// interpolates within its bins; <see cref="ProbabilisticDistribution"/> is integer-only and
    /// draws without replacement).
    /// </summary>
    [Serializable]
    public class BootstrapDistribution : IDistribution<double>
    {
        #region cvar

        private double[] values = Array.Empty<double>();
        private double mean = double.NaN;
        private IRandomSource rnd = null!;

        #endregion
        #region prop

        public string Name
        {
            get { return "Bootstrap"; }
        }

        public bool Initialized
        {
            get
            {
                return (rnd != null && rnd.Initialized);
            }
        }

        public bool Configured { get; private set; }

        /// <summary>
        /// CAUTION: setting the value will reset the random number stream
        /// </summary>
        public int? Seed
        {
            get
            {
                if (Initialized) return rnd.Seed;
                return null;
            }
            set
            {
                rnd.Reset(value!.Value);
            }
        }

        /// <summary>
        /// Get or set the number of values which have been
        /// drawn from this distribution since the last reset.
        /// </summary>
        public int DrawCount { get; set; }

        /// <summary>The mean of the observations, which a non-stochastic run uses.</summary>
        public double NonStochasticValue
        {
            get { return mean; }
        }

        public double Mean { get { return mean; } }

        public double Minimum { get { return values.Length > 0 ? values.Min() : double.NaN; } }

        public double Maximum { get { return values.Length > 0 ? values.Max() : double.NaN; } }

        /// <summary>The observations drawn from, in the order given.</summary>
        public IReadOnlyList<double> Values { get { return values; } }

        /// <summary>
        /// CAUTION: setting the value will reset the random number stream
        /// </summary>
        public bool Antithetic
        {
            get { return rnd.Antithetic; }
            set { rnd.Reset(value); }
        }

        #endregion
        #region ctor

        /// <summary>
        /// creates a configured instance
        /// CAUTION: initialization required before use!
        /// </summary>
        public BootstrapDistribution(IEnumerable<double> values)
        {
            Configure(values);
        }

        /// <summary>
        /// creates a mersenne twister based, initialized instance, ready for immediate use
        /// </summary>
        public BootstrapDistribution(int seed, IEnumerable<double> values, bool antithetic = false)
        {
            Configure(values);
            Initialize(seed, antithetic);
        }

        #endregion
        #region init

        public void Initialize(int seed, bool antithetic = false)
        {
            this.rnd = new MersenneTwister(seed, antithetic);
        }

        public void Initialize(IRandomSource rnd)
        {
            this.rnd = rnd;
        }

        #endregion
        #region conf

        public void Configure(IEnumerable<double> values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            double[] observed = values.ToArray();
            if (observed.Length == 0)
                throw new ArgumentException("A bootstrap needs at least one observation.", nameof(values));
            if (observed.Any(v => double.IsNaN(v) || double.IsInfinity(v)))
                throw new ArgumentException("Every observation must be a finite number.", nameof(values));

            this.values = observed;
            this.mean = observed.Average();

            Configured = true;
        }

        #endregion
        #region impl

        public double Next()
        {
            DrawCount++;
            // NextDouble is on [0, 1); the bound guards any source that returns 1
            int index = Math.Min((int)(rnd.NextDouble() * values.Length), values.Length - 1);
            return values[index];
        }

        #endregion
        #region rset

        public void Reset()
        {
            DrawCount = 0;
            rnd.Reset();
        }

        public void Reset(int seed)
        {
            DrawCount = 0;
            rnd.Reset(seed);
        }

        public void Reset(int seed, bool antithetic)
        {
            DrawCount = 0;
            rnd.Reset(seed, antithetic);
        }

        public void Reset(bool antithetic)
        {
            DrawCount = 0;
            rnd.Reset(antithetic);
        }

        #endregion
    }
}
