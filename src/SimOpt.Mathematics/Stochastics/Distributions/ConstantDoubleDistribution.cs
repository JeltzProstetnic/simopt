using SimOpt.Mathematics.Stochastics.Interfaces;
using System;

namespace SimOpt.Mathematics.Stochastics.Distributions
{
    /// <summary>
    /// provides a double precision constant number
    /// The Constant Integer and Constant Double Distributions provide a constant number on each invocation for cases where a distribution has to be given but only a fixed number is required.
    /// </summary>

    [Serializable]
    public class ConstantDoubleDistribution : IDistribution<double>
    {
        #region cvar

        private double value;

        #endregion
        #region prop

        public string Name
        {
            get { return "Constant"; }
        }

        public bool Initialized { get; private set; }

        public bool Configured { get; private set; }

        /// <summary>
        /// The seed is not used here
        /// </summary>
        public int? Seed { get; set; }

        /// <summary>
        /// Get or set the number of values which have been
        /// drawn from this distribution since the last reset.
        /// </summary>
        public int DrawCount { get; set; }

        public double NonStochasticValue
        {
            get { return value; }
        }

        /// <summary>
        /// The constant distribution ignores the antithetic parameter
        /// </summary>
        public bool Antithetic { get; set; }

        public double Mean { get { return value; } }

        #endregion
        #region ctor

        /// <summary>
        /// creates a default instance. the instance must be 
        /// configured and initialized before it can be used.
        /// </summary>
        public ConstantDoubleDistribution() { }

        /// <summary>
        /// creates a configured and initialized instance
        /// </summary>
        /// <param name="seed"></param>
        /// <param name="mu"></param>
        /// <param name="sigma"></param>
        /// <param name="antithetic"></param>
        public ConstantDoubleDistribution(double value = 0d, bool initialize = true)
        {
            Configure(value);
            Initialized = initialize;
        }

        #endregion
        #region init

        /// <summary>
        /// caution: seed and antithetic flag will be irgnored
        /// </summary>
        /// <param name="seed"></param>
        /// <param name="antithetic"></param>
        public void Initialize(int seed, bool antithetic = false)
        {
            Seed = seed;
            Antithetic = antithetic;
            Initialized = true;
        }

        /// <summary>
        /// caution: the random source will not be used
        /// </summary>
        /// <param name="rnd"></param>
        public void Initialize(IRandomSource rnd)
        {
            Initialized = true;
        }

        #endregion
        #region conf

        public void Configure(double value)
        {
            this.value = value;
            Configured = true;
        }

        #endregion
        #region impl

        public double Next()
        {
            DrawCount++;
            return value;
        }

        #endregion
        #region rset

        public void Reset() { DrawCount = 0; }

        public void Reset(int seed) { Reset(); }

        public void Reset(int seed, bool antithetic) { Reset(); }

        public void Reset(bool antithetic) { Reset(); }

        #endregion
    }
}