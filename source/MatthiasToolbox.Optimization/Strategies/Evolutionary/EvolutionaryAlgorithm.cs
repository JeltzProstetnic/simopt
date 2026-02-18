using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Optimization.Interfaces;
using MatthiasToolbox.Mathematics.Stochastics;

namespace MatthiasToolbox.Optimization.Strategies.Evolutionary
{
    /// <summary>
    /// A generic, object oriented evolutionary algorithm.
    /// </summary>
    /// <remarks>
    /// <ul>
    /// <li>Tuning is not implemented yet.</li>
    /// <li>This could be extended with a Taboo concept.</li>
    /// </ul>
    /// </remarks>
    public class EvolutionaryAlgorithm : IStrategy
    {
        #region cvar

        #region static

        public readonly static string StatusUndefined = "Undefined";
        public readonly static string StatusInitializing = "Initializing";
        public readonly static string StatusEvaluating = "Evaluating";
        public readonly static string StatusMutating = "Mutating";
        public readonly static string StatusProcreating = "Procreating";
        public readonly static string StatusFinished = "Finished";

        #endregion
        #region main

        private IEvolutionaryAlgorithmConfiguration config;
        private Random rnd;

        #endregion
        #region temporary

        private bool bestSolutionChanged;
        private ISolution previousBestSolution;
        private List<ISolution> fillingCandidates = new List<ISolution>();

        #endregion

        #endregion
        #region dele

        /// <summary>
        /// Handler signature for the GenerationFinishedHandler event.
        /// </summary>
        /// <param name="sender">The IStrategy which invoked the event.</param>
        /// <param name="e">The event arguments</param>
        public delegate void GenerationFinishedHandler(object sender, GenerationFinishedEventArgs e);

        #endregion
        #region evnt

        /// <summary>
        /// Occurs each time a generation was processed.
        /// </summary>
        public event GenerationFinishedHandler GenerationFinished;

        /// <summary>
        /// Occurs each time a new best solution was found, but not more than once per generation.
        /// </summary>
        public event BestSolutionChangedHandler BestSolutionChanged;

        #endregion
        #region prop

        #region Main

        public string Name { get { return "Evolutionary Algorithm A"; } }

        /// <summary>
        /// Indicate if the strategy is already initialized.
        /// </summary>
        public bool IsInitialized { get; private set; }

        public IProblem CurrentProblem { get; private set; }

        public List<ISolution> CurrentGeneration { get; private set; }

        public List<ISolution> CurrentElite { get; private set; }

        #endregion
        #region Status

        /// <summary>
        /// The number of generations remaining to process.
        /// </summary>
        public int RemainingGenerations { get; private set; }

        /// <summary>
        /// The number of generations already processed.
        /// </summary>
        public int ProcessedGenerations
        {
            get
            {
                if (config == null) return 0; 
                return config.Generations - RemainingGenerations;
            }
        }

        /// <summary>
        /// The current status of the strategy.
        /// </summary>
        public string ProcessingStatus { get; private set; }

        /// <summary>
        /// The best solution found so far.
        /// </summary>
        public ISolution BestSolution { get; private set; }

        /// <summary>
        /// The best solution found in the current generation.
        /// </summary>
        public ISolution CurrentGenerationBestSolution { get; private set; }

        /// <summary>
        /// The average fitness of the current generation
        /// </summary>
        public double CurrentGenerationAverageFitness { get; private set; }

        #endregion

        #endregion
        #region ctor

        /// <summary>
        /// default constructor
        /// </summary>
        public EvolutionaryAlgorithm() { }

        #endregion
        #region init

