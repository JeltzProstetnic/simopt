using MatthiasToolbox.Mathematics.Stochastics.Interfaces;
using MatthiasToolbox.Mathematics.Stochastics.RandomSources;
using System;

namespace MatthiasToolbox.Mathematics.Stochastics.Distributions
{
    [Serializable]
    public class UniformDoubleDistribution : IDistribution<double>
    {
        #region cvar

        private double interval;
        private double min = 0;
        private double max = 1;
        private IRandomSource rnd;

        #endregion
        #region prop

        public string Name
        {
            get { return "Uniform"; }
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
                if(Initialized) return rnd.Seed;
                return null;
            }
            set
            {
                rnd.Reset((int)value);
            }
        }

        /// <summary>
        /// Get or set the number of values which have been
        /// drawn from this distribution since the last reset.
        /// </summary>
        public int DrawCount { get; set; }

        public double NonStochasticValue
        {
            get { return min + interval / 2; }
        }

        public double Minimum
        {
            get { return min; }
        }

        public double Maximum
        {
            get { return max; }
        }

        /// <summary>
        /// CAUTION: setting the value will reset the random number stream
        /// </summary>
        public bool Antithetic
        {
            get { return rnd.Antithetic; }
            set { rnd.Reset(value); }
        }

        public double Mean { get { return (max - min) / 2; } }

        #endregion
        #region ctor

        /// <summary>
        /// creates a default instance. the instance must be initialized before it can be used.
        /// </summary>
        public UniformDoubleDistribution() { }

        /// <summary>
        /// creates a mersenne twister based, initialized instance, ready for immediate use
        /// </summary>
        /// <param name="seed"></param>
        /// <param name="mu"></param>
        /// <param name="sigma"></param>
        /// <param name="antithetic"></param>
        public UniformDoubleDistribution(int seed, bool antithetic = false) 
        {
            Initialize(seed, antithetic);
        }
        
        /// <summary>
        /// creates a mersenne twister based, initialized instance, ready for immediate use
        /// </summary>
        /// <param name="seed"></param>
        /// <param name="mu"></param>
        /// <param name="sigma"></param>
        /// <param name="antithetic"></param>
        public UniformDoubleDistribution(int seed, double min = 0d, double max = 1d, bool antithetic = false) 
        {
            if (min < 0) throw new ArgumentOutOfRangeException("The minimum must not be smaller than zero.");
            if (max <= min) throw new ArgumentOutOfRangeException("The maximum must be greater than the minimum.");
            Initialize(seed, min, max, antithetic);
        }

        /// <summary>
        /// creates an initialized instance using Environment.TickCount as seed, ready for immediate use
        /// </summary>
        /// <param name="rnd"></param>
        /// <param name="mu"></param>
        /// <param name="sigma"></param>
        /// <param name="antithetic"></param>
        public UniformDoubleDistribution(IRandomSource rnd, double min = 0d, double max = 1d) 
        {
            if (min < 0) throw new ArgumentOutOfRangeException("The minimum must not be smaller than zero.");
            if (max <= min) throw new ArgumentOutOfRangeException("The maximum must be greater than the minimum.");
            Initialize(rnd, min, max);
        }

        /// <summary>
        /// creates a configured instance
        /// CAUTION: initialization required before use!
        /// </summary>
        /// <param name="rnd"></param>
        /// <param name="mu"></param>
        /// <param name="sigma"></param>
        /// <param name="antithetic"></param>
        public UniformDoubleDistribution(double min = 0d, double max = 1d)
        {
            if (min < 0) throw new ArgumentOutOfRangeException("The minimum must not be smaller than zero.");
            if (max <= min) throw new ArgumentOutOfRangeException("The maximum must be greater than the minimum.");
            Configure(min, max);
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

        public void Initialize(int seed, double min = 0d, double max = 1d, bool antithetic = false)
        {
            if (min < 0) throw new ArgumentOutOfRangeException("The minimum must not be smaller than zero.");
            if (max <= min) throw new ArgumentOutOfRangeException("The maximum must be greater than the minimum.");
            Initialize(seed, antithetic);
            Configure(min, max);
        }

        public void Initialize(IRandomSource rnd, double min = 0d, double max = 1d)
        {
            if (min < 0) throw new ArgumentOutOfRangeException("The minimum must not be smaller than zero.");
            if (max <= min) throw new ArgumentOutOfRangeException("The maximum must be greater than the minimum.");
            Initialize(rnd);
            Configure(min, max);
        }

        #endregion
        #region conf

        public void Configure(double min = 0d, double max = 1d)
        {
            if (min < 0) throw new ArgumentOutOfRangeException("The minimum must not be smaller than zero.");
            if (max <= min) throw new ArgumentOutOfRangeException("The maximum must be greater than the minimum.");

            this.min = min;
            this.max = max;
            this.interval = max - min;

            Configured = true;
        }

        #endregion
        #region impl

        public double Next()
        {
            DrawCount++;
            return min + (rnd.NextDouble() * interval);
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