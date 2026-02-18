using System;
using MatthiasToolbox.Mathematics.Stochastics.Interfaces;
using MatthiasToolbox.Mathematics.Stochastics.Distributions;

namespace MatthiasToolbox.Mathematics.Stochastics.Distributions
{
	/// <summary>
	/// LOGNORMAL DISTRIBUTION [0;inf)
	/// the log-normal distribution is the single-tailed probability distribution
	/// of any random variable whose logarithm is normally distributed.
	/// </summary>
    public class LogNormalDistribution : IDistribution<double>
	{
		#region cvar
		
		private GaussianDistribution dblGaussian;
		private double shift = 0;
		private double mean = Math.Exp(0.5);
		
		#endregion
		#region prop
		
        public string Name
        {
            get
            {
                return "Log Normal Distribution";
            }
        }

        public bool Initialized
        {
            get
            {
                return dblGaussian != null && dblGaussian.Initialized;
            }
        }

        public bool Configured { get; set; }

        public double NonStochasticValue
        {
            get
            {
                return mean;
            }
        }

		/// <summary>
		/// Get and set seed of internal gaussian distribution
		/// </summary>
        public int? Seed
        {
            get { return dblGaussian.Seed; }
            set { dblGaussian.Seed = value; }
        }
		
		/// <summary>
		/// Get antithetic parameter
		/// </summary>
        public bool Antithetic
        {
            get
            {
                return dblGaussian.Antithetic;
            }
            set
            {
                dblGaussian.Antithetic = value;
            }
        }

        /// <summary>
        /// Get or set the number of values which have been
        /// drawn from this distribution since the last reset.
        /// </summary>
        public int DrawCount { get; set; }

        public double Mean
        {
            get { return mean; }
        }
		
		/// <summary>
		/// Get mean mu of internal gaussian distribution
		/// </summary>
		public double Mu { get { return dblGaussian.Mean; } }
		
		/// <summary>
		/// Get standard deviation sigma of internal gaussian distribution
		/// </summary>
		public double Sigma { get { return dblGaussian.Sigma; }	}

		/// <summary>
		/// Get internal gaussian distribution
		/// </summary>
		public GaussianDistribution DblGaussian { get { return dblGaussian; } }
		
		/// <summary>
		/// Get and set shift parameter
		/// </summary>
		public double Shift {
			get { return this.shift; }
			set { this.shift = value; }
		}
		
		#endregion
		#region ctor
		
		/// <summary>
		/// empty constructor
		/// </summary>
		public LogNormalDistribution() { }
		
		/// <summary>
		/// Constructor using random generator
		/// </summary>
		/// <param name="rnd"> random generator </param>
        public LogNormalDistribution(IRandomSource rnd)
		{
			this.dblGaussian = new GaussianDistribution(rnd);
		}

		/// <summary>
		/// Constructor using seed and antithetic parameter
		/// </summary>
		/// <param name="seed"> random number seed </param>
		/// <param name="antithetic"> antithetic parameter </param>
		public LogNormalDistribution(int seed, bool antithetic)
		{
			this.dblGaussian = new GaussianDistribution(seed, antithetic: antithetic);
		}
		
		/// <summary>
		/// Constructor using mean and standard deviation and shift parameter
		/// </summary>
		/// <param name="mean"> mean </param>
		/// <param name="stddev"> standard deviation </param>
		/// <param name="shift"> shift parameter </param>
		public LogNormalDistribution(double mean, double stddev, double shift = 0d) : this()
        {
			ConfigureMean(mean, stddev, shift);
		}
		
		/// <summary>
		/// Constructor using mean, standard deviation and random
		/// generator and shift parameter
		/// </summary>
		/// <param name="rnd"> random generator </param>
		/// <param name="mean"> mean </param>
		/// <param name="stddev"> standard deviation </param>
		/// <param name="shift"> shift parameter </param>
        public LogNormalDistribution(IRandomSource rnd, double mean, double stddev, double shift = 0d)
            : this(rnd)
        {
            ConfigureMean(mean, stddev, shift);
		}
		
