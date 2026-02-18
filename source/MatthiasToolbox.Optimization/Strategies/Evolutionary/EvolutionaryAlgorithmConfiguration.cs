using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Optimization;
using MatthiasToolbox.Optimization.Interfaces;
using MatthiasToolbox.Mathematics.Stochastics;

namespace MatthiasToolbox.Optimization.Strategies.Evolutionary
{
    /// <summary>
    /// Configuration class for the optimization algorithm as provided in this namespace.
    /// 
    /// Tip: if more than 1/5 of the children are fitter than their parents your search is too 
    /// local (maybe you should increase mutation rate or decrease the tournament parameter)
    /// If less than 1/5 of the children are fitter than the parents you explore too much.
    /// 
    /// TODO: Predefined elitism, parameter epsilon?
    /// </summary>
    public class EvolutionaryAlgorithmConfiguration : IEvolutionaryAlgorithmConfiguration
    {
        #region cvar

        private int seed;
        private double mutationRate = 1;
        private Random rnd;

        #endregion
        #region prop

        #region Main

        /// <summary>
        /// If the default generation selector is used, this will switch between (µ+λ) and (µ,λ) mode.
        /// Please note that other selectors may ignore this flag.
        /// 
        ///     MU = µ = number of parents to select for mating
        /// LAMBDA = λ = total number of children created by all parents (λ / µ each)
        /// 
        /// KeepParentsAlive = false => (µ,λ) => next generation consists only of children
        /// KeepParentsAlive =  true => (µ+λ) => next generation includes µ parents
        /// </summary>
        public bool KeepParentsAlive { get; set; }

        /// <summary>
        /// Used by the default generation selector.
        /// Please note that other selectors may ignore this setting.
        /// 
        /// MU = µ = number of parents to select for mating
        /// </summary>
        public int Mu { get; set; }

        /// <summary>
        /// Used by the default generation selector.
        /// Please note that other selectors may ignore this setting.
        /// 
        /// LAMBDA = λ = total number of children created by all parents (λ / µ each)
        /// </summary>
        public int Lambda { get; set; }

        /// <summary>
        /// For use with tournament selection: the t parameter (number of rounds)
        /// </summary>
        public int NumberOfTournamentRounds { get; set; }

        #endregion
        #region ISolution

        /// <summary>
        /// The fitness value of this configuration for use in tuning.
        /// This will be set by a tuning problem class.
        /// </summary>
        public double Fitness { get; set; }

        /// <summary>
        /// Indicates if a valid fitness value is available for this instance.
        /// </summary>
        public bool HasFitness { get; set; }

        #endregion
        #region IConfiguration

        /// <summary>
        /// A seed for random numbers.
        /// </summary>
        public int Seed
        {
            get { return seed; }
            set 
            {
                seed = value;
                rnd = new Random(seed);
            }
        }

        /// <summary>
        /// Gets and sets the number of generations.
        /// </summary>
        public int NumberOfIterations
        {
            get { return Generations; }
            set { Generations = value; }
        }

        /// <summary>
        /// Setting this sets the number of generations to the given value divided by
        /// the generation size. To track the actual number of evaluations you must
        /// count how often IProblem.Evaluate is called.
        /// </summary>
        public int NumberOfEvaluations
        {
            get { return Generations * PopulationSize; }
            set { Generations = value / PopulationSize; }
        }

        #endregion
        #region IEvolutionaryAlgorithmConfiguration

        /// <summary>
        /// The number of generations to process.
        /// </summary>
        public int Generations { get; set; }

        /// <summary>
        /// The size of one generation. Setting this does 
        /// not change MU and LAMBDA. These have to be set 
        /// separately when using the default selectors.
        /// </summary>
        public int PopulationSize { get; set; }
        
        /// <summary>
        /// The mutation operator to use.
        /// </summary>
        public IMutationOperator Mutation { get; set; }

        /// <summary>
        /// The crossover operator to use.
        /// </summary>
        public ICrossoverOperator Crossover { get; set; }

        /// <summary>
        /// Default is 1. This is the fraction of children to be changed by a mutation.
        /// </summary>
        public double MutationRate { get { return mutationRate; } set { mutationRate = value; } }
        
        /// <summary>
        /// An elite list for evolutionary algorithms using elitism.
        /// This should be set to an empty list if no elitism is used.
        /// </summary>
        public List<ISolution> InitialElite { get; set; }

