using System;
using MatthiasToolbox.Mathematics.Stochastics.Interfaces;

namespace MatthiasToolbox.Mathematics.Stochastics.Distributions
{
	/// <summary>
	/// BINOMIAL DISTRIBUTION {0,...,trials}
	/// generates an integer binomial distributed random number. The binomial
	/// distribution is the discrete probability distribution of the number of
	/// successes in a sequence of "trials" independent yes/no experiments, each
	/// of which yields success with probability "prob". Such a success/failure
	/// experiment is also called a Bernoulli experiment or Bernoulli trial.
	/// In fact, when "trials" = 1, the binomial distribution is a Bernoulli
	/// distribution.
	/// </summary>
    public class BinomialDistribution : IDistribution<int>, IDistribution<double>
	{
		#region cvar
		
		private int trials = 1;
		private int mean = 0;
		private BernoulliDistribution intBernoulli;
		
		#endregion
		#region prop

		public string Name
		{
			get
			{
				return "Binominal Distribution";
			}
		}

		public bool Initialized
		{
			get
			{
				return (intBernoulli != null && intBernoulli.Initialized);
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
		/// Get and set seed of internal Bernoulli distribution
		/// </summary>
		public int? Seed {
			get {
				return intBernoulli.Seed;
			}
			set {
				intBernoulli.Seed = value;
			}
		}
		
		/// <summary>
		/// Get antithetic parameter
		/// </summary>
		public bool Antithetic {
			get {
				return intBernoulli.Antithetic;
			}
			set {
				intBernoulli.Antithetic = value;
			}
		}
		
		public int Mean
		{
			get { return mean; }
		}
		
		/// <summary>
		/// Get success probability
		/// </summary>
		public double Probability {	get { return intBernoulli.Probability; }	}
		
		/// <summary>
		/// Get number of trials
		/// </summary>
		public int Trials {	get { return trials; } }
		
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
		public BinomialDistribution() { }
		
		/// <summary>
		/// Constructor using random generator
		/// </summary>
		/// <param name="seed"> random generator </param>
		public BinomialDistribution(IRandomSource rnd)
		{
			Initialize(rnd);
		}
		
		/// <summary>
		/// Constructor using seed and antithetic parameter
		/// </summary>
		/// <param name="seed"> random number seed </param>
		/// <param name="antithetic"> antithetic parameter </param>
		public BinomialDistribution(int seed, bool antithetic)
		{
			Initialize(seed, antithetic);
		}
		
		/// <summary>
		/// Constructor using probability and trials
		/// </summary>
		/// <param name="probabiltiy"> success probability </param>
		/// <param name="trials"> number of trials </param>
		public BinomialDistribution(double probabiltiy, int trials) : this()
		{
			Configure(probabiltiy, trials);
		}
		
		/// <summary>
		/// Constructor using probability, trials and random generator
		/// </summary>
		/// <param name="rnd"> random generator </param>
		/// <param name="probabiltiy"> success probability </param>
		/// <param name="trials"> number of trials </param>
		public BinomialDistribution(IRandomSource rnd, double probabiltiy, int trials)
			: this(rnd)
		{
			Configure(probabiltiy, trials);
		}
		
		/// <summary>
		/// Constructor using probability, trials and seed and
		/// antithetic parameter
		/// </summary>
		/// <param name="seed"> random number seed </param>
		/// <param name="probabiltiy"> success probability </param>
		/// <param name="trials"> number of trials </param>
		/// <param name="antithetic"> antithetic parameter </param>
		public BinomialDistribution(int seed, double probabiltiy, int trials, bool antithetic) : this(seed, antithetic)
		{
			Configure(probabiltiy, trials);
		}
		
		#endregion
		#region init

		public void Initialize(int seed, bool antithetic)
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
		/// Parameterize probabiltiy and trials
		/// Caution! probability must be between 0 and 1 and trials must be >= 0,
		/// otherwise an ArgumentOutOfRangeException will be thrown.
		/// </summary>
		/// <param name="probability"> success probability (0 - 1) </param>
		/// <param name="trials"> number of trials >=0 </param> </param>
		public void Configure(double probability, int trials)
		{
			if (trials < 0) throw new ArgumentOutOfRangeException("trials", "trials must be >= 0");
			intBernoulli.Configure(probability);
			this.trials = trials;
			this.mean = trials * (int)probability;
			Configured = true;
		}

		#endregion
		#region impl

		/// <summary>
		/// generate next binomial distributed random number
		/// </summary>
		/// <returns> random number of binomial distribution </returns>
		public int Next()
		{
            DrawCount++;

			int res = 0;
			
			for (int count = 1; count <= this.trials; count++)
			{
				res += intBernoulli.Next();
			}

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