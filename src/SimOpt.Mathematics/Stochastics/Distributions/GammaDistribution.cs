using System;
using System.Collections.Generic;
using SimOpt.Mathematics.Stochastics.Interfaces;

namespace SimOpt.Mathematics.Stochastics.Distributions
{
	/// <summary>
	/// GAMMA DISTRIBUTION [0;inf)
	/// generate a double precision gamma distributed random number using mean and
	/// shape parameter k (scale parameter theta is implicitly given by mean = k*theta).
	/// </summary>
	public class GammaDistribution : IDistribution<double>
	{
		#region cvar
		
		private double theta = 1d;
		private double k = 1d;
		private int diff = 1;
		private double mean = 1;
		private GaussianDistribution dblGaussian;
		private UniformDoubleDistribution dblUniform;
		private double shift = 0;
		
		#endregion
		#region prop

		public string Name
		{
			get { return "Gamma Distribution"; }
		}

		public bool Initialized
		{
			get
			{
				return (dblUniform != null && dblUniform.Initialized);
			}
		}

		public bool Configured { get; private set; }

		/// <summary>
		/// CAUTION: setting the value will reset the random number stream
		/// </summary>
		public int? Seed
		{
			get
			{
				if (Initialized) 
					return dblUniform.Seed;
				return null;
			}
			set
			{
				dblUniform.Reset((int)value);
				dblGaussian.Reset((int)value + diff);
			}
		}

		public double NonStochasticValue
		{
			get { return mean; }
		}

		/// <summary>
		/// CAUTION: setting the value will reset the random number stream
		/// </summary>
		public bool Antithetic
		{
			get { return dblUniform.Antithetic; }
			set
			{
				dblUniform.Reset(value);
				dblGaussian.Reset(value);
			}
		}

		public double Mean { get { return mean; } }

		/// <summary>
		/// Get scale parameter theta
		/// </summary>
		public double Theta { get { return theta; } }

		/// <summary>
		/// Get shape parameter k
		/// </summary>
		public double K { get { return k; } }

		/// <summary>
		/// Get difference of seed for internal distributions
		/// </summary>
		public int Diff { get { return diff; } }

