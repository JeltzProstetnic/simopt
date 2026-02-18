using SimOpt.Mathematics.Stochastics.Interfaces;
using System;

namespace SimOpt.Mathematics.Stochastics.Distributions
{
    [Serializable]
    public class ConstantIntegerDistribution: IDistribution<int>, IDistribution<double>
    {
        #region cvar

        private int value;

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

        public int NonStochasticValue
        {
            get { return value; }
        }

        /// <summary>
        /// Get or set the number of values which have been
        /// drawn from this distribution since the last reset.
        /// </summary>
        public int DrawCount { get; set; }

        /// <summary>
        /// The constant distribution ignores the antithetic parameter
        /// </summary>
        public bool Antithetic { get; set; }

        public int Mean { get { return value; } }

        #region IDistribution<double>

        double IDistribution<double>.NonStochasticValue
        {
            get { return NonStochasticValue; }
        }

        double IDistribution<double>.Mean
        {
            get { return Mean; }
        }

        #endregion

        #endregion
        #region ctor

        /// <summary>
        /// creates a default instance. the instance must be 
        /// configured and initialized before it can be used.
        /// </summary>
        public ConstantIntegerDistribution() { }

        /// <summary>
        /// creates a configured and initialized instance
        /// </summary>
        /// <param name="seed"></param>
        /// <param name="mu"></param>
        /// <param name="sigma"></param>
        /// <param name="antithetic"></param>
        public ConstantIntegerDistribution(int value = 0, bool initialize = true)
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

        public void Configure(int value)
        {
            this.value = value;
            Configured = true;
        }

        #endregion
        #region impl

        public virtual int Next()
        {
            DrawCount++;
            return value;
        }

        #region IDistribution<double>

        double IDistribution<double>.Next()
        {
            return Next();
        }

        #endregion

        #endregion
        #region rset

        public void Reset() { DrawCount = 0; }

        public void Reset(int seed) { Reset(); }

        public void Reset(int seed, bool antithetic) { Reset(); }

        public void Reset(bool antithetic) { Reset(); }

        #endregion
    }
}