using System;
using System.Collections.Generic;
using MatthiasToolbox.Mathematics.Stochastics.Interfaces;
using MatthiasToolbox.Mathematics.Stochastics.RandomSources;

namespace MatthiasToolbox.Mathematics.Stochastics.Distributions
{
	/// <summary>
	/// LOGLOGISTIC DISTRIBUTION [0;inf)
	/// </summary>
    public class LogLogisticDistribution : IDistribution<double>
	{
		#region cvar
		
		private double alpha = 1;
		private double beta = 1;
		private double mean;
        private IRandomSource rnd;
		private double shift = 0;
		
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
                return "Log Logistic Distribution";
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
                return mean;
            }
        }

        public double Mean
        {
            get
            {
                return mean;
            }
        }

		/// <summary>
		/// Get scale parameter alpha
		/// </summary>
		public double Alpha { get { return alpha; } }
		
		/// <summary>
		/// Get shape parameter beta
		/// </summary>
		public double Beta { get { return beta; } }
		
		/// <summary>
		/// Get and set shift parameter
		/// </summary>
		public double Shift
        {
			get { return this.shift; }
			set { this.shift = value; }
		}
		
		#endregion
		#region ctor
		
		/// <summary>
		/// empty constructor
		/// </summary>
		public LogLogisticDistribution() { }
		
		/// <summary>
		/// Constructor using random generator
		/// </summary>
		/// <param name="rnd"> random generator </param>
        public LogLogisticDistribution(IRandomSource rnd)
		{
            Initialize(rnd);
		}
		
		/// <summary>
		/// Constructor using seed and antithetic
		/// </summary>
		/// <param name="seed"> random number seed </param>
		/// <param name="antithetic"> antithetic parameter </param>
		public LogLogisticDistribution(int seed, bool antithetic)
		{
            Initialize(seed, antithetic);
		}
		
		/// <summary>
		/// Constructor using scale parameter and shape parameter
		/// and shift parameter
		/// </summary>
		/// <param name="alpha"> scale parameter </param>
		/// <param name="beta"> shape parameter </param>
		/// <param name="shift"> shift parameter </param>
		public LogLogisticDistribution(double alpha, double beta, double shift = 0d) : this()
        {
			Configure(alpha, beta, shift);
			this.shift = shift;
		}
		
		/// <summary>
		/// Constructor using scale parameter, shape parameter
		/// and random generator and shift parameter
		/// </summary>
		/// <param name="rnd"> random generator </param>
		/// <param name="alpha"> scale parameter </param>
		/// <param name="beta"> shape parameter </param>
		/// <param name="shift"> shift parameter </param>
        public LogLogisticDistribution(IRandomSource rnd, double alpha, double beta, double shift = 0d)
            : this(rnd)
        {
			Configure(alpha, beta, shift);
		}
		
		/// <summary>
		/// Constructor using scale parameter, shape parameter
		/// and seed and antithetic parameter and shift parameter
		/// </summary>
		/// <param name="seed"> random number seed </param>
		/// <param name="alpha"> scale parameter </param>
		/// <param name="beta"> shape parameter </param>
		/// <param name="antithetic"> antithetic parameter </param>
		/// <param name="shift"> shift parameter </param>
		public LogLogisticDistribution(int seed, double alpha, double beta, bool antithetic, double shift = 0d) : this(seed, antithetic) 
        {
			Configure(alpha, beta, shift);
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

        #endregion
        #region conf

        /// <summary>
        /// Parametrize the scale parameter alpha and shape parameter beta
        /// Caution! alpha and beta must be > 0, otherwise an ArgumentOutOfRangeException will be thrown.
        /// </summary>
        /// <param name="alpha"> scale parameter > 0 </param> </param>
        /// <param name="beta"> shape parameter > 0 </param> </param>
        public void Configure(double alpha, double beta, double shift = 0d)
        {
            if (alpha <= 0) throw new ArgumentOutOfRangeException("alpha", "alpha must be > 0");
            if (beta <= 0) throw new ArgumentOutOfRangeException("beta", "beta must be > 0");
            this.alpha = alpha;
            this.beta = beta;
            this.mean = (this.alpha * (Math.PI / this.beta)) / (Math.Sin(Math.PI / this.beta));
            this.shift = shift;
            Configured = true;
        }

        #endregion
		#region impl
		
		/// <summary>
		/// generate the next loglogistic distributed random number
		/// </summary>
		/// <returns> random number of loglogistic distribution </returns>
		public double Next()
		{
            DrawCount++;
			double rndVal = rnd.NextDouble();
			return shift + alpha * Math.Exp((1 / beta ) * Math.Log(rndVal / (1 - rndVal)));
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