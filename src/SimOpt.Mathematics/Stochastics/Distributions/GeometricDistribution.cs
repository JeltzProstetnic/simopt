using System;
using SimOpt.Mathematics.Stochastics.Interfaces;

namespace SimOpt.Mathematics.Stochastics.Distributions
{
	/// <summary>
	/// GEOMETRIC DISTRIBUTION {0,1,2,3,...}
	/// generate an integer precision geometric distributed random number using the mean or probability.
	/// The geometric distribution describes the probability distribution of the number
	/// Y = X − 1 of failures before the first success, whereas X is bernoulli distributed.
	/// </summary>
    public class GeometricDistribution : IDistribution<int>, IDistribution<double>
	{
		#region cvar
		
		private BernoulliDistribution intBernoulli;
		private int mean = 1;
		
		#endregion
		#region prop
		
        public string Name
        {
            get
            {
                return "Geometric Distribution";
            }
        }

        public bool Initialized
        {
            get
            {
                return intBernoulli != null && intBernoulli.Initialized;
            }
        }

        public bool Configured { get; set; }

        public int NonStochasticValue
        {
            get
            {
                return mean;
            }
        }

        public int Mean
        {
            get
            {
                return mean;
            }
        }

		/// <summary>
		/// Get and set seed of internal Bernoulli distribution
		/// </summary>
		public int? Seed 
        {
			get 
            { 
				return intBernoulli.Seed; 
			}
			set 
            {
				intBernoulli.Seed = value;
			}
		}
		
		/// <summary>
		/// Get antithetic parameter
		/// </summary>
		public bool Antithetic
        {
			get 
            { 
				return intBernoulli.Antithetic; 
			}
			set 
            {
				IntBernoulli.Antithetic = value;
			}
		}
		
		/// <summary>
		/// Get success probability
		/// </summary>
		public double Probability { get { return intBernoulli.Probability; } }

		/// <summary>
		/// Get internal Bernoulli distribution
		/// </summary>
		public BernoulliDistribution IntBernoulli 
        {
			get { return intBernoulli; }
		}

        /// <summary>
        /// Get or set the number of values which have been
        /// drawn from this distribution since the last reset.
        /// </summary>
        public int DrawCount { get; set; }

        #region IDistribution<double>

        double IDistribution<double>.NonStochasticValue
        {
            get { return mean; }
        }

        double IDistribution<double>.Mean
        {
            get { return mean; }
        }

        #endregion

		#endregion
		#region ctor
		
		/// <summary>
		/// empty constructor
		/// </summary>
		public GeometricDistribution() { }
		
		/// <summary>
		/// Constructor using random generator
		/// </summary>
		/// <param name="seed"> random generator </param>
        public GeometricDistribution(IRandomSource rnd)
		{
			this.intBernoulli = new BernoulliDistribution(rnd);
		}
		
		/// <summary>
		/// Constructor using seed and antithetic parameter
		/// </summary>
		/// <param name="seed"> random number seed </param>
		/// <param name="antithetic"> antithetic parameter </param>
		public GeometricDistribution(int seed, bool antithetic)
		{
			this.intBernoulli = new BernoulliDistribution(seed, antithetic);
		}
		
		/// <summary>
		/// Constructor using mean and random generator
		/// </summary>
		/// <param name="rnd"> random generator </param>
		/// <param name="mean"> mean </param>
        public GeometricDistribution(IRandomSource rnd, double mean)
            : this(rnd)
		{
			ConfigureMean(mean);
		}
		
		/// <summary>
		/// Constructor using mean and seed and antithetic parameter
		/// </summary>
		/// <param name="seed"> random number seed </param>
		/// <param name="mean"> mean </param>
		/// <param name="antithetic"> antithetic parameter </param>
		public GeometricDistribution(int seed, double mean, bool antithetic) : this(seed, antithetic)
		{
            ConfigureMean(mean);
		}
		
		#endregion
        #region init

        public void Initialize(int seed, bool antithetic)
        {
            intBernoulli = new BernoulliDistribution(seed, antithetic);
        }

        public void Initialize(IRandomSource rnd)
        {
            intBernoulli = new BernoulliDistribution(rnd);
        }

        #endregion
        #region conf

        /// <summary>
        /// Parameterize mean
        /// Caution! mean must be >= 0,
        /// otherwise an ArgumentOutOfRangeException will be thrown.
        /// <param name="mean"> mean >= 0 </param></param>
        public void ConfigureMean(double mean)
        {
            if (mean < 0) throw new ArgumentOutOfRangeException("mean", "mean must be >= 0");
            intBernoulli.Configure(1 / (1 + mean));
            this.mean = (int)mean;
            Configured = true;
        }

        /// <summary>
        /// Parameterize probabiltiy and trials
        /// Caution! probability must be between 0 and 1,
        /// otherwise an ArgumentOutOfRangeException will be thrown.
        /// </summary>
        /// <param name="probability"> success probability (0 - 1) </param>
        public void Configure(double probability)
        {
            intBernoulli.Configure(probability);
            this.mean = (int)((1 - probability) / probability);
            Configured = true;
        }

        #endregion
        #region impl

        /// <summary>
		/// Generate next geometric distributed random number
		/// </summary>
		/// <returns> random number of geometric distribution </returns>
		public int Next()
		{
            DrawCount++;

			int res = 0;

            do
            {
                res += 1;
            } while (intBernoulli.Next() == 0);
			
            return res;
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
            intBernoulli.Reset();
        }

        public void Reset(int seed)
        {
            DrawCount = 0;
            intBernoulli.Reset(seed);
        }

        public void Reset(int seed, bool antithetic)
        {
            DrawCount = 0;
            intBernoulli.Reset(seed, antithetic);
        }

        public void Reset(bool antithetic)
        {
            DrawCount = 0;
            intBernoulli.Reset(antithetic);
        }

        #endregion
	}
}