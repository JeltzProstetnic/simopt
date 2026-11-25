using System;
using System.Collections.Generic;
using SimOpt.McpServer.Models;
using SimOpt.Optimization;
using SimOpt.Optimization.Interfaces;
using SimOpt.Optimization.Strategies.Evolutionary;

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
    public sealed class TopologySolution : IVectorSolution, ITweakable, ICombinable<ISolution>
    {
        private readonly IReadOnlyList<DecisionVariable> variables;
        private SplitMix64 moves;

        internal TopologySolution(IReadOnlyList<DecisionVariable> variables, double[] parameters, ulong state)
        {
            this.variables = variables;
            Parameters = parameters;
            moves = new SplitMix64(state);
        }

        /// <summary>The point, one coordinate per decision variable, before quantisation.</summary>
        public double[] Parameters { get; set; }

        public double Fitness { get; set; } = double.NegativeInfinity;

        public bool HasFitness { get; set; }

        public int CompareTo(ISolution? other) => Fitness.CompareTo(other?.Fitness ?? double.NegativeInfinity);

        public object Clone() =>
            new TopologySolution(variables, (double[])Parameters.Clone(), 0) { Fitness = Fitness, HasFitness = HasFitness, moves = moves };

        /// <summary>
        /// Moves one coordinate, chosen at random: an integer or stepped variable by one step up or down,
        /// a real one by up to a tenth of its range; the point stays inside the bounds.
        /// </summary>
        public void Tweak()
        {
            int k = moves.Below(variables.Count);
            DecisionVariable v = variables[k];
            double move = v.Type == "integer" || v.Step.HasValue
                ? (Next() % 2 == 0 ? -1d : 1d) * (v.Step ?? 1d)
                : (Uniform() * 2d - 1d) * 0.1 * (v.Max - v.Min);
            Parameters[k] = Math.Clamp(Parameters[k] + move, v.Min, v.Max);
            HasFitness = false;
        }

        /// <summary>
        /// SIM-68 step 4, the EA's crossover: each coordinate from either parent at even odds. The child
        /// starts a generator of its own from this one, and has no fitness.
        /// </summary>
        public ISolution CombineWith(ISolution other)
        {
            if (other is not TopologySolution partner || partner.Parameters.Length != Parameters.Length)
                throw new ArgumentException("A TopologySolution combines only with one of the same problem.", nameof(other));
            var child = new double[Parameters.Length];
            for (int i = 0; i < child.Length; i++) child[i] = moves.Next() % 2 == 0 ? Parameters[i] : partner.Parameters[i];
            return new TopologySolution(variables, child, moves.Next());
        }

        internal ulong Next() => moves.Next();

        /// <summary>A uniform draw on [0, 1).</summary>
        internal double Uniform() => moves.Uniform();
    }
}
