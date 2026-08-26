<!-- Action: reference -->
<!-- Tracked-by: SIM-63, SIM-64 (both closed — retained as the standing derivation record) -->
<!-- consumed-by: docs/plans/2026-08-26-sim-64-analytic-battery.md (implements §3 and §4), tests/SimOpt.Tests/Simulation/Statistics/AnalyticBatteryTests.cs (every pinned constant) -->
# Analytic reference: CI machinery and closed-form queueing constants

> **SIM-63 and SIM-64 are both closed.** This file is kept because it is the derivation, not the
> handover: every constant pinned in the battery traces back here. Three places where the build
> departed from what is written below — the 99.9% containment gate, the ρ = 0.5 thinned
> distributional check, and the single-fixture structure — are recorded with their reasons in
> `docs/plans/2026-08-26-sim-64-analytic-battery.md`. Read that alongside §4 before changing a run
> length.

Derived 2026-08-26 for SIM-63 (replication confidence intervals) and SIM-64 (the analytic
benchmark battery). Every constant here is destined to be pinned as a literal test value, so each
was computed in exact rational arithmetic and cross-checked by an independent route.

---

## 1. What the codebase already provides

**There is no quantile or inverse CDF of any kind** — a repo-wide search for
`InverseCdf|Quantile|Ppf|TInv|NormInv|InvErf` returns only map-projection hits. `TStudent.cs:13` in
`SimOpt.Statistics/Kernels/` is an **SVM Mercer kernel**, not a t-distribution; do not be misled by
the filename.

But the special functions needed to *build* a quantile are already there, in
`src/SimOpt.Mathematics/MMath.cs` (Cephes ports):

| Member | Line | Note |
|---|---|---|
| `Ibeta(aa, bb, xx)` | 4406 | **Regularized** incomplete beta I_x(a,b) — this is the t-CDF |
| `Incbcf` / `Incbd` / `PowerSeries` | 4498 / 4585 / 4674 | `Ibeta` internals |
| `Gamma(x)` / `Lgamma(x)` | 3775 / 4311 | |
| `Igam(a,x)` / `Igamc(a,x)` / `Rgamma(a,z)` | 4073 / 4011 / 3880 | regularized incomplete gamma |
| `Normal(value)` | 4167 | normal **CDF** (not the inverse) |
| `Erf` / `Erfc` | 4278 / 4190 | |
| `ChiSq(df,x)` / `ChiSqc(df,x)` | 4112 / 4129 | chi-square CDF / **upper tail** — gives a GOF p-value directly |
| `Variance(double[], mean)` | 2800 | **already N−1**, and NaN at N=1 — the contract we want |
| `Mean` / `StandardDeviation` / `StandardError` / `Median` | 2707 / 2721 / 2742 / 2761 | |

**A Welford accumulator already exists** — `TallyCollector.cs:82-84` (SIM-63, committed). The CI
layer sits *above* it: one collector per replication → R replication means → the interval below.

### Distribution API traps

`NegExponentialDistribution` has **no constructor taking `(seed, lambda)` or any mean**. The common
paths — `new NegExponentialDistribution(seed, antithetic)` and `(IRandomSource)` — leave
`lambda = 1.0` and `Configured == false`, so `Configure`/`ConfigureMean` must be called afterwards.

> **The trap:** `Configure` takes a **rate**, `ConfigureMean` takes a **mean**. Passing a service
> *rate* μ=1.0 to `ConfigureMean` happens to work; passing λ=0.8 to `ConfigureMean` silently gives a
> rate of 1.25. Any benchmark written carelessly here validates the wrong system and passes.

`ConstantDoubleDistribution(double value = 0d, bool initialize = true)` — the second parameter is
`initialize`, **not** a seed. `new ConstantDoubleDistribution(1.0, false)` yields
`Configured == true, Initialized == false`, which is the form the engine's `Random<T>` wrapper
requires.

---

## 2. The confidence interval

R independent replications with replication means Y₁…Y_R, each the within-replication average from
a `TallyCollector`. The Y_i are i.i.d. **because the replications are independent** — the
customer-level observations inside one run are emphatically not, which is the entire reason for the
replication method.

```
Ȳ  = (1/R) Σ Yᵢ
s² = (1/(R−1)) Σ (Yᵢ − Ȳ)²
SE = s/√R
h  = t_{1−α/2, R−1} · s/√R          interval: Ȳ ± h
```

