using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Optimization.Interfaces;
using MatthiasToolbox.SupplyChain.Simulator;

namespace MatthiasToolbox.SupplyChain.Optimizer
{
    public class Problem : IProblem
    {
        public event Action TenEvaluationsDone;

        private int evalCounter = 0;
        public int EvaluationCounter = 0;

        public double OptimumFitness
        {
            get { return double.MaxValue; }
        }

        public bool IsValid(ISolution solutionCandidate)
        {
            return true;
        }

        public Problem()
        {
            EvaluationCounter = 0;
        }

        public bool Evaluate(ISolution solutionCandidate)
        {
            evalCounter++;
            EvaluationCounter++;
            Solution toEval = solutionCandidate as Solution;
            toEval.Run();
            if (evalCounter == 20 && TenEvaluationsDone!=null)
            {
                evalCounter = 0;
                TenEvaluationsDone.Invoke();
            }
            return true;
        }

        public IEnumerable<ISolution> GenerateCandidates(int seed, int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return new Solution(seed);
            }
        }
    }
}