        /// <summary>
        /// Initialize the strategy.
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns>always true</returns>
        public bool Initialize(IConfiguration parameters)
        {
            if (!(parameters is IEvolutionaryAlgorithmConfiguration)) return false;
            ProcessingStatus = StatusInitializing;
            config = parameters as IEvolutionaryAlgorithmConfiguration;
            rnd = new Random(config.Seed);
            bestSolutionChanged = false;
            // CurrentGeneration = new SortedDictionary<double, ISolution>();
            CurrentGeneration = new List<ISolution>();
            CurrentElite = config.InitialElite;
            CurrentProblem = null;
            RemainingGenerations = config.Generations;
            BestSolution = null;
            CurrentGenerationBestSolution = null;
            CurrentGenerationAverageFitness = 0;
            IsInitialized = true;
            previousBestSolution = null;
            return true;
        }

        #endregion
        #region rset

        /// <summary>
        /// Resets the strategy. All data will be emptied.
        /// </summary>
        public void Reset()
        {
            bestSolutionChanged = false;
            config = null;
            rnd = null;
            CurrentGeneration = null;
            CurrentElite = null;
            CurrentProblem = null;
            RemainingGenerations = 0;
            BestSolution = null;
            CurrentGenerationBestSolution = null;
            CurrentGenerationAverageFitness = 0;
            previousBestSolution = null;
            ProcessingStatus = StatusUndefined;
            IsInitialized = false;
        }

        #endregion
        #region impl

        #region main

        /// <summary>
        /// Solves the optimization problem and returns the last generation.
        /// </summary>
        /// <param name="problem"></param>
        /// <returns>The last generation</returns>
        public IEnumerable<ISolution> Solve(IProblem problem)
        {
            // initialize status info
            CurrentProblem = problem;
            
            // get first generation
            List<ISolution> candidates = problem.GenerateCandidates(config.Seed, config.PopulationSize).ToList();
            if (candidates.Count() == 0) throw new ArgumentException("No candidates were provided!");

            // evaluate
            Evaluate(candidates);

            // create first generation
            CreateCurrentGeneration(ref candidates);

            // evolve
            while (RemainingGenerations > 0) ProcessGeneration(CurrentGeneration);

            // done
            ProcessingStatus = StatusFinished;
            return CurrentGeneration;
        }

        /// <summary>
        /// Stops the solver by setting the remaining generations to zero.
        /// </summary>
        public void Stop()
        {
            RemainingGenerations = 0;
        }
        
        /// <summary>
        /// This function processes a generation applying the following steps:
        /// <ol>
        /// <li>Update the elite list by invoking the EliteSelector</li>
        /// <li>Select parents from the given generation (sorted descending by fitness) using the ParentSelector.</li>
        /// <li>Breed the parents using the MatingSelector to create pairs and applying the crossover operator</li>
        /// <li>Mutate the newly created individuals using the mutation operator</li>
        /// <li>Kill the parents which were not selected to survive by the SelectToSurvive function</li>
        /// <li>Select a new generation from parents, children and elite using the SelectNewGeneration function</li>
        /// <li>Evaluate the new generation and save it to CurrentGeneration</li>
        /// </ol>
        /// </summary>
        /// <param name="generation">The individuals to evolve.</param>
        public void ProcessGeneration(List<ISolution> generation) 
        {
            ProcessingStatus = StatusProcreating;
            CurrentElite = UpdateElite(generation, CurrentElite);
            List<ISolution> parents = SelectParents(generation);
            List<ISolution> children = Breed(parents).ToList();
            ProcessingStatus = StatusMutating;
            List<ISolution> mutatedChildren = Mutate(children);
            List<ISolution> oldies = Kill(parents);
            List<ISolution> newGeneration = config.SelectNewGeneration.Invoke(mutatedChildren, oldies, CurrentElite).ToList();

            CurrentGenerationAverageFitness = Evaluate(newGeneration);
            CreateCurrentGeneration(ref newGeneration);

            RemainingGenerations--;
            OnGenerationFinished(new GenerationFinishedEventArgs(CurrentGeneration));
            if (bestSolutionChanged)
            {
                bestSolutionChanged = false;
                OnBestSolutionChanged(new BestSolutionChangedEventArgs(previousBestSolution, BestSolution));
            }
        }

        #endregion
        #region evolution helpers

