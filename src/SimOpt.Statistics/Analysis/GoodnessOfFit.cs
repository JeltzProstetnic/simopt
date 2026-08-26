using System;
using System.Collections.Generic;
using System.Linq;
using SimOpt.Mathematics;

namespace SimOpt.Statistics.Analysis
{
    /// <summary>
    /// One-sample goodness-of-fit tests against a <b>fully specified</b> null distribution.
    /// </summary>
    /// <remarks>
    /// <para>
    /// SIM-64, serving UN-007. The queueing benchmarks in
    /// <c>SimOpt.Simulation.Statistics.QueueingFormulas</c> check that the engine gets its
    /// <b>means</b> right. They are blind to a sampler drawing from the wrong distribution with the
    /// right mean — and a mean-correct sampler with the wrong shape produces confident, plausible,
    /// wrong answers everywhere the tail matters, which in queueing is everywhere.
    /// </para>
    /// <para>
    /// <b>Fully specified means no parameter may be estimated from the sample being tested.</b>
    /// Fitting λ from the data and then testing against the fitted distribution changes the null
    /// distribution of the statistic, and the critical values below become badly wrong in the
    /// permissive direction — the Lilliefors case. Every use here compares a sampler against the
    /// parameters it was configured with, which is the legitimate case.
    /// </para>
    /// <para>
    /// <b>Both tests, not one.</b> Kolmogorov–Smirnov is weakest in the upper tail, which is
    /// exactly where a queueing model is most sensitive: rare long service times drive queue
    /// length. Anderson–Darling weights the tails and catches what KS misses. They cost the same
    /// sample, so there is no reason to run only one.
    /// </para>
    /// </remarks>
    public static class GoodnessOfFit
    {
        // ── Kolmogorov–Smirnov ───────────────────────────────────────────────

        /// <summary>
        /// The two-sided KS statistic D — the largest vertical distance between the empirical
        /// distribution of <paramref name="sample"/> and <paramref name="cdf"/>.
        /// </summary>
        /// <remarks>
        /// Both one-sided deviations are taken, because the empirical CDF is a step function and
        /// the largest gap can occur on either side of a step. Computing only
        /// <c>max(i/n − F(xᵢ))</c> — the common shortcut — misses a sample that sits entirely above
        /// where the null puts it, and reports a comfortable statistic for a sample that is nowhere
        /// near the null.
        /// </remarks>
        public static double KolmogorovSmirnov(IReadOnlyList<double> sample, Func<double, double> cdf)
        {
            double[] sorted = Sorted(sample, cdf, nameof(sample));

            int n = sorted.Length;
            double dPlus = 0d, dMinus = 0d;

            for (int i = 0; i < n; i++)
            {
                double f = cdf(sorted[i]);
                dPlus = Math.Max(dPlus, (i + 1) / (double)n - f);
                dMinus = Math.Max(dMinus, f - i / (double)n);
            }

            return Math.Max(dPlus, dMinus);
        }

        /// <summary>
        /// Critical value c_α for the statistic √n·D, from the asymptotic Kolmogorov distribution:
        /// c_α = √(−½·ln(α/2)). Reject the null when √n·D exceeds it.
        /// </summary>
        /// <remarks>
        /// Asymptotic rather than exact. At the sample sizes this is used on — thousands — the
        /// difference is far below the resolution of any decision made from it; at n below about
        /// 30 it is not, and this should not be used there.
        /// </remarks>
        public static double KolmogorovSmirnovCriticalValue(double alpha)
        {
            if (alpha <= 0d || alpha >= 1d)
                throw new ArgumentOutOfRangeException(nameof(alpha), "Significance level must be strictly between 0 and 1.");
            return Math.Sqrt(-0.5 * Math.Log(alpha / 2d));
        }

        // ── Anderson–Darling ─────────────────────────────────────────────────

