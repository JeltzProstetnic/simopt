using System;

namespace SimOpt.Simulation.Statistics
{
    /// <summary>
    /// Closed-form results for the standard queueing systems, used to verify that the simulator
    /// agrees with theory where theory has an answer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// SIM-64, serving UN-007. This is simultaneously the product's quality gate and the strongest
    /// marketing asset it has: a simulator whose output is checked against results a sceptical
    /// reader can derive independently is making a claim an LLM-written one-off script cannot.
    /// </para>
    /// <para>
    /// It is production code rather than test-only code because the same formulas answer the
    /// pre-run sanity question of UN-008 — "is this station overloaded" is exactly ρ ≥ 1, and
    /// telling a user that before they wait for a run is worth more than telling them after.
    /// </para>
    /// <para>
    /// Every value returned here is a <b>mean</b>. An engine that matches all of them can still
    /// have a broken service-time variance, tie-break or queue discipline, so these results bound
    /// what the analytic battery can prove and must not be mistaken for a full verification.
    /// </para>
    /// </remarks>
    public static class QueueingFormulas
    {
        /// <summary>Utilisation ρ = λ/μ for a single server. Stable only when ρ &lt; 1.</summary>
        public static double Utilization(double arrivalRate, double serviceRate)
        {
            RequirePositive(arrivalRate, nameof(arrivalRate));
            RequirePositive(serviceRate, nameof(serviceRate));
            return arrivalRate / serviceRate;
        }

        /// <summary>
        /// Whether a c-server system is stable, i.e. λ &lt; cμ. Equality is unstable, not borderline:
        /// at ρ = 1 the queue grows without bound, just more slowly.
        /// </summary>
        public static bool IsStable(double arrivalRate, double serviceRate, int servers = 1)
            => arrivalRate < servers * serviceRate;

        // ── M/M/1 ───────────────────────────────────────────────────────────

        /// <summary>Mean number in system L = ρ/(1−ρ) for M/M/1.</summary>
        public static double MM1_L(double lambda, double mu)
        {
            double rho = RequireStable(lambda, mu, 1);
            return rho / (1d - rho);
        }

        /// <summary>Mean number waiting Lq = ρ²/(1−ρ) for M/M/1.</summary>
        public static double MM1_Lq(double lambda, double mu)
        {
            double rho = RequireStable(lambda, mu, 1);
            return rho * rho / (1d - rho);
        }

        /// <summary>Mean time in system W = 1/(μ−λ) for M/M/1.</summary>
        public static double MM1_W(double lambda, double mu)
        {
            RequireStable(lambda, mu, 1);
            return 1d / (mu - lambda);
        }

        /// <summary>Mean waiting time Wq = ρ/(μ−λ) for M/M/1.</summary>
        public static double MM1_Wq(double lambda, double mu)
        {
            double rho = RequireStable(lambda, mu, 1);
            return rho / (mu - lambda);
        }

        // ── M/M/c ───────────────────────────────────────────────────────────

        /// <summary>
        /// Erlang-C: the probability that an arriving customer has to wait, for M/M/c.
        /// </summary>
        /// <remarks>
        /// Computed through the Erlang-B recursion rather than the direct sum. The textbook form
        /// evaluates a^c and c! separately, and both overflow a double long before c = 200 while
        /// their ratio does not — so the direct form fails on exactly the call-centre and
        /// multi-machine sizings this is most useful for. The recursion is unconditionally stable
        /// and, on the pinned case, exact to the last bit.
        /// </remarks>
        public static double ErlangC(double lambda, double mu, int servers)
        {
            double rho = RequireStable(lambda, mu, servers);
            double a = lambda / mu;

            double b = 1d;                                   // Erlang-B for 0 servers
            for (int n = 1; n <= servers; n++)
                b = (a * b) / (n + a * b);

            return b / (1d - rho * (1d - b));
        }

        /// <summary>Probability of an empty system P0 for M/M/c.</summary>
        public static double MMc_P0(double lambda, double mu, int servers)
        {
            double rho = RequireStable(lambda, mu, servers);
            double a = lambda / mu;

            // P0 = C·(1−ρ)·c!/a^c, rearranged from the Erlang-C definition so that the same
            // overflow-free recursion carries it. Evaluated as a running product for the same
            // reason the recursion is used above.
            double erlangC = ErlangC(lambda, mu, servers);
            double factorialOverPower = 1d;
            for (int n = 1; n <= servers; n++) factorialOverPower *= n / a;

            return erlangC * (1d - rho) * factorialOverPower;
        }

