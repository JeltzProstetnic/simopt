using System;
using System.Collections.Generic;
using System.Linq;
using SimOpt.Optimization.Interfaces;

namespace SimOpt.Optimization.Strategies.ParticleSwarm
{
    /// <summary>
    /// Particle Swarm Optimization (PSO) strategy.
    ///
    /// Each particle maintains a position (solution candidate), a velocity, and a personal best.
    /// The swarm tracks a global best. At each iteration, particles update their velocity based on
    /// inertia, cognitive (personal best) and social (global best) components, then move to a new position.
    ///
    /// Standard PSO with inertia weight (Shi &amp; Eberhart, 1998).
    /// </summary>
    public class ParticleSwarmOptimization : IStrategy
    {
        #region cvar

        private IParticleSwarmConfiguration config;
        private int iterationsRemaining;
        private bool stopRequested;

        // Swarm state
        private ISolution[] positions;
        private double[][] velocities;
        private ISolution[] personalBests;
        private ISolution globalBest;
        private Random rng;

        #endregion
        #region evnt

        /// <inheritdoc />
        public event BestSolutionChangedHandler BestSolutionChanged;

        #endregion
        #region prop

        /// <inheritdoc />
        public string Name => "Particle Swarm Optimization";

        /// <inheritdoc />
        public string ProcessingStatus { get; private set; }

        /// <inheritdoc />
        public bool IsInitialized { get; private set; }

        #endregion
        #region impl

        /// <summary>
        /// Initializes the strategy with the given PSO configuration.
        /// </summary>
        /// <param name="parameters">Must implement <see cref="IParticleSwarmConfiguration"/>.</param>
        /// <returns><see langword="true"/> if initialization succeeded.</returns>
        public bool Initialize(IConfiguration parameters)
        {
            if (parameters is not IParticleSwarmConfiguration psoConfig)
                throw new ArgumentException("The parameters for this strategy must implement IParticleSwarmConfiguration.");

            config = psoConfig;
            iterationsRemaining = config.NumberOfIterations;
            stopRequested = false;
            IsInitialized = true;
            ProcessingStatus = "Initialized";
            return true;
        }

        /// <summary>
        /// Resets the strategy to its uninitialized state, clearing all swarm data.
        /// </summary>
        public void Reset()
        {
            config = null;
            positions = null;
            velocities = null;
            personalBests = null;
            globalBest = null;
            rng = null;
            iterationsRemaining = 0;
            stopRequested = false;
            IsInitialized = false;
            ProcessingStatus = null;
        }

        /// <summary>
        /// Runs the PSO loop until the configured iteration limit is reached or <see cref="Stop"/> is called.
        /// Yields a clone of the global best solution after each iteration.
        /// </summary>
        /// <param name="problem">The optimization problem to solve.</param>
        /// <returns>A sequence of global-best solution snapshots, one per completed iteration.</returns>
        public IEnumerable<ISolution> Solve(IProblem problem)
        {
            rng = new Random(config.Seed);
            int swarmSize = config.SwarmSize;

            // Initialize swarm
            ProcessingStatus = "Initializing swarm";
            positions = problem.GenerateCandidates(rng.Next(), swarmSize).ToArray();
            if (positions.Length < swarmSize)
            {
                // Pad with more candidates if needed
                var extra = problem.GenerateCandidates(rng.Next(), swarmSize - positions.Length);
                positions = positions.Concat(extra).ToArray();
            }

            // Evaluate initial positions
            foreach (var p in positions)
            {
                if (!p.HasFitness) problem.Evaluate(p);
            }

            // Initialize personal bests as clones of initial positions
            personalBests = positions.Select(p => (ISolution)p.Clone()).ToArray();

            // Find initial global best
            globalBest = (ISolution)personalBests.OrderByDescending(p => p.Fitness).First().Clone();

            // Initialize velocities to small random values
            int dimensions = GetDimensions(positions[0]);
            velocities = new double[swarmSize][];
            for (int i = 0; i < swarmSize; i++)
            {
                velocities[i] = new double[dimensions];
                for (int d = 0; d < dimensions; d++)
                    velocities[i][d] = (rng.NextDouble() - 0.5) * 0.1;
            }

            // Main PSO loop
            ProcessingStatus = "Optimizing";
            while (iterationsRemaining > 0 && !stopRequested)
            {
                for (int i = 0; i < swarmSize; i++)
                {
                    double[] pos = GetParameters(positions[i]);
                    double[] pBest = GetParameters(personalBests[i]);
                    double[] gBest = GetParameters(globalBest);

                    // Update velocity: v = w*v + c1*r1*(pBest - pos) + c2*r2*(gBest - pos)
                    for (int d = 0; d < dimensions; d++)
                    {
                        double r1 = rng.NextDouble();
                        double r2 = rng.NextDouble();
                        velocities[i][d] = config.InertiaWeight * velocities[i][d]
                            + config.CognitiveCoefficient * r1 * (pBest[d] - pos[d])
                            + config.SocialCoefficient * r2 * (gBest[d] - pos[d]);
                    }

                    // Update position: pos = pos + v
                    for (int d = 0; d < dimensions; d++)
                        pos[d] += velocities[i][d];

                    // Apply new position and evaluate
                    SetParameters(positions[i], pos);
                    positions[i].HasFitness = false;
                    problem.Evaluate(positions[i]);

                    // Update personal best
                    if (positions[i].Fitness > personalBests[i].Fitness)
                    {
                        personalBests[i] = (ISolution)positions[i].Clone();

                        // Update global best
                        if (positions[i].Fitness > globalBest.Fitness)
                        {
                            var oldBest = globalBest;
                            globalBest = (ISolution)positions[i].Clone();
                            OnBestSolutionChanged(new BestSolutionChangedEventArgs(oldBest, globalBest));
                        }
                    }
                }

                iterationsRemaining--;
                yield return (ISolution)globalBest.Clone();
            }
        }

        /// <summary>
        /// Signals the strategy to stop after the current iteration completes.
        /// </summary>
        public void Stop()
        {
            stopRequested = true;
            iterationsRemaining = 0;
        }

        /// <summary>
        /// Not supported. PSO does not expose a tunable configuration that implements <see cref="ISolution"/>.
        /// </summary>
        /// <param name="representatives">Representative problem instances.</param>
        /// <param name="tuningStrategy">The strategy to use for tuning.</param>
        /// <returns>Always throws <see cref="NotImplementedException"/>.</returns>
        public bool Tune(IEnumerable<IProblem> representatives, IStrategy tuningStrategy)
        {
            throw new NotImplementedException("This algorithm cannot be tuned.");
        }

        #endregion
        #region util

        protected virtual void OnBestSolutionChanged(BestSolutionChangedEventArgs e)
        {
            BestSolutionChanged?.Invoke(this, e);
        }

        private static double[] GetParameters(ISolution solution)
        {
            // TestSolution has a Parameters property — use reflection or casting
            var prop = solution.GetType().GetProperty("Parameters");
            if (prop != null)
                return (double[])((double[])prop.GetValue(solution)).Clone();

            // Fallback: treat solution as single-value
            return new[] { solution.Fitness };
        }

        private static void SetParameters(ISolution solution, double[] parameters)
        {
            var prop = solution.GetType().GetProperty("Parameters");
            if (prop != null)
            {
                prop.SetValue(solution, parameters);
                return;
            }
        }

        private static int GetDimensions(ISolution solution)
        {
            var prop = solution.GetType().GetProperty("Parameters");
            if (prop != null)
                return ((double[])prop.GetValue(solution)).Length;
            return 1;
        }

        #endregion
    }
}
