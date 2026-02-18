using System;
using System.Collections.Generic;
using SimOpt.Mathematics.Stochastics.Interfaces;
using SimOpt.Mathematics;
using SimOpt.Mathematics.Stochastics.RandomSources;

namespace SimOpt.Mathematics.Stochastics.Distributions
{
	/// <summary>
	/// WEIBULL DISTRIBUTION [0;inf)
	/// generate a double precision weibull distributed random number using
	/// scale parameter lambda and shape parameter k
	/// </summary>
    public class WeibullDistribution : IDistribution<double>
	{
		#region cvar
		
		private double lambda = 0.5;
		private double k = 2;
		private double mean = (0.5 * MMath.Gamma(1d / 2d)) / 2d;
        private IRandomSource rnd;
		private double shift = 0;
		
		#endregion
		#region prop

        #region IDistribution

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
                return "Weibull Distribution";
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

        #endregion
		
		/// <summary>
		/// Get scale parameter lambda
		/// </summary>
		public double Lambda { get { return lambda; } }
		
		/// <summary>
		/// Get shape parameter k
		/// </summary>
		public double K { get { return k; } }
		
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
		public WeibullDistribution() { }
		
		/// <summary>
		/// Constructor using seed and antithetic parameter
		/// </summary>
		/// <param name="seed"> random number seed </param>
		/// <param name="antithetic"> antithetic parameter </param>
		public WeibullDistribution(int seed, bool antithetic)
        {
            Initialize(seed, antithetic);
		}
		
		/// <summary>
		/// Constructor using mean and shape parameter k
		/// </summary>
		/// <param name="mean"> expected value </param>
		/// <param name="k"> shape parameter </param>
		public WeibullDistribution(double mean, double k, double shift = 0d)  : this() {
			ConfigureMean(mean, k, shift);
		}
		
		
		/// <summary>
		/// Constructor using mean, shape parameter k, seed,
		/// and shift parameter
		/// </summary>
		/// <param name="seed"> random number seed </param>
		/// <param name="mean"> expected value </param>
		/// <param name="k"> shape parameter </param>
		/// <param name="shift"> shift parameter </param>
        public WeibullDistribution(int seed, double mean, double k, double shift, bool antithetic)
            : this(seed, antithetic)
        {
            ConfigureMean(mean, k, shift);
		}
		
		/// <summary>
		/// Constructor using mean, shape parameter k,
		/// random generator, and shift parameter
		/// </summary>
		/// <param name="rnd"> random generator </param>
		/// <param name="mean"> expected value </param>
		/// <param name="k"> shape parameter </param>
		/// <param name="shift"> shift parameter </param>
        public WeibullDistribution(IRandomSource rnd, double mean, double k, double shift)
            : this(mean, k, shift)
        {
            Initialize(rnd);
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

        public void Initialize(int seed, double mean = 0d, double k = 1d, double shift = 0d, bool antithetic = false)
        {
            Initialize(seed, antithetic);
            Configure(mean, k, shift);
        }

        public void Initialize(IRandomSource rnd, double mean = 0d, double k = 1d, double shift = 0d)
        {
            Initialize(rnd);
            Configure(mean, k, shift);
        }

        #endregion
        #region conf

        public void ConfigureMean(double mean = 0d, double k = 1d, double shift = 0d)
        {
            this.mean = mean;
            this.k = k;
            this.shift = shift;

            this.lambda = (mean * this.k) / MMath.Gamma(1 / this.k);

            Configured = true;
        }

        public void Configure(double lambda = 0d, double k = 1d, double shift = 0d)
        {
            this.lambda = lambda;
            this.k = k;
            this.shift = shift;

            this.mean = (this.lambda * MMath.Gamma(1 / this.k)) / this.k;

            Configured = true;
        }

        #endregion
        #region impl
        
        /// <summary>
        /// Generate next weibull distributed random number
        /// </summary>
        /// <returns> random number of weibull distribution </returns>
        public double Next()
        {
            DrawCount++;
            return shift + lambda * Math.Pow(-Math.Log(1 - rnd.NextDouble()), 1 / k);
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