**N−1, not N.** Ȳ is estimated from the same data, so Σ(Yᵢ−Ȳ)² has expectation (R−1)σ². Dividing by
R gives a biased-low variance and an interval that is **systematically too narrow** — for a coverage
claim that is the dangerous direction of error, because the tool would overstate its own precision.
Degrees of freedom is R−1. The t rather than z is required because σ is estimated: at R=10 the t
inflates the interval by 15% over the normal, which is not cosmetic.

### t-quantiles, α = 0.05 two-sided → t_{0.975, ν}

Computed by root-finding on `F(t;ν) = 1 − ½·I_{ν/(ν+t²)}(ν/2, ½)`, verified by 15-digit CDF
round-trip and against the exact closed forms for ν=1 (Cauchy, tan(19π/40)), ν=2 and ν=4.

| ν | t | ν | t | ν | t |
|---|---|---|---|---|---|
| 1 | 12.7062047362 | 11 | 2.2009851601 | 21 | 2.0796138447 |
| 2 | 4.3026527297 | 12 | 2.1788128297 | 22 | 2.0738730679 |
| 3 | 3.1824463053 | 13 | 2.1603686565 | 23 | 2.0686576104 |
| 4 | 2.7764451052 | 14 | 2.1447866879 | 24 | 2.0638985616 |
| 5 | 2.5705818356 | 15 | 2.1314495456 | 25 | 2.0595385528 |
| 6 | 2.4469118511 | 16 | 2.1199052992 | 26 | 2.0555294386 |
| 7 | 2.3646242516 | 17 | 2.1098155778 | 27 | 2.0518305165 |
| 8 | 2.3060041352 | 18 | 2.1009220402 | 28 | 2.0484071418 |
| 9 | 2.2621571628 | 19 | 2.0930240544 | 29 | 2.0452296421 |
| 10 | 2.2281388520 | 20 | 2.0859634473 | 30 | 2.0422724563 |

Full precision for the pinned cases:

```
ν = 1   →  12.706204736175    (exact: tan(19π/40))
ν = 9   →   2.262157162799
ν = 19  →   2.093024054409
ν → ∞   →   1.959963984540    (z_{0.975} — pin as the asymptote test)
```

**Implement by root-finding on `MMath.Ibeta`, and pin the table as regression tests.** For
expert-evidence software a lookup table alone is indefensible (silently wrong at ν=31, 47, 200) and
a root-finder alone is unverified. Both.

### R = 1 and R = 2

**R = 1: refuse.** s² is 0/0; a single replication carries no information about its own
variability. Return the point estimate with the half-width `NaN`/`null` and make it impossible to
render "±0.000". **A half-width of zero on one replication is the most dangerous number this
subsystem could emit, because it reads as infinite precision.** Mirror `TallyCollector.Variance`,
which already returns NaN at count ≤ 1.

**R = 2: compute it and let it be embarrassing.** ν=1, t=12.7062047362, s = |Y₁−Y₂|/√2, so
h ≈ 6.353·|Y₁−Y₂|. Honest and nearly useless, which is the correct signal. Do not special-case, do
not substitute z, do not clamp. An advisory flag (`R < 5: valid but low precision`) is fine; a
silent narrowing never is.

---

## 3. Closed-form queueing results

All cross-checked by Little's law at every station and by `L = Lq + ρ`.

### M/M/1
```
ρ = λ/μ                 stability: ρ < 1 strictly
L = ρ/(1−ρ)      Lq = ρ²/(1−ρ) = L − ρ
W = 1/(μ−λ)      Wq = ρ/(μ−λ) = W − 1/μ
```
**λ = 0.8, μ = 1.0:** ρ = **0.8**, L = **4**, Lq = **3.2**, W = **5**, Wq = **4** (exact integers).

Checks: λW = 4 = L ✓, λWq = 3.2 = Lq ✓, Lq+ρ = 4 = L ✓.

### M/M/c
```
a = λ/μ,  ρ = a/c                    stability: ρ < 1
P0 = [ Σ_{n=0}^{c−1} aⁿ/n!  +  a^c/(c!(1−ρ)) ]⁻¹
C  = Erlang-C = [ a^c/(c!(1−ρ)) ]·P0
Lq = C·ρ/(1−ρ)     Wq = Lq/λ = C/(cμ−λ)     W = Wq + 1/μ     L = λW = Lq + a
```
**λ = 2.4, μ = 1.0, c = 3:** a = 2.4, ρ = 0.8.

| quantity | exact | decimal |
|---|---|---|
| P0 | 5/89 | **0.05617977528089888** |
| Erlang-C | 288/445 | **0.6471910112359550** |
| Lq | 1152/445 | **2.5887640449438** |
| Wq | 96/89 | **1.078651685393258** |
| W | 185/89 | **2.0786516853933** |
| L | 444/89 | **4.9887640449438** |

