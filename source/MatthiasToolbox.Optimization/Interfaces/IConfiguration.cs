using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Optimization.Interfaces
{
    /// <summary>
    /// A configuration or configuration candidate for some IStrategy.
    /// </summary>
    public interface IConfiguration : ISolution
    {
        /// <summary>
        /// A random seed
        /// </summary>
        int Seed { get; set; }

        /// <summary>
        /// The number of iterations for the strategy. The
        /// actual meaning of this depends on the algorithm.
        /// </summary>
        int NumberOfIterations { get; set; }

        /// <summary>
        /// The number of evaluations the strategy should
        /// aim for. This does not necessarily hold exactly.
        /// </summary>
        int NumberOfEvaluations { get; set; }
    }
}
