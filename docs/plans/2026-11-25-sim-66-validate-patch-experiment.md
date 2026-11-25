# SIM-66: `validate_model`, `patch_model`, `run_experiment`

**Status:** in progress, started 2026-11-25 (night, unattended, cfg caretaking).
**Serves:** UN-003 (refinement, earlier versions addressable), UN-008 (pre-run sanity checking, all
problems at once, each with a correction), UN-012 (intervals by default), UN-013 (scenario comparison,
significance stated), UN-023 (compact results for an agent), UN-034 (provenance travels with the result).
**Builds on:** SIM-65 (schema v1, `SchemaCatalog`, declared metrics, `ActiveModel.Metrics`) and SIM-63
(`ReplicationRunner`). Tool design: `docs/commercial/2026-08-25-architecture-readiness-review.md` §C.
**Regression baseline:** 1196 passed, 1 skipped, 7 s.

## What exists and what is missing

The builder (`ModelRegistry.BuildModel`) throws at the first fault, so a client LLM fixing a model learns
one problem per call. Nothing checks structure (a node no item can reach, a dead end), nothing estimates
load before a run, and the only way to run is `run_simulation`, one replication with node states and no
intervals. A model cannot be changed, only rebuilt under a new id, and the declared metrics of SIM-65 are
collected but reported nowhere.

## Decisions

**D1. One set of rules: the builder collects, it does not throw.** The validator does not re-implement the
builder's checks. Each check throws a `TopologyException` (an `InvalidOperationException` carrying a code,
the node, the message and the suggested fix), and `BuildModel` catches at four points: per node, per
connection, per router, per metric. It records the fault and carries on, and a node that failed is
remembered, so a connection to it does not add a second "not found" error. At the end `Create` throws a
`ModelValidationException` (also an `InvalidOperationException`) whose message lists every error, one per
line. Every existing refusal test checks a substring of the message and keeps passing unchanged.

**D2. A report is `{valid, errors[], warnings[], info[]}`; an issue is `{code, node?, message,
suggestion?, value?}`.** The layout follows the readiness review. `value` carries a number where one
exists, such as a station's offered load, so a client can act on it without parsing text. `valid` means no
errors. Warnings never block a build: a user may want to study an overloaded system.

**D3. The schema layer is what the binder and the builder check together.** A document whose JSON does not
bind (a parameter that is an array, a connection with one end) is refused by the MCP binder before any tool
code runs, one fault at a time, with the converter's message. Making binding lenient would change the
refusal tests of SIM-65 (`ParamValueTests`, `SchemaCatalogTests`), and those pin deliberate behaviour.
Everything after binding is collected: schema version, node ids and types, parameter kinds, required
parameters, distribution parameters, connection ends, wiring rules and metrics.

**D4. Structural lint, as errors where the model cannot be what was meant:**

| code | severity | when |
|---|---|---|
| `NO_SOURCE` | error | no source: nothing ever enters |
| `NO_SINK` | warning | no sink: items stay in the system (the run still works) |
| `FAN_OUT_WITHOUT_ROUTER` | error | a source, station or delay has more than one connection leaving it. Every node downstream subscribes to the upstream node's release, so each item is put into all of them, duplicated. A router chooses one. |
| `UNREACHABLE` | warning | no path from any source reaches the node |
| `DEAD_END` | warning | the node is reached, but no path leads from it to a sink. Items pile up there. |
| `UNCONNECTED` | warning | the node has no connections |
| `QUEUE_NOT_TAKEN` | warning | a queue feeds no station, so it only fills |

**D5. Offered load is solved from the traffic equations, not walked.** The external arrival rate of a source
is 1 / mean of its arrival distribution. The rate into each node is `λ = γ + λP`, where `P` is the
routing matrix. A probabilistic router splits by weight (equal split without weights), a shortest-queue
router splits equally (an estimate, and the issue says so), and every other node passes its whole flow on.
Rework loops are legal, so the equations are solved by fixed-point iteration. If the iteration does not
converge, items circulate without an exit, reported as `NO_EXIT_FROM_LOOP`. A queue feeding several
stations pools them. For a station, ρ = λ × E[S] / c, with E[S] the service distribution's `Mean`, shift
included since SIM-102.
- ρ ≥ 1: warning `SATURATED`, *"station 'exam' offered load 1.38: its queue grows without bound"*. The
  suggestion names the smallest server count that brings ρ below 0.9. If the queue has a finite capacity,
  the message says items will be refused there instead.
- 0.9 ≤ ρ < 1: warning `NEAR_SATURATION`. Long warm-ups and wide intervals are to be expected.
- Every station: info `OFFERED_LOAD` with ρ in `value`.

**D6. The trial replication is short and runs only on a model that built.** Trial length: 500 mean
interarrival times of the busiest source, so about 500 arrivals there, run in ten chunks with a 1 s
wall-clock limit (it stops early and says so). It reports info `TRIAL` with created, finished, still in
the system and refused counts, plus warnings: `NOTHING_FINISHED` (no item reached a sink) and `REFUSED`
(a full queue, a delay or a router refused items). A queue's refusals are counted through
`NotifyItemNotAccepted`, attached by the builder; today such items vanish without a trace.

