<!-- Action: reference -->
<!-- Tracked-by: SIM-64 -->
<!-- consumed-by: docs/2026-08-26-analytic-reference.md (constants and run-length design this implements) -->
# SIM-64 — the analytic benchmark battery, as built

Closes SIM-64. Serves UN-007 (independently verifiable accuracy). Part 1 — the closed forms —
shipped as `QueueingFormulas` in `2f3968c`; this records part 2, the simulated half that actually
gates the engine.

Constants, CI machinery and run-length sizing come from `docs/2026-08-26-analytic-reference.md`.
This document records **what was built, what was measured, and the three places the build departs
from that reference** — with the reason in each case.

## What ships

| File | What |
|---|---|
| `tests/SimOpt.Tests/Simulation/Statistics/AnalyticBatteryTests.cs` | Four benchmark systems run against their closed forms, plus the distributional check. 11 tests. |
| `src/SimOpt.Statistics/Analysis/GoodnessOfFit.cs` | One-sample KS, Anderson–Darling and chi-square against a fully specified null. |
| `tests/SimOpt.Tests/Statistics/GoodnessOfFitTests.cs` | 17 tests: statistics pinned to hand computation, critical values to the published table, samplers accepted **and** wrong ones rejected. |

Suite 886 → 914. Wall clock 5 s → 31 s.

## Measured results

Pinned base seed `20_260_826`, R = 20 replications per system. `h` is the reported 95% half-width.

| system | statistic | analytic | simulated | h | h/analytic | error |
|---|---|---|---|---|---|---|
| M/M/1 λ=0.8 μ=1 | Wq | 4.00000 | 4.12095 | 0.14465 | 3.62% | 0.84·h |
| | Lq | 3.20000 | 3.30889 | 0.12377 | 3.87% | 0.88·h |
| | ρ | 0.80000 | 0.80367 | 0.00397 | 0.50% | 0.92·h |
| M/D/1 λ=0.5 | Wq | 0.50000 | 0.50436 | 0.01105 | 2.21% | 0.39·h |
| | Lq | 0.25000 | 0.25220 | 0.00632 | 2.53% | 0.35·h |
| M/G/1 U[0.5,1.5] | Wq | 0.54167 | 0.53950 | 0.01106 | 2.04% | 0.20·h |
| | Lq | 0.27083 | 0.26956 | 0.00694 | 2.56% | 0.18·h |
| M/M/3 λ=2.4 μ=1 | Wq | 1.07865 | 1.05606 | 0.02669 | 2.47% | 0.85·h |
| | Lq | 2.58876 | 2.53411 | 0.06686 | 2.58% | 0.82·h |
| Jackson station 1 | Wq / Lq / ρ | 0.5 / 0.5 / 0.5 | 0.50134 / 0.50211 / 0.50129 | — | ≤2.16% | ≤0.41·h |
| Jackson station 2 | Wq / Lq / ρ | 1.33333 / 1.33333 / 0.66667 | 1.33550 / 1.33759 / 0.66691 | — | ≤2.44% | ≤0.13·h |

Cross-checks that use no closed form at all: Little's law in the engine's own numbers
(λ·Wq = 3.29676 against Lq = 3.30889); M/M/3 pool utilisation totalling 2.39742 against an offered
load of 2.4; Jackson station 2's utilisation implying a throughput of 1.00037 against an external
rate of 1.0.

Second moments: Wq rises from 0.50436 (deterministic service) to 0.53950 (uniform service) — a
measured gap of 0.03514 against an analytic 1/24 = 0.04167, with λ, ρ and E[S] identical between
the two systems.

Distributional: M/M/1 sojourn times against the exact Exp(0.5), n = 2,000 thinned —
√n·D = 0.66404 (critical 1.62762) and A² = 0.55745 (critical 3.85700).

**M/M/3 dispatch, worth knowing before someone reads it as a defect:** the pool utilisations are
[0.8576, 0.8032, 0.7367], not equal. `FirstOrDefault(idle)` prefers the lowest-numbered free
server. This is a dispatch policy and cannot affect a queueing statistic — the system is
work-conserving either way, and the total is what theory constrains.

