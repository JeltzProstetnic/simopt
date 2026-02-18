using System;
using System.Linq;
using System.Collections.Generic;
using SimOpt.Mathematics.Stochastics.Interfaces;
using SimOpt.Mathematics.Stochastics.RandomSources;

namespace SimOpt.Mathematics.Stochastics.Distributions
{
    /// <summary>
    /// Calculate a random number on the interval (Minimum;Maximum) with respect to the Frequencies.
    /// </summary>
    public class HistogramIntegerDistribution : IDistribution<int>, IDistribution<double>
    {
        #region cvar
        
        private IRandomSource rnd;

        #endregion
        #region prop

        public int Minimum { get; set; }
        public int Maximum { get; set; }
        public List<double> Frequencies { get; private set; }

        /// <summary>
        /// Get or set the number of values which have been
        /// drawn from this distribution since the last reset.
        /// </summary>
        public int DrawCount { get; set; }

        #region IDistribution<int>

        public string Name
        {
            get { return "Integer Histogram Distribution"; }
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

        public int NonStochasticValue
        {
            get { return Mean; }
        }

        /// <summary>
        /// Caution: this is the Math.Round of the actual mean!
        /// </summary>
        public int Mean { get; private set; }

        #endregion
        #region IDistribution<double>

        double IDistribution<double>.NonStochasticValue
        {
            get { return Mean; }
        }

        double IDistribution<double>.Mean
        {
            get { return Mean; }
        }

        #endregion


        #endregion
        #region ctor

        public HistogramIntegerDistribution() { }

        public HistogramIntegerDistribution(IRandomSource rnd)
        {
            Initialize(rnd);
        }

        public HistogramIntegerDistribution(int seed, bool antithetic = false)
        {
            Initialize(seed, antithetic);
        }

        public HistogramIntegerDistribution(int min, int max, List<double> frequencies)
        {
            Configure(min, max, frequencies);
        }

        public HistogramIntegerDistribution(IRandomSource rnd, int min, int max, List<double> frequencies) : this(rnd)
        {
            Configure(min, max, frequencies);
        }

        public HistogramIntegerDistribution(int seed, int min, int max, List<double> frequencies, bool antithetic = false) : this(seed, antithetic)
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
        public void Configure(int min, int max, List<double> frequencies)
        {
            if ((max - min) + 1 != frequencies.Count) 
                throw new ArgumentException("The number of frequencies provided must be equal to max-min.");
            if (frequencies.Sum() > 1d) 
                throw new ArgumentException("The sum of the given probabilities must not be greater than 100%.");
            if (frequencies.Sum() < 1d) 
                throw new ArgumentException("The sum of the given probabilities must not be smaller than 100%.");

            this.Minimum = min;
            this.Maximum = max;
            this.Frequencies = frequencies;

            int inc = 0;
            double sum = 0;
            
            foreach (double f in frequencies)
            {
                sum += (double)(min + inc) * f;
                inc++;
            }

            Mean = (int)Math.Round(sum);

            Configured = true;
        }

        #endregion
        #region impl

        /// <summary>
        /// Return a number on the interval (Minimum;Maximum) with respect to the Frequencies.
        /// </summary>
        /// <returns>A number on the interval (Minimum;Maximum) with respect to the Frequencies.</returns>
        public int Next()
        {
            DrawCount++;
            return Minimum + rnd.ChooseIndex(Frequencies);
        }

        #region IDistribution<double>

        double IDistribution<double>.Next()
        {
            return Next();
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