**D7. `create_model` validates too and keeps its shape.** It returns `model_id` plus `validation` (errors
empty) when the model builds. When it does not, it returns `error` (every error message, one per line) and
`validation`, and nothing is registered. Existing callers that read `error` keep working.

**D8. A model keeps its versions.** The registry holds every version's topology under one `model_id`
(version 1 from `create_model`). `patch_model` applies a patch to a copy of the latest version. If the
patch validates, the copy becomes the next version. If not, nothing changes, and the report says why.
`run_experiment` runs the latest version, or the one named by `version`, so "compare v3 with v5" works
(UN-003). `run_simulation` and `get_status` keep working on the latest version.

**D9. Patch operations (all apply or none):**

| op | fields | effect |
|---|---|---|
| `set` | `path`, `value` | `name` or `seed`; `<node>.<param>` (a `null` value removes the parameter); `<node>.<param>.<field>` (one field of a distribution object, such as `arrivals.arrival.mean`) |
| `add_node` | `value` = `{id, type, params}` | adds a node; the id must be new |
| `remove_node` | `node` | removes the node, its connections and the metrics declared at it; the result lists what went with it |
| `set_connection` | `from`, `to`, `weight?` | adds the connection, or sets its weight if it exists |
| `remove_connection` | `from`, `to` | removes it |
| `set_metric` | `value` = `{id, kind, node?}` | adds or replaces the metric with that id |
| `remove_metric` | `id` | removes it |

A fault in an operation (an unknown op, a missing node, a bad path) is an error with code `PATCH_*` and the
operation's index, and all such faults are reported at once.

**D10. `run_experiment` builds a fresh model per scenario and reports declared metrics with intervals.**
Arguments: `model_id`, `duration`, `replications` (10, at most 1,000), `warmup` (0.1 × duration),
`metrics?` (ids, default all declared), `scenarios?` (`[{name, patch}]`), `version?`, `seed?` (the
model's seed). A model that declares no metric is measured by a default set: cycle time over all sinks,
throughput per sink, utilization per station and wait time per queue, with ids such as
`util_<station>`. Each scenario is the chosen version with its patch applied. A scenario whose patch does
not validate is reported with its errors and not run, and the other scenarios still run.

**D11. Scenarios share random numbers, and the comparison is a paired interval.** Every scenario runs
with the same base seed. Streams are keyed by node id (SIM-62/89), so a node the patch did not touch draws
the same numbers in both scenarios (common random numbers). For each metric present in both, the
difference from the baseline is taken replication by replication, and its 95 % interval comes from
`ReplicationRunner.ConfidenceHalfWidth`. `significant` means the interval excludes 0. That is what UN-013
asks: whether a difference is real is stated, not left to the eye.

**D12. The payload stays small.** Per scenario: `metrics: {id: {mean, ci95: [lo, hi]}}` with numbers
rounded to four significant digits, `vs_baseline` for each scenario after the first, and `flags` only
when something is wrong: `saturated` (stations with ρ ≥ 1 by D5), `refused` (by node, mean per
replication), `nothing_finished`. Once per call: `replications`, `duration`, `warmup`, `seed`,
`version`, the tool version (UN-034), and a one-line `summary`. A test pins the clinic run with one
scenario at 1,200 characters or fewer, about 400 tokens at the 3 characters per token that JSON with
numbers costs, and the clinic with a second scenario at 2,000 or fewer.

**D13. Where the code lives.** Logic goes into plain classes in `SimOpt.McpServer.Simulation`
(`TopologyLint`, `OfferedLoad`, `TrialRun`, `ModelValidator`, `TopologyPatcher`, `ExperimentRunner`).
The tool methods stay thin wrappers, so the second head (readiness review §D, UN-025) can call the same
classes without MCP.

## Steps (test-first, a commit each)

1. Structured issues; the builder collects every error (D1, D2, D7's report on `create_model`).
2. Structural lint (D4).
3. Offered load (D5).
4. Trial replication and the `validate_model` tool (D6).
5. Versions and `patch_model` (D8, D9).
6. `run_experiment` with scenarios, paired comparisons, flags, provenance and the size test (D10-D12).
   **Acceptance:** the clinic walkthrough of the readiness review: `create_model` from its JSON,
   `validate_model` shows exam load 0.75, `run_experiment` with 10 replications of an 8-hour day after a
   1-hour warm-up returns the three metrics with intervals, and a `flu` scenario
   (`arrivals.arrival.mean` → 2.5) is significantly worse on `wait_exam`, all in one call each.

## Outside this item

The optimiser over the same patches (`optimize`, SIM-69), a lenient binder that collects JSON-level faults
(D3), persistence of versions to disk (UN-005), and a stored, tamper-evident result record (UN-036).
