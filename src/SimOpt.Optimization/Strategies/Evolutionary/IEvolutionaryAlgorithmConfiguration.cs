using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Optimization.Interfaces;

namespace SimOpt.Optimization.Strategies.Evolutionary
{
    /// <summary>
    /// configuration interface for an evolutionary algorithm as provided in this namespace
    /// </summary>
    public interface IEvolutionaryAlgorithmConfiguration : IConfiguration, ISolution
    {
        #region prop

        /// <summary>
        /// The number of generations to process.
        /// </summary>
        int Generations { get; set; }

        /// <summary>
        /// The size of a generation at the beginning and end of each cycle.
        /// </summary>
        int PopulationSize { get; set; }

        /// <summary>
        /// The operator used to mutate individuals.
        /// </summary>
        IMutationOperator Mutation { get; set; }

        /// <summary>
        /// The operator used to generate crossover versions of individuals.
        /// </summary>
        ICrossoverOperator Crossover { get; set; }

        /// <summary>
        /// The percentage of items to be changed by a point mutation per generation.
        /// </summary>
        double MutationRate { get; set; }

        /// <summary>
        /// A list of elite individuals. Return an empty <code>List<ISolution></code>
        /// if you do not want to use elitism.
        /// </summary>
        List<ISolution> InitialElite { get; set; }

        /// <summary>
        /// Return mating pairs from the given parents.
        /// </summary>
        MatingSelector SelectForMating { get; set; }

        /// <summary>
        /// Return selected parents for mating.
        /// </summary>
        FractionSelector SelectAsParent { get; set; }

        /// <summary>
        /// Select the individuals to survive from the current list of parents.
        /// </summary>
        FractionSelector SelectToSurvive { get; set; }

        /// <summary>
        /// Select a new elite from the current generation and the old elite list.
        /// </summary>
        EliteSelector SelectElite { get; set; }

        /// <summary>
        /// Return a new generation picked from the given lists.
        /// </summary>
        GenerationSelector SelectNewGeneration { get; set; }

        #endregion
    }
}
