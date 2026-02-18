// Coded by Matthias Gruber

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Mathematics.Stochastics.Interfaces
{
    public interface IDistribution
    {
        #region prop

        string Name { get; }
        int? Seed { get; set; }
        bool Antithetic { get; set; }

        /// <summary>
        /// Get or set the number of values which have been
        /// drawn from this distribution since the last reset.
        /// </summary>
        int DrawCount { get; set; }

        /// <summary>
        /// Return true if the random source is 
        /// initialized with a seed
        /// </summary>
        bool Initialized { get; }

        /// <summary>
        /// Return true if the distribution-
        /// specific parameters are set
        /// </summary>
        bool Configured { get; }

        #endregion
        #region init

        void Initialize(int seed, bool antithetic = false);
        void Initialize(IRandomSource rnd);

        #endregion
        #region rset

        void Reset();
        void Reset(int seed);
        void Reset(int seed, bool antithetic);
        void Reset(bool antithetic);

        #endregion
    }

    public interface IDistribution<T> : IDistribution
    {
        #region prop

        /// <summary>
        /// Return a value which is not stochastically generated. 
        /// For most distributions this will be the mean value.
        /// </summary>
        T NonStochasticValue { get; }

        /// <summary>
        /// Return a mean value or NaN if not applicable
        /// </summary>
        T Mean { get; }

        #endregion
        #region impl

        /// <summary>
        /// return the next random number
        /// </summary>
        /// <returns></returns>
        T Next();

        #endregion
    }
}