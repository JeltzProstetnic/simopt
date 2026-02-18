namespace SimOpt.Statistics.Kernels
{
    /// <summary>
    /// Chi-Square Kernel.
    /// </summary>
    /// <remarks>
    /// The Chi-Square kernel comes from the Chi-Square distribution.
    /// </remarks>
    public class ChiSquare : IKernel
    {
        /// <summary>
        /// Instantiate a new Chi-Square kernel.
        /// </summary>
        public ChiSquare() { }

        /// <summary>
        /// The Chi-Square Kernel Function
        /// </summary>
        /// <param name="x">Vector x in input space.</param>
        /// <param name="y">Vector y in input space.</param>
        /// <returns>Dot product in feature (kernel) space.</returns>
        public double Function(double[] x, double[] y)
        {
            double sum = 0.0;

            for (int i = 0; i < x.Length; i++)
            {
                double num = x[i] - y[i];
                sum += (num * num) / (0.5 * (x[i] + y[i]));
            }

            return 1.0 - sum;
        }
    }
}