namespace SimOpt.Optimization.Interfaces
{
    /// <summary>
    /// SIM-68 D7, SIM-59: a solution candidate that is a point in a real vector space, which is what a
    /// strategy that moves coordinates (particle swarm) reads and writes.
    /// </summary>
    /// <remarks>
    /// This contract replaces reading a property named <c>Parameters</c> through reflection, which only
    /// the test helper had, so the swarm searched nothing on a real problem.
    /// </remarks>
    public interface IVectorSolution : ISolution
    {
        /// <summary>The point, one coordinate per dimension. Setting it does not evaluate it.</summary>
        double[] Parameters { get; set; }
    }
}
