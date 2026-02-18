using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Optimization.Interfaces;

namespace SimOpt.Optimization.Strategies.Evolutionary
{
    /// <summary>
    /// Interface for a crossover operation in evolutionary algorithms.
    /// The operator should be of cardinality 2 (taking two parents) and
    /// usually returns two children, but may also return n children.
    /// The number of children to return can be configured via GrowFactor.
    /// </summary>
    public interface ICrossoverOperator : IOperator<IEnumerable<ISolution>>
    {
        /// <summary>
        /// The number of events.
        /// </summary>
        int Size { get; set; }

        /// <summary>
        /// The number of results to produce.
        /// </summary>
        int GrowthFactor { get; set; }

        /// <summary>
        /// Set the initial configuration of this instance.
        /// </summary>
        /// <param name="seed">The random seed for stochastic operations.</param>
        /// <param name="growthFactor">The number of results to produce.</param>
        /// <param name="size">The number of events.</param>
        void Initialize(int seed, int growthFactor, int size);

        /// <summary>
        /// Set or change the configuration of this instance.
        /// </summary>
        /// <param name="growthFactor">The number of results to produce.</param>
        /// <param name="size">The number of events.</param>
        void Configure(int growthFactor, int size);
        
        /// <summary>
        /// Set or change the configuration of this instance.
        /// </summary>
        /// <param name="size">The number of events.</param>
        void Configure(int size);
    }
}