        /// <summary>
        /// The function used to select the next generation from
        /// the elite, parents and children of the current one.
        /// </summary>
        public GenerationSelector SelectNewGeneration { get; set; }

        /// <summary>
        /// The function used to select individuals as potential
        /// parents from the current generation.
        /// </summary>
        public FractionSelector SelectAsParent { get; set; }

        /// <summary>
        /// The function used to select pairs of 
        /// individuals for the crossover operation.
        /// </summary>
        public MatingSelector SelectForMating { get; set; }
        
        /// <summary>
        /// The function used to select parent individuals
        /// to survive for µ+λ mode.
        /// </summary>
        public FractionSelector SelectToSurvive { get; set; }
        
        /// <summary>
        /// The function used to update the elite list using
        /// the current elite list and the new parent list.
        /// </summary>
        public EliteSelector SelectElite { get; set; }

        #endregion

        #endregion
        #region ctor

        /// <summary>
        /// Empty constructor for cloning and serialization. InitialElite will be set to an empty list.
        /// </summary>
        internal EvolutionaryAlgorithmConfiguration() { InitialElite = new List<ISolution>(); }

        /// <summary>
        /// Default constructor.
        /// 
        /// SelectAsParent, SelectForMating, SelectToSurvive, SelectElite and 
        /// SelectNewGeneration will be set to the default implementations 
        /// DefaultParentSelector, DefaultCoupleSelector, 
        /// DefaultSurvivorSelector, DefaultEliteSelector and
        /// DefaultGenerationSelector as provided in this class.
        /// </summary>
        /// <param name="seed">A random seed value.</param>
        /// <param name="iterations">The number of generations to process.</param>
        /// <param name="mu">
        /// Used by the default generation selector.
        /// Mu = µ = number of parents to select for mating
        /// </param>
        /// <param name="lambda">
        /// Used by the default generation selector.
        /// Lambda = λ = total number of children created by all parents (λ / µ each)
        /// </param>
        /// <param name="mutation">
        /// The IMutationOperator to use for mutation. If not given, the SimpleMutation 
        /// operator will be used. CAUTION: This requires that the solution candidates 
        /// implement ITweakable, <code>IParametrizedTweakable&lt;int></code> 
        /// or <code>IParametrizedTweakable&lt;Tuple&lt;int, int>></code> 
        /// in addition to ISolution. <see cref="SimpleMutation"/>
        /// </param>
        /// <param name="crossover">
        /// The ICrossoverOperator to use for crossover. If not given, the SimpleCrossover
        /// operator will be used. CAUTION: This requires that the solution candidates 
        /// implement ICombinable in addition to ISolution. <see cref="SimpleCrossover"/>
        /// </param>
        /// <param name="keepParentsAlive">
        /// This will switch between (µ+λ) and (µ,λ) mode.
        /// KeepParentsAlive = false => (µ,λ) => next generation consists only of children
        /// KeepParentsAlive =  true => (µ+λ) => next generation includes µ parents
        /// </param>
        public EvolutionaryAlgorithmConfiguration(
            int seed, 
            int iterations, 
            int mu, 
            int lambda, 
            IMutationOperator mutation = null, 
            ICrossoverOperator crossover = null, 
            bool keepParentsAlive = false) 
        {
            InitialElite = new List<ISolution>();
            
            this.Mu = mu;
            this.Seed = seed;
            this.Lambda = lambda;
            this.NumberOfIterations = iterations;
            this.KeepParentsAlive = keepParentsAlive;

            if (keepParentsAlive) this.PopulationSize = mu + lambda;
            else this.PopulationSize = lambda;
            
            SelectElite = DefaultEliteSelector;
            SelectAsParent = DefaultParentSelector;
            SelectForMating = DefaultCoupleSelector;
            SelectToSurvive = DefaultSurvivorSelector;
            SelectNewGeneration = DefaultGenerationSelector;

            if (mutation == null) this.Mutation = new SimpleMutation();
            else this.Mutation = mutation;

            if (crossover == null) this.Crossover = new SimpleCrossover();
            else this.Crossover = crossover;
        }

