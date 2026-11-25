using System;
using System.Collections.Generic;
using SimOpt.McpServer.Models;
using SimOpt.Optimization.Interfaces;

namespace SimOpt.McpServer.Optimization
{
    /// <summary>
    /// SIM-68: a point in a <see cref="TopologyProblem"/>'s search space, one real per decision variable.
    /// </summary>
    /// <remarks>
    /// D8: its moves draw from a SplitMix64 generator whose state is part of the solution, so a clone
    /// moves exactly as the original would, and a search started from the same seed takes the same path.
    /// The wall-clock <c>new Random()</c> in the Ivotion operators is what broke that (SIM-59).
    /// </remarks>
    public sealed class TopologySolution : IVectorSolution, ITweakable
    {
        private readonly IReadOnlyList<DecisionVariable> variables;
        private ulong state;

        internal TopologySolution(IReadOnlyList<DecisionVariable> variables, double[] parameters, ulong state)
        {
            this.variables = variables;
            Parameters = parameters;
            this.state = state;
        }

        /// <summary>The point, one coordinate per decision variable, before quantisation.</summary>
        public double[] Parameters { get; set; }

        public double Fitness { get; set; } = double.NegativeInfinity;

        public bool HasFitness { get; set; }

        public int CompareTo(ISolution? other) => Fitness.CompareTo(other?.Fitness ?? double.NegativeInfinity);

        public object Clone() =>
            new TopologySolution(variables, (double[])Parameters.Clone(), state) { Fitness = Fitness, HasFitness = HasFitness };

        /// <summary>
        /// Moves one coordinate, chosen at random: an integer or stepped variable by one step up or down,
        /// a real one by up to a tenth of its range; the point stays inside the bounds.
        /// </summary>
        public void Tweak()
        {
            int k = (int)(Next() % (ulong)variables.Count);
            DecisionVariable v = variables[k];
            double move = v.Type == "integer" || v.Step.HasValue
                ? (Next() % 2 == 0 ? -1d : 1d) * (v.Step ?? 1d)
                : (Uniform() * 2d - 1d) * 0.1 * (v.Max - v.Min);
            Parameters[k] = Math.Clamp(Parameters[k] + move, v.Min, v.Max);
            HasFitness = false;
        }

        /// <summary>SplitMix64: small, fast and fully determined by its 64-bit state.</summary>
        internal ulong Next()
        {
            ulong z = state += 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        /// <summary>A uniform draw on [0, 1).</summary>
        internal double Uniform() => (Next() >> 11) * (1d / (1UL << 53));
    }
}
