using System;
using MatthiasToolbox.Mathematics.Stochastics.Interfaces;
using MatthiasToolbox.Mathematics.Stochastics.RandomSources;

namespace MatthiasToolbox.Mathematics.Stochastics.Distributions
{
    public class TriangularDistribution : IDistribution<double>
    {
        #region cvar

        private double minimum = 0;
        private double mode = 0.5;
        private double maximum = 1;
        private double mean = 0.5; // 1.5 / 3
        private IRandomSource rnd;

        #endregion
        #region prop

        public string Name
        {
            get { return "TriangularDistribution"; }
        }

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
                rnd.Reset((int)value);
            }
        }

        /// <summary>
        /// CAUTION: setting the value will reset the random number stream
        /// </summary>
        public bool Antithetic
        {
            get { return rnd.Antithetic; }
            set { rnd.Reset(value); }
        }

        public bool Initialized
        {
            get { return (rnd != null && rnd.Initialized); }
        }

        public bool Configured { get; private set; }

        /// <summary>
        /// Get or set the number of values which have been
        /// drawn from this distribution since the last reset.
        /// </summary>
        public int DrawCount { get; set; }

        public double NonStochasticValue
        {
            get { return mean; }
        }

        public double Mean
        {
            get { return mean; }
        }

        /// <summary>
        /// Get minimum value
        /// </summary>
        public double Minimum { get { return minimum; } }

        /// <summary>
        /// Get maximum value
        /// </summary>
        public double Maximum { get { return maximum; } }

        /// <summary>
        /// Get mode value
        /// </summary>
        public double Mode { get { return mode; } }

        #endregion
        #region ctor

        /// <summary>
        /// creates a default instance. the instance must be initialized before it can be used.
        /// </summary>
        public TriangularDistribution() { }

        /// <summary>
        /// creates a mersenne twister based, initialized instance, ready for immediate use
        /// </summary>
        /// <param name="seed"> random number seed </param>
        /// <param name="minimum"> lower limit </param>
        /// <param name="mode"> mode </param>
        /// <param name="maximum"> upper limit </param>
        /// <param name="antithetic"> antithetic parameter </param>
        public TriangularDistribution(int seed, bool antithetic = false)
        {
            Initialize(seed, antithetic);
        }
        
        /// <summary>
        /// creates a mersenne twister based, configured instance
        /// a seed has to be set before it can be used.
        /// </summary>
        /// <param name="minimum"> lower limit </param>
        /// <param name="mode"> mode </param>
        /// <param name="maximum"> upper limit </param>
        public TriangularDistribution(double minimum = 0, double mode = 0.5, double maximum = 1) : this()
        {
            Configure(minimum, mode, maximum);
        }

        /// <summary>
        /// creates a mersenne twister based, initialized instance, ready for immediate use
        /// </summary>
        /// <param name="seed"> random number seed </param>
        /// <param name="minimum"> lower limit </param>
        /// <param name="mode"> mode </param>
        /// <param name="maximum"> upper limit </param>
        /// <param name="antithetic"> antithetic parameter </param>
        public TriangularDistribution(int seed, double minimum = 0, double mode = 0.5, double maximum = 1, bool antithetic = false)
        {
            Initialize(seed, minimum, mode, maximum, antithetic);
        }

        /// <summary>
        /// creates an initialized instance, ready for immediate use
        /// </summary>
        /// <param name="rnd"></param>
        /// <param name="minimum"></param>
        /// <param name="mode"></param>
        /// <param name="maximum"></param>
        public TriangularDistribution(IRandomSource rnd, double minimum = 0, double mode = 0.5, double maximum = 1)
        {
            Initialize(rnd, minimum, mode, maximum);
        }
        
        #endregion
        #region init

        public void Initialize(int seed, bool antithetic)
        {
            this.rnd = new MersenneTwister(seed, antithetic); 
        }

        public void Initialize(IRandomSource rnd)
        {
            this.rnd = rnd;
        }

        public void Initialize(int seed, double minimum = 0, double mode = 0.5, double maximum = 1, bool antithetic = false)
        {
            Initialize(seed, antithetic);
            Configure(minimum, mode, maximum);
        }

        public void Initialize(IRandomSource rnd, double minimum = 0, double mode = 0.5, double maximum = 1)
        {
            Initialize(rnd);
            Configure(minimum, mode, maximum);
        }

        #endregion
        #region conf

		/// <summary>
		/// Parameterization using lower limit, mode and upper limit
		/// Caution! maximum must be > minimum and mode must be between minimum and maximum,
		/// otherwise an ArgumentOutOfRangeException will be thrown.
		/// </summary>
		/// <param name="minimum"> lower limit </param>
		/// <param name="mode"> mode (min <= mode <= max) </param>
		/// <param name="maximum"> upper limit </param>
        /// <remarks>
        /// old version:
        /// 
        /// public void ParametrizeUsingMinModeMax(double minimum, double mode, double maximum)
        /// {
        ///     if(maximum &lt;= minimum) throw new ArgumentOutOfRangeException("maximum", "maximum must be > minimum");
        ///     if(mode &lt; minimum) throw new ArgumentOutOfRangeException("mode", "mode must be >= minimum");
        ///     if(mode > maximum) throw new ArgumentOutOfRangeException("mode", "mode must be &lt;= maximum");
        ///     this.a = minimum;
        ///     this.b = maximum;
        ///     this.c = mode;
        ///     this.mean = (mode + minimum + maximum)/3;
        /// }
		/// </remarks>
        public void Configure(double minimum = 0, double mode = 0.5, double maximum = 1)
        {
            if (maximum <= minimum) throw new ArgumentOutOfRangeException("maximum", "maximum must be > minimum");
            if (mode < minimum) throw new ArgumentOutOfRangeException("mode", "mode must be >= minimum");
            if (mode > maximum) throw new ArgumentOutOfRangeException("mode", "mode must be <= maximum");

            this.minimum = minimum;
            this.mode = mode;
            this.maximum = maximum;
            this.mean = (mode + minimum + maximum) / 3;

            Configured = true;
        }

        /// <summary>
        /// Parameterization of limit parameters and mean
        /// Caution! maximum must be > minimum and mean must be between minimum and maximum,
        /// otherwise an ArgumentOutOfRangeException will be thrown.
        /// </summary>
        /// <param name="mean"> expected value (min < mean < max) </param>
        /// <param name="minimum"> lower limit </param>
        /// <param name="maximum"> upper limit </param>
        /// <remarks>
        /// old version:
        /// 
        /// public void ParametrizeMeanMinMax(double mean, double minimum, double maximum)
        /// {
        ///     if (maximum &lt;= minimum) throw new ArgumentOutOfRangeException("maximum", "maximum must be > minimum");
        ///     if (mean &lt;= minimum) throw new ArgumentOutOfRangeException("mean", "mean must be > minimum");
        ///     if (mean >= maximum) throw new ArgumentOutOfRangeException("mean", "mean must be &lt; maximum");
        ///     this.a = minimum;
        ///     this.b = maximum;
        ///     this.c = 3 * mean - minimum - maximum;
        ///     this.mean = mean;
        /// }
        /// </remarks>
        public void ConfigureMean(double minimum = 0, double mean = 0.5, double maximum = 1)
        {
            if (maximum <= minimum) throw new ArgumentOutOfRangeException("maximum", "maximum must be > minimum");
            if (mean <= minimum) throw new ArgumentOutOfRangeException("mean", "mean must be > minimum");
            if (mean >= maximum) throw new ArgumentOutOfRangeException("mean", "mean must be < maximum");
            if (3 * mean - (minimum + maximum) < minimum) throw new ArgumentOutOfRangeException("The mean is too small for this distribution. Mean must be greater than (2 x minimum + maximum) / 3.");

            this.minimum = minimum;
            this.maximum = maximum;
            this.mean = mean;
            this.mode = 3 * mean - (minimum + maximum);

            Configured = true;
        }

        #endregion
        #region impl

        /// <summary>
        /// generate a triangular distributed random number
        /// </summary>
        /// <returns> random number of triangular distribution</returns>
        /// <remarks>
        /// old version:
        /// 
        /// internal override double NextDoubleStochasticDelegate()
        /// {
        ///     double res;
        ///     double top = (c - a) / (b - a);
        ///     double rndVal = rnd.NextDouble();
        ///     if (rndVal &lt;= top)
        ///     {
        ///         res = Math.Sqrt(rndVal * top);
        ///     }
        ///     else
        ///     {
        ///         res = 1 - Math.Sqrt((1 - top) * (1 - rndVal));
        ///     }
        ///     return a + (res * (b - a));
        /// }
        /// </remarks>
        public double Next()
        {
            DrawCount++;

            double result;
            double top = (maximum - minimum) / (mode - minimum);
            double random = rnd.NextDouble();
            
            if (random <= top)
            {
                result = Math.Sqrt(random * top);
            }
            else
            {
                result = 1d - Math.Sqrt((1d - top) * (1d - random));
            }
            
            return minimum + (result * (mode - minimum));
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