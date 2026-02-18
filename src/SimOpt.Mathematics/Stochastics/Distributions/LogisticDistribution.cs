using System;
using SimOpt.Mathematics.Stochastics.Interfaces;
using SimOpt.Mathematics.Stochastics.RandomSources;

namespace SimOpt.Mathematics.Stochastics.Distributions
{
	/// <summary>
	/// LOGISTIC DISTRIBUTION (-inf;inf)
	/// </summary>
    public class LogisticDistribution : IDistribution<double>
	{
		#region cvar
		
		private double mu = 0;
		private double s = 1;
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
                return "Logistic Distribution";
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

        public double NonStochasticValue
        {
            get
            {
                return mu;
            }
        }

        public double Mean
        {
            get
            {
                return mu;
            }
        }

		/// <summary>
		/// Get location parameter mu
		/// </summary>
		public double Mu { get { return mu; } }
		
		/// <summary>
		/// Get scale paramter s
		/// </summary>
		public double Sigma { get { return s; } }
		
		#endregion
		#region ctor
		
		/// <summary>
		/// empty constructor
		/// </summary>
		public LogisticDistribution() { }
		
		/// <summary>
		/// Constructor using random generator
		/// </summary>
		/// <param name="rnd"> random generator </param>
        public LogisticDistribution(IRandomSource rnd)
        {
            Initialize(rnd);
		}
		
		/// <summary>
		/// Constructor using seed and antithetic parameter
		/// </summary>
		/// <param name="seed"> random number seed </param>
		/// <param name="antithetic"> antithetic parameter </param>
		public LogisticDistribution(int seed, bool antithetic)
		{
            Initialize(seed, antithetic);
		}
		
		/// <summary>
		/// Constructor using mean and standard deviation
		/// </summary>
		/// <param name="mean"> mean </param>
		/// <param name="stddev"> standard deviation </param>
		public LogisticDistribution(double mean, double stddev) : this()
        {
			ConfigureMean(mean, stddev);
		}
		
		/// <summary>
		/// Constructor using mean, standard deviation and random
		/// generator
		/// </summary>
		/// <param name="rnd"> random generator </param>
		/// <param name="mean"> mean </param>
		/// <param name="stddev"> standard deviation </param>
        public LogisticDistribution(IRandomSource rnd, double mean, double stddev)
            : this(rnd)
        {
			ConfigureMean(mean, stddev);
		}
		
		/// <summary>
		/// Constructor using mean, standard deviation, seed and
		/// antithetic parameter
		/// </summary>
		/// <param name="seed"> random number seed </param>
		/// <param name="mean"> mean </param>
		/// <param name="stddev"> standard deviation </param>
		/// <param name="antithetic"> antithetic parameter </param>
		public LogisticDistribution(int seed, double mean, double stddev, bool antithetic) : this(seed, antithetic) 
        {
			ConfigureMean(mean, stddev);
		}
		
		#endregion
        #region init

        public void Initialize(int seed, bool antithetic)
        {
            rnd = new MersenneTwister(seed, antithetic);
        }

        public void Initialize(IRandomSource rnd)
        {
            this.rnd = rnd;
        }

        #endregion
        #region conf

        /// <summary>
        /// Parametrize the location parameter mu and scale parameter s
        /// Caution! s must be > 0, otherwise an ArgumentOutOfRangeException will be thrown.
        /// </summary>
        /// <param name="mu"> location parameter </param>
        /// <param name="s"> scale parameter > 0 </param> </param>
        public void Configure(double mu, double s)
        {
            if (s <= 0) throw new ArgumentOutOfRangeException("s", "s must be > 0");
            this.mu = mu;
            this.s = s;

            Configured = true;
        }

        /// <summary>
        /// Parameterization of mean and standard deviation parameters
        /// Caution! standard deviation must be > 0,
        /// otherwise an ArgumentOutOfRangeException will be thrown.
        /// </summary>
        /// <param name="mean"> expected value </> </param>
        /// <param name="stddev"> standard deviation > 0 </param> </param>
        public void ConfigureMean(double mean, double stddev)
        {
            if (stddev <= 0) throw new ArgumentOutOfRangeException("stddev", "stddev must be > 0");
            this.mu = mean;
            this.s = Math.Sqrt(3) * stddev / Math.PI;

            Configured = true;
        }

        #endregion
		#region impl
		
		/// <summary>
		/// generate the next logistic distributed random number
		/// </summary>
		/// <returns> random number of logistic distribution </returns>
		public double Next()
		{
            DrawCount++;
			double rndVal = rnd.NextDouble();
			return mu + s * Math.Log(rndVal / (1 - rndVal));
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