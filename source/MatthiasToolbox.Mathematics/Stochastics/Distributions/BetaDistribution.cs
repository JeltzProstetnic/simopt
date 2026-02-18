using System;
using System.Collections.Generic;
using MatthiasToolbox.Mathematics.Stochastics.Interfaces;

namespace MatthiasToolbox.Mathematics.Stochastics.Distributions
{
	/// <summary>
	/// BETA DISTRIBUTION [0;1]
	/// generate a double precision beta distributed random number using mean
	/// shape paramters alpha and beta.
	/// </summary>
	public class BetaDistribution : IDistribution<double>
	{
		#region cvar
		
		private double factor = 1d;
		private int diff = 2;
		private GammaDistribution dblGamma1;
		private GammaDistribution dblGamma2;
		private double shift = 0;
		private double mean;
		
		#endregion
		#region prop
		
		public string Name
		{
			get
			{
				return "Beta Distribution";
			}
		}

		public bool Initialized
		{
			get
			{
				return (dblGamma1 != null && dblGamma1.Initialized);
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

		public double Mean
		{
			get
			{
				return mean;
			}
		}

		/// <summary>
		/// Get and set seed of internal distribution
		/// </summary>
		public int? Seed
		{
			get 
			{ 
				return dblGamma1.Seed; 
			}
			set
			{
				dblGamma1.Seed = value;
				dblGamma2.Seed = value + diff;
			}
		}
		
		/// <summary>
		/// Get antithetic parameter
		/// </summary>
		public bool Antithetic
		{
			get
			{
				return dblGamma1.Antithetic;
			}
			set
			{
				dblGamma1.Antithetic = value;
				dblGamma2.Antithetic = value;
			}
		}
		
		/// <summary>
		/// Get shape parameter alpha (shape parameter k of internal gamma distribution)
		/// </summary>
		public double Alpha { get { return dblGamma1.K; }	}
		
		/// <summary>
		/// Get shape parameter beta (shape parameter k of internal gamma distribution)
		/// </summary>
		public double Beta { get { return dblGamma2.K; } }
		
		/// <summary>
		/// Get factor
		/// is used as correction factor if mean and both shape parameters are parametrized
		/// </summary>
		public double Factor { get { return factor; } }
		
		/// <summary>
		/// Get difference of seed for internal gamma distributions
		/// </summary>
		public long Diff { get { return diff; } }
		
		/// <summary>
		/// Get internal gamma distribution
		/// </summary>
		public GammaDistribution DblGamma1 { get { return dblGamma1; } }
		
		/// <summary>
		/// Get internal gamma distribution
		/// </summary>
		public GammaDistribution DblGamma2 { get { return dblGamma2; } }
		
		/// <summary>
		/// Get and set shift parameter
		/// </summary>
		public double Shift {
			get { return this.shift; }
			set { this.shift = value; }
		}

        /// <summary>
        /// Get or set the number of values which have been
        /// drawn from this distribution since the last reset.
        /// </summary>
        public int DrawCount { get; set; }

		#endregion
		#region ctor
		
		/// <summary>
		/// empty constructor
		/// </summary>
		public BetaDistribution() { }
		
		/// <summary>
		/// Constructor using random generator
		/// </summary>
		/// <param name="rnd"> random generator </param>
		public BetaDistribution(IRandomSource rnd)
		{
			this.dblGamma1 = new GammaDistribution(rnd);
			this.dblGamma2 = new GammaDistribution(rnd);
		}
		
		/// <summary>
		/// Constructor using seed and antithetic parameter
		/// </summary>
		/// <param name="seed"> random number seed </param>
		/// <param name="antithetic"> antithetic parameter </param>
		public BetaDistribution(int seed, bool antithetic)
		{
			this.dblGamma1 = new GammaDistribution(seed, antithetic);
			this.dblGamma2 = new GammaDistribution(seed + diff, antithetic);
		}
		
		/// <summary>
		/// Constructor using mean and shape parameters alpha and beta
		/// and shift parameter
		/// </summary>
		/// <param name="mean"> expected value </param>
		/// <param name="alpha"> shape parameter </param>
		/// <param name="beta"> shape parameter </param>
		/// <param name="shift"> shift parameter </param>
		public BetaDistribution(double mean, double alpha, double beta, double shift = 0d) : this()
		{
			ConfigureMean(mean, alpha, beta, shift);
		}
		
		/// <summary>
		/// Constructor using mean and shape parameters alpha, beta
		/// and random generator and shift parameter
		/// </summary>
		/// <param name="rnd"> random generator </param>
		/// <param name="mean"> expected value </param>
		/// <param name="alpha"> shape parameter </param>
		/// <param name="beta"> shape parameter </param>
		/// <param name="shift"> shift parameter </param>
		public BetaDistribution(IRandomSource rnd, double mean, double alpha, double beta, double shift = 0d)
			: this(rnd)
		{
			ConfigureMean(mean, alpha, beta, shift);
		}
		
		/// <summary>
		/// Constructor using mean and shape parameters alpha, beta
		/// and seed and antithetic parameter and shift parameter
		/// </summary>
		/// <param name="seed"> random number seed </param>
		/// <param name="mean"> expected value </param>
		/// <param name="alpha"> shape parameter </param>
		/// <param name="beta"> shape parameter </param>
		/// <param name="antithetic"> antithetic parameter </param>
		/// <param name="shift"> shift parameter </param>
		public BetaDistribution(int seed, double mean, double alpha, double beta, bool antithetic, double shift = 0d)
			: this(seed, antithetic)
		{
			ConfigureMean(mean, alpha, beta, shift);
		}

		#endregion
		#region init

		public void Initialize(int seed, bool antithetic)
		{
			dblGamma1 = new GammaDistribution(seed, antithetic);
			dblGamma2 = new GammaDistribution(seed + diff, antithetic);
		}

		public void Initialize(IRandomSource rnd)
		{
			dblGamma1 = new GammaDistribution(rnd);
			dblGamma2 = new GammaDistribution(rnd);
		}

		#endregion
		#region conf

		/// <summary>
		/// Parametrize mean and anonymous shape parameters
		/// </summary>
		/// <param name="mean"> expected value </param>
		/// <param name="alphas"> list of shape parameters </param>
		public void ConfigureMean(double mean, double alpha, double beta, double shift = 0d)
		{
			dblGamma1.ConfigureKTheta(alpha, 1);
			dblGamma2.ConfigureKTheta(beta, 1);
			factor = mean / (Alpha / (Alpha + Beta));
			this.mean = mean;
			this.shift = shift;
			Configured = true;
		}

		/// <summary>
		/// Parametrize shape parameters alpha and beta
		/// </summary>
		/// <param name="alpha"> shape parameter </param>
		/// <param name="beta"> shape parameter </param>
		public void Configure(double alpha, double beta, double shift = 0d)
		{
			dblGamma1.ConfigureKTheta(alpha, 1);
			dblGamma2.ConfigureKTheta(beta, 1);
			factor = 1d;
			this.mean = (Alpha / (Alpha + Beta));
			this.shift = shift;
			Configured = true;
		}

		#endregion
		#region impl

		/// <summary>
		/// Generate next beta distributed random number
		/// </summary>
		/// <returns> random number of beta distribution </returns>
		public double Next()
		{
            DrawCount++;
			double gam1 = dblGamma1.Next();
			return shift + factor * gam1 / (gam1 + dblGamma2.Next());
		}
		
		#endregion
		#region rset

		public void Reset()
		{
            DrawCount = 0;
			dblGamma1.Reset();
			dblGamma2.Reset();
		}

		public void Reset(int seed)
		{
            DrawCount = 0;
			dblGamma1.Reset(seed);
			dblGamma2.Reset(seed);
		}

		public void Reset(int seed, bool antithetic)
		{
            DrawCount = 0;
			dblGamma1.Reset(seed, antithetic);
			dblGamma2.Reset(seed, antithetic);
		}

		public void Reset(bool antithetic)
		{
            DrawCount = 0;
			dblGamma1.Reset(antithetic);
			dblGamma2.Reset(antithetic);
		}

		#endregion
	}
}