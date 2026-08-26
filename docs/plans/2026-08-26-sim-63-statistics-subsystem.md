# SIM-63 — Output-Statistics Subsystem: Design

Source: Fable 5 design pass, 2026-08-26, over the full engine. Read-only analysis; every seam
cited at file:line. This is the implementation brief — build test-first against §G.

---

## A. Instrumentation seams (per template, with exact hooks)

**General finding first:** the engine's house pattern is `BinaryEvent<TSender,TArgs>` members raised either synchronously (`Raise`, e.g. `Buffer.Put` at `src/SimOpt.Simulation/Templates/Buffer.cs:417`) or as calendar events (`Model.AddEvent`, e.g. `Server.StartWorking` at `Templates/Server.cs:857-858`). `ImmediateEvents` is **not** defined in any csproj, so all `#else` branches are live: those "immediate" raises are synchronous inline calls at `Model.CurrentTime`. Statistics collectors therefore subscribe with `AddHandler(handler, priority)` and read `Model.CurrentTime` (Model.cs:283-287) at handler time.

### Source (`Templates/Source.cs`)
- **(i) entity created / entering system:** `Source<TEntity,TData>.EntityCreatedEvent` (property Source.cs:398-402). Raised synchronously when `Start(0)` (Source.cs:629-631) or as a calendar event (Source.cs:639-641). This is the birth-timestamp seam. **Trap that must be designed around:** `Source.Reset()` (Source.cs:354-359) calls `Start(autoStartDelay)`, and with `autoStartDelay: 0` — which `ModelRegistry` uses (`src/SimOpt.McpServer/Simulation/ModelRegistry.cs:89`) — the first `EntityCreatedEvent` fires **inside `Model.Reset()`**, mid-way through the entity reset loop (Model.cs:1207-1209). Collectors must be correct regardless of their reset order relative to the source (solved in §C via the reset probe).
- (ii)/(iii): n/a — the source hands off inline via `ConnectTo` handlers (Source.cs:669-674).

### Buffer (`Templates/Buffer.cs`)
- **(i) entering:** `Buffer<T>.ItemReceivedEvent` (property Buffer.cs:175, raised in `Put` at Buffer.cs:417). Raised only on *accepted* puts — the full/reject path returns earlier (Buffer.cs:398-403). Correct seam for queue-length increment and arrival-timestamp capture.
- **(ii) leaving: NO SEAM EXISTS.** All three `Get` overloads (`Get()` 431-458, `Get(int)` 460-486, `Get(string)` 488-515) fire only `BufferEmptyEvent` when the count hits zero (448/476/505); there is no per-item removal notification. **Minimal engine change:** add `public BinaryEvent<IEntity,T> ItemRemovedEvent`, constructed in `Initialize` beside `itemReceivedEvent` (Buffer.cs:293-294), raised synchronously in **all three** `Get` overloads immediately after the bookkeeping removal, before the empty-event. Reset-determinism honesty: `Buffer.Reset` (Buffer.cs:101-113) does not touch event objects — exactly like `itemReceivedEvent` today — and a synchronous `Raise` draws no random numbers and adds nothing to the calendar (`EventScheduler.Add`/`orderCounter` untouched, EventScheduler.cs:211-231). It cannot perturb the SIM-58-fixed reset path; the calendar of an instrumented run is bit-identical to an uninstrumented one.
- Rejection counting (balking, later): the seam is `NotifyItemNotAccepted` (Buffer.cs:165-169), but it is a single user-owned `Action<T>` slot; if rejected-count becomes a KPI, add an `ItemRejectedEvent` instead of squatting on it. Not MVP.

