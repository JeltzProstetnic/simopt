# SIM-65 — Topology schema v1

**Status:** in progress, started 2026-08-26.
**Serves:** UN-001 (natural-language model construction), UN-004 (structured authoring), and it is the
schema `validate_model` validates against in SIM-66.

## Why this is the first item of Slice 2

Today's MCP schema has four node types (`source`, `buffer`, `server`, `sink`), exponential service
only, and single-capacity servers. The product's own flagship example — *"3 triage nurses, 8 exam
rooms, ~12 patients/hour, exams 20–40 min"* — needs **multi-capacity stations** and a
**non-exponential service distribution**, so it cannot be expressed at all. A schema that cannot
express the demo is not a schema, and everything downstream (`validate_model`, `run_experiment`,
`TopologyProblem`, the optimiser) validates or patches against it.

## What v1 is

Vocabulary, from `docs/commercial/2026-08-25-architecture-readiness-review.md` §C:

```
source   {arrival: Dist, limit?: int}
queue    {capacity?: int, discipline?: fifo|lifo}
station  {servers?: int, service: Dist}          ← multi-capacity, the big one
router   {policy: probabilistic|shortest_queue, weights?: {targetId: w}}
delay    {duration: Dist}
sink     {}
Dist     {type: exponential|triangular|uniform|lognormal|gamma|constant|empirical, params...}
metrics  [{id, kind: wait_time|cycle_time|utilization|queue_length|throughput|count, node?}]
```

plus `"schema_version": "1"` on the document, and `get_schema` publishing the JSON Schema so a client
LLM can self-correct against it rather than against an error message.

## Decisions

**D1 — `buffer` and `server` stay as accepted aliases for `queue` and `station`.** Every existing
test, example and the visualization's own vocabulary uses them. Aliasing costs one dictionary entry
and keeps the pre-SIM-65 regression net intact, which is the only thing that can tell us this change
broke nothing. `list_templates` and `get_schema` advertise the v1 names only.

**D2 — `station(servers: c)` is built as a fan-out of c `SimpleServer`s behind a shortest-idle
dispatcher, not as a c-channel `Server`.** The engine generalisation is the better end state and is
deliberately *not* being done here: the fan-out is **the exact construction SIM-64's analytic battery
already validates against Erlang-C** (`AnalyticBatteryTests.RunMMc`, agreeing on both Wq and Lq at
c = 3), so shipping it means shipping a construction with a closed-form check already standing behind
it. A new c-channel `Server` would arrive with no such evidence and would have to earn it.

**D3 — a station with `servers > 1` must be fed by a queue, and saying so is a validation error with
a named fix.** The dispatcher hangs off the upstream queue's `ItemReceivedEvent`; a direct
`source → station` push reaches every server in the pool at once, which silently is not a c-server
station. Rejecting it with *"station 'exam' has 3 servers and no upstream queue — insert a queue
between 'arrivals' and 'exam'"* is what UN-008 asks for. `servers: 1` keeps working unqueued.

**D4 — the router is a new engine template (`SimpleRouter`), not a builder-level closure.** It needs
a random stream that (a) is derived from the model seed and the node id, and (b) resets with the
model. `SimpleRejectServer` — the existing probabilistic-routing template — does neither: it holds a
`System.Random` seeded from a constructor default of 42, independent of the model seed and untouched
by `Model.Reset`. That is the SIM-58 defect class and it is filed separately as **SIM-99**; this item
does not inherit it.

**D5 — deferred out of v1, deliberately, and filed:** `entity_class`, `per_class_service` and the
`by_attribute` router policy. All three need entity classes, which the engine does not have; adding
them properly means class-aware service, class-scoped metrics and class-aware validation, which is
its own item, not a corner of this one. Filed as **SIM-100**. Also deferred: `source.schedule`
(time-varying arrival rate, **SIM-101**) and `queue.renege_after`. Everything in the clinic
walkthrough is expressible without any of them.

**D6 — SIM-65 declares metrics and attaches collectors; SIM-66 reports them.** The schema's
`metrics` block is what makes a model self-describing about what it is measuring; turning those
collectors into a `run_experiment` payload with confidence intervals is the next item. This item
ends at "the collectors exist, are attached to the declared nodes, and are addressable by metric id".

**D7 — one source of truth for the vocabulary (`SchemaCatalog`).** The builder, `get_schema` and
`list_templates` all read the same catalogue, and a test asserts the set of node types the builder
handles equals the set the catalogue publishes. Hand-maintaining a JSON Schema next to a switch
statement is how the two drift, and a client LLM self-correcting against a stale schema is worse
than one with no schema at all.

## Testing

Test-first throughout. Baseline before any change: **922 passed, 1 skipped, 31 s**.

Three test classes carry the weight:

1. **`DistributionFactoryTests`** — every schema distribution type builds, is configured, and
   samples with the right mean; every malformed spec is refused with a message naming the parameter.
