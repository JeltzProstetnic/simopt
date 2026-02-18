// Accord Statistics Library
// Accord.NET framework
// http://www.crsouza.com
//
// Copyright © César Souza, 2009-2010
// cesarsouza at gmail.com
//

namespace Accord.Statistics.Distributions
{
    using Accord.Math;

    /// <summary>
    ///   Chi-Square (χ²) probability distribution
    /// </summary>
    /// <remarks>
    ///   In probability theory and statistics, the chi-square distribution (also chi-squared
    ///   or χ²-distribution) with k degrees of freedom is the distribution of a sum of the 
    ///   squares of k independent standard normal random variables. It is one of the most 
    ///   widely used probability distributions in inferential statistics, e.g. in hypothesis 
    ///   testing, or in construction of confidence intervals.
    ///   
    ///   References:
    ///    - http://en.wikipedia.org/wiki/Chi-square_distribution
    /// </remarks>
    public class ChiSquareDistribution : Distribution
    {
        //  Distribution parameters
        private int degreesOfFreedom;

        /// <summary>
        ///   Constructs a new Chi-Square distribution
        ///   with given degrees of freedom.
        /// </summary>
        public ChiSquareDistribution(int degreesOfFreedom)
        {
            this.degreesOfFreedom = degreesOfFreedom;
        }

        /// <summary>
        ///   Gets the Degrees of Freedom for this distribution.
        /// </summary>
        public int DegreesOfFreedom
        {
            get { return degreesOfFreedom; }
        }

        /// <summary>
        ///   Gets the probability density function evaluated
        ///   at point x.
        /// </summary>
        /// <remarks>
        ///   References:
        ///   - http://www.mathworks.com/access/helpdesk/help/toolbox/stats/chi2pdf.html
        /// </remarks>
        public override double ProbabilityDensityFunction(double x)
        {
            double v = degreesOfFreedom;
            double m1 = System.Math.Pow(x, (v - 2.0) / 2.0);
            double m2 = System.Math.Exp(-x / 2.0);
            double m3 = System.Math.Pow(2, v / 2.0) * Special.Gamma(v / 2.0);
            return (m1 * m2) / m3;
        }

        /// <summary>
        ///   Gets the cumulative distribution function
        ///   evaluated at point x.
        /// </summary>
        public override double DistributionFunction(double x)
        {
            return Special.ChiSq(degreesOfFreedom, x);
        }

        /// <summary>
        ///   Gets the complementary cumulative distribution
        ///   function evaluated at point x.
        /// </summary>
        public double SurvivalFunction(double x)
        {
            return Special.ChiSqc(degreesOfFreedom, x);
        }


        /// <summary>
        ///   Gets the mean for this distribution.
        /// </summary>
        public override double Mean
        {
            get { return degreesOfFreedom; }
        }

        /// <summary>
        ///   Gets the variance for this distribution.
        /// </summary>
        public override double Variance
        {
            get { return 2.0 * degreesOfFreedom; }
        }

        /// <summary>
        ///   Gets the entropy for this distribution.
        /// </summary>
        public override double Entropy
        {
            get
            {
                double kd2 = degreesOfFreedom / 2.0;
                double m1 = System.Math.Log(2.0 * Special.Gamma(kd2));
                double m2 = (1.0 - kd2) * Special.Digamma(kd2);
                return kd2 + m1 + m2;
            }
        }

    }

}