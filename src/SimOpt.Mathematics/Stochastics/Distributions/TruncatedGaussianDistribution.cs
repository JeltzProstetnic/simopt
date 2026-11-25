using System;
using SimOpt.Mathematics.Stochastics.Interfaces;
using SimOpt.Mathematics.Stochastics.RandomSources;

namespace SimOpt.Mathematics.Stochastics.Distributions
{
    /// <summary>
    /// SIM-110 step 2: a normal distribution truncated at zero, plus a shift, the topology schema's
    /// <c>normal</c> family, so a time drawn from it is never negative. A draw below zero is drawn again,
    /// so the distribution is the Gaussian conditioned on X ≥ 0, and <see cref="Mean"/> is its mean,
    /// μ + σ·φ(α)/(1 − Φ(α)) with α = −μ/σ, plus the shift.
    /// </summary>
    /// <remarks>
    /// Rejection costs about 1/(1 − Φ(α)) draws per value: 1.19 at μ = σ, effectively 1 at the viewer's
    /// default spread of 15 % of the mean. The Box–Muller transform is the one
    /// <see cref="GaussianDistribution"/> uses.
    /// </remarks>
    [Serializable]
    public sealed class TruncatedGaussianDistribution : IDistribution<double>
    {
        private double mu;
        private double sigma = 1d;
        private double shift;
        private double mean;
        private IRandomSource rnd = null!;

        public string Name { get { return "Truncated Gaussian Distribution"; } }

        public bool Initialized { get { return rnd != null && rnd.Initialized; } }

        public bool Configured { get; private set; }

        /// <summary>CAUTION: setting the value will reset the random number stream</summary>
        public int? Seed
        {
            get { return Initialized ? rnd.Seed : null; }
            set { rnd.Reset(value!.Value); }
        }

        /// <summary>Values handed out since the last reset; draws rejected below zero are not counted.</summary>
        public int DrawCount { get; set; }

        /// <summary>CAUTION: setting the value will reset the random number stream</summary>
        public bool Antithetic
        {
            get { return rnd.Antithetic; }
            set { rnd.Reset(value); }
        }

        /// <summary>The mean of the underlying Gaussian, before truncation.</summary>
        public double Mu { get { return mu; } }

        /// <summary>The standard deviation of the underlying Gaussian, before truncation.</summary>
        public double Sigma { get { return sigma; } }

        public double Shift { get { return shift; } }

        public double Mean { get { return mean; } }

        public double NonStochasticValue { get { return mean; } }

        /// <param name="mu">the mean of the Gaussian before it is truncated at zero</param>
        /// <param name="sigma">its standard deviation, positive</param>
        /// <param name="shift">added to every draw after the truncation</param>
        public void Configure(double mu, double sigma, double shift = 0d)
        {
            if (!(sigma > 0d) || double.IsInfinity(sigma))
                throw new ArgumentOutOfRangeException(nameof(sigma), "The standard deviation must be a positive number.");
            if (double.IsNaN(mu) || double.IsInfinity(mu))
                throw new ArgumentOutOfRangeException(nameof(mu), "The mean must be a number.");
            double alpha = -mu / sigma;
            double kept = 1d - MMath.Normal(alpha);
            if (!(kept > 0d))
                throw new ArgumentOutOfRangeException(nameof(mu), "The mean is so far below zero that no draw would ever be kept.");
            this.mu = mu;
            this.sigma = sigma;
            this.shift = shift;
            mean = shift + mu + sigma * Math.Exp(-0.5 * alpha * alpha) / Math.Sqrt(2d * Math.PI) / kept;
            Configured = true;
        }

        public void Initialize(int seed, bool antithetic = false) { rnd = new MersenneTwister(seed, antithetic); }

        public void Initialize(IRandomSource rnd) { this.rnd = rnd; }

        public double Next()
        {
            DrawCount++;
            while (true)
            {
                double x = mu + sigma * Math.Sqrt(-2.0d * Math.Log(1 - rnd.NextDouble())) * Math.Cos(2 * Math.PI * rnd.NextDouble());
                if (x >= 0d) return x + shift;
            }
        }

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
    }
}
