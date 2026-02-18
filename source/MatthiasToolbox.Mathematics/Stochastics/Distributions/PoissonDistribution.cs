using System;
using MatthiasToolbox.Mathematics.Stochastics.Interfaces;
using MatthiasToolbox.Mathematics.Stochastics.RandomSources;

namespace MatthiasToolbox.Mathematics.Stochastics.Distributions
{
	/// <summary>
	/// POISSON DISTRIBUTION {0,1,2,...}
	/// generate an integer precision poisson distributed random number using the mean.
	/// The Poisson distribution is a discrete probability distribution that expresses
	/// the probability of a number of events occurring in a fixed period of time if these
	/// events occur with a known average rate and independently of the time since
	/// the last event.
	/// </summary>
    public class PoissonDistribution : IDistribution<int>, IDistribution<double>
	{
		#region cvar
		
		private double lambda = 1d;
        private IRandomSource rnd;
		
		#endregion
		#region prop

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

        public string Name
        {
            get
            {
                return "Poisson Distribution";
            }
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
        /// Get or set the number of values which have been
        /// drawn from this distribution since the last reset.
        /// </summary>
        public int DrawCount { get; set; }

        /// <summary>
        /// The exact value is returned by (this as IDistribution&lt;double>).NonStochasticValue
        /// </summary>
        public int NonStochasticValue
        {
            get
            {
                return (int)Math.Round(lambda);
            }
        }

        /// <summary>
        /// Caution: this is round(lambda).
        /// The exact value is returned by (this as IDistribution&lt;double>).Mean
        /// </summary>
        public int Mean
        {
            get
            {
                return (int)Math.Round(lambda);
            }
        }

		/// <summary>
		/// Get mean lambda
		/// </summary>
		public double Lambda { get { return lambda; } }

        #region IDistribution<double>

        double IDistribution<double>.NonStochasticValue
        {
            get { return lambda; }
        }

        double IDistribution<double>.Mean
        {
            get { return lambda; }
        }

        #endregion

		#endregion
		#region ctor
		
		/// <summary>
		/// empty constructor
		/// </summary>
		public PoissonDistribution() { }
		
		/// <summary>
		/// Constructor using random generator
		/// </summary>
		/// <param name="rnd"> random generator </param>
        public PoissonDistribution(IRandomSource rnd)
        {
            Initialize(rnd);
		}
		
		/// <summary>
		/// Constructor using seed and antithetic parameter
		/// </summary>
		/// <param name="seed"> random number seed </param>
		/// <param name="antithetic"> antithetic parameter </param>
		public PoissonDistribution(int seed, bool antithetic)
        {
            Initialize(seed, antithetic);
		}
		
		/// <summary>
		/// Constructor using mean
		/// </summary>
		/// <param name="mean"> mean </param>
		public PoissonDistribution(double mean) : this()
		{
			Configure(mean);
		}
		
		/// <summary>
		/// Constructor using mean and random generator
		/// </summary>
		/// <param name="rnd"> random generator </param>
		/// <param name="mean"> mean </param>
        public PoissonDistribution(IRandomSource rnd, double mean)
            : this(rnd)
		{
            Initialize(rnd);
            Configure(mean);
		}
		
		/// <summary>
		/// Constructor using mean and seed and antithetic
		/// parameter
		/// </summary>
		/// <param name="seed"> random number seed </param>
		/// <param name="mean"> mean </param>
		/// <param name="antithetic"> antithetic parameter </param>
		public PoissonDistribution(int seed, double mean, bool antithetic) : this(seed, antithetic)
		{
            Initialize(seed, antithetic);
            Configure(mean);
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

        public void Initialize(int seed, double lambda = 0d, bool antithetic = false)
        {
            Initialize(seed, antithetic);
            Configure(lambda);
        }

        public void Initialize(IRandomSource rnd, double lambda = 0d)
        {
            Initialize(rnd);
            Configure(lambda);
        }

        #endregion
        #region conf

        public void Configure(double lambda = 0d)
        {
            this.lambda = lambda;

            Configured = true;
        }

        #endregion
        #region impl

        /// <summary>
        /// Generate next poisson distributed random number
        /// </summary>
        /// <returns> random number of poisson distribution </returns>
		public int Next()
		{
            DrawCount++;

            double a, b;
            int res = 0;
            a = Math.Exp(-this.lambda);
            b = 1;
            do
            {
                b *= rnd.NextDouble();
                res += 1;
            } while (b >= a);
            return res - 1;
		}

        #region IDistribution<double>

        double IDistribution<double>.Next()
        {
            return Next();
        }

        #endregion

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