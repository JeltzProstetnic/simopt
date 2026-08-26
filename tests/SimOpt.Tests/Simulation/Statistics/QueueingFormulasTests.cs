using System;
using FluentAssertions;
using SimOpt.Simulation.Statistics;
using Xunit;

namespace SimOpt.Tests.Simulation.Statistics;

/// <summary>
/// SIM-64 — the closed-form results the simulator will be validated against.
///
/// <para>
/// These constants were computed in exact rational arithmetic and cross-checked by Little's law at
/// every station and by the identity L = Lq + ρ; the M/M/c case was additionally verified through
/// an independent Erlang-B recursion. The derivations, with the arithmetic shown, are in
/// <c>docs/2026-08-26-analytic-reference.md</c>.
/// </para>
/// <para>
/// They are pinned as literals deliberately. This file is the fixed point of the whole verification
/// argument: if these are wrong, every simulated result that agrees with them is wrong in the same
/// direction and the gate that is supposed to prove the engine correct proves nothing.
/// </para>
/// </summary>
public class QueueingFormulasTests
{
    private const double Tol = 1e-10;

    // ── M/M/1, λ = 0.8, μ = 1.0 ──────────────────────────────────────────────

    [Fact]
    public void MM1_MatchesTheClosedForm()
    {
        const double lambda = 0.8, mu = 1.0;

        QueueingFormulas.Utilization(lambda, mu).Should().BeApproximately(0.8, Tol);
        QueueingFormulas.MM1_L(lambda, mu).Should().BeApproximately(4.0, Tol);
        QueueingFormulas.MM1_Lq(lambda, mu).Should().BeApproximately(3.2, Tol);
        QueueingFormulas.MM1_W(lambda, mu).Should().BeApproximately(5.0, Tol);
        QueueingFormulas.MM1_Wq(lambda, mu).Should().BeApproximately(4.0, Tol);
    }

    [Fact]
    public void MM1_SatisfiesLittlesLaw()
    {
        // Little's law is an identity, not an approximation, so it holds for every stable pair —
        // which makes it a far stronger check than any single pinned value. A formula with a
        // transposed term can match one constant by luck; it cannot satisfy L = λW everywhere.
        foreach ((double lambda, double mu) in new[] { (0.1, 1.0), (0.5, 1.0), (0.8, 1.0), (0.95, 1.0), (2.0, 3.0) })
        {
            QueueingFormulas.MM1_L(lambda, mu)
                .Should().BeApproximately(lambda * QueueingFormulas.MM1_W(lambda, mu), Tol);
            QueueingFormulas.MM1_Lq(lambda, mu)
                .Should().BeApproximately(lambda * QueueingFormulas.MM1_Wq(lambda, mu), Tol);
            QueueingFormulas.MM1_L(lambda, mu).Should().BeApproximately(
                QueueingFormulas.MM1_Lq(lambda, mu) + QueueingFormulas.Utilization(lambda, mu), Tol);
        }
    }

    // ── M/M/c, λ = 2.4, μ = 1.0, c = 3 ───────────────────────────────────────

    [Fact]
    public void MMc_MatchesTheClosedForm()
    {
        const double lambda = 2.4, mu = 1.0;
        const int c = 3;

        QueueingFormulas.MMc_P0(lambda, mu, c).Should().BeApproximately(0.05617977528089888, 1e-12);
        QueueingFormulas.ErlangC(lambda, mu, c).Should().BeApproximately(0.6471910112359550, 1e-12);
        QueueingFormulas.MMc_Lq(lambda, mu, c).Should().BeApproximately(2.5887640449438202, 1e-10);
        QueueingFormulas.MMc_Wq(lambda, mu, c).Should().BeApproximately(1.0786516853932584, 1e-10);
        QueueingFormulas.MMc_W(lambda, mu, c).Should().BeApproximately(2.0786516853932584, 1e-10);
        QueueingFormulas.MMc_L(lambda, mu, c).Should().BeApproximately(4.9887640449438202, 1e-10);
    }

    [Fact]
    public void MMc_WithOneServer_ReducesToMM1()
    {
        // The c-server formulas must degenerate to the single-server ones. This catches an
        // off-by-one in the Erlang recursion, which is otherwise invisible: a recursion that runs
        // one step too far or too few still returns a plausible probability.
        foreach ((double lambda, double mu) in new[] { (0.5, 1.0), (0.8, 1.0), (1.5, 2.0) })
        {
            QueueingFormulas.MMc_Lq(lambda, mu, 1).Should().BeApproximately(QueueingFormulas.MM1_Lq(lambda, mu), Tol);
            QueueingFormulas.MMc_Wq(lambda, mu, 1).Should().BeApproximately(QueueingFormulas.MM1_Wq(lambda, mu), Tol);
            QueueingFormulas.MMc_L(lambda, mu, 1).Should().BeApproximately(QueueingFormulas.MM1_L(lambda, mu), Tol);
        }
    }