        private List<ISolution> UpdateElite(List<ISolution> individuals, List<ISolution> currentElite)
        {
            return config.SelectElite.Invoke(individuals, currentElite).ToList();
        }

        private List<ISolution> SelectParents(List<ISolution> potentialParents)
        {
            return config.SelectAsParent.Invoke(potentialParents).ToList();
        }

        private IEnumerable<ISolution> Breed(List<ISolution> parents)
        {
            foreach (Tuple<ISolution, ISolution> couple in config.SelectForMating(parents))
            {
                foreach (ISolution kid in config.Crossover.Apply(couple.Item1, couple.Item2))
                    yield return kid;
            }
        }

        private List<ISolution> Mutate(List<ISolution> individuals)
        {
            List<ISolution> result = new List<ISolution>();
            foreach (ISolution individual in individuals)
            {
                if (!rnd.ExecuteConditionally(() => result.Add(config.Mutation.Apply(individual)), config.MutationRate))
                    result.Add(individual);
            }
            return result;
        }

        private List<ISolution> Kill(List<ISolution> parents)
        {
            return config.SelectToSurvive(parents).ToList();
        }

        private void CreateCurrentGeneration(ref List<ISolution> individuals) // TODO: rename to expandAndSort
        {
            CurrentGeneration.Clear();
            if (individuals.Count < config.PopulationSize) Expand(ref individuals, config.PopulationSize);
            individuals.Sort();
            CurrentGeneration = individuals;
        }

        private double Evaluate(List<ISolution> individuals)
        {
            CurrentGenerationBestSolution = null;
            double result = 0;
            double counter = 0;
            string oldStatus = ProcessingStatus;
            ProcessingStatus = StatusEvaluating;
            foreach (ISolution individual in individuals)
            {
                if(!individual.HasFitness) CurrentProblem.Evaluate(individual);
                if (BestSolution == null || BestSolution.Fitness <= individual.Fitness)
                {
                    previousBestSolution = BestSolution;
                    BestSolution = individual;
                    bestSolutionChanged = true;
                }
                if (CurrentGenerationBestSolution == null || CurrentGenerationBestSolution.Fitness <= individual.Fitness)
                    CurrentGenerationBestSolution = individual;
                result += individual.Fitness;
                counter += 1;
            }
            ProcessingStatus = oldStatus;
            return result / counter;
        }

        #endregion

        #endregion
        #region util

        /// <summary>
        /// expands the given solution set to the given size by taking
        /// the best candidates, cloning them and then applying a minimal
        /// mutation so as to avoid duplicate solution candidates.
        /// </summary>
        /// <param name="generation"></param>
        /// <param name="targetSize"></param>
        /// <returns>true if expansion was needed, false if nothing was changed</returns>
        private bool Expand(ref List<ISolution> generation, int targetSize)
        {
            if (generation.Count >= targetSize) return false;

            // initialize
            fillingCandidates.Clear();
            int toAdd = targetSize - generation.Count;

            // clone
            foreach (ISolution candidate in generation)
            {
                fillingCandidates.Add(candidate.Clone() as ISolution);
                toAdd--;
                if (toAdd == 0) break;
            }

            // mutate and evaluate
            foreach (ISolution candidate in fillingCandidates)
            {
                config.Mutation.Initialize(rnd.Next(), 1, 1);
                config.Mutation.Apply(candidate);
                CurrentProblem.Evaluate(candidate);
                generation.Add(candidate);
            }

            return true;
        }

        #region event invokers

        protected virtual void OnGenerationFinished(GenerationFinishedEventArgs e)
        {
            if (GenerationFinished != null) GenerationFinished.Invoke(this, e);
        }

        protected virtual void OnBestSolutionChanged(BestSolutionChangedEventArgs e)
        {
            if (BestSolutionChanged != null) BestSolutionChanged.Invoke(this, e);
        }

        #endregion

        #endregion
        #region todo

        public bool Tune(IEnumerable<IProblem> representatives, IStrategy tuningStrategy)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}