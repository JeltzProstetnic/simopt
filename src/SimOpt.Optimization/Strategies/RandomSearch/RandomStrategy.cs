using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Optimization.Interfaces;

namespace SimOpt.Optimization.Strategies.RandomSearch
{
    /// <summary>
    /// This strategy simply calls IProblem.GenerateSolutionCandidates
    /// repeatedly and memorizes the best solution found so far.
    /// </summary>
    public class RandomStrategy : IStrategy
    {
        #region cvar

        private int iterationsRemaining;

        #endregion
        #region evnt

        /// <summary>
        /// Occurs each time a new best solution was found, but not more than once per generation.
        /// </summary>
        public event BestSolutionChangedHandler BestSolutionChanged;

        #endregion
        #region prop

        public IConfiguration CurrentConfiguration { get; private set; }

        #region IStrategy

        /// <summary>
        /// Returns "Random Strategy"
        /// </summary>
        public string Name
        {
            get { return "Random Strategy"; }
        }

        public string ProcessingStatus { get; private set; }

        public bool IsInitialized { get; private set; }

        #endregion

        #endregion
        #region impl

        #region IStrategy Member

        public bool Initialize(IConfiguration parameters)
        {
            if (!(parameters is IConfiguration)) 
                throw new ArgumentException("The parameters for this strategy must implement IConfiguration.");

            CurrentConfiguration = parameters as IConfiguration;
            iterationsRemaining = CurrentConfiguration.NumberOfIterations;
            return true;
        }

        public void Reset()
        {
            CurrentConfiguration = null;
            iterationsRemaining = 0;
        }

        public IEnumerable<ISolution> Solve(IProblem problem)
        {
            ISolution candidate = problem.GenerateCandidates(CurrentConfiguration.Seed, 1).First();
            if (!candidate.HasFitness) problem.Evaluate(candidate);
            ISolution bestSolution = candidate;

            while (iterationsRemaining > 0)
            {
                candidate = problem.GenerateCandidates(CurrentConfiguration.Seed, 1).First();
                if (!candidate.HasFitness) problem.Evaluate(candidate);
                if (candidate.Fitness > bestSolution.Fitness)
                {
                    OnBestSolutionChanged(new BestSolutionChangedEventArgs(bestSolution, candidate));
                    bestSolution = candidate;
                }
                iterationsRemaining--;
            }

            yield return bestSolution;
        }

        /// <summary>
        /// Stops the search by setting the remaining iterations to zero.
        /// </summary>
        public void Stop()
        {
            iterationsRemaining = 0;
        }

        /// <summary>
        /// Not implemented!
        /// This algorithm cannot be tuned.
        /// </summary>
        /// <param name="representatives">Not implemented!</param>
        /// <param name="tuningStrategy">Not implemented!</param>
        /// <returns>Not implemented!</returns>
        public bool Tune(IEnumerable<IProblem> representatives, IStrategy tuningStrategy)
        {
            throw new NotImplementedException("This algorithm cannot be tuned.");
        }

        #endregion

        #endregion
        #region util
        
        #region event invokers

        protected virtual void OnBestSolutionChanged(BestSolutionChangedEventArgs e)
        {
            BestSolutionChanged.Invoke(this, e);
        }

        #endregion

        #endregion
    }
}