        /// <summary>
        /// Tournament selection constructor.
        /// 
        /// SelectAsParent, SelectForMating, SelectToSurvive and SelectElite 
        /// will be set to the default implementations DefaultParentSelector, 
        /// DefaultCoupleSelector, DefaultSurvivorSelector and DefaultEliteSelector 
        /// as provided in this class. SelectNewGeneration will be set to the
        /// TournamentGenerationSelector.
        /// </summary>
        /// <param name="seed">A random seed value.</param>
        /// <param name="iterations">The number of generations to process.</param>
        /// <param name="mu">
        /// Used by the default generation selector.
        /// Mu = µ = number of parents to select for mating
        /// </param>
        /// <param name="lambda">
        /// Used by the default generation selector.
        /// Lambda = λ = total number of children created by all parents (λ / µ each)
        /// </param>
        /// <param name="t">
        /// Tournament parameter for the number of rounds. This can be changed 
        /// during optimization using the NumberOfTournamentRounds property.
        /// </param>
        /// <param name="mutation">
        /// The IMutationOperator to use for mutation. If not given, the SimpleMutation 
        /// operator will be used. CAUTION: This requires that the solution candidates 
        /// implement ITweakable, <code>IParametrizedTweakable&lt;int></code> 
        /// or <code>IParametrizedTweakable&lt;Tuple&lt;int, int>></code> 
        /// in addition to ISolution. <see cref="SimpleMutation"/>
        /// </param>
        /// <param name="crossover">
        /// The ICrossoverOperator to use for crossover. If not given, the SimpleCrossover
        /// operator will be used. CAUTION: This requires that the solution candidates 
        /// implement ICombinable in addition to ISolution. <see cref="SimpleCrossover"/>
        /// </param>
        /// <param name="keepParentsAlive">
        /// This will switch between (µ+λ) and (µ,λ) mode.
        /// KeepParentsAlive = false => (µ,λ) => next generation consists only of children
        /// KeepParentsAlive =  true => (µ+λ) => next generation includes µ parents
        /// </param>
        public EvolutionaryAlgorithmConfiguration(
            int seed,
            int iterations,
            int mu,
            int lambda,
            int t, 
            IMutationOperator mutation = null,
            ICrossoverOperator crossover = null,
            bool keepParentsAlive = false)
            :this(seed, iterations, mu, lambda, mutation, crossover, keepParentsAlive)
        {
            NumberOfTournamentRounds = t;
            SelectNewGeneration = TournamentGenerationSelector;
        }

        #endregion
        #region impl

        #region Main

        /// <summary>
        /// This selects subsequent pairs from the given parents. In case 
        /// of an odd number of parents the first individual is used twice.
        /// In case of only one item, a tuple with both items being the same
        /// individual will be returned. If no candidates were provided
        /// this will return an empty enumerable.
        /// </summary>
        /// <param name="parents">The parents to process.</param>
        /// <returns>A list of couples for mating.</returns>
        public IEnumerable<Tuple<ISolution, ISolution>> DefaultCoupleSelector(List<ISolution> parents)
        {
            if (parents.Count == 0) yield break;
            ISolution firstParent = parents[0];
            if (parents.Count == 1) 
            {
                yield return new Tuple<ISolution, ISolution>(firstParent, firstParent);
                yield break;
            }

            for (int i = 1; i <= parents.Count; i += 2)
            {
                if (i == parents.Count)
                {
                    yield return new Tuple<ISolution, ISolution>(parents[i - 1], parents[0]);
                }
                else
                {
                    yield return new Tuple<ISolution, ISolution>(parents[i - 1], parents[i]);
                }
            }
        }

        /// <summary>
        /// Simply selects the first µ (MU) items from the current generation.
        /// </summary>
        /// <param name="currentGeneration">The current generation.</param>
        /// <returns>A list of parents.</returns>
        public IEnumerable<ISolution> DefaultParentSelector(List<ISolution> currentGeneration)
        {
            int max = Math.Min(Mu, currentGeneration.Count);
            for (int i = 0; i < max; i += 1) 
            {
                yield return currentGeneration[i];
            }
        }

        /// <summary>
        /// This will not kill anyone.
        /// </summary>
        /// <param name="currentParents">The parents in the current generation.</param>
        /// <returns>The complete contents of currentParents.</returns>
        public IEnumerable<ISolution> DefaultSurvivorSelector(List<ISolution> currentParents)
        {
            foreach (ISolution s in currentParents) yield return s;
        }

