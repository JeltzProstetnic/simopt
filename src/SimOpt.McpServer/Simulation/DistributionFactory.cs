using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SimOpt.Mathematics.Stochastics.Distributions;
using SimOpt.Mathematics.Stochastics.Interfaces;
using SimOpt.McpServer.Models;

namespace SimOpt.McpServer.Simulation
{
    /// <summary>
    /// Turns a schema-v1 <see cref="DistributionSpec"/> into a configured — and deliberately
    /// <b>uninitialised</b> — engine distribution.
    /// </summary>
    /// <remarks>
    /// <para>
    /// SIM-65. Uninitialised is not an oversight: <see cref="SimOpt.Simulation.Engine.Random{T}"/>
    /// rejects an already-initialised distribution because the wrapper is what registers the
    /// generator with its seed source, and seeding it here is precisely the SIM-89 defect that made
    /// every <c>create_model</c> call throw. Seeding is the builder's job, through
    /// <c>SeedID</c>.
    /// </para>
    /// <para>
    /// Every refusal names the offending parameter and the node it was found on, because the caller
    /// is usually a language model correcting itself and a message it cannot act on costs a whole
    /// round trip (UN-008).
    /// </para>
    /// </remarks>
    public static class DistributionFactory
    {
        /// <summary>The distribution families schema v1 accepts, in the order they are advertised.</summary>
        public static readonly IReadOnlyList<string> KnownTypes = new[]
        {
            "exponential", "triangular", "uniform", "lognormal", "gamma", "constant", "empirical",
        };

        /// <summary>
        /// Builds the distribution described by <paramref name="spec"/>.
        /// </summary>
        /// <param name="spec">The distribution as it appeared in the topology document.</param>
        /// <param name="context">
        /// Where it was found, e.g. <c>node 'exam', parameter 'service'</c>. Quoted verbatim into
        /// every error message.
        /// </param>
        public static IDistribution<double> Create(DistributionSpec? spec, string context)
        {
            if (spec is null)
                throw Refuse(context, "no distribution was given.");

            if (string.IsNullOrWhiteSpace(spec.Type))
                throw Refuse(context, $"the distribution has no 'type'. Valid types: {string.Join(", ", KnownTypes)}.");

            string type = spec.Type!.Trim().ToLowerInvariant();
            double shift = spec.Shift ?? 0d;

            return type switch
            {
                "exponential" => Exponential(spec, context, shift),
                "triangular" => Triangular(spec, context, shift),
                "uniform" => Uniform(spec, context, shift),
                "lognormal" => Lognormal(spec, context, shift),
                "gamma" => Gamma(spec, context, shift),
                "constant" => Constant(spec, context, shift),
                "empirical" => Empirical(spec, context, shift),
                _ => throw Refuse(context,
                        $"unknown distribution type '{spec.Type}'. Valid types: {string.Join(", ", KnownTypes)}."),
            };
        }

        // ── the families ─────────────────────────────────────────────────────

        private static IDistribution<double> Exponential(DistributionSpec spec, string context, double shift)
        {
            if (spec.Mean.HasValue && spec.Rate.HasValue)
                throw Refuse(context,
                    "an exponential takes either 'mean' or 'rate', not both — they disagree unless " +
                    $"mean = 1/rate, and here mean = {Fmt(spec.Mean)} implies rate = {Fmt(1d / spec.Mean!.Value)} " +
                    $"against the stated rate = {Fmt(spec.Rate)}. Remove one.");

            var dist = new NegExponentialDistribution();

            if (spec.Mean.HasValue)
            {
                RequirePositive(spec.Mean.Value, "mean", context);
                dist.ConfigureMean(spec.Mean.Value, shift);
            }
            else if (spec.Rate.HasValue)
            {
                RequirePositive(spec.Rate.Value, "rate", context);
                dist.Configure(spec.Rate.Value, shift);
            }
            else
            {
                throw Missing(context, "exponential", "mean", "or 'rate'");
            }

            return dist;
        }

        private static IDistribution<double> Triangular(DistributionSpec spec, string context, double shift)
        {
            double min = Require(spec.Min, context, "triangular", "min");
            double mode = Require(spec.Mode, context, "triangular", "mode");
            double max = Require(spec.Max, context, "triangular", "max");

            if (max <= min)
                throw Refuse(context, $"a triangular needs max > min, but min = {Fmt(min)} and max = {Fmt(max)}.");
            if (mode < min || mode > max)
                throw Refuse(context,
                    $"a triangular needs min <= mode <= max, but min = {Fmt(min)}, mode = {Fmt(mode)}, max = {Fmt(max)}.");

            var dist = new TriangularDistribution();
            dist.Configure(min + shift, mode + shift, max + shift);
            return dist;
        }

