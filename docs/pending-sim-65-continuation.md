<!-- Action: present -->
<!-- Tracked-by: SIM-65, SIM-66 -->
# SIM-65 is half built. The distribution layer is in; the node vocabulary is not.

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

## What is left in SIM-65, in order

1. **`ParamValue`** — `NodeDefinition.Params` is `Dictionary<string, double>` and cannot hold
   `service: {type: "triangular", ...}`. Make it `Dictionary<string, ParamValue>` where `ParamValue`
   is number | string | `DistributionSpec`, with a `JsonConverter` and **implicit conversions from
   `double`, `int` and `string`** so the six existing `ModelRegistrySmokeTests` compile untouched.
   That is the point: they are the only thing that can tell you this change broke nothing.
2. **Node vocabulary** — `queue`, `station`, `router`, `delay`, `sink`, `source`; `buffer` and
   `server` kept as accepted aliases (D1). Router weights go on the **connection**, not the node
   (`{"from":"r","to":"a","weight":0.7}`) — the weight belongs to the edge, and it keeps `ParamValue`
   free of maps.
3. **Multi-capacity `station(servers: c)`** — fan-out of c `SimpleServer`s behind a shortest-idle
   dispatcher on the upstream queue's `ItemReceivedEvent`. Copy `AnalyticBatteryTests.RunMMc`
   (`tests/SimOpt.Tests/Simulation/Statistics/AnalyticBatteryTests.cs:176`) — **that exact
   construction is already validated against Erlang-C on both Wq and Lq**, which is the whole reason
   D2 chose it over a c-channel `Server`. `servers > 1` without an upstream queue is a refusal with
   a named fix (D3).
4. **`SimpleRouter`** — new template in `SimOpt.Simulation/Templates/`, a `StochasticEntity`
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
