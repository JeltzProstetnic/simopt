# SIM-122: SimPy against SimOpt, and where "much wider intervals" come from

2026-11-24, WSL. Harness, scripts and raw results: `docs/plans/2026-11-24-sim-122/`.

**Verdict.** SimPy is not better at optimisation or precision: it has neither an optimiser nor any
statistics, and on the same model with the same number of replications it gives the same interval
width as current SimOpt. The much wider intervals come from two SimOpt defects that were fixed on
2026-11-03 but are still in commit `b171eb8`, the commit downstream builds pin. On a breakdown-heavy
queue that commit's 95 % interval is 9.5 times wider than current SimOpt's.

## The question

MG relayed on 2026-11-19 that a downstream integrator who runs SimOpt next to a SimPy-based simulator
sees "way higher" intervals on the SimOpt side. He suspected SimPy might simply be better at
optimisation. The integrator's own comparison results have not arrived yet. This brief tests the
claim on a model with a known answer, before they arrive.

## 1. The measurement

**Case.** M/G/1 with breakdowns, the textbook Pollaczek-Khinchine case an integrator would use as a
yardstick. Arrivals are exponential with a mean gap of 60 min, service is exponential with a mean of
45 min, and the time to failure is exponential with a mean of 480 *operating* minutes. Repairs are
exponential with a mean of 30 min, and a failed job resumes after its repair (preempt-resume).
Utilisation ≈ 0.797. Closed-form mean time in system: **242.31 min**.
Each replication runs 360 days (about 8,600 arrivals) and measures the mean time in system of the
items that arrive after a 30-day warm-up. Seeds come from `ReplicationRunner.SeedFor(42, r)` and
entity streams are pinned with `SeedID`.

| Engine, 100 replications | Mean | SD across replications | 95 % half-width | Bias vs closed form | Items lost |
|---|---|---|---|---|---|
| SimOpt `96f979c` (current main) | 243.70 | 23.57 | ±4.68 (1.9 %) | +0.6 % | 0 |
| SimPy 4.1.2, same model | 240.68 | 23.86 | ±4.73 (2.0 %) | −0.7 % | 0 |
| SimOpt `b171eb8` (pinned downstream) | 266.97 | 225.26 | ±44.70 (16.7 %) | +10.2 % | 8.6 % |

With 20 replications the pattern is the same: ±4.1 % on current SimOpt, ±3.8 % on SimPy and
±36.1 % on `b171eb8`. At `b171eb8`, matching current SimOpt's interval takes about
(44.70 / 4.68)² ≈ **91 times as many replications**. The worst `b171eb8` replication averaged
2,155 min in the system, nine times the closed form.

**Mechanism (both are already fixed).**

- **SIM-115, a single repair draw per replication** (fixed in `fff06dd`). At `b171eb8` the repair
  time is drawn once, at `Reset`, and every repair in the replication reuses that draw. Downtime then
  scales with one exponential draw R, so the utilisation in that replication is
  0.75 × (1 + R/480). R = 30 gives 0.80, R = 90 gives 0.89, and R = 240 gives 1.13, which is unstable.
  One draw dominates the spread between replications. Time in system is convex in R, so the mean is
  also biased upwards (Jensen), here by 10 %.
- **SIM-106, a resumed batch hands on a null product** (fixed in `bb835a1` and `cb53d09`). At
  `b171eb8`, 8.6 % of the items were handed on as `null` after a repair, and only the survivors were
  measured. A host that substitutes the batch it remembers can mask this, but nothing in a host can
  mask SIM-115.

So the fix downstream is to move the pin to `fff06dd` or later (main is `96f979c`; CI green) and rerun
the frozen references. Their down minutes and spread will change, as the SIM-115 commit message says.

**Reproduce.**

```bash
export DOTNET_ROOT=$HOME/.dotnet PATH=$HOME/.dotnet:$PATH
dotnet build docs/plans/2026-11-24-sim-122/CiExp.csproj -c Release
dotnet docs/plans/2026-11-24-sim-122/bin/Release/net9.0/CiExp.dll 100 42      # current checkout
git worktree add --detach /tmp/simopt-b171eb8 b171eb8
dotnet build docs/plans/2026-11-24-sim-122/CiExp.csproj -c Release -p:SimOptRoot=/tmp/simopt-b171eb8/ -o /tmp/ciexp-pin
dotnet /tmp/ciexp-pin/CiExp.dll 100 42
python3 -m venv /tmp/v && /tmp/v/bin/pip install simpy && /tmp/v/bin/python docs/plans/2026-11-24-sim-122/simpy_mg1.py 100
```

The tracked harness reproduces `results-head-r100.csv` byte for byte, which was checked on 2026-11-24.

## 2. SimPy and SimOpt side by side

SimPy 4.1.2 (MIT) is a process-interaction kernel. A process is a Python generator that yields
events (`Timeout`, `Process`, `AllOf`/`AnyOf`) and can be interrupted, which is how preemption is
modelled. It ships shared resources (`Resource`, `PriorityResource`, `PreemptiveResource`), level
stock (`Container`), object queues (`Store`, `FilterStore`, `PriorityStore`) and a real-time
environment. That is the whole library.

