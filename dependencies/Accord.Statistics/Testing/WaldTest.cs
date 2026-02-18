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
    ///   Wald's Test using the Normal distribution.
    /// </summary>
    /// <remarks>
    ///   Under the Wald statistical test, the maximum likelihood estimate of the
    ///   parameter(s) of interest θ is compared with the proposed value θ', with
    ///   the assumption that the difference between the two will be approximately
    ///   normal.
    /// </remarks>
    public class WaldTest : ZTest
    {

        /// <summary>
        ///   Constructs a Wald's test.
        /// </summary>
        /// <param name="statistic">The test statistic, as given by (θ-θ')/SE.</param>
        public WaldTest(double statistic)
            : base(statistic, Hypothesis.TwoTail)
        {
        }

        /// <summary>
        ///   Constructs a Wald's test.
        /// </summary>
        /// <param name="estimated">The estimated value θ.</param>
        /// <param name="proposed">The proposed value θ'.</param>
        /// <param name="standardError">The standard error.</param>
        public WaldTest(double estimated, double proposed, double standardError)
            : base((estimated - proposed) / standardError, Hypothesis.TwoTail)
        {
        }


    }
}
