using System;
using MatthiasToolbox.Mathematics.Stochastics.Interfaces;
using MatthiasToolbox.Mathematics.Stochastics.RandomSources;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace MatthiasToolbox.Mathematics.Stochastics.Distributions
{
    /// <summary>
    /// CAUTION: negative bounds cannot be processed correctly!
    /// </summary>
    [Serializable]
    public class UniformIntegerDistribution : IDistribution<int>, IDistribution<double> //, ISerializableGrubi
    {
        #region cvar

        private double interval = int.MaxValue;
        private int interva1 = int.MaxValue;
        private int min = 0;
        private int max = int.MaxValue;
        private IRandomSource rnd;

        #endregion
        #region prop

        public string Name
        {
            get { return "Uniform"; }
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
                if (Initialized) return rnd.Seed;
                return null;
            }
            set
            {
                rnd.Reset((int)value);
            }
        }

        /// <summary>
        /// CAUTION: this is not equal to the mean value if the mean value is not an integer!
        /// The exact Mean is returned by (this as IDistribution&lt;double>).NonStochasticValue
        /// </summary>
        public int NonStochasticValue
        {
            get { return min + (int)(interval / 2); }
        }

        public int Minimum
        {
            get { return min; }
        }

        public int Maximum
        {
            get { return max; }
        }

        /// <summary>
        /// Get or set the number of values which have been
        /// drawn from this distribution since the last reset.
        /// </summary>
        public int DrawCount { get; set; }

        /// <summary>
        /// CAUTION: setting the value will reset the random number stream
        /// </summary>
        public bool Antithetic
        {
            get { return rnd.Antithetic; }
            set { rnd.Reset(value); }
        }

        /// <summary>
        /// Caution, this is round((max - min) / 2).
        /// The exact value is returned by (this as IDistribution&lt;double>).Mean
        /// </summary>
        public int Mean { get { return (int)Math.Round(((double)max - (double)min) / 2d); } }

        #region IDistribution<double>

        double IDistribution<double>.NonStochasticValue
        {
            get { return (double)min + (interval / 2d); }
        }

        double IDistribution<double>.Mean
        {
            get { return ((double)max - (double)min) / 2d; }
        }

        #endregion

        #endregion
        #region ctor

        /// <summary>
        /// creates a default instance. the instance must be initialized before it can be used.
        /// </summary>
        public UniformIntegerDistribution() { }

        /// <summary>
        /// creates a mersenne twister based, initialized instance, ready for immediate use
        /// min will be 0, max will be int.MaxValue
        /// </summary>
        /// <param name="seed"></param>
        /// <param name="antithetic"></param>
        public UniformIntegerDistribution(int seed, bool antithetic = false)
        {
            Initialize(seed, antithetic);
        }

        /// <summary>
        /// creates an initialized instance using Environment.TickCount as seed, ready for immediate use
        /// </summary>
        /// <param name="rnd"></param>
        /// <param name="mu"></param>
        /// <param name="sigma"></param>
        /// <param name="antithetic"></param>
        public UniformIntegerDistribution(IRandomSource rnd)
        {
            Initialize(rnd);
        }

        /// <summary>
        /// creates a mersenne twister based, initialized instance, ready for immediate use
        /// </summary>
        /// <param name="seed"></param>
        /// <param name="mu"></param>
        /// <param name="sigma"></param>
        /// <param name="antithetic"></param>
        public UniformIntegerDistribution(int seed, uint min, uint max, bool antithetic = false)
        {
            if (min < 0) throw new ArgumentOutOfRangeException("The minimum must not be smaller than zero.");
            if (max <= min) throw new ArgumentOutOfRangeException("The maximum must be greater than the minimum.");
            if (max > int.MaxValue) throw new ArgumentOutOfRangeException("The maximum may not be greater than " + int.MaxValue.ToString() + ".");
            Initialize(seed, antithetic);
            Configure((int)min, (int)max);
        }
        
        /// <summary>
        /// creates a mersenne twister based, initialized instance, ready for immediate use
        /// </summary>
        /// <param name="seed"></param>
        /// <param name="mu"></param>
        /// <param name="sigma"></param>
        /// <param name="antithetic"></param>
        public UniformIntegerDistribution(IRandomSource rnd, int seed, uint min, uint max)
        {
            if (min < 0) throw new ArgumentOutOfRangeException("The minimum must not be smaller than zero.");
            if (max <= min) throw new ArgumentOutOfRangeException("The maximum must be greater than the minimum.");
            if (max > int.MaxValue) throw new ArgumentOutOfRangeException("The maximum may not be greater than " + int.MaxValue.ToString() + ".");
            Initialize(rnd);
            Configure((int)min, (int)max);
        }
        
        /// <summary>
        /// creates a configured instance using Environment.TickCount as seed
        /// CAUTION: initialization required before use!
        /// min and max are uint to avoid ambiguous calls
        /// </summary>
        /// <param name="rnd"></param>
        /// <param name="mu"></param>
        /// <param name="sigma"></param>
        /// <param name="antithetic"></param>
        public UniformIntegerDistribution(uint min, uint max)
        {
            if (min < 0) throw new ArgumentOutOfRangeException("The minimum must not be smaller than zero.");
            if (max <= min) throw new ArgumentOutOfRangeException("The maximum must be greater than the minimum.");
            if (max > int.MaxValue) throw new ArgumentOutOfRangeException("The maximum may not be greater than " + int.MaxValue.ToString() + ".");
            Configure((int)min, (int)max);
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

        public void Initialize(int seed, int min = 0, int max = int.MaxValue, bool antithetic = false)
        {
            if (min < 0) throw new ArgumentOutOfRangeException("The minimum must not be smaller than zero.");
            if (max <= min) throw new ArgumentOutOfRangeException("The maximum must be greater than the minimum.");
            Initialize(seed, antithetic);
            Configure(min, max);
        }

        public void Initialize(IRandomSource rnd, int min = 0, int max = int.MaxValue)
        {
            if (min < 0) throw new ArgumentOutOfRangeException("The minimum must not be smaller than zero.");
            if (max <= min) throw new ArgumentOutOfRangeException("The maximum must be greater than the minimum.");
            Initialize(rnd);
            Configure(min, max);
        }

        #endregion
        #region conf

        public void Configure(int min = 0, int max = int.MaxValue)
        {
            if (min < 0) throw new ArgumentOutOfRangeException("The minimum must not be smaller than zero.");
            if (max <= min) throw new ArgumentOutOfRangeException("The maximum must be greater than the minimum.");

            this.min = min;
            this.max = max;
            this.interval = max - min;
            this.interva1 = max - min;

            Configured = true;
        }

        #endregion
        #region impl

        public int Next()
        {
            DrawCount++;
            // TODO: test which one performs better
            return (rnd.NextInteger() % interva1) + min;
            // return min + (int)(rnd.NextDouble() * interval);
        }

        #region IDistribution<double>

        double IDistribution<double>.Next()
        {
            return Next();
        }

        #endregion
        #region ISerializableGrubi

        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("randomSource", rnd);
            info.AddValue("interval", interval);
            info.AddValue("interva1", interva1);
            info.AddValue("min", min);
            info.AddValue("max", max);
            info.AddValue("configured", Configured);
            info.AddValue("drawCount", DrawCount);
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