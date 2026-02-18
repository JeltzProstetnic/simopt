using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Optimization.Interfaces;

namespace SimOpt.Optimization.Strategies.SimulatedAnnealing
{
    /// <summary>
    /// Simulated Annealing Algorithm
    /// <br />
    /// Kirkpatrick, S.; C. D. Gelatt, M. P. Vecchi (1983-05-13). "Optimization by Simulated Annealing". Science. 
    ///     New Series 220 (4598): 671–680. doi:10.1126/science.220.4598.671. ISSN 00368075. PMID 17813860.
    /// <br />    
    /// V. Cerny, A thermodynamical approach to the travelling salesman problem: an efficient simulation algorithm. 
    ///     Journal of Optimization Theory and Applications, 45:41-51, 1985
    /// </summary>
    /// <remarks>
    /// <ul>
    /// <li>Tuning is not implemented yet.</li>
    /// <li>This could be extended with a Taboo concept.</li>
    /// </ul>
    /// </remarks>
    public class AnnealingAlgorithm : IStrategy
    {
        #region cvar

        public readonly static string StatusUndefined = "Undefined";
        public readonly static string StatusInitializing = "Initializing";
        public readonly static string StatusEvaluating = "Evaluating";
        public readonly static string StatusCooling = "Cooling";
        public readonly static string StatusFinished = "Finished";

        private ISimulatedAnnealingConfiguration config;

        private Random rnd;

        #endregion
        #region evnt

        /// <summary>
        /// Occurs each time a new best solution was found, but not more than once per generation.
        /// </summary>
        public event BestSolutionChangedHandler BestSolutionChanged;

        #endregion
        #region prop

        public string Name { get { return "Simulated Annealing"; } }

        public bool IsInitialized { get; private set; }

        public string ProcessingStatus { get; private set; }

        public double CurrentTemperature { get; private set; }

        public IProblem CurrentProblem { get; private set; }

        public ISolution CurrentCandidate { get; private set; }

        public ISolution BestCandidate { get; private set; }

        #endregion
        #region ctor

        /// <summary>
        /// default constructor
        /// </summary>
        public AnnealingAlgorithm() { }

        #endregion
        #region init

        /// <summary>
        /// initialize this algorithm instance
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public bool Initialize(IConfiguration parameters)
        {
            if (!(parameters is ISimulatedAnnealingConfiguration)) return false;
            ProcessingStatus = StatusInitializing;
            config = parameters as ISimulatedAnnealingConfiguration;
            rnd = new Random(config.Seed);
            CurrentTemperature = config.InitialTemperature;
            IsInitialized = true;
            return true;
        }

        #endregion
        #region rset

        /// <summary>
        /// Reset the strategy. All data will be erased.
        /// </summary>
        public void Reset()
        {
            config = null;
            rnd = null;
            CurrentTemperature = 0;
            CurrentProblem = null;
            CurrentCandidate = null;
            BestCandidate = null;
            CurrentTemperature = 0;
            ProcessingStatus = StatusUndefined;
            IsInitialized = false;
        }

        #endregion
        #region impl

        /// <summary>
        /// solve the given problem
        /// the strategy has to be initialized first
        /// </summary>
        /// <param name="problem"></param>
        /// <returns>the best solution</returns>
        public IEnumerable<ISolution> Solve(IProblem problem)
        {
            // initialize status info
            CurrentProblem = problem;

            // get initial candidate
            IEnumerable<ISolution> candidates = problem.GenerateCandidates(config.Seed, 1);
            if (candidates.Count() != 1) throw new ArgumentException("This algorithm can only work with exactly one initial candidate.");
            BestCandidate = candidates.First();

            // evaluate if required
            if (!BestCandidate.HasFitness)
            {
                ProcessingStatus = StatusEvaluating;
                problem.Evaluate(BestCandidate);
                ProcessingStatus = StatusInitializing;
            }

            // evolve
            CurrentCandidate = BestCandidate;
            while (CurrentTemperature >= 0) ProcessCandidate();
            ProcessingStatus = StatusFinished;

            // done
            yield return BestCandidate;
        }

        /// <summary>
        /// Stops the solver by setting the temperature to zero.
        /// </summary>
        public void Stop()
        {
            CurrentTemperature = 0;
        }

        private void ProcessCandidate()
        {
            ProcessingStatus = StatusCooling;
            if (config.Brownian is SimpleBrownian) (config.Brownian as SimpleBrownian).CurrentTemperature = CurrentTemperature;
            ISolution newCandidate = config.Brownian.Apply(CurrentCandidate.Clone() as ISolution);
            ProcessingStatus = StatusEvaluating;
            CurrentProblem.Evaluate(newCandidate);
            bool replace = newCandidate.Fitness >= CurrentCandidate.Fitness;
            if (!replace)
            {
                double rand = rnd.NextDouble();
                double dist = newCandidate.Fitness - CurrentCandidate.Fitness;
                if (rand < Math.Exp(dist / CurrentTemperature)) replace = true;
            }
            CurrentTemperature = config.DecreaseTemperature.Invoke(this);
            if (replace) CurrentCandidate = newCandidate;
            if (CurrentCandidate.Fitness >= BestCandidate.Fitness)
            {
                ISolution tmp = BestCandidate;
                BestCandidate = CurrentCandidate;
                OnBestSolutionChanged(new BestSolutionChangedEventArgs(tmp, BestCandidate));
            }
        }

        #endregion
        #region util

        #region event invokers

        protected virtual void OnBestSolutionChanged(BestSolutionChangedEventArgs e)
        {
            BestSolutionChanged.Invoke(this, e);
        }

        #endregion

        #endregion
        #region todo

        /// <summary>
        /// tuningStrategy must be initialized
        /// </summary>
        /// <param name="representatives"></param>
        /// <param name="tuningStrategy"></param>
        /// <returns></returns>
        public bool Tune(IEnumerable<IProblem> representatives, IStrategy tuningStrategy)
        {
            // tuningStrategy.Solve(SimulatedAnnealingTuningProblem);
            return true;
        }

        #endregion
    }
}