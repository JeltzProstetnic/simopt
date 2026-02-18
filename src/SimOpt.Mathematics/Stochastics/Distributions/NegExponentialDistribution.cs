using System;
using System.Collections.Generic;
using SimOpt.Mathematics.Stochastics.Interfaces;
using SimOpt.Mathematics.Stochastics.RandomSources;

namespace SimOpt.Mathematics.Stochastics.Distributions
{
	/// <summary>
	/// NEGATIVE EXPONENTIAL DISTRIBUTION [0;inf)
	/// The exponential distributions are a class of continuous probability distributions.
	/// They describe the times between events in a Poisson process, i.e. a process in which
	/// events occur continuously and independently at a constant average rate.
	/// </summary>
    public class NegExponentialDistribution : IDistribution<double>
	{
		#region cvar
		
		private double lambda = 1d;
		private double mean = 1d;
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
                return "Negative Exponential Distribution";
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
		/// Get rate parameter lambda
		/// </summary>
		public double Lambda { get { return lambda; }	}
		
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
		public NegExponentialDistribution() { }
		
		/// <summary>
		/// Constructor using random number generator
		/// </summary>
		/// <param name="rnd"> random generator </param>
        public NegExponentialDistribution(IRandomSource rnd)
		{
            Initialize(rnd);
		}
		
		/// <summary>
		/// Constructor using seed and antithetic parameter
		/// </summary>
		/// <param name="seed"> random number seed </param>
		/// <param name="antithetic"> antithetic parameter </param>
		public NegExponentialDistribution(int seed, bool antithetic)
		{
            Initialize(seed, antithetic);
		}
		
		/// <summary>
		/// Constructor using rate parameter lambda and
		/// random generator and shift parameter
		/// </summary>
		/// <param name="rnd"> random generator </param>
		/// <param name="lambda"> rate parameter </param>
		/// <param name="shift"> shift parameter </param>
        public NegExponentialDistribution(IRandomSource rnd, double lambda, double shift)
            : this(rnd)
        {
			Configure(lambda, shift);
		}

		/// <summary>
		/// Constructor using rate parameter lambda,
		/// seed and antithetic parameter and shift parameter
		/// </summary>
		/// <param name="seed"> random number seed </param>
		/// <param name="lambda"> rate parameter </param>
		/// <param name="antithetic"> antithetic parameter </param>
		/// <param name="shift"> shift parameter </param>
		public NegExponentialDistribution(int seed, double lambda, bool antithetic, double shift) : this(seed, antithetic)
		{
			Configure(lambda, shift);
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
        /// Parametrize rate parameter lambda
        /// Caution! lambda must be > 0, otherwise an ArgumentOutOfRangeException will be thrown.
        /// </summary>
        /// <param name="lambda"> rate parameter > 0 </param> </param>
        public void Configure(double lambda, double shift = 0d)
        {
            if (lambda <= 0) throw new ArgumentOutOfRangeException("lambda", "lambda must be > 0");
            this.lambda = lambda;
            this.mean = 1 / lambda;
            this.shift = shift;
            Configured = true;
        }

        /// <summary>
        /// Parametrize mean
        /// Caution! mean must be > 0, otherwise an ArgumentOutOfRangeException will be thrown.
        /// </summary>
        /// <param name="mean"> mean > 0 </param></param>
        public void ConfigureMean(double mean, double shift = 0d)
        {
            if (mean <= 0) throw new ArgumentOutOfRangeException("mean", "mean must be > 0");
            this.lambda = 1 / mean;
            this.mean = mean;
            this.shift = shift;
            Configured = true;
        }

        #endregion
		#region impl
		
		/// <summary>
		/// Generate next neg exponential distributed random number
		/// </summary>
		/// <returns> random number of neg exponential distribution </returns>
		public double Next()
		{
            DrawCount++;
			return -(1 / lambda) * Math.Log(rnd.NextDouble()) + shift;
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