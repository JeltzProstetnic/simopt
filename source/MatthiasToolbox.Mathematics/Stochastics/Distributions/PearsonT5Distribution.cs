using System;
using System.Collections.Generic;
using MatthiasToolbox.Mathematics.Stochastics.Interfaces;
using MatthiasToolbox.Mathematics.Stochastics.RandomSources;

namespace MatthiasToolbox.Mathematics.Stochastics.Distributions
{
	/// <summary>
	/// PEARSON T5 DISTRIBUTION (0;inf)
	/// generate a double precision pearson T5 distributed random number
	/// using shape parameter alpha and scale parameter beta.
	/// The inverse gamma distribution is a two-parameter family of
	/// continuous probability distributions on the positive real line,
	/// which is the distribution of the reciprocal of a variable
	/// distributed according to the gamma distribution.
	/// </summary>
    public class PearsonT5Distribution : IDistribution<double>
	{
		#region cvar
		
		private GammaDistribution dblGamma;
		private double mean;
		private double shift = 0;
		
		#endregion
		#region prop
		
		/// <summary>
		/// Get and set seed of internal gamma distribution
		/// </summary>
		public int? Seed {
			get { return  dblGamma.Seed; }
			set { dblGamma.Seed = value; }
		}
		
		/// <summary>
		/// Get antithetic parameter
		/// </summary>
		public bool Antithetic {
			get { 
				return dblGamma.Antithetic; 
			}
			set{
				dblGamma.Antithetic = value;
			}
		}

        public string Name
        {
            get
            {
                return "Pearson T5 Distribution";
            }
        }

        public bool Initialized
        {
            get
            {
                return (dblGamma != null && dblGamma.Initialized);
            }
        }

        public bool Configured { get; set; }

        /// <summary>
        /// Get or set the number of values which have been
        /// drawn from this distribution since the last reset.
        /// </summary>
        public int DrawCount { get; set; }

        public double NonStochasticValue
        {
            get
            {
                return Mean;
            }
        }

        public double Mean
        {
            get
            {
                if (Alpha <= 1)
                    throw new ArgumentException("Mean is undefined because shape parameter Alpha <= 1");
                else
                    return mean;
            }
        }

        /// <summary>
        /// Get shape parameter alpha
        /// </summary>
        public double Alpha { get { return dblGamma.K; } }

        /// <summary>
        /// Get scale parameter beta
        /// </summary>
        public double Beta { get { return 1 / dblGamma.Theta; } }

        /// <summary>
        /// Get internal gamma distribution
        /// </summary>
        public GammaDistribution DblGamma { get { return dblGamma; } }

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
		public PearsonT5Distribution(){
			this.dblGamma = new GammaDistribution();
		}
		
		/// <summary>
		/// Constructor using random generator
		/// </summary>
		/// <param name="rnd"> random generator </param>
        public PearsonT5Distribution(IRandomSource rnd)
        {
			this.dblGamma = new GammaDistribution(rnd);
		}
		
		/// <summary>
		/// Constructor using seed and antithetic parameter
		/// </summary>
		/// <param name="seed"> random number seed </param>
		/// <param name="antithetic"> antithetic parameter </param>
		public PearsonT5Distribution(int seed, bool antithetic)
        {
			this.dblGamma = new GammaDistribution(seed, antithetic);
		}
		
		/// <summary>
		/// Constructor using mean and shape parameter alpha and
		/// scale parameter beta and shift parameter
		/// </summary>
		/// <param name="alpha"> shape parameter </param>
		/// <param name="beta"> scale parameter </param>
		/// <param name="shift"> shift parameter </param>
		public PearsonT5Distribution(double alpha, double beta, double shift = 0d) : this() 
        {
			Configure(alpha, beta, shift);
			this.shift = shift;
		}
		
		/// <summary>
		/// Constructor using mean, shape parameters alpha, scale
		/// parameter beta and random generator and shift parameter
		/// </summary>
		/// <param name="rnd"> random generator </param>
		/// <param name="alpha"> shape parameter </param>
		/// <param name="beta"> scale parameter </param>
		/// <param name="shift"> shift parameter </param>
        public PearsonT5Distribution(IRandomSource rnd, double alpha, double beta, double shift)
            : this(rnd)
        {
            Configure(alpha, beta, shift);
		}
		
		/// <summary>
		/// Constructor using mean, shape parameters alpha, scale
		/// parameter beta and seed and antithetic parameter
		/// and shift parameter
		/// </summary>
		/// <param name="seed"> random number seed </param>
		/// <param name="alpha"> shape parameter </param>
		/// <param name="beta"> scale parameter </param>
		/// <param name="antithetic"> antithetic parameter </param>
		public PearsonT5Distribution(int seed, double alpha, double beta, bool antithetic, double shift) : this(seed, antithetic) 
        {
			Configure(alpha, beta, shift);
		}
		
		#endregion
        #region init

        public void Initialize(int seed, bool antithetic = false)
        {
            dblGamma.Initialize(new MersenneTwister(seed, antithetic));
        }

        public void Initialize(IRandomSource rnd)
        {
            dblGamma.Initialize(rnd);
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
        /// Parametrize shape parameters alpha and scale parameter beta
        /// </summary>
        /// <param name="alpha"> shape parameter </param>
        /// <param name="beta"> scale parameter </param>
        public void Configure(double alpha, double beta, double shift = 0d)
        {
            dblGamma.ConfigureKTheta(alpha, 1 / beta);
            if (alpha > 1) this.mean = beta / (alpha - 1);

            Configured = true;
        }

        /// <summary>
        /// Parametrize mean and anonymous shape parameter
        /// Caution!  mean exists only if alpha > 1, otherwise an
        /// ArgumentOutOfRangeException will be thrown.
        /// </summary>
        /// <param name="mean"> expected value </param>
        /// <param name="alphas"> list of ONE shape parameter > 1</param> </param>
        public void ConfigureMean(double mean, double alpha)
        {
            if (alpha <= 1) throw new ArgumentOutOfRangeException("alpha", "mean exists only if alpha > 1");
            dblGamma.ConfigureKTheta(alpha, 1 / (mean * (alpha - 1)));
            this.mean = mean;

            Configured = true;
        }

        #endregion
        #region impl

        /// <summary>
        /// Generate next pearson T5 distributed random number
        /// </summary>
        /// <returns> random number of pearson T5 distribution </returns>
        public double Next()
		{
            DrawCount++;
            return shift + 1 / dblGamma.Next();
		}

        #endregion
        #region rset

        public void Reset()
        {
            DrawCount = 0;
            dblGamma.Reset();
        }

        public void Reset(int seed)
        {
            DrawCount = 0;
            dblGamma.Reset(seed);
        }

        public void Reset(int seed, bool antithetic)
        {
            DrawCount = 0;
            dblGamma.Reset(seed, antithetic);
        }

        public void Reset(bool antithetic)
        {
            DrawCount = 0;
            dblGamma.Reset(antithetic);
        }

        #endregion
	}
}