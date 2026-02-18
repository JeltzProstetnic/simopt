using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Mathematics.Stochastics.Interfaces;
using SimOpt.Mathematics.Stochastics.RandomSources;

namespace SimOpt.Mathematics.Stochastics.Distributions
{
    public class ProbabilisticDistribution : IDistribution<int>, IDistribution<double>
    {
        #region cvar

        private List<int> pool = new List<int>();
        private List<int> currentPool = new List<int>();
        private IRandomSource rnd;

        #endregion
        #region prop

        public int Minimum { get; set; }
        public int Maximum { get; set; }
        public List<int> Frequencies { get; private set; }

        /// <summary>
        /// Get or set the number of values which have been
        /// drawn from this distribution since the last reset.
        /// </summary>
        public int DrawCount { get; set; }

        #region IDistribution<int>

        public string Name
        {
            get { return "Special Probabilistic Distribution"; }
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

        public bool Configured
        {
            get; private set;
        }

        public Func<int> NonStochasticValueDelegate { get; set; }

        public int NonStochasticValue
        {
            get { return NonStochasticValueDelegate.Invoke(); }
        }

        public int Mean
        {
            get; set;
        }

        #endregion
        #region IDistribution<double> Member

        double IDistribution<double>.NonStochasticValue
        {
            get { return NonStochasticValue; }
        }

        double IDistribution<double>.Mean
        {
            get { return Mean; }
        }

        #endregion

        #endregion
        #region ctor

        public ProbabilisticDistribution() 
        {
            NonStochasticValueDelegate = () => 0;
        }

        public ProbabilisticDistribution(IRandomSource rnd) : this()
        {
            Initialize(rnd);
        }

        public ProbabilisticDistribution(int seed, bool antithetic = false)
            : this()
        {
            Initialize(seed, antithetic);
        }

        public ProbabilisticDistribution(int min, int max, List<int> frequencies)
            : this()
        {
            Configure(min, max, frequencies);
        }

        public ProbabilisticDistribution(IRandomSource rnd, int min, int max, List<int> frequencies) : this(rnd)
        {
            Configure(min, max, frequencies);
        }

        public ProbabilisticDistribution(int seed, int min, int max, List<int> frequencies, bool antithetic = false)
            : this(seed, antithetic)
        {
            Configure(min, max, frequencies);
        }

        public ProbabilisticDistribution(Func<int> nonStochasticDelegate)
        {
            NonStochasticValueDelegate = nonStochasticDelegate;
        }

        public ProbabilisticDistribution(IRandomSource rnd, Func<int> nonStochasticDelegate)
            : this()
        {
            Initialize(rnd);
        }

        public ProbabilisticDistribution(int seed, Func<int> nonStochasticDelegate, bool antithetic = false)
            : this(nonStochasticDelegate)
        {
            Initialize(seed, antithetic);
        }

        public ProbabilisticDistribution(int min, int max, List<int> frequencies, Func<int> nonStochasticDelegate)
            : this(nonStochasticDelegate)
        {
            Configure(min, max, frequencies);
        }

        public ProbabilisticDistribution(IRandomSource rnd, int min, int max, List<int> frequencies, Func<int> nonStochasticDelegate)
            : this(rnd, nonStochasticDelegate)
        {
            Configure(min, max, frequencies);
        }

        public ProbabilisticDistribution(int seed, int min, int max, List<int> frequencies, Func<int> nonStochasticDelegate, bool antithetic = false)
            : this(seed, nonStochasticDelegate, antithetic)
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
        public void Configure(int min, int max, List<int> frequencies)
        {
            if ((max - min) + 1 != frequencies.Count)
                throw new ArgumentException("The number of frequencies provided must be equal to max-min.");

            this.Minimum = min;
            this.Maximum = max;
            this.Frequencies = frequencies;

            #region calc mean

            int inc = 0;
            double sum = 0;
            double total = frequencies.Sum();

            foreach (int f in frequencies)
            {
                sum += (double)(min + inc) * f;
                inc++;
            }

            Mean = (int)Math.Round(sum / total);

            #endregion
            #region prepare pool

            int n = 0;
            for (int i = Minimum; i <= Maximum; i++)
            {
                for (int j = 0; j < Frequencies[n]; j++)
                {
                    pool.Add(i);
                }
                n += 1;
            }

            #endregion

            Configured = true;
        }

        #endregion
        #region impl

        #region IDistribution<int>

        public int Next()
        {
            DrawCount++;

            if (currentPool.Count == 0)
                currentPool.AddRange(pool);

            int result = currentPool.RandomItem(rnd);
            currentPool.Remove(result);
            return result;
        }

        #endregion
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
            currentPool.Clear();
        }

        public void Reset(int seed)
        {
            DrawCount = 0;
            rnd.Reset(seed);
            currentPool.Clear();
        }

        public void Reset(int seed, bool antithetic)
        {
            DrawCount = 0;
            rnd.Reset(seed, antithetic);
            currentPool.Clear();
        }

        public void Reset(bool antithetic)
        {
            DrawCount = 0;
            rnd.Reset(antithetic);
            currentPool.Clear();
        }

        #endregion
    }
}