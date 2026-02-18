using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Optimization.Interfaces
{
    /// <summary>
    /// a solution candidate for an optimization problem
    /// </summary>
    public interface ISolution : IComparable<ISolution>, ICloneable
    {
        /// <summary>
        /// The fitness value of the solution candidate.
        /// This should be set to -double.MaxValue for invalid 
        /// candidates as soon as this becomes known. If the
        /// fitness value is no longer valid (e. g. a tweaking
        /// operation was applied to the candidate) set
        /// HasFitness to false instead.
        /// </summary>
        double Fitness { get; set; }

        /// <summary>
        /// Return true if a valid fitness value is available.
        /// </summary>
        bool HasFitness { get; set; }
    }
}