## Three departures from the reference design

**1. The gate is 99.9%, the reported interval is 95%.** The reference proposes asserting
containment in the reported interval. That is a mistake that looks like rigour: the battery makes
fourteen containment assertions, so a *correct* engine on a fresh set of seeds fails at least one
about half the time (0.95¹⁴ ≈ 0.49). Seeds are pinned, so today's build is deterministic — but
SIM-81 replaces the random number stream outright, which re-rolls all fourteen and would turn a
coin flip into a red build with nothing wrong. Containment is therefore gated at 99.9%
(0.999¹⁴ ≈ 0.986) while the 95% interval is still reported and still asserted to be *narrow*
(κ = 0.05). Both come from the same replication data; only the threshold for going red moves.

Verified: the battery passes on the pinned seed set and on three unrelated ones (111, 20260827,
987654321).

**2. The distributional check runs at ρ = 0.5, not ρ = 0.8, and is thinned.** The reference
proposes a KS test of M/M/1 sojourn times at λ = 0.8 without addressing autocorrelation — and
consecutive sojourn times are heavily autocorrelated, which is precisely the assumption a KS test
makes and this data violates. At ρ = 0.8 the integrated autocorrelation time is ≈ 82 customers, so
a naive test credits the sample with ~82× the information it holds and rejects a correct engine. At
ρ = 0.5 that time falls to ≈ 10 customers; keeping one completion in 50 leaves a lag-50 correlation
of ≈ e⁻⁵. The distributional claim (sojourn ~ Exp(μ−λ)) is exact at every stable ρ, so nothing is
given up.

**3. One fixture, not one experiment per test.** Each of the five systems is simulated once and all
assertions read from the same result. Re-running per assertion cost more wall clock than the entire
rest of the suite, and it let two tests disagree about "the same" system because they were in fact
two different sets of sample paths.

## What the gate actually catches — measured by mutation

Each mutation was applied, the battery run, then reverted.

| mutation | result |
|---|---|
| Exponential sampler returns its mean instead of a draw | **6 of 9 fail.** The three that pass are the internal-consistency checks (Little's law, load conservation, flow conservation) — correctly, since those hold for any system. |
| Time-weighted statistics inflated by 3% | **6 of 9 fail**, including Little's law and both flow-conservation checks. M/M/3 survives only because its estimate happens to sit 2% low on this seed set, so the error partially cancels. |
| FIFO → LIFO on every queue | **Only the sojourn test fails** — and it fails hard: √n·D 0.66 → 2.78 against a critical 1.63, A² 0.56 → 14.08 against 3.86, with the mean unmoved at 2.07. Every mean-based assertion in the battery passes, exactly as work-conservation predicts. |

The third row is the argument for the distributional check existing at all, and it is the reason
the battery is not just the four closed-form comparisons.

**Sensitivity.** From the half-widths above, the gate detects a systematic error of roughly 7% on
the ρ = 0.8 systems and roughly 1% on utilisation. It is a gate against defects, not a precision
instrument, and the second assertion (κ) is what stops a high-variance engine hiding inside a wide
interval.

## What this still does not prove

- **Every closed-form quantity is a first moment.** The M/D/1-against-M/G/1-uniform pair is the
  only lever on a second moment, and the sojourn KS/AD test the only one on a whole distribution.
- **Only one queue discipline is covered.** LIFO now demonstrably fails the sojourn test, but no
  benchmark exists for a priority discipline.
- **Coverage is not measured.** The reference recommends a meta-test over ~200 seed sets asserting
  empirical coverage in [0.90, 0.99]. That is minutes of run time, so it belongs in a nightly job
  rather than the per-commit gate. Not built.
- **No blocking, balking or finite-capacity benchmark.** Every queue in the battery is unbounded;
  M/M/1/K has a closed form and is the obvious next system.
- **No transient validation.** Warm-up periods are justified by relaxation-time arithmetic, not by
  a Welch plot. The reference asks for one plot kept in `docs/` and cited; not done.