        /// <summary>Mean number waiting Lq = C·ρ/(1−ρ) for M/M/c.</summary>
        public static double MMc_Lq(double lambda, double mu, int servers)
        {
            double rho = RequireStable(lambda, mu, servers);
            return ErlangC(lambda, mu, servers) * rho / (1d - rho);
        }

        /// <summary>Mean waiting time Wq = C/(cμ−λ) for M/M/c.</summary>
        public static double MMc_Wq(double lambda, double mu, int servers)
        {
            RequireStable(lambda, mu, servers);
            return ErlangC(lambda, mu, servers) / (servers * mu - lambda);
        }

        /// <summary>Mean time in system W = Wq + 1/μ for M/M/c.</summary>
        public static double MMc_W(double lambda, double mu, int servers)
            => MMc_Wq(lambda, mu, servers) + 1d / mu;

        /// <summary>Mean number in system L = λW for M/M/c.</summary>
        public static double MMc_L(double lambda, double mu, int servers)
            => lambda * MMc_W(lambda, mu, servers);

        // ── M/G/1 ───────────────────────────────────────────────────────────

        /// <summary>
        /// Mean waiting time for M/G/1 by the Pollaczek–Khinchine formula,
        /// Wq = λ·E[S²] / (2(1−ρ)).
        /// </summary>
        /// <param name="meanService">E[S].</param>
        /// <param name="serviceVariance">Var(S). Zero gives the M/D/1 case.</param>
        /// <remarks>
        /// This is the one result in the class that depends on a <b>second</b> moment, which makes
        /// it the only lever the analytic battery has on whether the engine propagates service
        /// variability at all rather than merely its mean. Two systems with identical λ and E[S]
        /// and different Var(S) must produce different answers here; if they do not, the service
        /// sampler is returning its mean.
        /// </remarks>
        public static double MG1_Wq(double lambda, double meanService, double serviceVariance)
        {
            RequirePositive(lambda, nameof(lambda));
            RequirePositive(meanService, nameof(meanService));
            if (serviceVariance < 0d)
                throw new ArgumentOutOfRangeException(nameof(serviceVariance), "Variance cannot be negative.");

            double rho = lambda * meanService;
            if (rho >= 1d)
                throw new ArgumentException($"Unstable system: rho = {rho:F6} must be below 1.", nameof(lambda));

            double secondMoment = serviceVariance + meanService * meanService;
            return lambda * secondMoment / (2d * (1d - rho));
        }

        /// <summary>Mean number waiting Lq = λ·Wq for M/G/1.</summary>
        public static double MG1_Lq(double lambda, double meanService, double serviceVariance)
            => lambda * MG1_Wq(lambda, meanService, serviceVariance);

        /// <summary>Mean time in system W = Wq + E[S] for M/G/1.</summary>
        public static double MG1_W(double lambda, double meanService, double serviceVariance)
            => MG1_Wq(lambda, meanService, serviceVariance) + meanService;

        /// <summary>Mean number in system L = λW for M/G/1.</summary>
        public static double MG1_L(double lambda, double meanService, double serviceVariance)
            => lambda * MG1_W(lambda, meanService, serviceVariance);

        // ── helpers ─────────────────────────────────────────────────────────

        private static void RequirePositive(double value, string name)
        {
            if (value <= 0d || double.IsNaN(value))
                throw new ArgumentOutOfRangeException(name, "Rate must be positive.");
        }

        private static double RequireStable(double lambda, double mu, int servers)
        {
            RequirePositive(lambda, nameof(lambda));
            RequirePositive(mu, nameof(mu));
            if (servers < 1)
                throw new ArgumentOutOfRangeException(nameof(servers), "At least one server is required.");

            double rho = lambda / (servers * mu);
            if (rho >= 1d)
                throw new ArgumentException(
                    $"Unstable system: rho = {rho:F6} must be below 1. At or above 1 the queue grows " +
                    "without bound and no steady-state mean exists.", nameof(lambda));
            return rho;
        }
    }
}
