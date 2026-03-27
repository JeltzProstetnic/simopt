using SimOpt.Optimization.Interfaces;

namespace SimOpt.Optimization.Strategies.ParticleSwarm
{
    /// <summary>
    /// Configuration for the Particle Swarm Optimization strategy.
    /// </summary>
    public interface IParticleSwarmConfiguration : IConfiguration
    {
        /// <summary>
        /// Number of particles in the swarm.
        /// </summary>
        int SwarmSize { get; set; }

        /// <summary>
        /// Inertia weight — controls momentum of particles.
        /// Typical range: [0.4, 0.9]. Higher values favor exploration.
        /// </summary>
        double InertiaWeight { get; set; }

        /// <summary>
        /// Cognitive coefficient — attraction toward particle's personal best.
        /// Typical value: 2.0
        /// </summary>
        double CognitiveCoefficient { get; set; }

        /// <summary>
        /// Social coefficient — attraction toward global best.
        /// Typical value: 2.0
        /// </summary>
        double SocialCoefficient { get; set; }
    }
}
