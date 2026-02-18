using System;
using MatthiasToolbox.Mathematics.Stochastics.Interfaces;
using MatthiasToolbox.Mathematics.Stochastics.RandomSources;

namespace MatthiasToolbox.Mathematics.Stochastics.Distributions
{
	/// <summary>
	/// BERNOULLI DISTRIBUTION {0,1}
	/// generate a bernoulli distributed discrete random number. Takes value 1 with
	/// success probability prob and value 0 with failure probability 1 - prob
	/// </summary>
    public class BernoulliDistribution : IDistribution<int>, IDistribution<double>
	{
		#region cvar
		
		private double prob = 0.5d;
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
                return "Bernoulli Distribution";
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

        public int NonStochasticValue
        {
            get;
            set;
        }

        public int Mean
        {
            get;
            set;
        }

		/// <summary>
		/// Get success probability (0..1)
		/// </summary>
		public double Probability {	get { return prob; } }

        /// <summary>
        /// Get or set the number of values which have been
        /// drawn from this distribution since the last reset.
        /// </summary>
        public int DrawCount { get; set; }

        #region IDistribution<double>

        /// <summary>
        /// ManualNonStochasticValue must be set for this to work.
        /// </summary>
        double IDistribution<double>.NonStochasticValue
        {
            get { return ManualNonStochasticValue; }
        }

        /// <summary>
        /// ManualMeanValue must be set for this to work.
        /// </summary>
        double IDistribution<double>.Mean
        {
            get { return ManualMeanValue; }
        }

        /// <summary>
        /// This will be used in (this as IDistribution&lt;double>).NonStochasticValue
        /// </summary>
        public double ManualNonStochasticValue { get; set; }

        /// <summary>
        /// This will be used in (this as IDistribution&lt;double>).Mean
        /// </summary>
        public double ManualMeanValue { get; set; }
        
        #endregion

		#endregion
		#region ctor

        /// <summary>
        /// empty constructor
        /// </summary>
        public BernoulliDistribution() { }

        /// <summary>
        /// Constructor using random generator
        /// </summary>
        /// <param name="rnd"> random generator </param>
        public BernoulliDistribution(IRandomSource rnd)
        {
            this.rnd = rnd;
        }

        /// <summary>
        /// Constructor using seed and antithetic parameter
        /// </summary>
        /// <param name="seed"> random number seed </param>
        /// <param name="antithetic"> antithetic parameter </param>
        public BernoulliDistribution(int seed, bool antithetic)
        {
            this.rnd = new MersenneTwister(seed, antithetic);
        }
		
		/// <summary>
		/// Constructor using probability
		/// </summary>
		/// <param name="probability"> success probability (0..1)</param>
		public BernoulliDistribution(double probability) : this()
		{
			Configure(probability);
		}
		
		/// <summary>
		/// Constructor using probability and random generator
		/// </summary>
		/// <param name="rnd"> random generator </param>
        /// <param name="probabiltiy"> success probabilty  (0..1)</param>
        public BernoulliDistribution(IRandomSource rnd, double probabiltiy)
             : this(rnd)
		{
            Configure(probabiltiy);
		}
		
		/// Constructor using probability and seed and antithetic
		/// parameter
		/// </summary>
		/// <param name="seed"> random number seed </param>
        /// <param name="probabiltiy"> success probabilty  (0..1)</param>
		/// <param name="antithetic"> antithetic parameter </param>
		public BernoulliDistribution(int seed, double probabiltiy, bool antithetic) : this(seed, antithetic)
		{
            Configure(probabiltiy);
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
        /// Parameterize probabiltiy
        /// Caution! probability must be between 0 and 1, otherwise an ArgumentOutOfRangeException will be thrown.
        /// </summary>
        /// <param name="probability"> success probabiltiy (0 - 1) </param>
        public void Configure(double probability = 0.5d)
        {
            if (probability < 0) throw new ArgumentOutOfRangeException("probability", "probability must be between 0 and 1");
            if (probability > 1) throw new ArgumentOutOfRangeException("probability", "probability must be between 0 and 1");

            this.prob = probability;

            Configured = true;
        }

        #endregion
        #region impl

        /// <summary>
        /// Generate next bernoulli distributed random number
        /// </summary>
        /// <returns> random number of bernoulli distribution </returns>
        public int Next()
        {
            DrawCount++;

            if (rnd.NextDouble() < this.prob)
            {
                return 1;
            }
            else
            {
                return 0;
            }
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