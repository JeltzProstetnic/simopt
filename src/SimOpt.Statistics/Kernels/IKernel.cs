namespace SimOpt.Statistics.Kernels
{
    /// <summary>
    /// Kernel function interface.
    /// </summary>
    /// <remarks>
    /// A Kernel is a function that returns the dot product between the two arguments.
    /// k(x,y) = ‹S(x),S(y)›
    /// </remarks>
    public interface IKernel
    {
        /// <summary>
        /// The kernel function.
        /// </summary>
        /// <param name="x">Vector x in input space.</param>
        /// <param name="y">Vector y in input space.</param>
        /// <returns>Dot product in feature (kernel) space.</returns>
        double Function(double[] x, double[] y);
    }
}