Verified independently via the Erlang-B recursion → B(3) = 288/1157, C = B/(1−ρ(1−B)) = 288/445,
**identical to the last bit**.

> **Implement Erlang-C via the Erlang-B recursion, not the direct sum.** `aⁿ` and `n!` both overflow
> `double` long before c = 200 while their ratio does not, so the direct form dies on realistic
> call-centre sizing:
> ```csharp
> double b = 1.0;
> for (int n = 1; n <= c; n++) b = (a * b) / (n + a * b);   // Erlang-B
> double erlangC = b / (1.0 - rho * (1.0 - b));
> ```

### M/G/1 — Pollaczek–Khinchine
```
ρ = λ·E[S]              E[S²] = Var(S) + E[S]²
Wq = λ·E[S²] / (2(1−ρ))          Lq = λ·Wq
W  = Wq + E[S]                   L  = λW = Lq + ρ
```
Equivalent SCV form (c_s² = Var(S)/E[S]²): `Wq = (ρ·E[S]/(1−ρ))·(1+c_s²)/2` — the M/M/1 answer
times (1+c_s²)/2. Worth implementing both and differential-testing them against each other.

**(i) M/D/1, λ = 0.5, E[S] = 1.0, Var = 0:** ρ = 0.5, E[S²] = 1.
Wq = **0.5**, Lq = **0.25**, W = **1.5**, L = **0.75**. Exactly half the M/M/1 Wq at the same ρ, as
(1+c_s²)/2 = ½ requires.

**(ii) Uniform service on [0.5, 1.5], λ = 0.5:** E[S] = 1.0, Var = 1/12, E[S²] = 13/12, c_s² = 1/12.
Wq = 13/24 = **0.54166666666667**, Lq = 13/48 = **0.27083333333333**,
W = 37/24 = **1.54166666666667**, L = 37/48 = **0.77083333333333**.

> These two share λ, ρ, E[S] and W−Wq and differ **only in service variance**, which makes them the
> cleanest possible pair for proving the engine propagates second moments rather than just means.
> **If both come out 0.5, the service sampler is returning its mean.**

### Two-station tandem Jackson network
λ = 1.0 external into station 1, μ₁ = 2.0, μ₂ = 1.5, exponential, single server per station, FCFS,
unbounded queues, all of station 1's output routed to station 2.

**Why product form applies.** Jackson's theorem: open network, Poisson external arrivals,
exponential service, one server per node, unlimited capacity, Markovian routing ⇒
P(n₁,n₂) = ∏ᵢ (1−ρᵢ)ρᵢ^{nᵢ}. Traffic equations give λ₁ = λ₂ = 1.0, so each node has the *marginal*
distribution of an independent M/M/1 at that rate.

Two caveats to state in the test's own comments, because they are the ones misremembered: product
form does **not** mean the queue-length processes are independent, only that the *stationary joint*
distribution factorises; and in a general Jackson network with feedback the internal flows are
**not** Poisson while the product form holds anyway. This particular network is feed-forward, so
Burke's theorem additionally makes station 2's input genuinely Poisson(1.0) — do not let that
special case teach the wrong lesson to whoever extends the battery.

| | ρ | L | Lq | W | Wq |
|---|---|---|---|---|---|
| Station 1 | **0.5** | **1.0** | **0.5** | **1.0** | **0.5** |
| Station 2 | **0.66666666666667** | **2.0** | **1.33333333333333** | **2.0** | **1.33333333333333** |
| System | — | **3.0** | **1.83333333333333** | **3.0** | **1.83333333333333** |

Little's law at network level: 1.0 × 3.0 = 3.0 = L ✓.

---

## 4. Making the gate stable rather than flaky

### The variance you actually face
Per-replication mean of n post-warm-up delays has Var ≈ σ²∞/n where σ²∞ is the **time-average
variance constant** (sum of autocovariances), *not* the marginal variance. For M/M/1 with μ=1,
σ²∞ = ρ(2+5ρ−4ρ²+ρ³)/(1−ρ)⁴.

**At ρ = 0.8: σ²∞ = 1976, against a marginal Var(Wq) of only 24.** The integrated autocorrelation
time is ≈ 82 customers. **Sizing a run off the marginal variance understates the required length by
a factor of 82 — the single most common way this gate ends up flaky.** (1976 confirmed two ways: a
Bartlett-windowed spectral estimate over 29M customers, and the observed replication-level standard
deviations below matching to within 1.5%.)