2. **`GoodnessOfFitTests` additions** — per the SIM-64 handoff, **each newly exposed sampler gets a
   goodness-of-fit test against its own fully specified CDF**. Triangular, lognormal and gamma are
   being handed to users for the first time here; none has ever been checked against its own
   distribution function, and a moment test cannot see a wrong shape (SIM-64 measured exactly that:
   FIFO → LIFO moved no mean and failed only the distributional check). Each also gets a
   wrong-parameter twin, so the instrument is shown to have power against the sampler it is
   certifying.
3. **`TopologySchemaV1Tests`** — the clinic walkthrough builds and runs end to end; a multi-capacity
   station spreads its offered load across the pool; a probabilistic router splits in the declared
   proportion; a shortest-queue router balances; declared metrics produce addressable collectors;
   node ordering and re-runs stay reproducible (the SIM-89/SIM-91 properties must survive the new
   node types).

## Progress

**Landed 2026-08-26 — the distribution layer.** `DistributionSpec`, `DistributionFactory`, and the
three engine repairs that wiring it uncovered. Suite **922 → 974**, zero regressions, 31 s → 33 s.

Still to build (the rest of SIM-65): `ParamValue` so `params` can hold a distribution object, the
node vocabulary (`queue`/`station`/`router`/`delay` + the `buffer`/`server` aliases), multi-capacity
stations, `SimpleRouter`, declared metrics, `SchemaCatalog` and `get_schema`.

### Three defects found by wiring the distributions in

Every one of them was found by *exposing* a sampler, not by reading it — which is the argument for
doing the schema before the tools that consume it.

**SIM-103 — `LogNormalDistribution` could not be used by the engine at all.** Its parameterless
constructor left the internal Gaussian null and `Configure` dereferenced it on the first line, so
`new LogNormalDistribution(); d.ConfigureMean(3, 1);` threw `NullReferenceException` — and since the
public `LogNormalDistribution(mean, stddev, shift)` constructor chains to the parameterless one,
**that constructor threw on every call it has ever received**. Behind it sat a second defect that
would have surfaced the moment the first was fixed naively: `Initialize(seed)` *replaced* the
internal Gaussian instead of re-seeding it, discarding the mu and sigma just configured and
silently reverting to the standard lognormal. Together they closed the only order `Random<T>`
permits — configure, then initialise — so lognormal was unreachable from any simulation model.
The second is the more dangerous one: it returns numbers rather than an exception.

**SIM-102 — three distributions reported a mean they do not draw.** `NegExponentialDistribution`,
`LogNormalDistribution` and `GammaDistribution` all accept a `shift`, all add it in `Next()`, and
all stored `mean` at configure time without it. Measured: a shifted exponential reported **2.00000**
and sampled **7.00111**; a shifted gamma reported **4.00000** and sampled **8.99503**.
`NonStochasticValue` — what a deterministic run substitutes for a draw — was wrong by the same
amount. Nothing in the repository had ever set a non-zero shift, which is why it had never bitten;
SIM-65 is what makes `shift` writable by a user, and SIM-66's analytic pre-check reads `Mean` to
estimate offered load. A station whose service mean reads 2.0 while it serves at 7.0 would be
certified stable at ρ = 0.29 while queueing without bound at ρ = 1.02 — **the pre-check would pass
exactly the model it exists to catch.** Fixed by reading the shift through on every access rather
than folding it in, because `Shift` is publicly settable and a mean computed once goes stale.

**SIM-99 — filed, not fixed.** `SimpleRejectServer`, the existing probabilistic-routing template,
draws its routing decisions from a `System.Random` seeded from a constructor default of 42,
independent of the model seed and untouched by `Model.Reset` — so the second replication continues
the first one's stream. Its only production caller compounds it: the visualization passes
`Topology.Seed + node.Id.GetHashCode()`, and `string.GetHashCode()` is .NET's **per-process
randomised** hash, which is the SIM-62 defect still live on that path. This is why D4 says the
router will be a new template rather than an extension of this one.

### Goodness-of-fit, per the SIM-64 handoff

Triangular, lognormal and gamma become writable by a user for the first time in schema v1, and none
had ever been checked against its own distribution function — only against its mean. Eight tests
added to `GoodnessOfFitTests`, each acceptance paired with a rejection at a *mean-preserving* wrong
parameter, so the instrument is shown to have power over the family it certifies:

| sampler | accepted against | rejected against | why that twin |
|---|---|---|---|
| Triangular(20, 30, 40) | its own CDF | Triangular(20, 26, 44) | both have mean 30 |
| Lognormal(μ=1, σ=0.5) | its own CDF | σ = 0.6 | a 20% shape error |
| Lognormal from moments (3, 1) | the implied μ = 1.04654, σ = 0.32459 | — | catches wrong moment algebra, which leaves the scale roughly right |
| Gamma(k=2, θ=3) | its own CDF | Gamma(k=3, θ=2) | both have mean 6 |
| Gamma(k=1, θ=2.5) | Exp(0.4) | — | Gamma(1, θ) *is* exponential; free identity check |

The mean-preserving twins are the point. SIM-64 measured that a FIFO → LIFO switch left all nine
mean-based comparisons passing and failed only the distributional check; the same blindness applies
to every sampler here, and a moment test would certify all four wrong parameterisations above.
