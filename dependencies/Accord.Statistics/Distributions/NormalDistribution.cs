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
    ///   Normal (Gaussian) distribution.
    /// </summary>
    /// <remarks>
    ///   The Gaussian is the most widely used distribution for continuous
    ///   variables. In the case of a single variable, it is governed by
    ///   two parameters, the mean and the variance.
    /// </remarks>
    public class NormalDistribution : Distribution
    {
        private const double SQRT2 =  1.4142135623730950488016887;
        private const double SQRTPI = 2.50662827463100050242E0;

        // Distribution parameters
        private double mean;
        private double variance;


        /// <summary>
        ///   Constructs a Gaussian distribution with zero mean
        ///   and unit variance.
        /// </summary>
        public NormalDistribution()
            : this(0.0, 1.0)
        {
        }

        /// <summary>
        ///   Constructs a Gaussian distribution with given mean
        ///   and unit variance.
        /// </summary>
        /// <param name="mean"></param>
        public NormalDistribution(double mean)
            : this(mean, 1.0)
        {
        }

        /// <summary>
        ///   Constructs a Gaussian distribution with given mean
        ///   and given variance.
        /// </summary>
        /// <param name="mean"></param>
        /// <param name="variance"></param>
        public NormalDistribution(double mean, double variance)
        {
            this.mean = mean;
            this.variance = variance;
        }

        /// <summary>
        ///   Gets the Mean for the Gaussian distribution.
        /// </summary>
        public override double Mean
        {
            get { return mean; }
        }

        /// <summary>
        ///   Gets the Variance for the Gaussian distribution.
        /// </summary>
        public override double Variance
        {
            get { return variance; }
        }

        /// <summary>
        ///   Gets the Entropy for the Gaussian distribution.
        /// </summary>
        public override double Entropy
        {
            get
            {
                double b = 2.0 * System.Math.PI * Variance;
                return System.Math.Log(System.Math.Sqrt(b));
            }
        }

        /// <summary>
        ///   The cumulative distribution function evaluated at point x.
        /// </summary>
        /// <remarks>
        ///  The calculation is computed throught the relationship to
        ///  the erfc function as erfc(-z/sqrt(2)) / 2.
        ///  
        ///  References:
        ///  - http://mathworld.wolfram.com/NormalDistributionFunction.html
        ///  
        /// </remarks>
        public override double DistributionFunction(double x)
        {
            double z = ZScore(x);
            return Special.Erfc(-z / SQRT2) / 2.0;
        }

        /// <summary>
        ///   The probability density function evaluated at point x.
        /// </summary>
        public override double ProbabilityDensityFunction(double x)
        {
            double z = ZScore(x);
            return ((1.0 / (SQRTPI * this.variance)) * System.Math.Exp((-z * z) / 2.0));
        }

        /// <summary>
        ///   Gets the Z-Score for a given value.
        /// </summary>
        public double ZScore(double x)
        {
            return (x - this.mean) / this.variance;
        }



        /// <summary>
        ///   Gets the Standard Gaussian Distribution,
        ///   with zero mean and unit variance.
        /// </summary>
        public static readonly NormalDistribution Standard = new NormalDistribution();

    }
}
