# SIM-126, last part: processes in the topology schema

2026-11-25 (restart #5, unattended), WSL. Design brief; MG chose the recommended option of
**Q-PROC-SCHEMA** on 2026-11-25 (morning), and all six slices were built the same morning (§ Built). Builds on SIM-126 slices 1-6
(`docs/plans/2026-11-25-sim-126-process-modelling.md`) and SIM-65 (`docs/plans/2026-08-26-sim-65-topology-schema-v1.md`).

## Goal

Let a topology document say what the C# process layer already lets a programmer say: an item that holds
one of a pool of shared units (a bay, a nurse, a forklift), waits for it only so long, draws material
from a stock, and a behaviour that runs beside the item flow (a supplier, a breakdown). The vocabulary
is the contract that `create_model`, `validate_model`, `patch_model`, `run_experiment`, `optimize` and
the Python package all inherit, so it has to stay something a validator can check, an offered-load
pre-check can read, a patch path can address and a plain sentence can describe back (UN-002, UN-004,
UN-008, UN-015).

## The car wash as a document

SimPy's car wash, extended as in `examples/SimOpt.Examples.CarWash` (2 bays, drivers who give up after
5-20 minutes, a 40-unit soap tank, a supplier who refills it when it falls to 5):

```json
{
  "schema_version": "1",
  "name": "car wash",
  "nodes": [
    {"id": "arrivals", "type": "source",   "params": {"arrival": {"type": "exponential", "mean": 3}}},
    {"id": "bays",     "type": "resource", "params": {"capacity": 2}},
    {"id": "soap",     "type": "stock",    "params": {"initial": 40, "capacity": 40}},
    {"id": "wash", "type": "process", "steps": [
      {"type": "seize", "resource": "bays", "patience": {"type": "uniform", "min": 5, "max": 20}, "outlet": "gave_up"},
      {"type": "take",  "stock": "soap", "amount": 1},
      {"id": "washing", "type": "delay", "duration": {"type": "uniform", "min": 4, "max": 6}}
    ]},
    {"id": "supplier", "type": "process", "steps": [
      {"id": "low", "type": "wait_until", "stock": "soap", "at_most": 5},
      {"type": "delay", "duration": {"type": "uniform", "min": 30, "max": 60}},
      {"type": "put", "stock": "soap", "up_to": 40}
    ]},
    {"id": "washed", "type": "sink"},
    {"id": "left",   "type": "sink"}
  ],
  "connections": [
    ["arrivals", "wash"],
    ["wash", "washed"],
    {"from": "wash", "to": "left", "outlet": "gave_up"}
  ],
  "metrics": [
    {"id": "gave-up",      "kind": "share",       "node": "left"},
    {"id": "wait-for-bay", "kind": "wait_time",   "node": "bays"},
    {"id": "bays-busy",    "kind": "utilization", "node": "bays"},
    {"id": "soap-empty",   "kind": "empty_share", "node": "soap"}
  ]
}
```

Read back: *every car arriving at `wash` waits for one of 2 `bays`, giving up after 5-20 minutes
(uniform) and leaving by `left`; with a bay it takes 1 unit of `soap`, waiting in the bay while the tank
is empty, washes for 4-6 minutes, frees the bay and leaves by `washed`. Beside that, `supplier` waits
until `soap` holds 5 or less, takes 30-60 minutes to deliver, and fills it to 40, over and over.*

## Proposed vocabulary (recommended option of Q-PROC-SCHEMA)

Three node types and six step types. Nothing else changes: every v1 document stays valid and builds
the same model, so no template result and none of the colleague's frozen runs move.

| node | params | on the item flow |
|---|---|---|
| `resource` | `capacity` (whole number, default 1) | no: processes seize it |
| `stock` | `initial` (default 0), `capacity` (default unlimited) | no: processes take from it and put into it |
| `process` | `steps` (a list, beside `params`) | fed: runs every item it receives through its steps, any number at once. Not fed: one instance repeats the steps until the run ends |

| step | fields | engine wait it maps to |
|---|---|---|
| `delay` | `duration` (distribution) | `p.Timeout` |
| `seize` | `resource`; `patience` (distribution) with `outlet` | `resource.Request()`, with `Wait.AnyOf(request, timeout)` when patient |
| `release` | `resource` | `resource.Release()` |
| `take` | `stock`, `amount` (number or distribution); `patience` with `outlet` | `stock.TakeWait` |
| `put` | `stock`, `amount` or `up_to` | `stock.PutWait` (`up_to`: the amount that fills to that level when the step starts) |
| `wait_until` | `stock`, `at_most` or `at_least` | `Wait.Next(stock.LevelChangedEvent, filter)`, at once when already true |

Each step may carry an `id`; without one it is `<type>#<n>` (`delay#1`, the first delay of the list).
A step field is a number, a string or a distribution, the same `ParamValue` node params use.

### Rules the recommended option fixes