        private static IDistribution<double> Uniform(DistributionSpec spec, string context, double shift)
        {
            double min = Require(spec.Min, context, "uniform", "min");
            double max = Require(spec.Max, context, "uniform", "max");

            if (max <= min)
                throw Refuse(context, $"a uniform needs max > min, but min = {Fmt(min)} and max = {Fmt(max)}.");

            var dist = new UniformDoubleDistribution();
            dist.Configure(min + shift, max + shift);
            return dist;
        }

        private static IDistribution<double> Lognormal(DistributionSpec spec, string context, double shift)
        {
            var dist = new LogNormalDistribution();

            if (spec.Mean.HasValue || spec.Stddev.HasValue)
            {
                double mean = Require(spec.Mean, context, "lognormal", "mean");
                double stddev = Require(spec.Stddev, context, "lognormal", "stddev");
                RequirePositive(mean, "mean", context);
                RequirePositive(stddev, "stddev", context);
                dist.ConfigureMean(mean, stddev, shift);
            }
            else if (spec.Mu.HasValue || spec.Sigma.HasValue)
            {
                double mu = Require(spec.Mu, context, "lognormal", "mu");
                double sigma = Require(spec.Sigma, context, "lognormal", "sigma");
                RequirePositive(sigma, "sigma", context);
                dist.Configure(mu, sigma, shift);
            }
            else
            {
                throw Missing(context, "lognormal", "mean", "and 'stddev' (or 'mu' and 'sigma')");
            }

            return dist;
        }

        private static IDistribution<double> Gamma(DistributionSpec spec, string context, double shift)
        {
            var dist = new GammaDistribution();
            double k = Require(spec.K, context, "gamma", "k");
            RequirePositive(k, "k", context);

            if (spec.Mean.HasValue && spec.Theta.HasValue)
                throw Refuse(context,
                    "a gamma takes 'k' with either 'mean' or 'theta', not both — remove one.");

            if (spec.Mean.HasValue)
            {
                RequirePositive(spec.Mean.Value, "mean", context);
                dist.ConfigureMeanK(spec.Mean.Value, k, shift);
            }
            else if (spec.Theta.HasValue)
            {
                RequirePositive(spec.Theta.Value, "theta", context);
                dist.ConfigureKTheta(k, spec.Theta.Value, shift);
            }
            else
            {
                throw Missing(context, "gamma", "mean", "or 'theta'");
            }

            return dist;
        }

        private static IDistribution<double> Constant(DistributionSpec spec, string context, double shift)
        {
            double value = Require(spec.Value, context, "constant", "value");
            var dist = new ConstantDoubleDistribution();
            dist.Configure(value + shift);
            return dist;
        }

        private static IDistribution<double> Empirical(DistributionSpec spec, string context, double shift)
        {
            double min = Require(spec.Min, context, "empirical", "min");
            double max = Require(spec.Max, context, "empirical", "max");
            List<double> p = spec.Probabilities
                ?? throw Missing(context, "empirical", "probabilities", "");

            if (max <= min)
                throw Refuse(context, $"an empirical needs max > min, but min = {Fmt(min)} and max = {Fmt(max)}.");
            if (p.Count < 2)
                throw Refuse(context,
                    $"an empirical needs at least 2 probabilities, one per equally spaced value from min to max; {p.Count} given.");
            if (p.Any(x => x < 0d))
                throw Refuse(context, "an empirical cannot have a negative probability.");

            double sum = p.Sum();
            if (Math.Abs(sum - 1d) > 1e-9)
                throw Refuse(context,
                    $"the empirical probabilities sum to {Fmt(sum)} and must sum to 1. " +
                    $"They are the probabilities of {p.Count} equally spaced values from min to max.");

            var dist = new HistogramDoubleDistribution();
            // Configure re-checks the sum with an exact comparison, so hand it a normalised copy:
            // a list that sums to 1 to within 1e-9 can still fail `sum > 1d` on the last bit.
            List<double> normalised = p.Select(x => x / sum).ToList();
            dist.Configure(min + shift, max + shift, normalised);
            return dist;
        }

        // ── refusals ─────────────────────────────────────────────────────────

        private static double Require(double? value, string context, string type, string parameter)
            => value ?? throw Missing(context, type, parameter, "");

        private static void RequirePositive(double value, string parameter, string context)
        {
            if (value <= 0d)
                throw Refuse(context, $"'{parameter}' must be positive, but {Fmt(value)} was given. " +
                                      "Every distribution in a topology models a duration or an interval.");
        }

        private static InvalidOperationException Missing(string context, string type, string parameter, string alternative)
            => Refuse(context,
                $"a {type} distribution needs '{parameter}'{(alternative.Length > 0 ? " " + alternative : "")}, which is missing.");

        private static InvalidOperationException Refuse(string context, string problem)
            => new InvalidOperationException($"{context}: {problem}");

        private static string Fmt(double? v)
            => v.HasValue ? v.Value.ToString("0.######", CultureInfo.InvariantCulture) : "(none)";
    }
}
