<!-- Action: reference -->
<!-- Tracked-by: SIM-56, SIM-57, SIM-58, SIM-59, SIM-60 -->
# Critical-subsystem code review — 2026-07-05

Three parallel Opus 4.8 review agents (Fable substitute; Fable geo-block was stale but review
already ran on Opus — see decisions.md), each scoped to one correctness-critical core:
DES engine (`SimOpt.Simulation`), optimization + coupling (`SimOpt.Optimization`), and
math/stats (`SimOpt.Mathematics` + `SimOpt.Statistics`). Read-only, static analysis (no
build/test to avoid concurrent obj/bin collisions). ~40 findings.

**Framing:** these are review findings, i.e. hypotheses to confirm. The fix path is TDD — the
failing test confirms each bug before any code changes. Line numbers are as of commit at review
time; re-locate before editing.

**Root-cause chain (highest leverage):** the default RNG defect (MT-1) is the upstream cause of
the exponential-family infinities (MATH-5) and the `Extensions`/`ProbabilisticDistribution`
crashes. Fix MT first.

---

## 🔴 CRITICAL — silently corrupts every run

### MT-1 · `MersenneTwister.NextInteger()` — broken two ways (default RNG)
`src/SimOpt.Mathematics/Stochastics/RandomSources/MersenneTwister.cs:165,175`
- `Math.Abs((int)NextUInt())` throws `OverflowException` when the draw is `0x80000000` (~2⁻³² per
  draw → effectively certain over a long optimization run → hard crash).
- `NextDouble()` ranges over the **closed** `[0.0, 1.0]`, not `[0,1)` as documented (`NextUInt()==0`→0.0; abs can equal `int.MaxValue`→1.0).
- Also: the "MT" omits the tempering step and pre-seeds state from `System.Random`, so the first 624 outputs are just `System.Random.Next()` — quality far below a real MT (Medium on its own).
- **Fix:** derive the double from the raw uint (`NextUInt()*(1.0/4294967296.0)` for `[0,1)`); stop using `Math.Abs((int)…)`.

### TRI-1 · Triangular distribution sampler is mathematically wrong
`src/SimOpt.Mathematics/Stochastics/Distributions/TriangularDistribution.cs:278,290`
`top = (max-min)/(mode-min)` is the reciprocal of the correct CDF-at-mode `(mode-min)/(max-min)`,
and the final rescale uses `mode-min` instead of `max-min`. `top>1` always → descending branch
never taken; `Tri(0,0.5,1)` confined to ≈[0,0.707]. Correct formula is in the `<remarks>` "old
version" directly above the method. **Every triangular-distributed duration/quantity is invalid.**
- **Fix:** restore `top=(mode-min)/(max-min)`, final scale `min + result*(max-min)`.

---

## 🟠 HIGH

### DES engine
- **DES-1 · `Delay<T>.Reset()` NRE on null initial item** — `Simulation/Templates/Delay.cs:60`.
  `!initialItem.Equals(default(T))` dereferences null; `Reset()` runs on every `Evaluate()`.
  Fix: mirror ctor `:174` → `!object.Equals(initialItem, default(T))`.
- **DES-2 · `Delay<T>.Reset()` never re-schedules the initial item's release** — `Delay.cs:57-62`.
  First run OK (`InitializeDelay` schedules it), but after any reset the item is stuck forever and
  blocks all `Put` → eval #1 vs eval #2 diverge silently. Fix: re-draw delay + re-schedule
  `itemReleased` in `Reset()` as `InitializeDelay` does.

### Optimization (strategy↔solution seam = debt concentration)
- **OPT-1 · Wall-clock RNG in solution operators → reproducibility broken** —
  `IvotionSolution.cs:75,96` (`Tweak()`/`CombineWith()` no-arg → `new Random()` per call);
  `SimpleMutation.cs:95` / `SimpleCrossover.cs:105` call exactly those no-arg overloads, ignoring
  `config.Seed`. Same seed → different run every time. Seeded overloads exist but are never called.
  Fix: thread the operator's seeded `Random` through the tweak/combine interface.
