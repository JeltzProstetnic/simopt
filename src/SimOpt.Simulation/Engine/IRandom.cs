using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimOpt.Simulation.Engine
{
    public interface IRandom
    {
        /// <summary>
        /// Seed value of this seed source. This value is used
        /// to initialize the seed generator. May return null
        /// prior to initialization.
        /// </summary>
        int? Seed { get; }
        
        bool Antithetic { get; }
        bool NonStochasticMode { get; }

        void Reset(int seed, bool antithetic = false, bool nonStochasticMode = false);
        void Reset(bool antithetic = false, bool nonStochasticMode = false);
    }
}