### Server (`Templates/Server.cs`)
- **(i) entering:** no event (`Put` Server.cs:694-703; pull path `CheckMaterial`→`buffer.Get()` Server.cs:672-684). **Not needed for MVP:** waiting time is buffer-scoped, service time is derivable from the busy transitions below. Say so and don't add a seam.
- **(ii) leaving:** `EntityFinishedEvent` / `EntityWithDataFinishedEvent` (properties Server.cs:332-340), scheduled as calendar events at the finish time (Server.cs:857-858) — handler time *is* the departure time.
- **(iii) busy/idle: NO SEAM EXISTS.** `working` is set at `StartWorking` (Server.cs:831) and `StartFailing` (Server.cs:808) — both private, eventless — and cleared in `InternalFinishedHandler` (Server.cs:653) and `InternalFailureHandler` (Server.cs:645); `recovering` set/cleared at Server.cs:800 and 635-636. `failureEvent`/`recoverEvent` are private fields with **no public accessors** (Server.cs:215-218). **Minimal engine change:** add `public BinaryEvent<IEntity,bool> WorkingChangedEvent`, constructed in both `InitializeServer` overloads (beside Server.cs:556-568 and 612-624), raised `(this, true)` at the end of `StartWorking` and `StartFailing`, `(this, false)` at the end of `InternalFinishedHandler` and `InternalFailureHandler`. Same determinism argument as the Buffer event: synchronous, RNG-free, calendar-free — zero interaction with SIM-58. Semantic note to document: `working` is deliberately true during the pre-failure work interval (Server.cs:808); `Busy` = `working||recovering` (Server.cs:235) is a *different* measure. Utilisation ρ instruments `working` (matching what the polled viz sampled via `ns.Working`, `src/SimOpt.Visualization/Controls/SimulationCanvas.cs:304`); availability/repair analysis is a later, separate collector.

### Sink (`Templates/Sink.cs`)
- **(i) entering / leaving system:** `Sink<T>.ItemReceived` (property Sink.cs:64), raised synchronously in both `Put` overloads (Sink.cs:98, 117). Death-timestamp seam for cycle time; the existing `Count` (Sink.cs:49-52, reset at 33-37) stays the throughput count.

### Delay (`Templates/Delay.cs`)
- **(i) entering:** no event (`Put` Delay.cs:240-263 raises nothing on receive). Skip for MVP.
- **(ii) leaving:** `ItemReleased` (`TernaryEvent<IEntity,T,double>`, property Delay.cs:116-119) — its **third argument is the sampled delay itself** (instances built with `delay` at Delay.cs:254 and, post-SIM-58, in `Reset` at Delay.cs:77). So time-in-delay is a direct tally `Observe(delayValue)` at release time with no entry seam and no timestamp map needed.
- (iii) occupancy: `hasItem` is private with no seam; if ever needed, mirror the Buffer `ItemReceivedEvent`. Not MVP.

**Summary of engine changes:** exactly two — `Buffer<T>.ItemRemovedEvent` and `Server<..>.WorkingChangedEvent`. Both are additive event objects created at initialize time (not per reset), raised synchronously, with no per-reset state, no RNG draws, no calendar writes. That is the whole SIM-58 threat model, and these changes sit outside it.

## B. Timestamp provenance

**`SimpleEntity` has no attribute bag.** Its full state is position + attachment: `positionInitialized`/`initialPosition`/`currentPosition` (Entities/SimpleEntity.cs:79-84) plus `Container`/`IsAttached` (103-105); base `Entity` adds only id/name/model (Engine/Entity.cs:44-55). Do **not** add one for SIM-63.

**Design: per-station timestamps live in the collector, keyed by entity reference.** A `WaitingTimeInstrument` holds `Dictionary<T,double> arrivals` (with `ReferenceEqualityComparer` where `T` is a class): insert on `ItemReceivedEvent`, remove-and-`Observe(now − arrival)` on the new `ItemRemovedEvent`. One instrument per station ⇒ one map per station ⇒ the "several stations" problem dissolves; no entity mutation, no serialization impact, no Reset coupling. Memory is bounded by station WIP; entries leave when items leave (all three `Get` overloads fire the event); rejected puts never enter. The instrument's `Reset()` clears the map **and then re-stamps any items still sitting in the buffer at `Model.StartTime`** — `Buffer<T>` is `IEnumerable<T>` (Buffer.cs:140-148) — which also absorbs the AutoStart(0)-during-Reset ordering hazard from §A.

