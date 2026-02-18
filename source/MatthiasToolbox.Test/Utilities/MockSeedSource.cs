using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Mathematics.Stochastics.Distributions;
using MatthiasToolbox.Simulation.Engine;

namespace MatthiasToolbox.Test.Utilities
{
    public class MockSeedSOurce : ISeedSource
    {
        public int? Seed
        {
            get;
            set;
        }

        public UniformIntegerDistribution SeedGenerator
        {
            get;
            set;
        }

        public IEnumerable<IRandom> RandomGenerators
        {
            get;
            set;
        }

        public void AddRandomGenerator(IRandom generator)
        {

        }

        public int GetRandomSeedFor(int seedID)
        {
            return 0;
        }

        public void Reset()
        {

        }

        public MockSeedSOurce(int seed)
        {
            Seed = seed;
            RandomGenerators = new List<IRandom>();
            SeedGenerator = new UniformIntegerDistribution(seed);
        }
    }
}
