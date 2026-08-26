using System;

namespace SimOpt.Mathematics.SpecialFunctions
{
    /// <summary>
    /// Student's t-distribution: CDF and the quantile (inverse CDF) needed to put a confidence
    /// interval around a mean estimated from a small number of simulation replications.
    /// </summary>
    /// <remarks>
    /// <para>
    /// SIM-63. Nothing in the codebase provided a quantile of any kind before this — and note that
    /// <c>SimOpt.Statistics/Kernels/TStudent.cs</c> is an <b>SVM Mercer kernel</b> despite the name,
    /// with no relation to this distribution.
    /// </para>
    /// <para>
    /// The quantile is obtained by root-finding on the exact CDF identity rather than from a lookup
    /// table. A table is indefensible for software whose output is used as expert evidence: it is
    /// silently wrong at every degree of freedom it does not contain. The published table is
    /// retained as a regression test instead, so the root-finder is pinned rather than merely
    /// trusted — neither alone would be adequate.
    /// </para>
    /// <para>
    /// Using the t rather than the normal quantile is required whenever the standard deviation is
    /// estimated from the same sample, which it always is here. At 10 replications the t inflates
    /// the interval by about 15% over the normal; treating that as a rounding detail would make
    /// every reported interval too narrow, which is the dangerous direction of error for a claim
    /// about precision.
    /// </para>
    /// </remarks>
    public static class StudentT
    {
        /// <summary>
        /// Cumulative distribution function P(T ≤ t) for <paramref name="degreesOfFreedom"/> ν.
        /// </summary>
        /// <remarks>
        /// Uses the standard identity F(t) = 1 − ½·I_{ν/(ν+t²)}(ν/2, ½) for t ≥ 0, mirrored for
        /// negative t, where I is the regularized incomplete beta already available as
        /// <see cref="MMath.Ibeta"/>.
        /// </remarks>
        public static double Cdf(double t, int degreesOfFreedom)
        {
            if (degreesOfFreedom < 1)
                throw new ArgumentOutOfRangeException(nameof(degreesOfFreedom), "Degrees of freedom must be at least 1.");
            if (double.IsNaN(t)) return double.NaN;
            if (double.IsPositiveInfinity(t)) return 1d;
            if (double.IsNegativeInfinity(t)) return 0d;

            double nu = degreesOfFreedom;
            double x = nu / (nu + t * t);
            double half = 0.5 * MMath.Ibeta(0.5 * nu, 0.5, x);

            return t >= 0d ? 1d - half : half;
        }

        /// <summary>
        /// Quantile (inverse CDF): the value t such that P(T ≤ t) = <paramref name="probability"/>.
        /// </summary>
        /// <param name="probability">Strictly between 0 and 1.</param>
        /// <param name="degreesOfFreedom">ν ≥ 1.</param>
        /// <remarks>
        /// Bisection on <see cref="Cdf"/>. Bisection rather than Newton deliberately: the CDF is
        /// monotone so bisection cannot fail to converge, it needs no derivative, and at the ~60
        /// iterations used here it reaches the limit of double precision. This runs once per
        /// reported interval, so its speed is irrelevant next to being unconditionally reliable.
        /// </remarks>
        public static double Quantile(double probability, int degreesOfFreedom)
        {
            if (degreesOfFreedom < 1)
                throw new ArgumentOutOfRangeException(nameof(degreesOfFreedom), "Degrees of freedom must be at least 1.");
            if (probability <= 0d || probability >= 1d)
                throw new ArgumentOutOfRangeException(nameof(probability), "Probability must be strictly between 0 and 1.");

            if (probability == 0.5d) return 0d;

            // The distribution is symmetric, so solve in the upper tail and mirror. This keeps the
            // bracket away from the region where the CDF is flat and ill-conditioned.
            bool upper = probability > 0.5d;
            double p = upper ? probability : 1d - probability;

            double low = 0d;
            double high = 1d;
            while (Cdf(high, degreesOfFreedom) < p)
            {
                low = high;
                high *= 2d;
                if (high > 1e12) break;   // ν=1 has very heavy tails; this is far beyond any usable p
            }

            for (int i = 0; i < 200; i++)
            {
                double mid = 0.5 * (low + high);
                if (mid <= low || mid >= high) break;   // converged to adjacent doubles
                if (Cdf(mid, degreesOfFreedom) < p) low = mid;
                else high = mid;
            }

            double t = 0.5 * (low + high);
            return upper ? t : -t;
        }

        /// <summary>
        /// Two-sided critical value t_{1−α/2, ν} — the multiplier in a
        /// (1−<paramref name="alpha"/>) confidence interval on a mean.
        /// </summary>
        public static double TwoSidedCriticalValue(double alpha, int degreesOfFreedom)
        {
            if (alpha <= 0d || alpha >= 1d)
                throw new ArgumentOutOfRangeException(nameof(alpha), "Alpha must be strictly between 0 and 1.");
            return Quantile(1d - alpha / 2d, degreesOfFreedom);
        }
    }
}
