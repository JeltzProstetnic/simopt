using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Mathematics.Stochastics.Distributions;

namespace MatthiasToolbox.Simulation.Engine
{
    /// <summary>
    /// Interface for classes which provide seeds for random
    /// distributions. The Random class in this namespace
    /// will use this. All contained random generators will be
    /// reset automatically when this seed source is reset.
    /// </summary>
    /// <remarks>beta</remarks>
    public interface ISeedSource : IResettable
    {
        /// <summary>
        /// Seed value of this seed source. This value is used
        /// to initialize the seed generator. May return null
        /// prior to initialization.
        /// </summary>
        int? Seed { get; }

        /// <summary>
        /// A random generator to pull seed values from.
        /// Be aware that the order in which seeds are 
        /// pulled can change the whole experiment, so be
        /// sure to always pull seeds in the same order.
        /// </summary>
        UniformIntegerDistribution SeedGenerator { get; set; }

        /// <summary>
        /// the contained random generators which are 
        /// managed by this seed source
        /// </summary>
        IEnumerable<IRandom> RandomGenerators { get; }

        /// <summary>
        /// Add a random manager (generator)
        /// to this seed source, so it can be reset when needed.
        /// </summary>
        /// <param name="distribution"></param>
        void AddRandomGenerator(IRandom generator);

        /// <summary>
        /// Get a random seed for a certain ID value. Given
        /// the same base seed and the same seed ID, the
        /// returned by this will always be the same.
        /// </summary>
        /// <param name="seedID">
        /// A unique integer id for the seed generation, 
        /// intended for objects which can be created
        /// in different order.
        /// </param>
        int GetRandomSeedFor(int seedID);
    }
}
