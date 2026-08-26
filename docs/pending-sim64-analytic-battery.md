<!-- Action: act -->
<!-- Tracked-by: SIM-64, SIM-90 -->
# Next: finish SIM-64 — the simulated half of the analytic battery

**The analytic half is done and pinned** (`QueueingFormulas` + 11 tests, commit `2f3968c`). What
remains is the part that actually gates the engine: run each system in the simulator and assert the
closed-form answer falls inside the reported confidence interval.

Everything needed is already derived. **Do not re-derive it** — `docs/2026-08-26-analytic-reference.md`
carries the constants, the CI formula, the t-quantile table, the goodness-of-fit critical values,
and a *measured* run-length and replication design.

## Build order

| # | Model | Wiring | Notes |
|---|---|---|---|
| 1 | **M/M/1** | source(exp) → buffer → server(exp) → sink | Already wired in `ReplicationRunnerTests.BuildStochastic` — lift that helper. λ=0.8, μ=1.0 ⇒ Wq = 4 exactly. |
| 2 | **M/D/1 and M/G/1-uniform** | as above, service `ConstantDoubleDistribution` / `UniformDoubleDistribution` | **The only second-moment check in the battery.** Same λ, ρ and E[S]; only Var(S) differs. If the two come out equal, the service sampler is returning its mean and every mean-value test still passes. |
| 3 | **M/M/c** | one buffer, c servers each `ConnectTo(buffer)` with its own kick handler | λ=2.4, μ=1.0, c=3. Verify the kick handler starts *an* idle server, not always the same one. |
| 4 | **Jackson tandem** | source → buf1 → srv1 → buf2 → srv2 → sink | **BLOCKED ON SIM-90** — a server feeding a downstream buffer fails today, because the default product generator emits an entity with a null Identifier and `Buffer.Put` keys on it. |

## SIM-90 investigation — launched, findings NOT received

A Fable investigation into SIM-90 was launched near the end of the 2026-08-26 session and had not
returned when the session closed, so **none of its findings were captured and nothing from it is
recorded anywhere**. Re-run it rather than assuming it concluded anything.

What was already established before it was launched, so it need not be re-derived: the obvious fix
(`createProduct: m => m[0]`) throws `IndexOutOfRange`, because `Server.StartWorking` passes
`ReturnProduct` as a **deferred delegate** to `GetInstance` while `InternalFinishedHandler` clears
`activeMaterial` — and with `AutoContinue = true` the repeater event is scheduled at the same time
as the finish event and added *first*, so it can re-enter `StartWorking` before the finish instance
materialises its product. The candidate fix is to snapshot the material into the closure at
scheduling time (`var batch = new List<TMaterial>(activeMaterial)`), leaving the deferral intact but
immune to the list being cleared or reused. Two things must be checked before adopting it: whether
the delegate can be invoked **more than once** per scheduled event (if so, the default generator has
been producing two products per service, which would be far worse than the defect being chased), and
what it does to `SimOpt.Ivotion` — `RolandPrinter` emits one representative entity per batch and
`IvotionKpis` multiplies the sink count by the batch size to compensate, so a change to product
generation could silently double or halve its throughput.

## The three traps that will otherwise cost a day each

1. **`Configure` takes a RATE, `ConfigureMean` takes a MEAN.** Passing λ=0.8 to `ConfigureMean`
   silently gives a rate of 1.25 and validates a completely different system — which will then
   *fail* against the closed form and send you hunting in the engine.
2. **Size the run from σ²∞, not from the marginal variance.** At ρ=0.8 the marginal variance of Wq
   is 24 but the time-average variance constant is **1976** — a factor of 82. Sizing off the
   marginal figure is the single most common way this gate ends up flaky.
3. **Pin the seeds.** A 95% CI gate run on fresh entropy fails 1 build in 20 *by construction*.
   Fixed seeds keep the interval meaning exactly what it says while making the test deterministic.

## Assert in both directions

`|estimate − analytic| ≤ halfWidth` **and** `halfWidth ≤ κ·analytic` (κ = 0.05 fast, 0.02 nightly).
Without the second assertion **a broken engine returning garbage with huge variance sails through**,
because a wide enough interval contains anything.

## Recommended design, measured rather than guessed

M/M/1 at ρ=0.8: **R = 20 replications, warm-up 1,000 time units, 20,000 time units post-warm-up.**
Predicted half-width 0.1645; measured 0.1634 over 200 trials, mean estimate 4.0007 against a truth
of 4. Warm-up is **ρ-specific** — it scales as 1/(1−√ρ)², so ρ=0.9 needs 379 time units. Rescale,
do not copy.

## Worth adding beyond the backlog text

Every quantity in the battery is a **first moment**. An engine that matches all of them can still
have a broken service-time variance, tie-break or queue discipline. Two cheap strengtheners:

- **A distributional check.** For M/M/1 the sojourn time is *exactly* Exp(μ−λ), so a KS test of
  simulated sojourn times against Exp(0.2) at λ=0.8, μ=1.0 is a far stronger statement than
  Wq = 4.0 and costs nothing that is not already being simulated. Use **n = 10,000 with a pinned
  seed** against c₀.₀₁ = 1.62762 — not a huge sample, which would have enough power to reject on
  the Mersenne Twister's genuine but irrelevant deviations (SIM-81).
- **Anderson–Darling alongside KS** for the exponential samplers: KS is weakest in the upper tail,
  which is exactly where queueing is most sensitive. Critical values 1.933 / 2.492 / 3.070 / 3.857.

`MMath.ChiSqc(df, x)` already exists for a chi-square GOF p-value on discrete samplers.
