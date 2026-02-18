using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Optimization.Interfaces;
using MatthiasToolbox.Simulation.Entities;

namespace MatthiasToolbox.SimOptExample.Optimizer
{
    /// <summary>
    /// The optimization problem
    /// </summary>
    public class Problem : IProblem
    {
        #region cvar

        private Model.Simulation sim;

        public event Action TenEvaluationsDone;

        private int evalCounter = 0;
        public int EvaluationCounter = 0;

        #endregion
        #region prop

        #region IProblem

        /// <summary>
        /// Assuming the optimum is zero time consumption
        /// </summary>
        public double OptimumFitness
        {
            get { return 0; }
        }

        #endregion

        #endregion
        #region ctor

        public Problem(Model.Simulation sim)
        {
            this.sim = sim;
        }

        #endregion
        #region impl

        #region IProblem

        /// <summary>
        /// No invalid solutions in this scenario
        /// </summary>
        /// <param name="solutionCandidate"></param>
        /// <returns></returns>
        public bool IsValid(ISolution solutionCandidate)
        {
            return true;
        }

        public bool Evaluate(ISolution solutionCandidate)
        {
            Solution candidate = solutionCandidate as Solution;
            
            solutionCandidate.Fitness = sim.Evaluate(candidate.Data);
            solutionCandidate.HasFitness = true;

            evalCounter++;
            EvaluationCounter++;

            if (evalCounter == 20 && TenEvaluationsDone != null)
            {
                evalCounter = 0;
                TenEvaluationsDone.Invoke();
            }

            return true;
        }

        public IEnumerable<ISolution> GenerateCandidates(int seed, int count)
        {
            int i = 0;
            while (i < count)
            {
                i++;
                yield return new Solution(sim.CreateCandidate());
            }
        }

        #endregion

        #endregion
    }
}
