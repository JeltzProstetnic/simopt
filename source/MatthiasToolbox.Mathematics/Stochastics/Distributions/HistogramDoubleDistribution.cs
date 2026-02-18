using System;
using System.Linq;
using System.Collections.Generic;
using MatthiasToolbox.Mathematics.Stochastics.Interfaces;
using MatthiasToolbox.Mathematics.Stochastics.RandomSources;

namespace MatthiasToolbox.Mathematics.Stochastics.Distributions
{
    /// <summary>
    /// Calculate a random number on the interval (Minimum;Maximum) with respect to the Frequencies.
    /// </summary>
    public class HistogramDoubleDistribution : IDistribution<double>
    {
        #region cvar
        
        private IRandomSource rnd;
        private double increment;

        #endregion
        #region prop

        /// <summary>
        /// If interpolate is set to true, all values between minimum and maximum may be returned.
        /// If it is set to false, only (Maximum - Minimum) / Frequencies.Count distinct values occur.
        /// The default value is true.
        /// </summary>
        public bool Interpolate { get; set; }
        
        public double Minimum { get; set; }
        public double Maximum { get; set; }
        public List<double> Frequencies { get; private set; }

        /// <summary>
        /// Get or set the number of values which have been
        /// drawn from this distribution since the last reset.
        /// </summary>
        public int DrawCount { get; set; }

        #region IDistribution<double>

        public string Name
        {
            get { return "Double Histogram Distribution"; }
        }

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
            get { return Mean; }
        }

        /// <summary>
        /// Caution: the mean is only an approximation!
        /// </summary>
        public double Mean { get; private set; }

        #endregion

        #endregion
        #region ctor

        public HistogramDoubleDistribution() 
        {
            Interpolate = true;
        }

        public HistogramDoubleDistribution(IRandomSource rnd) : this()
        {
            Initialize(rnd);
        }

        public HistogramDoubleDistribution(int seed, bool antithetic = false) : this()
        {
            Initialize(seed, antithetic);
        }

        public HistogramDoubleDistribution(double min, double max, List<double> frequencies)
            : this()
        {
            Configure(min, max, frequencies);
        }

        public HistogramDoubleDistribution(IRandomSource rnd, double min, double max, List<double> frequencies)
            : this(rnd)
        {
            Configure(min, max, frequencies);
        }

        public HistogramDoubleDistribution(int seed, double min, double max, List<double> frequencies, bool antithetic = false)
            : this(seed, antithetic)
        {
            Configure(min, max, frequencies);
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
        /// Configure this instance with the given data.
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="frequencies"></param>
        public void Configure(double min, double max, List<double> frequencies)
        {
            if (frequencies.Count < 2)
                throw new ArgumentException("The number of frequencies provided must be at least 2.");
            if (frequencies.Sum() > 1d)
                throw new ArgumentException("The sum of the given probabilities must not be greater than 100%.");
            if (frequencies.Sum() < 1d)
                throw new ArgumentException("The sum of the given probabilities must not be smaller than 100%.");

            this.Minimum = min;
            this.Maximum = max;
            this.Frequencies = frequencies;

            double i = min;
            double sum = 0;

            increment = (max - min) / ((double)frequencies.Count - 1);

            foreach (double f in frequencies)
            {
                sum += i * f;
                i+=increment;
            }

            Mean = sum;

            Configured = true;
        }

        #endregion
        #region impl

        /// <summary>
        /// Return a number on the interval (Minimum;Maximum) with respect to the Frequencies.
        /// </summary>
        /// <returns>A number on the interval (Minimum;Maximum) with respect to the Frequencies.</returns>
        public double Next()
        {
            DrawCount++;

            int index = rnd.ChooseIndex(Frequencies);
            double result = (double)index * increment;

            if (Interpolate)
            {
                double lower = index == 0 ? 0d : result - increment / 2d;
                double upper = index == Frequencies.Count - 1 ? result : result + increment / 2d;
                return Minimum + rnd.NextDouble() * (upper - lower) + lower;
            }
            else
                return Minimum + result;
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