        /// <summary>
        /// This will select the next generation using the µ (MU) and λ (LAMBDA) parameters.
        /// If KeepParentsAlive is set to true, it will select µ parents and λ children, 
        /// otherwise only a maximum of λ children will be returned. The elite will be ignored.
        /// </summary>
        /// <param name="children">A list of children to select from.</param>
        /// <param name="parents">A list of parent individuals to select from.</param>
        /// <param name="elite">Will be ignored.</param>
        /// <returns>A new generation with no elitism.</returns>
        public IEnumerable<ISolution> DefaultGenerationSelector(List<ISolution> children, List<ISolution> parents, List<ISolution> elite)
        {
            int i = 0;
            if (KeepParentsAlive)
            {
                // return a maximum of λ children
                foreach (ISolution s in children)
                {
                    i += 1;
                    yield return s;
                    if (i == Lambda) break;
                }

                // return a maximum of µ parents
                i = 0;
                foreach (ISolution s in parents)
                {
                    i += 1;
                    yield return s;
                    if (i == Mu) break;
                }
            }
            else 
            {
                // return a maximum of λ children
                foreach (ISolution s in children)
                {
                    i += 1;
                    yield return s;
                    if (i == Lambda) break;
                }
            }
        }

        /// <summary>
        /// This will select the next generation using the µ (MU) and λ (LAMBDA) parameters.
        /// If KeepParentsAlive is set to true, it will select µ parents and λ children, 
        /// otherwise only a maximum of λ children will be returned. The elite will be ignored.
        /// Tournament selection will be used with NumberOfTournamentRounds as t parameter.
        /// </summary>
        /// <param name="children">A list of children to select from.</param>
        /// <param name="parents">A list of parent individuals to select from.</param>
        /// <param name="elite">Will be ignored.</param>
        /// <returns>A new generation with no elitism.</returns>
        public IEnumerable<ISolution> TournamentGenerationSelector(List<ISolution> children, List<ISolution> parents, List<ISolution> elite)
        {
            if (KeepParentsAlive)
            {
                // return a maximum of λ children
                for (int i = 0; i < Lambda; i++)
                {
                    yield return children.TournamentSelect(rnd, NumberOfTournamentRounds);
                }

                // return a maximum of µ parents
                for (int i = 0; i < Mu; i++)
                {
                    yield return parents.TournamentSelect(rnd, NumberOfTournamentRounds);
                }
            }
            else
            {
                // return a maximum of λ children
                for (int i = 0; i < Lambda; i++)
                {
                    yield return children.TournamentSelect(rnd, NumberOfTournamentRounds);
                }
            }
        }

        /// <summary>
        /// Returns only the current elite
        /// </summary>
        /// <param name="currentGeneration">Will be ignored</param>
        /// <param name="currentElite">Will be returned</param>
        /// <returns>The current elite.</returns>
        public IEnumerable<ISolution> DefaultEliteSelector(List<ISolution> currentGeneration, List<ISolution> currentElite)
        {
            foreach (ISolution s in currentElite) yield return s;
        }

        #endregion
        #region IComparable

        public int CompareTo(ISolution other)
        {
            return Fitness.CompareTo(other.Fitness);
        }

        #endregion
        #region IClonable

        /// <summary>
        /// Creates a clone of this instance.
        /// </summary>
        /// <returns>A clone of this instance.</returns>
        public object Clone()
        {
            EvolutionaryAlgorithmConfiguration clone = new EvolutionaryAlgorithmConfiguration();
            clone.Crossover = Crossover;
            clone.Fitness = Fitness;
            clone.Generations = Generations;
            clone.PopulationSize = PopulationSize;
            clone.HasFitness = HasFitness;
            clone.InitialElite = InitialElite;
            clone.KeepParentsAlive = KeepParentsAlive;
            clone.Lambda = Lambda;
            clone.Mu = Mu;
            clone.Mutation = Mutation;
            clone.Seed = Seed;
            clone.SelectAsParent = SelectAsParent;
            clone.SelectElite = SelectElite;
            clone.SelectForMating = SelectForMating;
            clone.SelectNewGeneration = SelectNewGeneration;
            clone.SelectToSurvive = SelectToSurvive;
            return clone;
        }

        #endregion

        #endregion
    }
}
