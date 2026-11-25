<!-- Action: reference -->
<!-- Tracked-by: SIM-65, SIM-66 -->
# SIM-65 is half built. The distribution layer is in; the node vocabulary is not.

## Status 2026-11-03

The two startup decisions this file carried are resolved: SIM-104 is done and the
`docs/decisions.md` review is done. Nothing of SIM-65 changed: **resume at step 1 (`ParamValue`)
below, exactly as written.** The live handover is `docs/pending-sim-105-continuation.md`.

Slice 2 started 2026-08-26. `SIM-65` is marked `[>]` in the backlog. The suite is **974 passing,
1 skipped, 33 s**, and everything below is already committed.

## What landed

`DistributionSpec` + `DistributionFactory` — all seven schema-v1 families, both parameterisations
each, a `shift` on every family, and refusals that name the parameter and the node. Wiring three of
those samplers in for the first time found three engine defects: **SIM-102** and **SIM-103** are
fixed here, **SIM-99** is filed and untouched.

Full design brief, all decisions (D1–D7) and every measured number:
`docs/plans/2026-08-26-sim-65-topology-schema-v1.md`. **Read it before continuing** — the decisions
are what make the remaining work mechanical rather than open.

## Status 2026-11-25 (night)

Step 1 (`ParamValue`) is done, commit "SIM-65 step 1"; suite 1091 passed, 1 skipped. Step 2a is done too
(`queue`/`station` with aliases, `arrival`/`service` distributions, `discipline`, `servers` other than 1
refused; `TopologyVocabularyTests`, suite 1102). Resume at step 2b: the `delay` node and `weight` on
connections for the router.

**Step 2b found SIM-132 first.** `Delay<T>` (`src/SimOpt.Simulation/Templates/Delay.cs`) holds one item:
`Put` returns false while an item is inside, and the handler `ConnectTo(IItemSource)` attaches ignores the
return value, so a second item arriving during a delay disappears. It has no tests and no user here. The
schema's `delay {duration: Dist}` means every item gets its own timer. Plan, test-first: give `Delay<T>` a
`capacity` (default 1, today's behaviour, so nothing downstream moves; unlimited holds a timer per item),
make a refusal visible (an event or the not-accepted notification the other templates use), check its
`Reset` drops items in flight; then the builder builds `delay` as a `SimpleDelay` with
`seedID: StableHash.Of(node.Id)` and unlimited capacity, wires it as a push node (source/station → delay →
queue/station/sink), and reads `duration` with `TimeDistribution(node, label, "duration", "delay_time")`.
Connection `weight` (a `double?` on `ConnectionDefinition`) can land in the same step; only the router
(step 4) reads it.

## Status 2026-11-25 (late night, unattended)

Steps 2b, 3 and 4 are done; suite **1163 passed, 1 skipped**. Commits: `e1c9899` SIM-132 (`Delay<T>`
capacity), `63806e0` step 2b (`delay` node, connection `weight`), `7d8f789` step 3 (multi-server
stations; D3 corrected by SIM-135), `c60ddd2` step 4 (`router`). On the way: SIM-133 (entity auto-ids
raced across threads, `fa1fba8`) and SIM-134 (the logger's dispatcher thread killed the test host,
`b2a065e`). Open from the same hunt: SIM-137 (battery fixture NRE, 1 in 60) and SIM-138 (allocation
tests, about 1 in 30). **A single red run is not a regression signal until SIM-137/138 are fixed;
re-run before concluding.**

**Resume at step 5 (declared metrics), below.** What the builder now knows that step 5 needs: a
station is a pool `stations[nodeId]` of `SimpleServer`s (`<id>#k` for c > 1), so a station's
`utilization` is the pool's busy count over c; `queue_length` and `wait_time` attach to
`buffers[queueId]` (`Instrumentation.ObserveQueueLength`/`ObserveWaitingTime`); `count`/`throughput`
read a sink. ActiveModel has `Delays` and `Routers` besides the old four.

## What is left in SIM-65, in order

1. ~~**`ParamValue`**~~ **done 2026-11-25** — `NodeDefinition.Params` is `Dictionary<string, double>` and cannot hold
   `service: {type: "triangular", ...}`. Make it `Dictionary<string, ParamValue>` where `ParamValue`
   is number | string | `DistributionSpec`, with a `JsonConverter` and **implicit conversions from
   `double`, `int` and `string`** so the six existing `ModelRegistrySmokeTests` compile untouched.
   That is the point: they are the only thing that can tell you this change broke nothing.
2. ~~**Node vocabulary**~~ **done 2026-11-25 (steps 2a, 2b, 4)** — `queue`, `station`, `router`, `delay`, `sink`, `source`; `buffer` and
   `server` kept as accepted aliases (D1). Router weights go on the **connection**, not the node
   (`{"from":"r","to":"a","weight":0.7}`) — the weight belongs to the edge, and it keeps `ParamValue`
   free of maps.
3. ~~**Multi-capacity `station(servers: c)`**~~ **done 2026-11-25 (step 3)** — fan-out of c `SimpleServer`s behind a shortest-idle
   dispatcher on the upstream queue's `ItemReceivedEvent`. Copy `AnalyticBatteryTests.RunMMc`
   (`tests/SimOpt.Tests/Simulation/Statistics/AnalyticBatteryTests.cs:176`) — **that exact
   construction is already validated against Erlang-C on both Wq and Lq**, which is the whole reason
   D2 chose it over a c-channel `Server`. `servers > 1` without an upstream queue is a refusal with
   a named fix (D3).
4. ~~**`SimpleRouter`**~~ **done 2026-11-25 (step 4)** — new template in `SimOpt.Simulation/Templates/`, a `StochasticEntity`
   implementing `IItemSink<SimpleEntity>`, with `probabilistic` and `shortest_queue` policies. Build
   it on `StochasticEntity(model, seedID, id, name)` so its stream is a function of (model seed,
   node id) and resets with the model. **Do not extend `SimpleRejectServer`** — SIM-99 is exactly
   why.
5. **Declared metrics** — `metrics: [{id, kind, node?}]`. `wait_time`, `queue_length` and
   `utilization` have collectors already (`Instrumentation`); `count`/`throughput` read the sink;
   `cycle_time` needs a new `Instrumentation.ObserveCycleTime` (unblocked now that SIM-90 preserves
   entity identity through servers). Attaching them is this item; **reporting them is SIM-66** (D6).
6. **`SchemaCatalog` + `get_schema`** — one source of truth that the builder, `get_schema` and
   `list_templates` all read, plus a test asserting the node types the builder handles equal the
   ones the catalogue publishes (D7). `DistributionSpec`'s doc comment already `<see cref>`s
   `SchemaCatalog`, so it must be created in `SimOpt.McpServer.Models`.

The acceptance test for the whole item is the clinic walkthrough in
`docs/commercial/2026-08-25-architecture-readiness-review.md` §C: *"3 triage nurses, 8 exam rooms,
~12 patients/hour, exams 20–40 min"* must build and run from one `create_model` call.

## Also open

- **SIM-99** (P2) `SimpleRejectServer` routes outside the seed system, and its only production caller
  seeds it with `string.GetHashCode()` — the SIM-62 defect still live on the visualization path.
  Small fix, and it breaks UN-009 for both showcase models.
- **SIM-92** (P1) visualization computes five unsound statistics.
- **SIM-93** (P1) Ivotion KPIs — owner ruled fix, do not retire.
- **SIM-98** (P2) the other three random sources have never been checked against any reference.
- **SIM-96** (P2) nightly analytic gate.
