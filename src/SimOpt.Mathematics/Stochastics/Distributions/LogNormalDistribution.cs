using System;
using SimOpt.Mathematics.Stochastics.Interfaces;
using SimOpt.Mathematics.Stochastics.Distributions;

namespace SimOpt.Mathematics.Stochastics.Distributions
{
	/// <summary>
	/// LOGNORMAL DISTRIBUTION [0;inf)
	/// the log-normal distribution is the single-tailed probability distribution
	/// of any random variable whose logarithm is normally distributed.
	/// </summary>
    public class LogNormalDistribution : IDistribution<double>
	{
		#region cvar
		
		private GaussianDistribution dblGaussian = null!;
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

        /// <summary>
        /// The value substituted for a draw in a deterministic run — the mean, shift included.
        /// </summary>
        public double NonStochasticValue
        {
            get
            {
                return Mean;
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

        /// <summary>
        /// The expected value of this distribution, <b>including</b> the shift.
        /// </summary>
        /// <remarks>
        /// SIM-102: the shift is read through rather than folded in at configure time, because
        /// <see cref="Shift"/> is publicly settable and a mean computed once would go stale the
        /// moment anyone used it. Before this, <c>Next()</c> added the shift and <c>Mean</c> did
        /// not, so a distribution drawing around 8.0 reported 3.0.
        /// </remarks>
        public double Mean
        {
            get { return mean + shift; }
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
		/// <remarks>
		/// SIM-103: the internal Gaussian is created here rather than left null. Every
		/// <c>Configure</c> overload dereferences it immediately, so without this the parameterless
		/// constructor produced an object on which the only useful methods threw
		/// <c>NullReferenceException</c> — and since <c>LogNormalDistribution(mean, stddev, shift)</c>
		/// chains to this constructor, that public constructor threw on every call. It also closed
		/// the configure-then-seed order that <c>Random&lt;T&gt;</c> requires, which is the only way
		/// a distribution reaches a simulation model.
		/// </remarks>
		public LogNormalDistribution()
		{
			this.dblGaussian = new GaussianDistribution();
		}
		
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

        /// <summary>
        /// Seeds this distribution, preserving whatever it was configured with.
        /// </summary>
        /// <remarks>
        /// SIM-103: this used to <em>replace</em> the internal Gaussian, discarding the mu and sigma
        /// that <c>Configure</c> had just set and silently reverting to the standard lognormal.
        /// <c>Random&lt;T&gt;</c> configures first and initialises second, so on the engine's own
        /// path the configuration was always thrown away — and unlike the constructor defect this
        /// one produced numbers rather than an exception.
        /// </remarks>
        public void Initialize(int seed, bool antithetic = false)
        {
            dblGaussian.Initialize(seed, antithetic);
        }

        public void Initialize(IRandomSource rnd)
        {
            dblGaussian.Initialize(rnd);
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