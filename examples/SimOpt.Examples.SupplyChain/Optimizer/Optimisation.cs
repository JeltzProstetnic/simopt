using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MatthiasToolbox.Optimization.Strategies.SimulatedAnnealing;
using MatthiasToolbox.Optimization.Strategies.Evolutionary;

namespace MatthiasToolbox.SupplyChain.Optimizer
{
    public class Optimisation
    {
        public AnnealingAlgorithm AnnealingAlgorithm;
        public EvolutionaryAlgorithm EvolutionaryAlgorithm;
        public Problem Problem;

        public Optimisation()
        {
            Problem = new Problem();
            AnnealingAlgorithm = new AnnealingAlgorithm();
            EvolutionaryAlgorithm = new EvolutionaryAlgorithm();
            SimulatedAnnealingConfiguration sac = new SimulatedAnnealingConfiguration();
            EvolutionaryAlgorithmConfiguration eac = new EvolutionaryAlgorithmConfiguration(123, 150, 10, 20);
            // AnnealingAlgorithm.Initialize(sac);
            EvolutionaryAlgorithm.Initialize(eac);
        }

        public void Solve()
        {
            // AnnealingAlgorithm.Solve(Problem);
            EvolutionaryAlgorithm.Solve(Problem);
        }
    }
}