**Cycle time (source→sink): reference identity dies at the server.** The default product generator creates a **new** entity (`DefaultSimpleProductGenerator` → `new TProduct()`, Server.cs:130-133; `ReturnProduct` 986-989), and `ModelRegistry` passes no `createProduct` (ModelRegistry.cs:118-122). A birth map keyed on the source entity can therefore *never* match at the sink — it would deterministically record zero observations. Fix at the **builder** level, not the engine: `ModelRegistry` passes `createProduct: mats => mats[0]` (pass-through), which is semantically right for a flow twin (the part that leaves is the part that entered) and is exact under the default single-material completion check (`DefaultMaterialCompleteCheck`, Server.cs:977). Keep the door open for assembly semantics by giving instruments a `Func<T,object> keySelector` (default: identity); when entity classes/attributes arrive in schema v1.x, a real `Dictionary<string,object>` bag on `SimpleEntity` is the successor mechanism — out of SIM-63 scope.

## C. Collector API

**Location: new folder `src/SimOpt.Simulation/Statistics/` inside `SimOpt.Simulation`. No new project reference.** Rationale: collectors need `IModel`, `IEntity`, the event types — all in `SimOpt.Simulation`. `SimOpt.Statistics` is an unrelated ML grab-bag (its `Kernels/TStudent.cs` is an **SVM kernel**, not a t-distribution) with zero DES types; wiring it in buys nothing and blurs the FSL engine boundary. The one genuinely mathematical addition (Student-t inverse CDF, §E) goes to `SimOpt.Mathematics`, which `SimOpt.Simulation` already references (SimOpt.Simulation.csproj, `ProjectReference` block).

**Collectors are entities.** They implement `IEntity` (Engine/IEntity.cs:23-35 — just `Identifier`, `EntityName`, `Model`, `Reset()`), extend `Entity`, and the `Entity` ctor auto-registers with the model (Entity.cs:160). That means: automatic reset via the `Model.Reset` items loop (Model.cs:1207-1209), and headless discovery via `model.FindEntities<IStatisticCollector>()` (Model.cs:1093-1099) — exactly what the MCP head and the replication runner need. Naming convention: `"<subject-id>.stat.<kind>"` (e.g. `"buf.stat.queue_length"`).

```csharp
namespace SimOpt.Simulation.Statistics;

public interface IStatisticCollector : IEntity
{
    string StatisticName { get; }
    double WarmupTime { get; set; }   // absolute sim time; default = model start (no warm-up)
    long Count { get; }
    bool HasData { get; }             // false ⇒ report "no post-warm-up data", never 0
}

/// Observation-based statistic (waiting time, cycle time, time-in-delay).
public sealed class TallyCollector : Entity, IStatisticCollector
{
    public TallyCollector(IModel model, string statisticName, string id = "", string name = "");
    public void Observe(double value);               // gated on Model.CurrentTime >= WarmupTime
    public void Observe(double value, double time);
    public long   Count    { get; }
    public double Mean     { get; }                  // NaN when Count == 0
    public double Min      { get; }
    public double Max      { get; }
    public double Variance { get; }                  // sample (n−1); NaN when Count < 2
    public double StdDev   { get; }
    public override void Reset();                    // zeroes state, keeps WarmupTime
}
```

**Welford (mean + M2 accumulation) is mandatory, not a preference:** waits/cycle times in a long run sit on a large mean with small spread; the naive `Σx² − n·x̄²` form cancels catastrophically in doubles (observations near 1e9 with unit variance lose *all* significant digits), and this product's only asset is being right. Welford is single-pass, O(1) state, with bounded relative error. Test T4 pins it.

```csharp
/// Time-persistent statistic (queue length, utilisation, WIP): integrates a step function.
public sealed class TimeWeightedCollector : Entity, IStatisticCollector
{
    public TimeWeightedCollector(IModel model, string statisticName,
                                 Func<double> currentValueProbe,   // e.g. () => buffer.Count
                                 string id = "", string name = "");
    public void Record(double newValue);             // change effective at Model.CurrentTime
    public void Record(double newValue, double time);
    public void Increment(double delta = 1);
    public void Decrement(double delta = 1);
    public double CurrentValue { get; }
    public double TimeIntegral(double now);          // closes the open interval VIRTUALLY (read-only)
    public double TimeAverage(double now);           // integral / (now − max(WarmupTime, start)); NaN if span ≤ 0
    public double Max { get; }                       // over step values in force at/after WarmupTime
    public double Min { get; }
    public override void Reset();                    // integral=0; currentValue = currentValueProbe();
                                                     // lastChangeTime = Model.CurrentTime (== StartTime during Reset, Model.cs:1198)
}
```

