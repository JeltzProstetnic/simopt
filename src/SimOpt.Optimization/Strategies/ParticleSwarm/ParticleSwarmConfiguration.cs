using SimOpt.Optimization.Interfaces;

namespace SimOpt.Optimization.Strategies.ParticleSwarm
{
    /// <summary>
    /// The settings <see cref="ParticleSwarmOptimization"/> reads. The defaults are the constriction
    /// coefficients of Clerc and Kennedy (2002), inertia 0.7298 and 1.49618 for both pulls, which keep a
    /// swarm from flying apart without a velocity limit.
    /// </summary>
    public sealed class ParticleSwarmConfiguration : IParticleSwarmConfiguration
    {
        public int Seed { get; set; }

        /// <summary>Sweeps of the swarm; each moves and evaluates every particle once.</summary>
        public int NumberOfIterations { get; set; }

        public int NumberOfEvaluations { get; set; }

        public int SwarmSize { get; set; } = 10;

        public double InertiaWeight { get; set; } = 0.7298;

        public double CognitiveCoefficient { get; set; } = 1.49618;

        public double SocialCoefficient { get; set; } = 1.49618;

        public double Fitness { get; set; }

        public bool HasFitness { get; set; }

        public int CompareTo(ISolution? other) => Fitness.CompareTo(other?.Fitness ?? double.NegativeInfinity);

        public object Clone() => MemberwiseClone();
    }
}