1. **No control flow inside a process.** No loop over part of the list, no condition, no variable.
   Branching stays in routers between nodes, which the traffic equations and the lint already read.
   Conditional logic, per-item attributes and preemption stay in C# and Python processes (R4, the
   integrator), and SIM-100 (entity classes) adds attributes to the schema later.
2. **An item leaving a process node releases every unit it still holds there.** `release` is needed only
   to free a unit before the item is done (a bay freed before paying). A forgotten release cannot leak a
   unit and deadlock the model, which is the most common error in hand-written seize/release models.
   A `release` of a resource the item does not hold at that step is a validation error. The price: an
   item cannot hold a unit across two nodes (a patient keeping the exam room through a lab delay and a
   router); that waits for a later version.
3. **Patience needs an outlet.** An item that gives up leaves by the connection carrying that `outlet`,
   holding nothing. A step with `patience` and no `outlet`, an outlet with no connection and a connection
   outlet no step names are each an error with the fix named. Outlets are distinct exits, so the lint
   rule that refuses a push node wired to two nodes (an item duplicated per connection) does not apply
   to them.
4. **An unfed process carries no item.** It may have no outgoing connection, and it needs at least one
   step that can wait (a `delay`, `seize`, `take`, `put` or `wait_until`): a list without one would loop
   at one instant (the SIM-128 caution). Reachability lint skips unfed processes, resources and stocks.
5. **Draws happen on entry, from streams the node owns.** A fed process draws all of an item's step
   values when the item enters, in entry order, from one stream per distribution field, seeded from the
   node's id (SIM-125) and the step's id (`StableHash.Of("step:" + id + "." + field)`, as slice 5 seeds a
   process's named streams). Per-item processes then hold no stream of their own (two generators, about
   5 kB, would cost that per item), and inserting a step does not move the other steps' numbers when the
   steps carry ids or keep their per-type position.
6. **Wiring follows the existing words.** A process is fed like a delay (by a source, station, delay,
   router or process) and feeds what a delay feeds (a queue, delay, router, sink or process). A queue
   feeding a process is refused with the fix named: the process waits at its `seize` step, so its feeder
   connects to it directly.

### Measured: draws on entry against draws at the step

Rule 5 was checked on the C# car wash before choosing (scratch harness, not committed): 2 bays against 3,
50 replications per side at common random numbers, three base seeds. In variant B a car draws its wash
time when it reaches the wash, from the same stream; patience is needed at the first step and is drawn
on arrival in both.

| 95 % half-width of the 2-3 bay difference | draws on entry | draws at the step | independent seeds |
|---|---|---|---|
| wait for a bay (min), seed 2026 | 0.155 | 0.168 | 0.198 |
| seed 7 | 0.180 | 0.152 | 0.272 |
| seed 31337 | 0.154 | 0.161 | 0.198 |
| share who gave up, seed 2026 | 0.0087 | 0.0086 | 0.0167 |
| seed 7 | 0.0088 | 0.0093 | 0.0204 |
| seed 31337 | 0.0077 | 0.0082 | 0.0174 |

Common random numbers cut the interval of the difference in the give-up share by about half and the
wait's by a fifth to a third; when the wash time is drawn makes no measurable difference in this model,
which has one step after the bay. Draws on entry are kept for the reasons in rule 5 and because every item
then consumes the same draws whichever exit it takes, the textbook synchronisation for longer step
lists (Law and Kelton). The differences themselves agree across variants (2.02 against 2.03 minutes,
0.064 against 0.064).

### Metrics

| kind | at | meaning | engine |
|---|---|---|---|
| `utilization` | a resource (new) | time-average busy share of its units | `Instrumentation.ObserveResourceUtilization` (exists) |
| `wait_time` | a resource (new) | mean time from request to grant, over granted requests | new instrument; those who gave up are not in it, which the read-back states |
| `queue_length` | a resource (new) | time-average number of waiting requests | new instrument |
| `level` | a stock | time-average level | `ObserveStockLevel` (exists) |
| `empty_share` | a stock | share of time empty | `ObserveStockEmpty` (exists) |
| `share` | a sink | items reaching this sink over items reaching any sink, per replication | new; the give-up share, a scrap rate, "left without being seen" |

### Validation, pre-check and patching

- **Offered load (UN-008).** A resource's load is the sum, over the fed processes that seize it, of the
  node's arrival rate (traffic equations, a process passing its items on like a delay) times the mean time
  a unit is held (the mean durations of the delay steps from the `seize` to the `release` or the node's
  end), divided by its capacity. Waits on stocks are unknown and counted as zero, so the figure is a lower
  bound and says so. An unfed process holds a unit for its share of each cycle (hold time over the sum
  of its mean step durations). With patience, a load above capacity does not make a queue grow without
  bound, because items give up; the flag then says that a share of items will give up, not that the queue
  will grow.
- **Stocks.** A warning when the mean consumption during one supplier lead time exceeds the reorder level,
  so the stock will run empty before most deliveries arrive. Candidate check, measured on the car wash
  before it is kept.
