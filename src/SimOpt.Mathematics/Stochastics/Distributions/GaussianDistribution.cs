using System;
using SimOpt.Mathematics.Stochastics.Interfaces;
using SimOpt.Mathematics.Stochastics.RandomSources;
using System.Runtime.Serialization;

namespace SimOpt.Mathematics.Stochastics.Distributions
{
    /// <summary>
    /// Provides double precision normal distributed random numbers on the interval (-inf;inf) parametrized by mean mu and stdev sigma
    /// </summary>
    [Serializable]
    public class GaussianDistribution : IDistribution<double> //, ISerializableGrubi
    {
        #region cvar

        private double mu = 0d;
        private double sigma = 1d;
        private IRandomSource rnd;

        #endregion
        #region prop

        public string Name
        {
            get { return "Gaussian Distribution"; }
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
        /// CAUTION: setting the value will reset the random number stream
        /// </summary>
        public int? Seed
        {
            get
            {
                if(Initialized) return rnd.Seed;
                return null;
            }
            set
            {
                rnd.Reset((int)value);
            }
        }

        /// <summary>
        /// Get or set the number of values which have been
        /// drawn from this distribution since the last reset.
        /// </summary>
        public int DrawCount { get; set; }

        public double NonStochasticValue
        {
            get { return mu; }
        }

        public double Sigma
        {
            get { return sigma; }
        }

        /// <summary>
        /// CAUTION: setting the value will reset the random number stream
        /// </summary>
        public bool Antithetic
        {
            get { return rnd.Antithetic; }
            set { rnd.Reset(value); }
        }

        public double Mean { get { return mu; } }

        #endregion
        #region ctor

        protected GaussianDistribution(SerializationInfo info, StreamingContext context)
        {
            this.mu = info.GetDouble("mu");
            this.sigma = info.GetDouble("sigma");
            this.rnd = (IRandomSource)info.GetValue("rnd", typeof(IRandomSource));
            this.Configured = info.GetBoolean("Configured");
            this.DrawCount = info.GetInt32("DrawCount");
        }

        /// <summary>
        /// creates a default instance. the instance must be initialized before it can be used.
        /// </summary>
        public GaussianDistribution() { }

        /// <summary>
        /// creates a mersenne twister based, initialized instance, ready for immediate use
        /// </summary>
        /// <param name="seed"></param>
        /// <param name="mu"></param>
        /// <param name="sigma"></param>
        /// <param name="antithetic"></param>
        public GaussianDistribution(int seed, bool antithetic = false) 
        {
            Initialize(seed, antithetic);
        }
        
        /// <summary>
        /// creates a mersenne twister based, configured instance
        /// a seed has to be set before it can be used.
        /// </summary>
        /// <param name="seed"></param>
        /// <param name="mu"></param>
        /// <param name="sigma"></param>
        /// <param name="antithetic"></param>
        public GaussianDistribution(double mu = 0d, double sigma = 1d)
        {
            Configure(mu, sigma);
        }

        /// <summary>
        /// creates a mersenne twister based, initialized instance, ready for immediate use
        /// </summary>
        /// <param name="seed"></param>
        /// <param name="mu"></param>
        /// <param name="sigma"></param>
        /// <param name="antithetic"></param>
        public GaussianDistribution(int seed, double mu = 0d, double sigma = 1d, bool antithetic = false) 
        {
            Initialize(seed, mu, sigma, antithetic);
        }

        /// <summary>
        /// creates an initialized instance, ready for immediate use
        /// </summary>
        /// <param name="rnd"></param>
        /// <param name="mu"></param>
        /// <param name="sigma"></param>
        /// <param name="antithetic"></param>
        public GaussianDistribution(IRandomSource rnd, double mu = 0d, double sigma = 1d) 
        {
            Initialize(rnd, mu, sigma);
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

        public void Configure(double mu = 0d, double sigma = 1d)
        {
            this.sigma = sigma;
            this.mu = mu;

            Configured = true;
        }

        #endregion
        #region impl

        public double Next()
        {
            DrawCount++;
            return (mu + (sigma * Math.Sqrt(-2.0d * Math.Log(1 - rnd.NextDouble())) * Math.Cos(2 * Math.PI * rnd.NextDouble())));
        }

        #region ISerializableGrubi

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("mu", mu);
            info.AddValue("sigma", sigma);
            info.AddValue("rnd", rnd);
            info.AddValue("Configured", Configured);
            info.AddValue("DrawCount", DrawCount);
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