using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Accord.Statistics.Distributions;

namespace Accord.Statistics.Testing
{
    /// <summary>
    ///   Chi-Square Test (Upper one-tail)
    /// </summary>
    /// <remarks>
    ///   A chi-square test (also chi-squared or χ2  test) is any statistical
    ///   hypothesis test in which the sampling distribution of the test statistic
    ///   is a chi-square distribution when the null hypothesis is true, or any in
    ///   which this is asymptotically true, meaning that the sampling distribution
    ///   (if the null hypothesis is true) can be made to approximate a chi-square
    ///   distribution as closely as desired by making the sample size large enough.
    /// 
    ///   Use the Chi (pronounced KY as in sky) square test to look at whether
    ///   actual data differ from a random distribution.
    ///   
    ///   For example, say you want to find out whether students prefer particular
    ///   T-shirt colors. Assume there are five different colors and each student
    ///   could get one free at registration (there are enough so that everyone
    ///   could choose the same color). If people chose at random, the proportion
    ///   of each color chosen would be equal (about 20% of the total shirts chosen
    ///   would be in each category). You might not be surprised to find 19% of the
    ///   shirts chosen were red and 21% were black, but when do you have enough
    ///   evidence to say people are choosing them non-randomly? This test will
    ///   tell you.
    ///   
    ///   The chi-square goodness of fit is a one-tailed test with the rejection
    ///   region in the right tail.
    ///   
    ///   As a final note, always remember statistical hypotheses can be
    ///   rejected or supported by a test, but not proven.
    ///   
    ///   References:
    ///   - http://depts.alverno.edu/nsmt/stats.htm
    ///   - http://www.graphpad.com/articles/pvalue.htm
    ///   - http://www2.lv.psu.edu/jxm57/irp/chisquar.html
    ///   - http://xudaniel.com/Documents/Chapter13.ppt
    ///   
    /// </remarks>
    public class ChiSquareTest : HypothesisTest
    {

        private ChiSquareDistribution distribution;


        //---------------------------------------------


        #region Constructors
        /// <summary>
        ///   Constructs a Chi-Square Test.
        /// </summary>
        /// <param name="statistic">The test statistic.</param>
        /// <param name="degreesOfFreedom">The chi-square distribution degrees of freedom.</param>
        /// <param name="threshold">The significance threshold. By default, 0.05 will be used.</param>
        public ChiSquareTest(double statistic, int degreesOfFreedom, double threshold)
        {
            this.statistic = statistic;
            this.threshold = threshold;
            this.distribution = new ChiSquareDistribution(degreesOfFreedom);

            this.compute();
        }

        /// <summary>
        ///   Constructs a Chi-Square Test.
        /// </summary>
        /// <param name="statistic">The test statistic.</param>
        /// <param name="degreesOfFreedom">The chi-square distribution degrees of freedom.</param>
        public ChiSquareTest(double statistic, int degreesOfFreedom)
            : this(statistic, degreesOfFreedom, 0.05)
        {
        }

        /// <summary>
        ///   Construct a Chi-Square Test.
        /// </summary>
        /// <param name="expected">The expected variable values.</param>
        /// <param name="observed">The observed variable values.</param>
        /// <param name="degreesOfFreedom">The chi-square distribution degrees of freedom.</param>
        /// <param name="threshold">The significance threshold. By default, 0.05 will be used.</param>
        public ChiSquareTest(double[] expected, double[] observed, int degreesOfFreedom, double threshold)
        {
            this.threshold = threshold;
            this.distribution = new ChiSquareDistribution(degreesOfFreedom);

            this.compute(expected, observed);
        }
        #endregion


        //---------------------------------------------


        #region Public Properties
        /// <summary>
        ///   Gets the degrees of freedom for the Chi-Square distribution.
        /// </summary>
        public int DegreesOfFreedom
        {
            get { return distribution.DegreesOfFreedom; }
        }
        #endregion


        //---------------------------------------------


        private void compute(double[] observed, double[] expected)
        {
            // X² = sum(o - e)²
            //          -----
            //            e

            double sum = 0.0, d;
            for (int i = 0; i < observed.Length; i++)
            {
                d = observed[i] - expected[i];
                sum += (d * d) / expected[i];
            }

            this.statistic = sum;
            this.pvalue = distribution.SurvivalFunction(statistic);
        }

        private void compute()
        {
            this.pvalue = distribution.SurvivalFunction(statistic);
        }


    }
}