Core accumulation, stated precisely because this is where implementations go wrong:
- On `Record(v, t)`: `integral += currentValue * (t − max(lastChangeTime, WarmupTime))` when positive; on the *first* update with `t ≥ WarmupTime`, seed Min/Max with the value **held across** the boundary (`currentValue` before the update) — it was in force at W. Then `currentValue = v; lastChangeTime = t`.
- Reads take `now` as a parameter and add `currentValue * max(0, now − max(lastChangeTime, WarmupTime))` **without mutating state** — no finalize event, no double-finalize bug, `Continue()` after a read stays correct. `DoRun` sets `currentTime = targetTime` before firing the finished event even when the calendar ran dry (Model.cs:736-737, 762-764), so `TimeAverage(model.CurrentTime)` uses the full horizon.
- The `currentValueProbe` in `Reset()` is the deliberate defense against the AutoStart(0)-during-Reset ordering hazard (§A): whether the collector resets before or after the source in the items iteration, it ends Reset holding the *true* current value.

**Instrument layer** (adapters, same folder) — this is what the MCP builder calls:

```csharp
public static class Instrumentation
{
    public static TimeWeightedCollector ObserveQueueLength<T>(Buffer<T> buffer, double warmup = 0);
    public static TallyCollector        ObserveWaitingTime<T>(Buffer<T> buffer, double warmup = 0) where T : class;
    public static TimeWeightedCollector ObserveUtilization<TM,TP,TD>(Server<TM,TP,TD> server, double warmup = 0) where TP : new();
    public static TallyCollector        ObserveCycleTime<TE,TD>(Source<TE,TD> birth, Sink<TE> death, double warmup = 0) where TE : class, new();
    public static TallyCollector        ObserveTimeInDelay<T>(Delay<T> delay, double warmup = 0);
}
```