        /// <summary>
        /// The Anderson–Darling statistic A², which weights deviations in the tails far more
        /// heavily than <see cref="KolmogorovSmirnov"/> does.
        /// </summary>
        /// <remarks>
        /// A² = −n − (1/n)·Σᵢ (2i−1)·[ln F(x₍ᵢ₎) + ln(1 − F(x₍ₙ₊₁₋ᵢ₎))], one-based i over the
        /// ordered sample.
        /// </remarks>
        public static double AndersonDarling(IReadOnlyList<double> sample, Func<double, double> cdf)
        {
            double[] sorted = Sorted(sample, cdf, nameof(sample));

            int n = sorted.Length;
            double sum = 0d;

            for (int i = 0; i < n; i++)
            {
                double lower = cdf(sorted[i]);
                double upper = cdf(sorted[n - 1 - i]);

                // A sample point the null assigns zero probability to sends a logarithm to −∞ and
                // the statistic to infinity, which would be read as an overwhelming rejection. It
                // is not one: the sample is outside the null's support, so the fit was never the
                // question. Say so rather than returning a number.
                if (lower <= 0d || upper >= 1d)
                    throw new ArgumentException(
                        $"Sample value {(lower <= 0d ? sorted[i] : sorted[n - 1 - i])} lies outside the " +
                        "support the null distribution assigns positive probability to, so no " +
                        "goodness-of-fit statistic is defined. Check the null, not the fit.",
                        nameof(sample));

                sum += (2 * (i + 1) - 1) * (Math.Log(lower) + Math.Log(1d - upper));
            }

            return -n - sum / n;
        }

        /// <summary>
        /// Critical value for A² against a <b>fully specified</b> null, for one of the four
        /// tabulated significance levels.
        /// </summary>
        /// <remarks>
        /// These are tabulated constants rather than a closed form, and they apply only when no
        /// parameter was estimated from the sample — the estimated-parameter case has entirely
        /// different, much smaller, critical values. An untabulated α is refused rather than
        /// interpolated, because silently answering a different question than the one asked is how
        /// a stated significance level becomes a lie.
        /// </remarks>
        public static double AndersonDarlingCriticalValue(double alpha) => alpha switch
        {
            0.10 => 1.933,
            0.05 => 2.492,
            0.025 => 3.070,
            0.01 => 3.857,
            _ => throw new ArgumentOutOfRangeException(nameof(alpha),
                     "Only the tabulated levels 0.10, 0.05, 0.025 and 0.01 are available for the " +
                     "fully-specified Anderson-Darling test.")
        };

        // ── chi-square, for discrete samplers ────────────────────────────────

        /// <summary>Pearson's χ² statistic Σ(observed − expected)²/expected.</summary>
        public static double ChiSquare(IReadOnlyList<int> observed, IReadOnlyList<double> expected)
        {
            if (observed == null) throw new ArgumentNullException(nameof(observed));
            if (expected == null) throw new ArgumentNullException(nameof(expected));
            if (observed.Count != expected.Count)
                throw new ArgumentException(
                    $"Observed and expected must have the same number of cells ({observed.Count} against {expected.Count}).",
                    nameof(expected));
            if (observed.Count < 2)
                throw new ArgumentException("At least two cells are required.", nameof(observed));

            double sum = 0d;
            for (int i = 0; i < observed.Count; i++)
            {
                if (expected[i] <= 0d)
                    throw new ArgumentException(
                        $"Cell {i} has an expected count of {expected[i]}, so its contribution is undefined. " +
                        "Merge cells until every expected count is positive (conventionally at least 5).",
                        nameof(expected));

                double diff = observed[i] - expected[i];
                sum += diff * diff / expected[i];
            }

            return sum;
        }

        /// <summary>
        /// Upper-tail probability of the χ² statistic on k−1 degrees of freedom — the p-value.
        /// </summary>
        /// <remarks>
        /// The <b>upper</b> tail. A large statistic means a poor fit, so the evidence against the
        /// null lives in the upper tail; returning the lower tail would produce a p-value near 1
        /// for exactly the samples that should be rejected.
        /// <para>
        /// Degrees of freedom is k−1, valid only when no parameter was estimated from the sample.
        /// Each estimated parameter costs a further degree of freedom.
        /// </para>
        /// </remarks>
        public static double ChiSquarePValue(IReadOnlyList<int> observed, IReadOnlyList<double> expected)
            => MMath.ChiSqc(observed.Count - 1, ChiSquare(observed, expected));

        // ── shared ───────────────────────────────────────────────────────────

        private static double[] Sorted(IReadOnlyList<double> sample, Func<double, double> cdf, string paramName)
        {
            if (sample == null) throw new ArgumentNullException(paramName);
            if (cdf == null) throw new ArgumentNullException(nameof(cdf));
            if (sample.Count == 0)
                throw new ArgumentException("A goodness-of-fit statistic needs at least one observation.", paramName);

            // The caller hands over a sample, not a sorted sample; ordering is the instrument's job
            // and a copy is taken so the caller's array is not reordered underneath them.
            double[] sorted = sample.ToArray();
            Array.Sort(sorted);
            return sorted;
        }
    }
}
