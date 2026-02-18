using System;
using MatthiasToolbox.Mathematics.Stochastics.Interfaces;
using MatthiasToolbox.Mathematics.Stochastics.RandomSources;

namespace MatthiasToolbox.Mathematics.Stochastics.Distributions
{
	public class ErlangDistribution : IDistribution<double>
	{
		#region cvar

		private int k = 1;
		private double lambda = 0.5;
		private double shift = 0;
		private double mean = 2;
		private IRandomSource rnd;

		#endregion
		#region prop

		public string Name
		{
			get { return "ErlangDistribution"; }
		}

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

		public bool Initialized
		{
			get { return (rnd != null && rnd.Initialized); }
		}

		public bool Configured { get; private set; }

		public double NonStochasticValue
		{
			get { return mean; }
		}

		public double Mean
		{
			get { return mean; }
		}

        /// <summary>
        /// Get or set the number of values which have been
        /// drawn from this distribution since the last reset.
        /// </summary>
        public int DrawCount { get; set; }

		/// <summary>
		/// shape parameter k
		/// </summary>
		public int K { get { return k; } }

		/// <summary>
		/// rate parameter lambda
		/// </summary>
		public double Lambda { get { return lambda; } }

		/// <summary>
		/// shift parameter
		/// </summary>
		public double Shift
		{
			get { return this.shift; }
			set { this.shift = value; }
		}

		#endregion
		#region ctor
		
		/// <summary>
		/// creates a default instance. the instance must be initialized before it can be used.
		/// </summary>
		public ErlangDistribution() { }
		
		/// <summary>
		/// Constructor using mean, shape parameter k
		/// and seed and shift parameter
		/// </summary>
		/// <param name="seed"> random number seed </param>
		/// <param name="mean"> expected value </param>
		/// <param name="k"> shape parameter </param>
		/// <param name="shift"> shift parameter </param>
		public ErlangDistribution(int seed, bool antithetic = false)
		{
			Initialize(seed, antithetic);
		}
		
		/// <summary>
		/// Constructor using mean and shape parameter k and shift parameter
		/// </summary>
		/// <param name="mean"> expected value </param>
		/// <param name="k"> shape parameter </param>
		/// <param name="shift"> shift parameter </param>
		public ErlangDistribution(double mean, int k, double shift = 0)
		{
			Configure(mean, k, shift);
		}

		/// <summary>
		/// Constructor using mean, shape parameter k
		/// and seed and shift parameter
		/// </summary>
		/// <param name="seed"> random number seed </param>
		/// <param name="mean"> expected value </param>
		/// <param name="k"> shape parameter </param>
		/// <param name="shift"> shift parameter </param>
		public ErlangDistribution(int seed, double mean, int k, double shift = 0, bool antithetic = false)
		{
			Initialize(seed, antithetic);
			Configure(mean, k, shift);
		}

		/// <summary>
		/// Constructor using mean, shape parameter k
		/// and random generator and shift parameter
		/// </summary>
		/// <param name="rnd"> random generator </param>
		/// <param name="mean"> expected value </param>
		/// <param name="k"> shape parameter </param>
		/// <param name="shift"> shift parameter </param>
		public ErlangDistribution(IRandomSource rnd, double mean, int k, double shift = 0)
		{
			Initialize(rnd);
			Configure(mean, k, shift);
		}
		
		#endregion
		#region init

		public void Initialize(int seed, bool antithetic)
		{
			this.rnd = new MersenneTwister(seed, antithetic);
		}

		public void Initialize(IRandomSource rnd)
		{
			this.rnd = rnd;
		}

		public void Initialize(int seed, double mean = 2, int k = 1, double shift = 0, bool antithetic = false)
		{
			Initialize(seed, antithetic);
			Configure(mean, k, shift);
		}

		public void Initialize(IRandomSource rnd, double mean = 2, int k = 1, double shift = 0)
		{
			Initialize(rnd);
			Configure(mean, k, shift);
		}

		#endregion
		#region conf

		private void Configure(double mean = 2, int k = 1, double shift = 0)
		{
			if (mean <= 0) throw new ArgumentOutOfRangeException("mean", "mean and k must be > 0");
			if (k <= 0) throw new ArgumentOutOfRangeException("k", "mean and k must be > 0");
			this.k = k;
			this.lambda = this.k / mean;
			this.mean = mean;
			this.shift = shift;

			Configured = true;
		}

		private void ConfigureLambda(double lambda = 0.5, int k = 1, double shift = 0)
		{
			if (lambda <= 0) throw new ArgumentOutOfRangeException("lambda", "lambda and k must be > 0");
			if (k <= 0) throw new ArgumentOutOfRangeException("k", "lambda and k must be > 0");

			this.k = k;
			this.lambda = lambda;
			this.mean = k / lambda;
			this.shift = shift;

			Configured = true;
		}

		#endregion
		#region impl

		/// <summary>
		/// generate erlang distributed random number
		/// </summary>
		/// <returns>random number of erlang distribution</returns>
		public double Next()
		{
            DrawCount++;

			double res = 1;

			for (int lc = 1; lc <= k; lc++)
			{
				res *= rnd.NextDouble();
			}

			return -(1 / lambda) * Math.Log(res) + shift;
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