		/// <summary>
		/// Get and set shift parameter
		/// </summary>
		public double Shift
		{
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
		public GammaDistribution() { }
		
		/// <summary>
		/// Constructor using random generators
		/// </summary>
		/// <param name="rnd"> random generator </param>
		public GammaDistribution(IRandomSource rnd)
		{
			Initialize(rnd);
		}
		
		/// <summary>
		/// Constructor using seed and antithetic parameter
		/// </summary>
		/// <param name="seed"> random number seed </param>
		/// <param name="antithetic"> antithetic parameter </param>
		public GammaDistribution(int seed, bool antithetic)
		{
			Initialize(seed, antithetic);
		}
		
		/// <summary>
		/// Constructor using mean (theta*k) and shape parameter k
		/// </summary>
		/// <param name="mean"> expected value </param>
		/// <param name="k"> shape parameter </param>
		public GammaDistribution(double mean, double k, double shift = 0) : this()
		{
			ConfigureMeanK(mean, k, shift);
		}
		
		/// <summary>
		/// Constructor using mean (theta*k), shape parameter k
		/// and random generators
		/// </summary>
		/// <param name="rnd"> random generator </param>
		/// <param name="mean"> expected value </param>
		/// <param name="k"> shape parameter </param>
		public GammaDistribution(IRandomSource rnd, double mean, double k, double shift = 0)
			: this(rnd)
		{
			ConfigureMeanK(mean, k, shift);
		}
		
		/// <summary>
		/// Constructor using mean (theta*k), shape parameter k
		/// and seed and antithetic parameter
		/// </summary>
		/// <param name="seed"> random number seed </param>
		/// <param name="mean"> expected value </param>
		/// <param name="k"> shape parameter </param>
		/// <param name="antithetic"> antithetic parameter </param>
		public GammaDistribution(int seed, double mean, double k, double shift = 0, bool antithetic = false) : this(seed, antithetic)
		{
			ConfigureMeanK(mean, k, shift);
		}
		
		#endregion
		#region init

		public void Initialize(int seed, bool antithetic)
		{
			this.dblUniform = new UniformDoubleDistribution(seed, antithetic: antithetic);
			this.dblGaussian = new GaussianDistribution(seed + diff, antithetic: antithetic);
//			dblGaussian.Initialize(seed, antithetic);
//			dblUniform.Initialize(seed + diff, antithetic);
		}
		
		public void Initialize(IRandomSource rnd)
		{
			this.dblUniform = new UniformDoubleDistribution(rnd);
			this.dblGaussian = new GaussianDistribution(rnd);
//			dblGaussian.Initialize(rnd);
//			dblUniform.Initialize(rnd);
		}

		#endregion
		#region conf

		/// <summary>
		/// Parameterize mean (theta*k) and scale parameter theta
		/// Caution! theta and mean must be > 0, otherwise an ArgumentOutOfRangeException will be thrown.
		/// </summary>
		/// <param name="mean"> expected value > 0 </param> </param>
		/// <param name="theta"> scale parameter > 0 </param> </param>
		public void ConfigureMeanTheta(double mean, double theta, double shift = 0)
		{
			if (theta <= 0) throw new ArgumentOutOfRangeException("theta", "theta and mean must be > 0");
			if (mean <= 0) throw new ArgumentOutOfRangeException("mean", "theta and mean must be > 0");
			this.theta = theta;
			this.k = mean / this.theta;
			this.mean = mean;
			this.Shift = shift;

			Configured = true;
		}

		/// <summary>
		/// Parameterize shape parameter k and scale parameter theta
		/// Caution! k and theta must be > 0, otherwise an ArgumentOutOfRangeException will be thrown.
		/// </summary>
		/// <param name="k"> shape parameter > 0 </param> </param>
		/// <param name="theta"> scale parameter > 0 </param></param>
		public void ConfigureKTheta(double k, double theta, double shift = 0)
		{
			if (theta <= 0) throw new ArgumentOutOfRangeException("theta", "k and theta must be > 0");
			if (k <= 0) throw new ArgumentOutOfRangeException("k", "k and theta must be > 0");
			this.k = k;
			this.theta = theta;
			this.mean = k * theta;
			this.Shift = shift;

			Configured = true;
		}

		/// <summary>
		/// Parameterize mean and anonymous parameter (shape parameter k)
		/// Only alphas[0] will be used.
		/// Caution! mean and anonymous parameter must be > 0, otherwise an ArgumentOutOfRangeException will be thrown.
		/// </summary>
		/// <param name="mean"> expected value > 0 </param></param>
		/// <param name="alphas"> List of ONE anonymous parameter > 0 </param> </param>
		public void ConfigureMeanK(double mean, double k, double shift = 0)
		{
			if (mean <= 0) throw new ArgumentOutOfRangeException("mean", "mean and alphas must be > 0");
			if (k <= 0) throw new ArgumentOutOfRangeException("alphas", "mean and alphas must be > 0");
			this.k = k;
			this.theta = mean / this.k;
			this.mean = mean;
			this.Shift = shift;

			Configured = true;
		}

		#endregion
		#region impl

		/// <summary>
		/// Generate next gamma distributed random number
		/// </summary>
		/// <returns> random number of gamma distribution </returns>
		public double Next()
		{
            DrawCount++;
			return shift + this.theta * StandardDoubleGamma(this.k);
		}

		#endregion
		#region rset

		public void Reset()
		{
            DrawCount = 0;
			dblGaussian.Reset();
			dblUniform.Reset();
		}

		public void Reset(int seed)
		{
            DrawCount = 0;
			dblGaussian.Reset(seed);
			dblUniform.Reset(seed);
		}

		public void Reset(int seed, bool antithetic)
		{
            DrawCount = 0;
			dblGaussian.Reset(seed, antithetic);
			dblUniform.Reset(seed, antithetic);
		}

		public void Reset(bool antithetic)
		{
            DrawCount = 0;
			dblGaussian.Reset(antithetic);
			dblUniform.Reset(antithetic);
		}

		#endregion
		#region util

		/// <summary>
		/// STANDARD GAMMA DISTRIBUTION
		/// generate a double precision standard gamma distributed random number using
		/// shape parameter k (scale parameter theta is 1).
		/// </summary>
		/// <param name="k"> shape parameter </param>
		/// <returns></returns>
		private double StandardDoubleGamma(double k)
		{
			double d = k - 1 / 3;
			double c = 1 / Math.Sqrt(9 * d);
			double v, u, x;
			bool fin = true;

			do
			{
				do
				{
					x = dblGaussian.Next();
					v = Math.Pow(1 + c * x, 3);
				} while (v <= 0);

				u = dblUniform.Next();
				if (u < 1 - 0.0331 * Math.Pow(x, 4))
				{
					fin = false;
				}
				else if (Math.Log(u) < 0.5 * Math.Pow(x, 2) + d * (1 - v + Math.Log(v)))
				{
					fin = false;
				}
			} while (fin);

			return d * v;
		}

		#endregion
	}
}