### Relaxation time
M/M/1 transient decays with time constant 1/(√μ−√λ)². At ρ=0.8 that is **89.7213595500** time
units ≈ 72 customers.

### Recommended design for M/M/1 at ρ = 0.8

| | per-commit gate | nightly gate |
|---|---|---|
| Replications R | **20** (ν=19, t=2.0930240544) | **30** (ν=29, t=2.0452296421) |
| Warm-up | **1,000 time units** | **1,000 time units** |
| Post-warm-up run | **20,000** time units (≈16,000 customers) | **100,000** (≈80,000 customers) |
| Predicted half-width | 0.1645 (4.1% of Wq=4) | 0.0587 (1.5%) |
| **Measured** half-width | **0.1634** (200 trials) | **0.0578** (60 trials) |
| Measured mean estimate | 4.0007 | 3.9987 |
| Measured coverage | 0.940 | 0.917 |

Prediction and measurement agree to under 1%, so any other ρ can be sized directly from σ²∞/n.
**General sizing rule: R·n ≥ σ²∞·(t/h)².**

**Warm-up 1,000 time units ≈ 11 relaxation times.** Residual bias after deleting d customers is
≈ (E[Wq]·τ/n)·e^{−d/τ}; at d = 800, τ ≈ 80 that is e⁻¹⁰ ≈ 4.5×10⁻⁵ — undetectable against a
half-width of 0.16. Two conditions on reusing it: **it is ρ-specific** (relaxation time grows as
1/(1−√ρ)², so ρ=0.9 needs 379 time units — rescale, do not copy), and a Welch moving-average plot
should be produced once, kept in `docs/`, and cited, because "10 relaxation times" is defensible
only when someone has looked at the curve.

### Three things that decide flaky vs stable, none of them run length

1. **Pin the seeds.** A 95% CI gate on fresh entropy fails 1 build in 20 *by construction* — that
   is what 95% means. Fix the R seeds as literals; the test becomes deterministic and the interval
   still means what it says about the estimator. Derive replication seeds from one pinned root so R
   can change without invalidating the pin.
2. **Assert both directions:** `|Ȳ − analytic| ≤ h` **and** `h ≤ κ·analytic` for a stated κ (0.05
   fast, 0.02 nightly). Without the second, **a broken engine returning garbage with huge variance
   sails through a coverage-only check.**
3. **Run coverage as a meta-test, not a gate** — occasionally 200 independent seed sets, assert
   empirical coverage in [0.90, 0.99]. The measured 0.940/0.917 undercoverage is expected and
   benign (replication means of a heavily right-skewed delay distribution are not exactly normal at
   R=20–30), but it means the meta-test's lower bound must not be 0.95.

### Goodness of fit

**Kolmogorov–Smirnov, one-sample, against the fully specified distribution** — no parameters
estimated from the sample (estimating λ from the data changes the null distribution and makes the
standard critical values badly wrong; that is the Lilliefors case). Reject if √n·Dₙ > c_α:

| α | c_α | exact |
|---|---|---|
| 0.10 | **1.22387** | √(−½ ln 0.05) |
| 0.05 | **1.35810** | √(−½ ln 0.025) |
| 0.01 | **1.62762** | √(−½ ln 0.005) |

> **Do not use a huge sample.** At n = 10⁶ the KS test has enough power to reject on deviations of
> order 10⁻³ — including the ones the Mersenne Twister genuinely has (SIM-81). Use **n = 10,000
> with a pinned seed** against c_{0.01}: that detects a genuinely wrong sampler (wrong rate, missing
> shift, the `1−U` vs `U` inverse-transform bug SIM-56 fixed) while staying deterministic and fast.

**Add Anderson–Darling for the exponential samplers.** KS is weakest exactly where queueing is most
sensitive — the upper tail — and A² weights the tails. Fully-specified critical values:
**1.933** (α=0.10), **2.492** (0.05), **3.070** (0.025), **3.857** (0.01).

For discrete samplers use chi-square GOF with df = k−1; `MMath.ChiSqc(df, x)` gives the upper tail
so the assertion can be on the p-value directly.

### The gap the battery above does not close

**Every quantity in §3 is a first moment.** An engine that gets L, Lq, W and Wq right on all four
models can still have a broken service-time variance, a broken tie-break, or a broken queue
discipline. The M/D/1-vs-M/G/1-uniform pair is the one lever on second moments. Add at least one
*distributional* check as well: **for M/M/1 the sojourn time is exactly Exp(μ−λ)**, so a KS test of
simulated sojourn times against Exp(0.2) at λ=0.8, μ=1.0 is a far stronger statement than Wq = 4.0
and costs nothing that is not already being simulated.