- **Patch paths.** Resources and stocks are nodes, so `bays.capacity` and `soap.capacity` are `set` paths
  and decision variables as they stand. Steps get `<node>.steps.<step id>.<field>[.<distribution field>]`,
  so the (s, S) policy of the supplier is `supplier.steps.low.at_most` and `supplier.steps.put#1.up_to`, and
  the optimiser can search bays and reorder level together (UN-015).
- **Schema version stays "1".** The additions are node types, and a server without them refuses a
  document that uses them by name (`UNKNOWN_NODE_TYPE`, `ModelRegistry.cs:316`), so an older server
  cannot silently build half the model. That is also why resources and stocks are nodes rather than
  top-level arrays: the topology reader skips unknown properties (System.Text.Json's default; no
  `UnmappedMemberHandling` is set), so an older server would drop a `resources` array without a word.

## Alternatives in Q-PROC-SCHEMA

- **Flowchart blocks** (Arena, AnyLogic's process library): `seize`, `release`, `take` and `put` as graph
  nodes between delays. An item can hold a unit across nodes and routers. A modelled activity then needs
  three to five nodes, and whether every path releases what it seized becomes a path analysis over a graph
  with cycles, with a leaked unit showing up as a deadlock at run time.
- **Scripted processes**: a small language in JSON with loops, conditions and variables, as close to SimPy as
  JSON gets. It reaches the most models, and costs the offered-load pre-check (a loop's demand cannot be
  read off it), a read-back that has to narrate code, and the grammar SIM-83 wants to constrain a local
  model with. The vision says the user never writes code; C# and Python already give integrators the full
  process layer.

## Build plan, after Q-PROC-SCHEMA (test-first, each slice its own commit)

1. `resource` and `stock` nodes with their metrics; catalog, JSON Schema, `get_status` and
   `run_simulation` reporting.
2. Fed `process` with `delay`, `seize`, `release`, `take`, `put`; release on exit; draws on entry; wiring
   and step lint. Acceptance: an M/M/3 written as a process (resource of 3, one delay) agrees with
   Erlang-C on Wq as the station does (`AnalyticBatteryTests`).
3. `patience` and outlets, the `share` metric.
4. Unfed processes and `wait_until`, `put up_to`. Acceptance: the car wash document above, run through
   `run_experiment` with 20 replications, agrees with the C# example on all four metrics (the interval of
   each difference covers zero).
5. Offered load and the stock warning; step patch paths; `optimize` over bays and reorder level on the
   car wash.
6. Python helpers (`Topology.resource`, `.stock`, `.process(steps=…)`), README, and the per-event cost of
   a process node against a station on `benchmarks/SimOpt.Benchmarks` (SIM-129's target is 250 ns for
   template models).

Size L. No template result moves; `EngineFingerprintTests` stays as it is.

## Built (2026-11-25, morning)

Six commits, test-first: `120ed99` (slice 1), `9ad8890` (2), `6295e50` (3), `9d47545` (4), `b12c848` (5) and the
slice 6 commit. On the way, `69476ff` fixed SIM-142: no topology with two sources could be created. Measured:

| check | result |
|---|---|
| M/M/3 as a process (resource of 3, one delay), 20 x 21,000 | Wq 1.098 +- 0.023 against Erlang-C 1.079 (the station 1.121 +- 0.031); Lq 2.636 +- 0.055 against 2.589 |
| patience 0 on 3 lines at a = 2.4, 20 x 10,000 | lost 0.26711 +- 0.00116 against Erlang B 0.26841 |
| the car-wash document above against `examples/SimOpt.Examples.CarWash`, 20 days | every difference inside its 99 % interval: gave up 0.1637 vs 0.1696, wait for a bay 3.33 vs 3.24, bays busy 0.838 vs 0.836, soap empty 0.179 vs 0.186 |
| the stock warning, kept after measuring | reorder level 5 (warned, 15 used per delivery): tank empty 0.199 of the day; 20 (not warned): 0.0046 |
| `optimize` over `bays.capacity` 2-3 and `supplier.steps.low.at_most` 5-25 (grid, 10 replications) | 3 bays and 25: 0.0009 gave up against 0.160 for 2 and 5 |
| cost per event (`benchmarks`, scale 0.3, 10 replications, best of 3) | process node M/M/3 234 ns, 392 bytes; the station pool `mmc` 205 ns, 193 bytes; a C# process M/M/1 231 ns |

Two choices the brief did not settle. A step's `amount` has no default, so no step field is ever "assumed". A
node nothing feeds that repeats its steps 100,000 times at one instant stops the run naming itself
(`ProcessNode.MaxCyclesPerInstant`), so `validate_model` reports `TRIAL_FAILED` instead of the server hanging; rule
4's "a step that can wait" holds for every valid list, so it could not prevent the loop on its own.

## Not in this version

Units per seize above 1 (needs an all-or-nothing multi-unit request in `Resource`), priorities and
preemption on a resource, holding a unit across nodes, arrival and capacity schedules (SIM-101), entity
classes and attributes (SIM-100), and `renege_after` on a plain queue, which a process's `seize` with
patience covers for models that need it (SIM-65 D5).