		/// <summary>
		/// Constructor using mean, standard deviation and seed
		/// and antithetic parameter and shift parameter
		/// </summary>
		/// <param name="seed"> random number seed </param>
		/// <param name="mean"> mean </param>
		/// <param name="stddev"> standard deviation </param>
		/// <param name="antithetic"> antithetic parameter </param>
		/// <param name="shift"> shift parameter </param>
        public LogNormalDistribution(int seed, double mean, double stddev, bool antithetic, double shift = 0d)
            : this(seed, antithetic)
        {
            ConfigureMean(mean, stddev, shift);
        }
		
		#endregion
        #region init

        public void Initialize(int seed, bool antithetic = false)
        {
            dblGaussian = new GaussianDistribution(seed, antithetic: antithetic);
        }

        public void Initialize(IRandomSource rnd)
        {
            dblGaussian = new GaussianDistribution(rnd);
        }

        public void Initialize(int seed, double mu = 0d, double sigma = 1d, bool antithetic = false)
        {
            Initialize(seed, antithetic);
            Configure(mu, sigma);
        }

        public void Initialize(IRandomSource rnd, double mu = 0d, double sigma = 1d)
        {
            Initialize(rnd);
            Configure(mu, sigma);
        }

        #endregion
        #region conf

        /// <summary>
        /// Parametrize the mean mu and standard deviation sigma
        /// of the internal gaussian distribution
        /// Caution! sigma must be > 0, otherwise an ArgumentOutOfRangeException will be thrown.
        /// </summary>
        /// <param name="mu"> mean </param>
        /// <param name="sigma"> standard deviation > 0 </param> </param>
        public void Configure(double mu, double sigma, double shift = 0d)
        {
            if (sigma <= 0) throw new ArgumentOutOfRangeException("sigma", "sigma must be > 0");
            dblGaussian.Configure(mu, sigma);
            this.mean = Math.Exp(mu + Math.Pow(sigma, 2) / 2);
			this.shift = shift;

            Configured = true;
        }

        /// <summary>
        /// Parameterization of mean and standard deviation parameters
        /// Caution! standard deviation must be > 0 and mean must not be 0,
        /// otherwise an ArgumentOutOfRangeException will be thrown.
        /// </summary>
        /// <param name="mean"> expected value <> 0 </> </param>
        /// <param name="stddev"> standard deviation > 0 </param> </param>
        public void ConfigureMean(double mean, double stddev, double shift = 0d)
        {
            if (stddev <= 0) throw new ArgumentOutOfRangeException("stddev", "stddev must be > 0");
            if (mean == 0) throw new ArgumentOutOfRangeException("mean", "mean must not be 0");
            double p1 = mean * mean;
            double p2 = stddev * stddev + p1;
            double mu = Math.Log(p1 / Math.Sqrt(p2));
            double sigma = Math.Sqrt(Math.Log(p2 / p1));
            dblGaussian.Configure(mu, sigma);
            this.mean = mean;
			this.shift = shift;

            Configured = true;
        }

        #endregion
        #region impl

        /// <summary>
		/// generate the next lognormal distributed random number
		/// </summary>
		/// <returns> random number of lognormal distribution </returns>
		public double Next()
		{
            DrawCount++;
			return shift + Math.Exp(dblGaussian.Next());
		}
		
		#endregion
        #region rset

        public void Reset()
        {
            DrawCount = 0;
            dblGaussian.Reset();
        }

        public void Reset(int seed)
        {
            DrawCount = 0;
            dblGaussian.Reset(seed);
        }

        public void Reset(int seed, bool antithetic)
        {
            DrawCount = 0;
            dblGaussian.Reset(seed, antithetic);
        }

        public void Reset(bool antithetic)
        {
            DrawCount = 0;
            dblGaussian.Reset(antithetic);
        }

        #endregion
	}
}