using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimOpt.Optimization.Interfaces
{
	/// <summary>
	/// To solve an optimization problem, a class has to be provided to the strategy, which implements "IProblem". This interface prescribes an "Evaluate" method which the strategy 
	/// will use to determine the fitness of a given solution candidate. The function's return value indicates, if the solution candidate is valid.
	/// An additional "IsValid" method is prescribed, so that the strategy can determine if a candidate is valid without the need to evaluate the candidates fitness. In some 
	/// cases this may be possible and thereby save processing resources.
	/// A property "OptimumFitness" is prescribed, which should return the global optimum, if it is known for the given problem. Depending on the strategy this may be required.
	/// Finally there is a "GenerateCandidates" method, which most optimization strategies will need to create a number of initial candidates. This method takes a seed value as 
	/// parameter for cases in which initial candidates are generated randomly, so that the optimization experiment is reproducible. The second parameter indicates the number of 
	/// candidates to generate.
	/// Any problem which allows at least the implementation of the GenerateCandidates method and the Evaluate method is compatible with the framework and can be solved given a 
	/// viable strategy.
	/// </summary>
    public interface IProblem
    {
        /// <summary>
        /// If known, the global optimum for this problem.
        /// Note: some algorithms may require this to be set.
        /// </summary>
        double OptimumFitness { get; }
        
        /// <summary>
        /// Check if the given candidate is a viable solution.
        /// </summary>
        /// <param name="solutionCandidate"></param>
        /// <returns></returns>
        bool IsValid(ISolution solutionCandidate);

        /// <summary>
        /// Evaluate the solution candidate and set its
        /// fitness value. Also set its HasFitness flag 
        /// to true. Note: A higher fitness value must 
        /// indicate a better solution. If the candidate is
        /// invalid, set its fitness to -Double.MaxValue
        /// </summary>
        /// <param name="solutionCandidate">a solution candidate to evaluate</param>
        /// <returns>true if the solution candidate is valid</returns>
        bool Evaluate(ISolution solutionCandidate);

        /// <summary>
        /// Provide a number of initial solution candidates for
        /// optimization. Different seed values should yield
        /// different candidates. The method may return less 
        /// but not more candidates than specified in count.
        /// if <code>DiscardInvalidSolutionsImmediately</code> is
        /// set to true, only valid candidates must be returned.
        /// </summary>
        /// <param name="seed">a random seed value</param>
        /// <param name="count">requested number of candidates</param>
        /// <returns>not more than <code>count</code> solution candidate</returns>
        IEnumerable<ISolution> GenerateCandidates(int seed, int count);
    }
}
