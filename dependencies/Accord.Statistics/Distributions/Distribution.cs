// Accord Statistics Library
// Accord.NET framework
// http://www.crsouza.com
//
// Copyright © César Souza, 2009-2010
// cesarsouza at gmail.com
//

namespace Accord.Statistics.Distributions
{
    /// <summary>
    ///   Abstract class for Probability Distributions.
    /// </summary>
    /// <remarks>
    ///   A probability distribution identifies either the probability of each value of an
    ///   unidentified random variable (when the variable is discrete), or the probability
    ///   of the value falling within a particular interval (when the variable is continuous).
    ///   
    ///   The probability distribution describes the range of possible values that a random
    ///   variable can attain and the probability that the value of the random variable is
    ///   within any (measurable) subset of that range.
    ///   
    ///   The function describing the probability that a given value will occur is called
    ///   the probability function (or probability density function, abbreviated PDF), and
    ///   the function describing the cumulative probability that a given value or any value
    ///   smaller than it will occur is called the distribution function (or cumulative
    ///   distribution function, abbreviated CDF).
    ///   
    ///   References:
    ///    - http://en.wikipedia.org/wiki/Probability_distribution
    ///    - http://mathworld.wolfram.com/StatisticalDistribution.html
    ///    
    /// </remarks>
    public abstract class Distribution
    {

        protected Distribution()
        {
        }

        /// <summary>
        ///   Gets the mean for this distribution.
        /// </summary>
        public abstract double Mean { get; }

        /// <summary>
        ///   Gets the variance for this distribution.
        /// </summary>
        public abstract double Variance { get; }

        /// <summary>
        ///   Gets the entropy for this distribution.
        /// </summary>
        public abstract double Entropy { get; }


        /// <summary>
        ///   Gets the Standard Deviation (the square root of
        ///   the variance) for the current distribution.
        /// </summary>
        public double StandardDeviation
        {
            get { return System.Math.Sqrt(this.Variance); }
        }


        /// <summary>
        ///   The Cumulative Distribution Function (CDF) describes the cumulative
        ///   probability that a given value or any value smaller than it will occur.
        /// </summary>
        public abstract double DistributionFunction(double x);

        /// <summary>
        ///   The Probability Density Function (PDF) describes the
        ///   probability that a given value x will occur.
        /// </summary>
        public abstract double ProbabilityDensityFunction(double x);

    }


}