    [Fact]
    public void ErlangC_SurvivesAServerCountThatOverflowsTheDirectFormula()
    {
        // a^c and c! both overflow a double well before c = 200 while their ratio does not, so the
        // textbook form returns NaN here. The recursion must return a finite probability — this is
        // the assertion that stops someone "simplifying" it back to the direct sum.
        double c200 = QueueingFormulas.ErlangC(lambda: 180d, mu: 1d, servers: 200);

        c200.Should().BeGreaterThan(0d).And.BeLessThan(1d);
        double.IsNaN(c200).Should().BeFalse();
    }

    // ── M/G/1, λ = 0.5 ──────────────────────────────────────────────────────

    [Fact]
    public void MD1_MatchesTheClosedForm()
    {
        // Deterministic service: Var(S) = 0.
        const double lambda = 0.5, meanService = 1.0;

        QueueingFormulas.MG1_Wq(lambda, meanService, 0d).Should().BeApproximately(0.5, Tol);
        QueueingFormulas.MG1_Lq(lambda, meanService, 0d).Should().BeApproximately(0.25, Tol);
        QueueingFormulas.MG1_W(lambda, meanService, 0d).Should().BeApproximately(1.5, Tol);
        QueueingFormulas.MG1_L(lambda, meanService, 0d).Should().BeApproximately(0.75, Tol);
    }

    [Fact]
    public void MG1_WithUniformService_MatchesTheClosedForm()
    {
        // Service uniform on [0.5, 1.5]: E[S] = 1, Var(S) = (1.5−0.5)²/12 = 1/12.
        const double lambda = 0.5, meanService = 1.0;
        const double variance = 1d / 12d;

        QueueingFormulas.MG1_Wq(lambda, meanService, variance).Should().BeApproximately(13d / 24d, Tol);
        QueueingFormulas.MG1_Lq(lambda, meanService, variance).Should().BeApproximately(13d / 48d, Tol);
        QueueingFormulas.MG1_W(lambda, meanService, variance).Should().BeApproximately(37d / 24d, Tol);
        QueueingFormulas.MG1_L(lambda, meanService, variance).Should().BeApproximately(37d / 48d, Tol);
    }

    [Fact]
    public void ServiceVariabilityAloneChangesTheAnswer()
    {
        const double lambda = 0.5, meanService = 1.0;

        double deterministic = QueueingFormulas.MG1_Wq(lambda, meanService, 0d);
        double uniform = QueueingFormulas.MG1_Wq(lambda, meanService, 1d / 12d);
        double exponential = QueueingFormulas.MG1_Wq(lambda, meanService, 1d);   // Var = E[S]² for Exp

        // Identical λ, ρ and E[S]; only Var(S) differs. This is the whole second-moment lever the
        // analytic battery has — if a simulated M/D/1 and M/G/1 come out equal, the service sampler
        // is returning its mean and every mean-value check would still pass.
        deterministic.Should().BeLessThan(uniform);
        uniform.Should().BeLessThan(exponential);

        // With Var = E[S]² the P-K formula must reproduce M/M/1 exactly, since M/M/1 is the case
        // of exponential service. A cross-family identity like this catches an error in either
        // formula that no single pinned constant would.
        exponential.Should().BeApproximately(QueueingFormulas.MM1_Wq(lambda, 1d / meanService), Tol);
    }

    // ── stability ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData(1.0, 1.0)]     // rho == 1 exactly
    [InlineData(1.5, 1.0)]     // rho > 1
    public void AnUnstableSystem_IsRejectedRatherThanAnswered(double lambda, double mu)
    {
        // At ρ ≥ 1 there is no steady-state mean — the queue grows without bound. Returning a
        // negative or infinite number here would be far worse than throwing: it would flow into a
        // report as though it meant something.
        Action act = () => QueueingFormulas.MM1_Wq(lambda, mu);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IsStable_TreatsExactSaturationAsUnstable()
    {
        QueueingFormulas.IsStable(1.0, 1.0).Should().BeFalse("at rho = 1 the queue still grows without bound");
        QueueingFormulas.IsStable(2.9999, 1.0, servers: 3).Should().BeTrue();
        QueueingFormulas.IsStable(3.0, 1.0, servers: 3).Should().BeFalse();
    }
}