- **OPT-2 · EA `BestSolution` stored by reference, not cloned** — `EvolutionaryAlgorithm.cs:325`.
  A live generation member; can be mutated in place later (SA `:171-172` and PSO `:169` clone; EA
  doesn't). Fix: `BestSolution = (ISolution)individual.Clone();` (+ CurrentGenerationBest).
- **OPT-3 · PSO reflection contract incompatible with every real solution** —
  `ParticleSwarm/ParticleSwarmOptimization.cs:208-235` needs a settable `double[] Parameters`; only
  `tests/.../TestSolution.cs` satisfies it. `IvotionSolution` (`int[]`, get-only)→`InvalidCastException`;
  demo/supply-chain have no such property → particles never move (**zero search, silent**).
  **PSO IS a real impl** (old `SwarmingAlgorithm.cs` is the `[Obsolete]` stub) but non-functional on
  real problems — reconcile with SIM-36's "stub" note. Fix: explicit `GetVector()/SetVector(double[])`
  + bounds + int-var rounding/repair.
- **OPT-4 · EA in-place mutation corrupts parents/elite when crossover returns aliases** —
  `SimpleMutation.Apply` mutates in place; SimOptDemo `Solution.CombineWith` returns `this`/`other`,
  so EA tweaks a parent that is also kept as elite (elitism defeated). IvotionSolution is immune
  (returns fresh child). Fix: clone before mutating in `EvolutionaryAlgorithm.Mutate`, or forbid
  `CombineWith` from returning operands.
- **OPT-5 · SA `while (T >= 0)` infinite loop under geometric cooling** — `AnnealingAlgorithm.cs:138`.
  OK with shipped linear `T-1`; geometric `T*=α` never reaches ≤0. `T==0` also hits `Math.Exp(dist/0)`
  (`:165`). SA ignores `NumberOfIterations`/`Evaluations` entirely. Fix: `while (T > 0)` + guard T==0.

### Math
- **MATH-3 · Gamma uses integer `1/3 == 0`** — `GammaDistribution.cs:306` `k - 1/3` → `d=k`. Biases
  every gamma draw. Fix: `k - 1.0/3.0`.
- **MATH-4 · `MMath.Median` off-by-one both branches + crashes N≤2** — `MMath.cs:2780-2782`. Even N
  should use `data[N/2-1],data[N/2]`; odd N should use `data[N/2]` (`data[(N-1)/2]`). N=1→`data[1]`,
  N=2→`data[2]` throw. Fix as noted.
- **MATH-5 · Exp-family `+∞` on `U=0`** — `NegExponentialDistribution.cs:211`, `ErlangDistribution.cs:223`,
  Gamma via normals. `-(1/λ)ln(0)=+∞` (Gaussian/Weibull die at U=1 instead). Reachable via MT-1.
  Fix at source (MT-1) and/or resample `U∈(0,1)`.
- **MATH-6 · `Complex.Divide(double, Complex)` isn't complex division** — `Numerics/Complex.cs:688-695`
  (+ ref overload `:755-764`). Returns `s/Re, s/Im`; should be `s*conj(a)/|a|²`. Also throws
  `DivideByZeroException` when either part is 0.
- **MATH-7 · `R250_521` buffer overrun crash after 521 draws** — `R250_521.cs:186` `!= 521` should be
  `!= 520` (buffer indices 0..520). Also shares MT's `Math.Abs(int.MinValue)` overflow.
- **MATH-8 · `LinearCongruentialGenerator.NextInteger()` always returns 0** — `:150` passes a bit-count
  where a shift is expected; `UniformIntegerDistribution` on an LCG always returns `min`. `NextDouble()`
  unaffected. Fix: `Next(31)`.

---

## 🟡 MEDIUM
- **DES-3 · `EventScheduler.timeOfNextScheduledEvent` stale / sentinel inconsistency** —
  `Engine/EventScheduler.cs`: `Remove()`→empty leaves stale finite key (`:257-259`); `Reset()` never
  resets it (`:345-356`); `MaxValue` vs `PositiveInfinity` sentinel mismatch (`:87,325,156`) →
  `KeyNotFoundException`/stale time for external consumers (viz/logging). Main loop is safe. Fix:
  set `PositiveInfinity` on empty, reset in `Reset()`, test `Count==0`.
- **DES-4 · `ResourceManager.Reset()` aliases the snapshot list** — `Tools/ResourceManager.cs:141`
  `managedResources = initialResources;` (should be `new List<>(...)`) → later `Manage`/`UnManage`
  mutates the snapshot; resets stop restoring the original pool (capacity-planning models diverge).
- **DES-5 · `Server<>.ClearCurrentMaterial()` clears the wrong list** — `Templates/Server.cs:886-889`
  clears `activeMaterial` (copy-paste) instead of `currentMaterial` → WIP mis-accounting.
- **DES-6 · `Model.RemoveEvent` can corrupt the in-progress iteration** — `Engine/Model.cs:1007-1010`
  lacks the `evnt.Time <= currentTime` guard that `TryRemoveEvent` has (`:997`); a same-time handler
  calling `Stop(true)` mutates the enumerated `SortedDictionary` → `InvalidOperationException`.
- **DES-7 · `Source<>` with `autoStartDelay==0` raises entity creation during reset** —
  `Templates/Source.cs:357,619-632` — zero-delay `Start` takes the synchronous `Raise()` branch during
  `Model.Reset()`, before `SimulationStartedEvent` and before downstream is guaranteed reset.
- **DES-8 · `MovableEntity.Stop()` uses full-decel distance regardless of current speed** —
  `Entities/MovableEntity.cs:398-400` (author TODO) — wrong stop pos/arrival for short moves → biases
  conveyor/AGV throughput.
- **OPT-6 · First-eval state contamination (SimOptDemo)** — `examples/.../Simulation.cs:137`
  `if (!Model.IsReset) Model.Reset();` — after ctor `IsReset==true`, so first `Evaluate` skips reset and
  double-fills the buffer (~200 vs cap 100). Fix: always `Reset()` at top of `Evaluate`.
- **OPT-7 · SimOptDemo `Solution.Clone` shares mutable entity refs** — `Solution.cs:73-79` copies same
  `SimpleEntity` refs; sim mutates/removes them → re-eval non-idempotent.
- **MATH-9 · `Uniform*.Mean` returns `(max-min)/2` not `(min+max)/2`** — `UniformDoubleDistribution.cs:81`,
  `UniformIntegerDistribution.cs:94,105`. Wrong when `min≠0`. Int `Next()` also yields `[min,max-1]` with
  biased modulo — inconsistent with the symmetric Mean.
- **MATH-10 · `ConfusionMatrix.MatthewsCorrelationCoefficient` int overflow** —
  `Statistics/Analysis/ConfusionMatrix.cs:210-215` — `int` product overflows for counts ≥~1000 →
  `Sqrt(negative)=NaN`. Use `double`/`long`. (Sensitivity/Specificity/FPR lack divide-by-zero guards.)
- **MATH-13 · `Extensions` roulette-wheel off-by-one + wrong RNG method** —
  `Stochastics/Extensions.cs:105,214,282` loop `i<=Count` → `IndexOutOfRangeException` when probs don't
  sum to 1; `RandomItem` overloads index `[Count]` when `NextDouble()==1.0` (reachable via MT-1); the
  `System.Random` `ChooseIndex`/`ReturnConditionally` overloads (`:45,101`) call `rnd.Next()` (large int)
  vs probabilities → always fall through to `-1`/`default`.
- **MATH-14 · Antithetic streams can emit `U<0`** — MT/LCG/R250/Subtractive use summand `int.MaxValue-1`
  with factor −1; abs can reach `int.MaxValue` → antithetic `NextDouble()` slightly negative → `Log(neg)=NaN`.

---

## ⚪ LOW
- **MATH-11 · `Complex.Sqrt` of negative real returns real not imaginary** — `Complex.cs:809-812` (`sqrt(-4)→2`).
- **MATH-12 · `Complex.Arg`/`Phase` wrong-quadrant** — `Complex.cs:193-196,908-929` (use `Atan` not `Atan2`;
  `Log` internally is correct, public `Arg`/`Phase` are not).
- **MATH-15 · `LogNormalDistribution(mean,stddev)` ctor NREs** — `:140→234` `dblGaussian` null; usable only
  via `Initialize(...)`. Shifted distributions (LogNormal/NegExp/Weibull/Erlang/Gamma) report unshifted mean.
- **MATH-16 · PCA reports singular-value² as eigenvalues (missing `1/(n-1)`)** — `PrincipalComponentAnalysis.cs:212`;
  proportions unaffected (ratio cancels). `Revert` dimensionally muddled when `components<cols`.
- **MATH-17 · ROC sweep excludes `x=1` endpoint** — `ReceiverOperatingCharacteristic.cs:131` (`while x<1`) →
  slightly truncated trapezoidal AUC; `if sum<0.5 area=1-sum` can mask a genuinely <0.5 classifier.
- **MATH-18 · `MMath.Factorial` overflows past 12!, `Falling(value,power)` ignores `value`** — `MMath.cs:69,252`.
  `SubtractiveCongruentialGenerator.Reset` leaves `seed` field stale (`:191`).
- **DES-9 · `Buffer<T>.Reset()` leaves wiring handlers attached** — `Templates/Buffer.cs:112` (`sources`
  record vs behavior mismatch; re-wiring after reset would double-`Put`).
- **DES-10 · Unsynchronized static counters** — `Entity.cs:45`, `Model.cs:54`, `Node.cs:25` race under
  parallel model construction → duplicate default IDs/names (seeds unaffected). Also `AsyncModelRunner.cs:383`
  worker/control data race (animation path only, benign).
- **DES-11 · misc** — `Buffer.RandomSelector` relies on Dictionary enum order; `EventScheduler.Add` orphans a
  key if the same live instance is re-added; integer counter overflow on astronomical runs.
- **OPT-8 · Example problems ignore `GenerateCandidates(seed)`** — SimOptDemo/SupplyChain (IvotionProblem is
  correct, `:79-89`). **OPT-9 · SupplyChain legacy mock** reproduces wall-clock RNG + stale-fitness bugs
  (quarantine). **OPT-10 · Strategies consume their iteration budget** and need re-`Initialize` to re-run.
  **OPT-11 · `DefaultEliteSelector` never captures new elites** (`EvolutionaryAlgorithmConfiguration.cs:462-465`).
  **OPT-12 · `Mathematics/Stochastics/Extensions.ChooseIndex` uses `rnd.Next()` vs probabilities ≤1** (=MATH-13).

---

## ✅ Healthy (do NOT spend effort here)
- **Scheduler kernel:** event ordering fully deterministic (SortedDictionary on Priority→Type→Number→AddedOrder;
  FIFO tie-break); no Dictionary/HashSet iteration-order reliance in ordering path; old Remove/duplicate-priority
  bugs (SIM-01/05) genuinely fixed; clock cannot go backward.
- **Engine reset (`Model.Reset`):** comprehensive & deterministic — clock, event scheduler, seed generator, all
  IRandom streams re-seeded to the same seed (common random numbers), entities + resettables. The reset *defects*
  are template-local (Delay, ResourceManager), not in the machinery.
- **Min/max sign consistency:** clean maximization across Random/EA/SA/PSO and both live coupling seams
  (SimOptDemo returns `-(elapsed)`; IvotionProblem negates every Minimize objective).
- **RandomStrategy same-seed bug (SIM-02): FIXED** — seeds a `seedGenerator` from `config.Seed`, fresh `Next()` per
  iteration (reproducible + varied).
- **Linear algebra:** Cholesky (incl. previously-buggy forward-sub SIM-24), LU, QR, Eigenvalue guards — correct.
- **Variance/stderr:** correct unbiased two-pass form (no `E[X²]−E[X]²` cancellation); `StandardError=s/√n` correct.
- **Gaussian:** Box-Muller correct, discards sine variate (wasteful, not a bug) — **no spare-variate caching bug.**
- **Exponential parameterization:** `mean=1/λ` consistent. Poisson/Weibull/Erlang/Bernoulli/Constant algorithmically correct.