`ModelRegistry.BuildModel` instruments every buffer/server/sink at build time (after wiring, before `ActiveModel` construction at ModelRegistry.cs:244); `SimulationTools.RunSimulation` (SimulationTools.cs:97-126) then adds per-node `wait_time {mean,max,n}`, `queue_length {time_avg,max}`, `utilization` to its stats payload — replacing the polled number at SimulationCanvas.cs:297-332, which becomes a *display* of the engine collector, never a computation (UN-018's "presentation only" boundary).

## D. Warm-up: fixed truncation time, lazily applied

**Recommendation: a single user-declared absolute truncation time W per experiment, identical across all collectors and all replications.** Default in `run_experiment`: 10% of run length, always reported.

Defense against alternatives, for a hostile reader:
- **Welch's moving average** requires pilot replications and a *human judgment* on a plot. It is a diagnostic, not a mechanism — it cannot run unattended inside an optimizer loop, and "an analyst eyeballed a curve" is weaker testimony than "W was declared in advance and is printed in the provenance record" (UN-034).
- **MSER-5** is automatic but makes the truncation point a *data-dependent random variable*: different replications (or different metrics in the same run) would truncate at different points, the CI math's "identically treated replicates" premise gets an asterisk, and the expert must defend an algorithm choice rather than a stated assumption. Known to misbehave on short runs.
- **Fixed W** is declared upfront, identical everywhere, trivially third-party-reproducible, and its adequacy can be *demonstrated* (doubling W leaves the answer inside the CI — a check `run_experiment` can automate later). It is what UN-010/UN-012/UN-035 actually require: a *stated* assumption.

**The mid-interval boundary (the case implementations get wrong):** for a time-weighted statistic with value v held over [t0, t1), t0 < W ≤ t1, exactly the part **v·(t1 − W)** counts. Implemented by the `max(lastChangeTime, WarmupTime)` clamp in §C — *lazily*, at the next `Record` or read. Explicitly rejected: the Arena-style "clear-statistics event scheduled at W", because (a) it adds a calendar entry, perturbing `EventCounter` and the `orderCounter` sequence of an instrumented run (a needless delta against SIM-58-era determinism baselines), and (b) naive clearing zeroes `currentValue` and loses the held value from Min/Max — the exact silent-wrong-number bug this section exists to prevent. The lazy clamp has no scheduled event and no clear step; the boundary split is arithmetic, not state.

**Tally convention:** an observation counts iff its *completion* time ≥ W; a wait spanning W is fully counted (Law & Kelton's standard convention). Declared in the provenance output. **Run ends before W:** `HasData == false`, mean is NaN, and the runner flags the metric invalid — never a silent zero.

## E. Replication runner

Location `src/SimOpt.Simulation/Statistics/ReplicationRunner.cs`.

```csharp
public sealed class ReplicationRunner
{
    public ReplicationRunner(Model model, double runLength, double warmupTime = 0);
    public ReplicationResult Run(int replications, int baseSeed, double confidenceLevel = 0.95);
}
public sealed record MetricSummary(string StatisticId, string StatisticName, int N,
    double Mean, double StdDev, double? CiHalfWidth, double[] ReplicateMeans, bool HasData);
public sealed record ReplicationResult(int Replications, int BaseSeed, double RunLength,
    double WarmupTime, double ConfidenceLevel, IReadOnlyList<int> ReplicationSeeds,
    IReadOnlyList<MetricSummary> Metrics);
```

**Loop:** for r = 0..N−1: `model.Reset(seed_r)` → `model.Run(runLength)` → read every `IStatisticCollector` via `FindEntities` **before** the next Reset (Reset wipes them, e.g. Sink.cs:33-37) → aggregate by `Identifier`, asserting the collector set is identical each replication.

**Seeds — reproducible and independent (SIM-62 mechanism):** `seed_r = baseSeed ^ StableHash.Of("replication:" + r)` using the FNV-1a `StableHash` (currently `src/SimOpt.McpServer/Simulation/StableHash.cs:23-45`). **`StableHash` must move down** — `SimOpt.Simulation` cannot reference the MCP head — to `SimOpt.Basics` (e.g. `src/SimOpt.Basics/Tools/StableHash.cs`); the pinned-value tests in `tests/SimOpt.Tests/McpServer/StableHashTests.cs` move with it and their constants must not change. Reseeding propagates because `Model.Reset(seed)` sets `SeedChange` and resets the seed generator (Model.cs:1189-1200), and every `StochasticEntity.Reset` then re-derives its own seed (Entities/StochasticEntity.cs:37-61): via `GetRandomSeedFor(seedID)` when a SeedID is set, else `Model.SeedGenerator.Next()` in items-insertion order — deterministic because `ModelRegistry` builds nodes in `topology.Nodes` order (ModelRegistry.cs:65). Caveat to document: distinct MT seeds are standard practice, not a proof of stream independence; a counter-based RNG is the v1.x upgrade.

**Reset-path trust:** this is exactly the loop SIM-58 fixed and pinned — `tests/SimOpt.Tests/Simulation/Engine/ResetPathTests.cs` (`Delay_AfterReset_StillReleasesItsInitialItem`, `Delay_ConsecutiveEvaluations_Agree`, lines 42-108), plus `EventScheduler.Reset` internals exposed via `InternalsVisibleTo` (SimOpt.Simulation.csproj SIM-58 comment). MCP's `RunSimulation` already does one iteration of it (SimulationTools.cs:89-94); the runner is that loop, generalized.

**⚠ Pre-existing landmine found during this read, verify before building on the MCP path:** `ModelRegistry.CreateNegExp` **pre-initializes** its distributions (`dist.Initialize(seed)`, ModelRegistry.cs:250-256 → `MersenneTwister(seed,antithetic)` ctor calls `Initialize`, MersenneTwister.cs:84-87/115 → `Initialized == true`), but the engine's `Random<T>` ctor **throws on an initialized distribution** (`Engine/Random.cs:75`) — and `Source.InitializeSource`/`Server.InitializeServer` construct exactly that wrapper around the passed distribution (Source.cs:577, Server.cs:538). By code reading, `create_model` should throw for every source/server node; there are zero MCP integration tests to contradict this (SIM-20 open, only `StableHashTests` exists). First implementation step: one smoke test. Likely fix, which is also the *better* seeding design: drop the pre-initialized distribution and instead construct `SimpleSource`/`SimpleServer` with `seedID: StableHash.Of(node.Id)` — the engine's own per-node mechanism (StochasticEntity.cs:301-316), which is insertion-order-independent on reseed (`GetRandomSeedFor`, Model.cs:1109-1112) and re-derives correctly on every `Reset(seed_r)`.

**Confidence interval:** replicate means X̄₁..X̄_N; report `mean ± t_{1−α/2, N−1} · s/√N` with s computed **across replicate means** (n−1), never within-run variance (autocorrelated observations make within-run CIs falsely narrow — the classic silent error). **t-quantile source: none exists in this codebase** — `src/SimOpt.Statistics/Kernels/TStudent.cs` is an SVM kernel; the Mathematics distributions are samplers with no inverse CDFs. Write `StudentT.InverseCdf(double p, int df)` in `SimOpt.Mathematics` (new `SpecialFunctions/StudentT.cs`), via the inverse regularized incomplete beta (continued-fraction `betacf` + Newton), pinned against published tables (T20). **N=1:** mean reported, `CiHalfWidth = null`, flagged `insufficient_replications` (UN-012 makes N≥2 the default anyway; default N=10). **N=2:** df=1, t₀.₉₇₅ = 12.706 — return the honest, enormous half-width with a `low_replication_count` flag; never substitute anything narrower.

## F. Risks — ways this produces a WRONG number silently, each with a deterministic killing test

1. **Final open interval dropped/double-counted** (time-weighted). Test: value 1 from t=0, no changes, T=10 → `TimeAverage(10) == 1.0` exactly; reading twice returns identical values (reads must not mutate).
2. **Warm-up boundary mid-interval:** v=5 held from 0, W=4, no change until T=10 → integral **exactly 30**, average 5, and Max==5 (held value seeded at the crossing). This kills both the "clear zeroes currentValue" bug and the lost-held-Min/Max bug.
3. **Run ends before W → silent zero.** W=10, T=5 → NaN + `HasData=false`; runner marks metric invalid. Assert it is *not* 0.
4. **Reset-order loss of the t=0 arrival** (Source AutoStart(0) raising inside `Model.Reset`, Source.cs:354-359 + 629-631): build the identical deterministic model twice, collector registered before vs after the source; both runs must yield the identical queue-length integral, and immediately after Reset `collector.CurrentValue == buffer.Count`.
5. **Identity death at the server** (new product ≠ material, Server.cs:130-133): cycle-time instrument without the pass-through generator records 0 observations. Tripwire assertion in every cycle-time test: `collector.Count == sink.Count` — count mismatch is loud where a biased mean is silent.
6. **Utilisation semantics drift** (`working` true during pre-failure interval Server.cs:808; `Busy` includes recovering Server.cs:235): deterministic failure model (constant MTTF/MTTR/service) with hand-computed working fraction; assert exact.
7. **Increment/Decrement drift from truth** (e.g. one `Get` overload left uninstrumented): post-run invariant `collector.CurrentValue == buffer.Count` after a run exercising `Get()`, `Get(int)`, `Get(string)`.
8. **Welford replaced by naive Σx²:** observations {1e9+1, 1e9+2, 1e9+3} → variance 1.0 ± 1e-6 (naive form fails outright in double).
9. **CI from within-run variance** (too narrow, confidently wrong): unit-test the CI math on injected replicate means {10, 12} → 11 ± 12.7062·1 exactly.
10. **Wrong t tail** (t₀.₉₅ vs t₀.₉₅₊half): table tests, T20.
11. **Per-process seed instability regression** (the SIM-62 class): pinned `seed_r` constants + same-baseSeed-twice ⇒ bitwise-identical `ReplicateMeans`.
12. **Statistics observed during Reset counted as run data** (immediate raises at reset time): after Reset but before Run, all integrals are 0 and tallies empty while `CurrentValue` is truthful — asserted in test 4.
13. **The §E `Random<T>`-throws landmine:** the MCP smoke test (create → run → non-error JSON) is the deterministic detector; must exist before any MCP-facing statistics work.

## G. Test plan — ordered, failing-first

Phase 1 — `TallyCollector` (pure, no engine): **T1** empty state: Count 0, Mean/Min/Max NaN, Variance NaN. **T2** single obs 3.0 → Mean 3, Min=Max=3, Variance NaN. **T3** {2,4,4,4,5,5,7,9} → Mean **5.0**, sample Variance **32/7** (≈4.5714285714), Min 2, Max 9. **T4** Welford: {1e9+1, 1e9+2, 1e9+3} → Variance **1.0** ± 1e-6. **T5** warm-up gate W=10: obs at t=9.999 ignored, at t=10.0 counted (boundary is ≥).

Phase 2 — `TimeWeightedCollector` (pure): **T6** constant 1 over [0,10] → `TimeAverage(10) == 1.0` exactly. **T7** hand-computed steps: 0@0, 2@1, 5@3, 0@7, read at 10 → integral **0·1+2·2+5·4+0·3 = 24**, average **2.4**, Max 5, Min 0. **T8** open interval: last change 3@7, `TimeIntegral(10)` includes 3·3 = 9; second read identical. **T9** warm-up mid-interval: risk-2 numbers (integral 30, avg 5, Max 5). **T10** end-before-warm-up: NaN + `HasData=false`. **T11** Reset probe: probe→3 ⇒ after Reset `CurrentValue==3`, integral 0.

Phase 3 — engine seams (all with `ConstantDoubleDistribution`, exact assertions): **T12** `ItemRemovedEvent` fires from all three `Get` overloads with the correct items (3 puts, 3 distinct gets, 3 notifications). **T13** `WorkingChangedEvent`: const service 2, one entity → exactly (true, t_s), (false, t_s+2). **T14** D/D/1 overload: interarrival 1, service 2, T=10 → hand-computed queue-length integral asserted exactly (arithmetic, computable on paper: arrivals at 0..9, departures at 2,4,6,8,10). **T15** same model, waiting times are exactly 0,1,2,… → mean over completed services asserted as an exact rational. **T16** anti-polling test: service 0.5, arrivals every 2 → utilisation exactly **0.25** — the number the polled UI (SimulationCanvas.cs:297-332) structurally cannot guarantee. **T17** cycle time with pass-through generator: single entity, cycle == wait+service exactly, and `Count == sink.Count`. **T18** statistics reproducibility across Reset (SIM-58 extension): run → record all metrics → `Reset()` → run → identical values. **T19** reset-order independence (risk 4).

Phase 4 — replication/CI: **T20** `StudentT.InverseCdf(0.975, df)` vs tables: df 1→12.7062, 2→4.3027, 5→2.5706, 10→2.2281, 30→2.0423 (4 decimals); large-df → 1.9600 ± 5e-4. **T21** CI math on {10,12} → 11 ± 12.7062. **T22** N=1 → `CiHalfWidth == null` + flag. **T23** same baseSeed twice → bitwise-identical `ReplicateMeans`. **T24** pinned `seed_r` constants (StableHash, cross-process). **T25** distinct seeds produce non-identical replicate means on a stochastic model.

Phase 5 — the Slice-1 analytic gate (sampled, explicitly separated from the deterministic suite, run as the M/M/1 CI test): λ=0.5, μ=1, ρ=0.5 → L=1, Lq=0.5, W=2, Wq=1; N=10, long T, warm-up applied; assert each closed form lies inside the reported CI. This is the marketing artifact, not the correctness net — the deterministic tests above are the net.

**One-sentence verdict:** two small additive events (Buffer item-removed, Server working-changed) plus a self-contained `SimOpt.Simulation/Statistics/` folder give exact event-driven statistics with zero contact with the SIM-58 reset machinery; the real traps are the warm-up boundary clamp, the server's identity-destroying product generator, the AutoStart(0)-during-Reset ordering, and a likely-latent `Random<T>`-rejects-initialized-distribution throw in the MCP builder that must be smoke-tested before anything is built on that path.
