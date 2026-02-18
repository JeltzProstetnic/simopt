// Accord Statistics Library
// Accord.NET framework
// http://www.crsouza.com
//
// Copyright © César Souza, 2009
// cesarsouza at gmail.com
//


using Accord.Statistics.Distributions;

namespace Accord.Statistics.Testing
{
    /// <summary>
    ///   Z-Test (One-sample location test)
    /// </summary>
    /// <remarks>
    ///   The term Z-test is often used to refer specifically to the one-sample
    ///   location test comparing the mean of a set of measurements to a given
    ///   constant.
    ///   
    ///   If the observed data X1, ..., Xn are (i) uncorrelated, (ii) have a common
    ///   mean μ, and (iii) have a common variance σ², then the sample average X has
    ///   mean μ and variance σ² / n. If our null hypothesis is that the mean value
    ///   of the population is a given number μ0, we can use X −μ0 as a test-statistic,
    ///   rejecting the null hypothesis if X −μ0 is large.
    /// </remarks>
    public class ZTest : HypothesisTest
    {


        /// <summary>
        ///   Constructs a Z test.
        /// </summary>
        /// <param name="sample">The samples.</param>
        /// <param name="x">The constant to be compared with the samples.</param>
        public ZTest(double[] samples, double x, Hypothesis hypothesis)
        {
            double mean = Tools.Mean(samples);
            double stdDev = Tools.StandardDeviation(samples, mean);
            double stdError = Tools.StandardError(samples.Length, stdDev);

            this.statistic = (x - mean) / stdError;

            this.hypothesis = hypothesis;
            this.compute();
        }

        /// <summary>
        ///   Constructs a Z test.
        /// </summary>
        public ZTest(double mean, double stdDev, double x, int samples, Hypothesis hypothesis)
        {
            double stdError = Tools.StandardError(samples, stdDev);

            this.statistic = (x - mean) / stdError;

            this.hypothesis = hypothesis;
            this.compute();
        }

        /// <summary>
        ///   Constructs a Z test.
        /// </summary>
        /// <param name="statistic">The test statistic, as given by (x-μ)/SE.</param>
        public ZTest(double statistic, Hypothesis hypothesis)
        {
            this.statistic = statistic;

            this.hypothesis = hypothesis;
            this.compute();
        }



        private void compute()
        {
            if (this.hypothesis == Hypothesis.OneLower || this.hypothesis == Hypothesis.OneUpper)
            {
                this.pvalue = NormalDistribution.Standard.
                      DistributionFunction(-System.Math.Abs(statistic));
            }
            else
            {
                this.pvalue = 2.0 * NormalDistribution.Standard.
                      DistributionFunction(-System.Math.Abs(statistic));
            }
        }
    }
}
