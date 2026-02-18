using System;
using SimOpt.Mathematics.Stochastics.Interfaces;
using SimOpt.Mathematics.Stochastics.RandomSources;

namespace SimOpt.Mathematics.Stochastics.Distributions
{
	/// <summary>
	/// NEGATIVE BINOMIAL DISTRIBUTION {0,1,2,...}
	/// generates an integer negative binomial distributed random number.
	/// In probability and statistics the negative binomial distribution is a
	/// discrete probability distribution. It can be used to describe the
	/// distribution arising from an experiment consisting of a sequence of
	/// independent trials, subject to several constraints. Firstly each trial
	/// results in success or failure, the probability of success for each trial
	/// is constant across the experiment and finally the experiment continues
	/// until a fixed number of successes have been achieved. For the special
	/// case where "successes" is an integer, the negative binomial distribution is
	/// known as the Pascal distribution. It is the probability distribution
	/// of a certain number of failures and successes in a series of independent
	/// and identically distributed Bernoulli trials.
	/// </summary>
    public class NegBinomialDistribution : IDistribution<int>, IDistribution<double>
	{
		#region cvar
		
		private int successes = 1;
		private int mean = 1;
		private BernoulliDistribution intBernoulli;
		
		#endregion
		#region prop
		
		public string Name
		{
			get
			{
				return "Negative Binominal Distribution";
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

		/// <summary>
		/// Get and set seed
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
				intBernoulli.Antithetic = value;
			}
		}

		public int Mean
		{
			get { return mean; }
		}
		
		/// <summary>
		/// Get success probabiltiy
		/// </summary>
		public double Probability
		{
			get { return intBernoulli.Probability; }
		}
		
		/// <summary>
		/// Get number of successes
		/// </summary>
		public int Successes {	get { return successes; } }
		
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
		public NegBinomialDistribution() { }
		
		/// <summary>
		/// Constructor using random generator
		/// </summary>
		/// <param name="seed"> random generator </param>
		public NegBinomialDistribution(IRandomSource rnd)
		{
			Initialize(rnd);
		}
		
		/// <summary>
		/// Constructor using seed and antithetic parameter
		/// </summary>
		/// <param name="seed"> random number seed </param>
		/// <param name="antithetic"> antithetic parameter </param>
		public NegBinomialDistribution(int seed, bool antithetic)
		{
			Initialize(seed, antithetic);
		}
		
		/// <summary>
		/// Constructor using probability and successes
		/// </summary>
		/// <param name="probabiltiy"> success probability </param>
		/// <param name="successes"> number of successes </param>
		public NegBinomialDistribution(double probabiltiy, int successes) : this()
		{
			Configure(probabiltiy, successes);
		}
		
		/// <summary>
		/// Constructor using probability, successes and random
		/// generator
		/// </summary>
		/// <param name="rnd"> random generator </param>
		/// <param name="probabiltiy"> success probability </param>
		/// <param name="successes"> number of successes </param>
		public NegBinomialDistribution(IRandomSource rnd, double probabiltiy, int successes)
			: this(rnd)
		{
			Configure(probabiltiy, successes);
		}
		
		/// <summary>
		/// Constructor using probability, successes and seed and
		/// antithetic parameter
		/// </summary>
		/// <param name="seed"> random number seed </param>
		/// <param name="probabiltiy"> success probability </param>
		/// <param name="successes"> number of successes </param>
		/// <param name="antithetic"> antithetic parameter </param>
		public NegBinomialDistribution(int seed, double probabiltiy, int successes, bool antithetic) : this(seed, antithetic)
		{
			Configure(probabiltiy, successes);
		}
		
		#endregion
		#region init

		public void Initialize(int seed, bool antithetic = false)
		{
			this.intBernoulli = new BernoulliDistribution(seed, antithetic);
			intBernoulli.Initialize(seed, antithetic);
		}

		public void Initialize(IRandomSource rnd)
		{
			this.intBernoulli = new BernoulliDistribution(rnd);
			intBernoulli.Initialize(rnd);
		}

		#endregion
		#region conf

		/// <summary>
		/// Parameterize probabiltiy and successes
		/// Caution! probability must be between 0 and 1 and successes must be > 0,
		/// otherwise an ArgumentOutOfRangeException will be thrown.
		/// </summary>
		/// <param name="probability"> success probability (0 - 1) </param>
		/// <param name="successes"> number of successes > 0 </param></param>
		public void Configure(double probability, int successes)
		{
			if (successes <= 0) throw new ArgumentOutOfRangeException("successes", "successes must be > 0");
			intBernoulli.Configure(probability);
			this.successes = successes;
			this.mean = successes * (int)((1 - probability) / probability);

			Configured = true;
		}

		#endregion
		#region impl
		
		/// <summary>
		/// generate next negative binomial distributed random number
		/// </summary>
		/// <returns> random number of negative binomial distribution </returns>
		public int Next()
		{
            DrawCount++;

			int res = 0;
			int count = 0;
			
			do
			{
				if (intBernoulli.Next() == 1)
				{
					count += 1;
				}
				else
				{
					res += 1;
				}
			} while(count < this.successes);

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