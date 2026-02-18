namespace MatthiasToolbox.Statistics.Kernels
{
    /// <summary>
    /// Kernel space distance interface.
    /// </summary>
    /// <remarks>
    /// Kernels which implement this interface can be used to solve pre-image 
    /// problem (as in KPCA and other methods based on multi-dimensional scaling).
    /// </remarks>
    interface IDistance
    {
        /// <summary>
        /// Compute the distance in input space between two points given in feature space.
        /// </summary>
        /// <param name="x">Vector x in feature (kernel) space.</param>
        /// <param name="y">Vector y in feature (kernel) space.</param>
        /// <returns>Distance between x and y in input space.</returns>
        double Distance(double[] x, double[] y);
    }
}