| | SimPy 4.1.2 | SimOpt |
|---|---|---|
| Modelling style | Processes as generators; arbitrary logic reads top to bottom | Event scheduling with templates (Source, Buffer, Server, Sink, Delay) and custom `StochasticEntity` subclasses |
| Random numbers | Python's `random`; streams and common random numbers are the modeller's job | `SeedID` per entity, combined with the model seed (dissertation ch. 4.3.3) |
| Replications, warm-up, intervals | None | `ReplicationRunner`, collectors, t-intervals (SIM-63), analytic battery (SIM-64) |
| Optimisation | None (pair with Optuna, DEAP, Nevergrad, SciPy) | EA, SA, PSO and random search behind `IProblem` / `ISolution` |
| Failures, batching | Hand-written per model | Built into `Server` |
| Stock and levels | `Container`, `Store` with blocking get/put | None; integrators build stock blocks themselves |
| Zero-delay events | Normal (`timeout(0)`) | `Model.AddEvent` refuses to schedule into the present (SIM-117) |
| Visualisation, agent access | None | Avalonia viewer, MCP server |
| Footprint on this case | 2.73 s, 25 MB for 100 replications | 6.87 s, 372 MB for 100 replications; 5.34 s, 66 MB after the SIM-124 fix |

The footprint row is not a like-for-like comparison. The SimPy model is a few dozen lines of bespoke logic,
while SimOpt runs generic templates with state machines and event handlers. Still, SimOpt is not
faster here, and most of its memory is the retention found below (SIM-124).

**Where SimPy is genuinely better.** Arbitrary operational logic (planners, buyers, trucks, shelves) is
a few lines per behaviour in SimPy. In SimOpt each one needs a custom entity with its own events, and
integrators end up writing such blocks themselves (SIM-118). SimPy also has stock primitives and
zero-delay events, and it lives in the Python ecosystem.

**Where it is not.** Precision, statistics and optimisation do not come from SimPy at all. A SimPy user
rebuilds entity-keyed random streams, replication statistics and an optimiser, or goes without them.

## 3. Recommendation: adopt three ideas, interoperate through the contract, do not port

- **Do not port to SimPy.** Porting would discard the seed management, statistics, optimisers,
  viewer and MCP head, which is what SimOpt has and SimPy lacks.
- **Interoperate through the contract, not through a SimPy bridge.** Python can already reach SimOpt
  in-process through pythonnet and out of process through the MCP server. The published topology schema (SIM-65) is the clean contract.
- **Adopt three ideas:**
  1. **A stock primitive** like SimPy's `Container` and `Store`: a level with blocking take and put,
     and waiting lines released on arrival. Proposed as item (4) of SIM-118.
  2. **Zero-delay scheduling**: an event at the current time is normal in a process kernel, and
     integrators work around the refusal. This joins SIM-117, which already lists zero draws throwing.
  3. **`SeedID` by default** from a stable hash of the entity id, so common random numbers survive a
     scenario that adds or removes a block. Without a `SeedID`, `StochasticEntity.Reset` takes
     `Model.SeedGenerator.Next()` in creation order, so adding one block shifts the streams of every
     block created after it, and paired scenario comparisons get noisier. Proposed as SIM-125.
- **Defer a process-style authoring layer** (C# iterators or `async` processes over the event engine).
  It is the biggest gain in modelling comfort, but it is large and should wait for SIM-65. Decision
  for MG (handover `Q-PROC`).
- **Optional:** use Ciw (MIT, a queueing-network simulator) as an independent cross-check in the
  analytic battery, for networks without a closed form. P3.

## 4. New engine finding: every entity stays registered for good (SIM-124)

**Fixed the same day, test-first** (see SIM-124 in the backlog). Items the simulation makes are now
unregistered when a sink absorbs them and at the next `Reset`. A second leak turned up on the way:
the static logger's queue never drained in a headless run. After both fixes the live heap stays flat
at 0.15 MB over 30 replications, peak RSS for 100 replications fell from 372 MB to 66 MB, and the
simulation results are byte-identical. The measurement below is the state before the fix.

`new SimpleEntity(model, ...)` registers the entity in `Model.Items` through `Model.AddEntity`, and its
summary says *"to permanently associate with this model"*. Neither `Sink` nor `Model.Reset` removes
transient entities. Measured with `GC.GetTotalMemory(true)` after each replication of this case: the
live heap was 1.9 MB after the first, 17.6 MB after 10, 35.5 MB after 20 and 49.2 MB after 30
replications, about +1.7 MB per replication of about 8,600 items. An optimisation loop that resets and
reruns the model for every evaluation therefore grows without bound. A generator that restarts its ids
in each replication would also collide with the retained ids. Fix test-first. Candidate fixes: a sink
removes what it absorbs, or `Reset` drops entities created after initialisation.

## 5. What this does not show

- It does not show the integrator's full-plant comparison, which has not arrived. A plant model can
  carry planning rules and waits that SimOpt's blocks do not hold yet, so means can differ for
  modelling reasons that have nothing to do with SIM-115.
- If "confidence" is read as a paired share of runs (same seed, scenario against baseline), SIM-115
  hurts it less than it hurts intervals: the one repair draw per replication is common to both arms
  when the streams align. The missing `SeedID` (idea 3) is what would weaken that pairing.
- SIM-119 (antithetic streams not mirrored) would distort intervals as well, but only where antithetic
  sampling is switched on.
