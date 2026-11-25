# SIM-129: speed per event

2026-11-25, WSL (RTX 4090 box, 24 cores; other sessions ran alongside, load average 1.5 to 5). Owner
ruling: top priority (`docs/decisions.md`, 2026-11-25). Every change keeps results byte-identical.

## Result

On the SIM-122 case (M/G/1 with breakdowns, 100 replications, about 860,000 items) a run took 5.3 s
before and takes 1.55 s now. SimPy takes 2.77 s for the same model on the same machine, and the
per-replication output of the old and new engine is identical (`2026-11-25-sim-129/sim122-case.txt`).

Across the harness's nine cases the engine spends 179 ns per event instead of 997, and allocates 193
to 455 bytes per event instead of 931 to 1,720:

| case | events | ns/event before | ns/event after | bytes/event before | after |
|---|---:|---:|---:|---:|---:|
| mm1 | 303,235 | 828 | 169 | 931 | 193 |
| md1 | 182,059 | 1,170 | 155 | 932 | 193 |
| mg1u | 181,441 | 1,076 | 155 | 933 | 193 |
| mmc | 908,044 | 948 | 188 | 948 | 193 |
| jackson | 556,546 | 1,290 | 180 | 988 | 197 |
| mg1-breakdowns | 160,653 | 864 | 167 | 934 | 192 |
| process-mm1 | 202,806 | 755 | 155 | 1,720 | 455 |
| process-stock | 283,555 | 446 | 143 | 1,069 | 417 |
| mm1-overload | 395,821 | 1,282 | 229 | 1,159 | 199 |

Scale 0.3, 10 replications, best of three, the same harness built against e3bb9e8 and against this
change; every fingerprint matched (`2026-11-25-sim-129/harness-*.csv`). At full length the overloaded
queue went from 3,240 to 251 ns per event, because its cost before grew with the queue.

Memory did not change: 66 MB peak for the SIM-122 case, SimPy 24 MB. The engine's own heap stays flat
across replications (SIM-124); most of the rest is the .NET runtime and the GC's first-generation
budget. With `DOTNET_GCgen0size=0x400000` the same run peaks at 51 MB and is no slower. That is a
setting for whoever hosts the engine, not an engine change.

## Target per event

- **Time:** at most 250 ns per event for the battery's template models on this machine, measured by
  the harness. Met: 155 to 188 ns; the overloaded queue 229 ns. Time depends on the machine and its
  load, so no test holds it.
- **Allocation:** bytes per event are deterministic, so `EngineAllocationBudgetTests` holds them: 260
  for template models (measured 201 to 213 at the test's scale), 520 for process-mm1 (454) and 480 for
  process-stock (417). A warm event list and a raise allocate nothing (`EventSchedulerContractTests`,
  `EventDispatchTests`).

## What was slow, and the fixes (one commit each)

1. **Logging with no logger** (`5cb47a1`, 1,050 to 488 ns). Every log call built a dictionary, read the
   wall clock, queued the message and woke the dispatcher thread, which dropped it; the model-aware
   overload also built and compiled two expression trees (10.7 kB per call). Templates log on the hot
   path: an empty buffer warns on every preview. `Logger.IsActive` now gates all 36 entry points.
2. **The event list** (`fc1fae7`, to 374 ns). A sorted dictionary of points in time, each holding a
   sorted dictionary: a new dictionary per distinct time, two priority clones per event, LINQ
   `First`/`Any` enumerators per instant, and `Priority.CompareTo` boxing its enum twice per
   comparison. Now a pooled binary heap in the same order, with an index per entry for removal.
3. **Raising an event** (`47c6de6`, to 269 ns). Enumerating the sorted handler dictionary allocates a
   stack each time, and each instance made a delegate when built and a tuple when raised. Handlers are
   now kept in an array in priority order; instances raise the values they hold.
4. **Seed generators** (`271c553`, process-mm1 531 to 169 ns). Every `StochasticEntity`, including
   each customer `Process`, built and seeded a 2.5 kB Mersenne Twister it mostly never used. It is now
   made on first use from the current seed, the state an eager one would be in.
5. **FIFO selection** (`eca2a8b`). The smallest queued id was found by a LINQ scan of the queue on
   every `Get`; now a min-heap of ids with stale ids dropped on the way.

The step timings above come from runs with different warm-ups and machine load, so they show direction;
the table is the like-for-like comparison.

## Behaviour that changed

Results did not. Three edge cases did, each from an exception to working code:

- A handler that adds or removes a handler of the event being raised no longer throws "Collection was
  modified"; the change takes effect at the next raise.
- `EventScheduler.Remove` called directly (not through `Model`) on an event of the instant being
  processed no longer breaks the iteration; `Model.RemoveEvent` still refuses it and
  `Model.TryRemoveEvent` still ignores it.
- `EventScheduler.ProcessNextPointInTime` on an empty schedule throws `InvalidOperationException` with
  its own message instead of LINQ's.

Also: a log call made while no logger is registered is dropped at once. Before, it was queued and
dropped by the dispatcher unless a logger happened to register before the dispatcher reached it.

## Method, and a trap

The harness is `benchmarks/SimOpt.Benchmarks` (`dotnet run -c Release --project
benchmarks/SimOpt.Benchmarks -- --scale 0.3 --reps 10 --repeat 3`); `--each` prints every repeat. Its
cases are compiled into the test project too, where `EngineFingerprintTests` pins each case's hash of
every number it computes, recorded on e3bb9e8.

- `dotnet-trace`'s sampled profile put 61 % of the time in `List.AddRange`. That was an artefact: the
  sampler suspends threads at safe points, so samples pile up at calls into native code. Allocation
  counts by type, from an in-process `EventListener` on the runtime's allocation-tick events, were
  reliable and drove steps 2 to 5. There is no `perf` on this box.
- A case timed on its own once showed 620 instead of 170 ns per event. The JIT stops counting calls
  while it is still compiling, so a short warm-up left the code unoptimised. The harness now warms up
  every case for 1.5 s before timing any.

## Left as it is

Per event, the template path still allocates its event instances (about 80 bytes), a priority copy per
instance (about 40; the public `Priority` is mutable per instance, so it cannot be shared), and the
server's per-service batch snapshot and product closure (SIM-90). A process allocates its iterator and
its waits. Pooling instances would cut most of this, but it touches every template and the public event
API, and the target is met without it.
