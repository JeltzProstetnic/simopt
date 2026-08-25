# SimOpt — Architectural & Commercial-Readiness Review (Fable, 2026-08-25)

Read-only review against the product vision: MCP-enabled sim-opt engine producing ad-hoc digital
twins from natural language, consumer UI with BYO-LLM chat, targeting non-expert operations users.
Sources: full read of `docs/2026-07-05-critical-code-review.md`, `backlog.md`, `docs/decisions.md`,
the complete `SimOpt.McpServer` source, and targeted reads across Simulation/Optimization/
Visualization/Ivotion/Statistics. No build or test runs (concurrent build in progress).

**One-sentence verdict:** The bones are good and the positioning is right, but today this is a
demo-grade framework with an untrustworthy stochastic layer, no output-statistics subsystem, and an
MCP surface that cannot express its own flagship example — a sellable v1 is ~3–4 focused months
away, not a rewrite.

---

## A. Production-readiness verdict

### DES engine kernel: architecturally sound, correctness-debt encumbered — KEEP
The 2026-07-05 review's "healthy" list held up under my re-inspection: the scheduler is
deterministic (SortedDictionary on Priority→Type→Number→AddedOrder, FIFO tie-break), `Model.Reset`
re-seeds all streams for common random numbers, and the template layer (Source/Buffer/Server/Sink/
Conveyor/Delay) is a reasonable process-interaction vocabulary. This is not a kernel that needs
rewriting.

But **every open item in SIM-56..SIM-59 is still open** (`backlog.md:101-105`). That means, today:

- The default RNG (`MersenneTwister`) violates its `[0,1)` contract and will eventually hard-crash
  on `Math.Abs(int.MinValue)` over a long optimization run (MT-1).
- **Every triangular draw ever made by this framework is mathematically wrong** (TRI-1) — and
  Triangular is *the* distribution a non-expert gives you ("takes 20 to 40 minutes, usually 30").
- Gamma is biased (`1/3` integer division), `Uniform*.Mean` is wrong for `min≠0`,
  exp-family samplers can return `+∞`.
- The reset path has silent cross-evaluation divergence (`Delay.Reset` never re-schedules, DES-2),
  which corrupts exactly the reset-and-re-run loop that `IProblem.Evaluate` — and the MCP
  `run_simulation` tool (`SimulationTools.cs:89`) — depends on.

For a product whose entire value proposition is "trust these numbers enough to make a staffing/
capital decision," shipping with a broken stochastic layer is disqualifying. The fixes are small,
cataloged, and test-first-able. They are the first slice, not an eventually.

### The bigger structural gap: there is NO output-statistics subsystem
This is the finding the prior review didn't make because it wasn't in scope, and it matters more
than any single bug:

- Grep for time-weighted statistics, waiting-time tallies, or utilization accounting across
  `SimOpt.Simulation` and `SimOpt.Statistics` comes back **empty**. `SimOpt.Statistics` is an ML
  grab-bag (PCA, ROC, confusion matrix, 20+ SVM kernels) — it contains zero DES output analysis.
- Server utilization — the headline KPI of the whole product — is computed **in the UI rendering
  layer by polling** (`src/SimOpt.Visualization/Controls/SimulationCanvas.cs:297-332`): it samples
  `ns.Working` per render tick and accumulates busy time. Sampled utilization misses any busy
  interval shorter than a poll gap, and the number lives in a place headless runs (MCP, optimizer)
  can't reach.
- The MCP `run_simulation` result (`SimulationTools.cs:97-126`) reports terminal sink counts,
  instantaneous queue depths, and boolean busy flags. No mean/max waiting time, no time-average
  queue length, no utilization, no confidence intervals, no warm-up handling, no replications.
  An operations manager asking "how long do patients wait?" gets no answer from this surface.

Every commercial competitor (Arena, Simio, AnyLogic, even SimPy with `salabim`'s monitors) treats
event-driven tally + time-persistent statistics, warm-up truncation, and replication CIs as the
non-negotiable core. **This subsystem must be built (size L). It is the largest genuinely new
engineering item between here and a product** — everything else is fixing, extending, or wiring.

### Optimizer layer: right interfaces, broken seams, missing generic coupling
`IStrategy`/`IProblem`/`ISolution` (`src/SimOpt.Optimization/Interfaces/`) is the correct minimal
shape, and the maximization-sign convention is applied consistently. But:

- The operator seam is broken (SIM-59 open): wall-clock `new Random()` in `Tweak`/`CombineWith`
  destroys reproducibility; EA aliases `BestSolution`; PSO's reflection contract fits only the unit
  test's `TestSolution` — zero search, silently, on every real problem.
- Stochastic fitness is treated as deterministic — single evaluation per candidate, no replication
  averaging (SIM-40 open), no ranking-and-selection. Optimizing on one noisy sample is how you
  confidently recommend the wrong configuration.
- **Every problem is hand-written** (`IvotionProblem`, the retired glass twin, SimOptDemo). There is
  no generic "topology + decision variables → IProblem" component. That component is the actual
  product; see §C.

### API surface for LLM-driven construction: right shape, toy coverage
The declarative flat-JSON `TopologyDefinition` (`src/SimOpt.McpServer/Models/TopologyDefinition.cs`)
is exactly what decisions.md's "Agent-Driven Sim-Opt Positioning" calls for, and the
`ModelRegistry.BuildModel` mapping (JSON → live object graph, `ModelRegistry.cs:51-245`) proves the
approach. But the current schema is a toy:

- **4 node types, exponential distributions only, hardcoded.** No distribution choice at all
  (`CreateNegExp` is the only path, `ModelRegistry.cs:250-256`).
- **Single-capacity servers only** — `SimpleServer` processes one entity at a time. The vision's own
  clinic example ("3 triage nurses, 8 exam rooms") is **inexpressible** without manually fanning out
  N parallel server nodes and a routing policy that doesn't exist.
- No probabilistic/attribute routing, no entity classes, no resource pools, no schedules/shifts,
  no priorities, no reneging/balking.
- **Fresh bug found in this review:** seed derivation uses `node.Id.GetHashCode()`
  (`ModelRegistry.cs:76,117`). .NET string hash codes are **randomized per process** — the same
  topology + seed produces different random streams after every server restart. Reproducibility
  holds only within one session. Fix: a stable hash (FNV-1a over UTF-8, or an ordinal node index).
- The five tools (create_model, run_simulation, get_status, list_templates, list_models) include
  **no optimization tool whatsoever**. The "opt" in SimOpt is absent from the MCP surface.
- `SimOpt.McpServer` has **zero tests** (grep across `tests/` finds no MCP references; SIM-20 open).

### Test coverage: credible regression net, not a correctness gate
~681 `[Fact]`/`[Theory]` methods across 44 files, with genuine coverage of the scheduler, templates,
matrix decompositions, PCA/ROC. But the suite happily coexisted with ~40 correctness bugs including
a totally wrong triangular sampler — it pins current behavior, it does not validate against ground
truth. What's missing for a product: an **analytic benchmark battery** (M/M/1, M/M/c, M/G/1
Pollaczek-Khinchine, small Jackson network — simulated results within CI of closed-form answers) as
a CI gate, plus distribution goodness-of-fit tests on large samples. Until that exists, "551/615
tests pass" is not evidence the numbers are right.

### Bottom line for A
Not a rewrite. The kernel, the interface layer, the declarative-JSON direction, and the viz
investment are all keepers. But it is **two new subsystems** (engine statistics; generic
topology-parameterized optimization) **plus paid-down bug debt** (SIM-56..59) **plus a schema an
order of magnitude richer** away from being chargeable. Anyone paying money today would be paying
for wrong numbers.

---

## B. Gap analysis: vision vs. present state

| # | Gap | What's missing concretely | Size | Paid-v1 blocker? |
|---|-----|---------------------------|------|------------------|
| 1 | Stochastic-layer correctness | SIM-56 (RNG contracts), SIM-57 (Triangular/Gamma/Median), SIM-58 (reset path) — all open | M+M+M | **YES** — poisons everything downstream |
| 2 | Optimizer integrity | SIM-59: seeded operators, EA cloning, SA loop guard, PSO vector contract | L | **YES** for the "opt" claim |
| 3 | Output statistics subsystem | Event-driven tallies (wait/cycle time), time-weighted stats (queue length, utilization), warm-up truncation, replications, CI reporting — in the ENGINE, not the UI | L | **YES** — this is the product's deliverable |
| 4 | Model schema expressiveness | Multi-capacity stations; distribution objects (exp/tri/uniform/lognormal/gamma/constant/empirical); routers (probabilistic, shortest-queue, by-attribute); entity classes + attributes; resource pools; arrival schedules | XL total; blocker subset = stations + distributions + routing ≈ L | Subset **YES**, rest v1.x |
| 5 | MCP optimization + experiment tools | define variables/objective, `optimize`, `run_experiment` (replications, scenario compare), `validate_model`, `patch_model`, `delete_model`, result summarization | L | **YES** |
| 6 | MCP integration testing | SIM-20; plus stable per-node seeding fix (GetHashCode bug above) | S–M | **YES** (cheap, mandatory) |
| 7 | BYO-LLM chat UI | Entire `SimOpt.Chat` app: provider abstraction, agent loop, key storage, streaming, result panes | XL | **YES for consumer v1** — but see §F: a "pro" v0.9 (Claude Code/Desktop + MCP) can start earning without it |
| 8 | Viz ↔ MCP unification | Two disjoint topology representations (`VizTopology` w/ 3 presets vs `TopologyDefinition`); MCP-built models cannot be visualized | M | No — v1.1 wow-feature; demo value is high though |
| 9 | Model persistence | Models die with the server process (in-memory `ConcurrentDictionary`, `ModelRegistry.cs:20`); no save/load/export | S–M | Borderline — cheap, do it |
| 10 | Analytic validation battery | M/M/1, M/M/c, M/G/1 vs closed form as CI gate | M | **YES** (credibility, and your marketing proof) |
| 11 | Commercial plumbing | License decision (repo has **no LICENSE file** — public + unlicensed), packaging/installer, license keys, update channel, crash reporting, privacy statement for LLM egress | M–L | YES for charging, trivially parallelizable |
| 12 | Docs/onboarding for non-experts | Guided first-run, example gallery, "what is a replication" explainers surfaced by the agent | M | Soft blocker |

Explicitly **not** gaps (per decisions.md, correctly): CSV/schema import layers (retired SIM-39 —
right call), RACI-grade enterprise config, 3D visualization, GridWorld/FMT (research track,
orthogonal to the product; SIM-61 notes crucible forked it to Python anyway).

---

## C. Architecture for ad-hoc digital twins (the crux)

### Design stance
1. **Few coarse tools, not many fine ones.** LLM tool-use degrades with tool count and per-call
   round-trips. One-shot model creation from a complete document beats 20 `add_node` calls: fewer
   opportunities for inconsistent intermediate state, one validation point, resumable. The existing
   `create_model(topology)` one-shot shape is right — keep it, enrich the schema.
2. **JSON schema, versioned — not a DSL, not object construction.** LLMs emit JSON with near-perfect
   syntax; a bespoke DSL adds a parser and a failure mode for zero expressive gain; direct object
   construction over MCP is impossible anyway. Publish the JSON Schema itself through a tool/resource
   (`get_schema`) so the client LLM can self-correct against it. Add `"schema_version": "1"` now.
3. **Validation is a first-class tool with machine-actionable output**, not an exception message.
   The current builder throws on first error (`ModelRegistry.cs:136,191-241`); an LLM iterating on a
   model needs *all* errors and warnings at once, structured.
4. **Results are summaries with drill-down, never traces.** Token economy: a result payload should
   be readable in <400 tokens and complete enough that the LLM never needs a second call for the
   common case.

### The missing keystone component: `TopologyProblem`
A generic `IProblem` whose genome is a vector of decision variables defined as **references into the
topology JSON** (`{node: "triage", param: "capacity", type: "int", min: 1, max: 6}`), whose
`Evaluate` applies the vector as a patch, rebuilds/resets the model, runs N replications with
warm-up, and computes fitness from a declared metric expression. This single class turns every
MCP-built model into an optimizable one and eliminates hand-written per-domain `IProblem`s
(IvotionProblem stays as the showcase vertical, but customers never write one). It also finally
gives PSO/EA/SA a uniform, non-reflection vector contract — fixing OPT-3 by design.

### Schema v1 (node vocabulary)
```
source   {arrival: Dist | schedule: [{start,end,rate}], entity_class?, limit?}
queue    {capacity?, discipline: fifo|lifo|priority, renege_after?: Dist}
station  {servers: int, service: Dist, per_class_service?: {class: Dist}}   ← multi-capacity, the big one
router   {policy: probabilistic|shortest_queue|by_attribute, weights?/rules?}
delay    {duration: Dist}
sink     {}
Dist     {type: exponential|triangular|uniform|lognormal|gamma|constant|empirical, params...}
metrics  [{id, kind: wait_time|cycle_time|utilization|queue_length|throughput|count, node?, class?}]
```
`station` with `servers: c` is implemented either as a c-channel Server generalization (preferred,
engine work) or short-term as a builder-level fan-out of c `SimpleServer`s behind a shortest-idle
dispatcher — expressible today, ugly, invisible to the user. Distribution objects are only shippable
**after SIM-56/57**; wiring Triangular into the schema before the fix would industrialize the bug.

### Tool surface to ship (8 tools)
```
create_model(topology: TopologyV1) -> {model_id, validation: ValidationReport}
    // always validates; refuses to register only on structural errors, registers with warnings otherwise

validate_model(topology: TopologyV1) -> ValidationReport
    // dry-run: (1) schema check, (2) semantic lint — unreachable nodes, missing sink, buffer→buffer,
    // (3) analytic pre-check: per-station rho via offered-load estimate, "station 'triage' rho=1.38,
    //     queue will grow without bound", (4) 1 short trial replication (<1s wall) — entities created/
    //     disposed, deadlock/starvation heuristics. Report = {errors[], warnings[], info[]}, each
    //     {code, node_id?, message, suggestion}

patch_model(model_id, patch: [{op: set|add_node|remove_node|set_connection, path/node, value}])
    -> {model_id, version, validation}
    // the iterate-and-refine loop; versions retained so "compare v3 vs v5" works

run_experiment(model_id, duration, replications=10, warmup=0.1*duration,
               metrics?: [metric_id], scenarios?: [{name, patch}])
    -> per scenario: {metrics: {id: {mean, ci95: [lo,hi], n}}, flags: [saturated_stations,
       dropped_entities, nonzero_end_queues], one_line_summary}
    // scenario array = A/B/C comparison in ONE call, returned as a compact table — the single
    // biggest token saver in the whole loop

optimize(model_id, variables: [{node, param, type, min, max, step?}],
         objective: {metric, direction, constraints?: [{metric, op, value}]},
         budget: {evaluations?, seconds?}, replications_per_eval=5, strategy="auto")
    -> {best: {variables, metrics}, top_k: [...], evaluations_used, convergence: sparkline-array}

get_results(model_id, detail: summary|per_node|distribution, node_id?) -> drill-down on demand
list_templates() -> (keep; regenerate from the schema so they can't drift)
delete_model(model_id) / list_models()
```

### Clinic walkthrough (the acceptance test for this whole design)
"3 triage nurses, 8 exam rooms, ~12 patients/hour, exams 20–40 min" becomes one `create_model`:
```json
{"schema_version":"1","name":"clinic","seed":7,
 "nodes":[
  {"id":"arrivals","type":"source","params":{"arrival":{"type":"exponential","mean":5.0}}},
  {"id":"triage_q","type":"queue"},
  {"id":"triage","type":"station","params":{"servers":3,"service":{"type":"triangular","min":3,"mode":5,"max":10}}},
  {"id":"exam_q","type":"queue"},
  {"id":"exam","type":"station","params":{"servers":8,"service":{"type":"triangular","min":20,"mode":30,"max":40}}},
  {"id":"exit","type":"sink"}],
 "connections":[["arrivals","triage_q"],["triage_q","triage"],["triage","exam_q"],["exam_q","exam"],["exam","exit"]],
 "metrics":[{"id":"wait_exam","kind":"wait_time","node":"exam_q"},
            {"id":"util_exam","kind":"utilization","node":"exam"},
            {"id":"los","kind":"cycle_time"}]}
```
`validate_model` warns: exam offered load = (12/hr × 30min)/8 = 0.75 — OK; then `run_experiment`
(10 reps, 8h day, 1h warm-up) returns means+CIs; user asks "what if flu season doubles arrivals?"
→ `run_experiment(scenarios=[{name:"flu", patch:[{op:"set", path:"arrivals.arrival.mean", value:2.5}]}])`;
"fewest rooms keeping wait under 15 min?" → `optimize(variables=[{node:"exam",param:"servers",
type:"int",min:4,max:12}], objective={metric:"servers",direction:"min",
constraints:[{metric:"wait_exam",op:"<",value:15}]})`. Every step is one tool call. That is the
product. None of it requires new science — it requires §B items 1–6.

---

## D. BYO-LLM chat UI architecture

**Factor the tool layer out of the MCP head.** Extract `SimulationTools` + `ModelRegistry` (+ the
new experiment/optimize layer) into a plain library — call it `SimOpt.Engine.Api` — with two heads:

1. `SimOpt.McpServer` — the existing stdio head, unchanged externally. Serves Claude Code/Desktop,
   Copilot, any MCP client. This is also your **pro/early-revenue channel** (see §E/F).
2. `SimOpt.App` (Avalonia) — consumer UI hosting the **agent loop in-process**, calling the same
   tool implementations directly. Do *not* spawn your own MCP server as a subprocess and talk
   JSON-RPC to yourself — pointless serialization, process lifecycle pain, worse debuggability. The
   fidelity argument (UI sees exactly what MCP clients see) is preserved by sharing the tool
   *definitions* (names, JSON schemas, implementations) in the library, with a contract test
   asserting both heads expose an identical tool list.

**Provider abstraction:** `Microsoft.Extensions.AI` (`IChatClient`) is the correct .NET seam —
first-party abstraction with function-calling normalized across OpenAI, Azure, Anthropic, and
Ollama/OpenAI-compatible local endpoints (LM Studio serves both `/v1/chat/completions` and, ≥0.4.1,
a native Anthropic `/v1/messages`). The agent loop is ~100 lines: system prompt (sim-analyst
persona + schema summary), stream response, execute tool calls against `SimOpt.Engine.Api`, append
results, repeat until no tool calls; hard caps on iterations and optimize budgets. Avalonia side:
chat pane (streaming markdown), model/result pane, and later the live `SimulationCanvas` — MVVM
with the ViewModel owning the loop via `IProgress<>` like `IvotionOptimizationEngine` already does.

**Security/key issues to flag:**
- **Key storage:** OS-native only — DPAPI (Windows), Keychain (macOS), libsecret (Linux). Never a
  JSON settings file. .NET has no cross-platform first-party API for this; budget a small
  per-platform shim and encrypt-at-rest fallback with a machine-bound key.
- **Keys in memory/logs:** keep out of logs and crash reports (interacts with item 11's crash
  reporting — scrub before upload).
- **Data egress:** the user's model of *their factory/clinic* goes to their chosen LLM provider.
  Needs a plain-language privacy statement and a prominently supported local-model path (Ollama/
  LM Studio) as the confidentiality answer. This is a selling point, not just compliance.
- **The BYO-key premise conflicts with the target user** — an ops manager has no Anthropic key and
  doesn't want one. See §F, product risk #1.
- Prompt-injection surface is small (all tools are local), but `optimize` is a CPU DoS if unbounded
  — enforce evaluation/time budgets server-side, not just in the prompt.
- App-level license enforcement can be modest; the moat is trust + iteration speed, not DRM.

---

## E. Build order — vertical slices to a sellable v1

One AI developer, busy human reviewer. Wall-clock estimates include review latency (which will
dominate), not just agent time. Each slice ends demonstrable.

| Slice | Content | Demo / exit criterion | Est. |
|-------|---------|----------------------|------|
| **0. Trustworthy engine** | SIM-56 + SIM-57 + SIM-58, strictly TDD; plus the `GetHashCode` seed fix in `ModelRegistry` | Same-seed reproducibility across process restarts; distribution moment/GoF tests green; reset-path divergence test green | 1–1.5 wk |
| **1. Statistics subsystem + analytic gate** | Engine-level tally & time-weighted collectors wired into Buffer/Server/Sink; warm-up truncation; replication runner with CIs; **M/M/1 + M/M/c vs closed-form as CI tests** (gap #3, #10) | `dotnet test` proves Wq/Lq/ρ within CI of textbook formulas — this is also the marketing artifact | 1.5–2 wk |
| **2. Schema v1 + experiment tools** | `station(servers=c)`, distribution objects, router, metrics declarations; `validate_model`, `run_experiment` (replications + scenarios), `patch_model`, persistence (save/load JSON); MCP integration tests (SIM-20) | **The clinic example end-to-end from Claude Code** — NL → model → validated → CI'd results → what-if scenario | 2 wk |
| **3. Generic optimization** | `TopologyProblem` + `optimize` tool + SIM-59 fixes (seeded operators, EA cloning, SA guard; PSO gets the explicit vector contract or ships disabled) | "Fewest exam rooms with wait <15 min" answered via one tool call, reproducibly | 1.5 wk |
| **→ v0.9 "Pro" ships here.** MCP server + Claude Code/Desktop/any MCP client. Early revenue from consultants/engineers; zero UI code. | | | **~6–7 wk in** |
| **4. Chat app shell** | Avalonia app, `Microsoft.Extensions.AI` loop, in-proc tool head, key storage, streaming chat + results table | Non-expert flow: paste key (or pick Ollama), describe clinic, read answer — no Claude Code anywhere | 2–3 wk |
| **5. Live viz unification** | `TopologyDefinition → VizTopology` mapping (AutoLayout exists); embed `SimulationCanvas`; run/pause from chat | "Watch your clinic run" — the demo-in-a-meeting killer feature from decisions.md, now for everyone | 1.5 wk |
| **6. Commercial plumbing** | License decision (repo currently has **no LICENSE**: default all-rights-reserved — decide proprietary core vs open-core *before* marketing drives eyeballs to the public repo), installer/signing, license keys, update channel, crash reporting (scrubbed), privacy note, docs + example gallery | Installable, purchasable beta | 2 wk |

**Honest calendar:** ~11–13 agent-weeks ⇒ **v0.9 pro-channel in ~2 months, consumer v1 beta in
~4 months, first real revenue realistically month 3–5** (pro channel) with the consumer product
earning meaningfully later. The classic failure mode here is skipping slices 0–1 because 2–5 demo
better. Resist: every week of UI built on wrong numbers is negative progress for a product whose
only asset is being right.

---

## F. Risks that could kill this

### Product risks (the sharpest ones)
1. **BYO-key contradicts the target user.** Ops managers and plant engineers do not have API keys
   and will not create one to evaluate your tool. BYO-key serves the *secondary* market (consultants,
   engineers — who mostly already live in Claude/Copilot and are served by the MCP head anyway).
   For the primary market you eventually need a bundled, metered key (you become a reseller with
   margin and abuse handling) or an explicit "your IT connects it once" enterprise story. Decide
   deliberately; don't let the architecture decide it for you. Mitigation: local-model support is
   partial cover, and v1 can target the pro market first — but then say so in the positioning.
2. **"LLM writes SimPy for free" is the real competitor.** Claude/GPT can already produce a working
   SimPy/salabim model from the clinic prompt at zero marginal cost. Your defensible deltas:
   validated engine + analytic benchmark battery (an LLM-written one-off has no such gate),
   structured validation with lint/pre-checks, the integrated optimize loop, live visualization,
   reproducibility, and no-code UX. Slice 1's benchmark gate is therefore *positioning*, not just QA.
3. **Wrong answer to a paying customer = product death.** One consultant makes a staffing
   recommendation off a SimOpt run that's wrong because of a sampler bug, and word-of-mouth in the
   small sim community does the rest. This is why slices 0–1 are non-negotiable and why every result
   should carry CIs and replication counts by default (also a differentiation vs. the one-off script).
4. **Solo/bus-factor + attention.** One human reviewer with a day job; the FMT/GridWorld research
   track (SIM-47..54) and Ivoclar demo obligations compete for the same attention. The product needs
   a protected lane or it becomes the fourth priority of four.

### Competitive landscape
- **Incumbents:** AnyLogic (multi-method, enterprise, actively adding AI-assistant features), Simio
  (process digital twins, has been marketing AI/LLM integration), FlexSim (3D, strong in
  manufacturing, now under Autodesk), Arena (Rockwell, aging but entrenched), Simul8 (mid-market,
  approachable). All cost $2k–$25k+/seat, all have decades of validation credibility and none will
  let "describe your factory in chat" stay uncontested — assume 12–24 months before an incumbent
  ships a credible LLM front-end. Your speed advantage is real but perishable.
- **Open source:** SimPy, salabim (free, Python, has animation and monitors), JaamSim, plus the
  "GPT + SimPy" pattern above. They cap your pricing for technical users.
- **Emerging "LLM+sim":** research prototypes and startups generating simulation models from text
  are appearing steadily; nobody owns the "chat-native DES for non-experts" niche yet. The window
  exists; it is not large.
- **Wedge that fits this codebase:** the decisions.md "spontaneous demo during the meeting" play —
  consultant-led, SMB, one queueing question, answer with CIs in an hour. That's a niche the
  incumbents' price points and the DIY stack's expertise floor both leave open.

### Technical risks
- **More debt below the waterline.** The 2026-07-05 review covered only three subsystems.
  `SimOpt.Learning`, `SimOpt.Logging`, `Network`/`Path` (2,000+ lines), `MobileEntity` are
  unreviewed; the same author-era patterns (the review's bug classes) likely recur there. Budget for
  a second review pass before anything unreviewed becomes load-bearing.
- **In-memory model state.** Registry dies with the process (`ModelRegistry.cs:20`); an MCP client
  reconnecting mid-analysis loses everything. Cheap fix (persist topology JSON + re-hydrate), do it
  in slice 2.
- **Single-entity-class engine assumptions.** Entity classes/attributes (schema v1.x) may cut
  deeper into `Server<TIn,TOut>` generics than expected; prototype early in slice 2 to de-risk.
- **Desktop distribution friction.** Code signing (EV cert for Windows SmartScreen), macOS
  notarization, per-distro Linux quirks — routinely underestimated; lands in slice 6 but buy the
  certificates early.
- **Avalonia + streaming-LLM UI polish.** Doable, but "consumer-friendly" is a high bar in Avalonia;
  keep v1 UI deliberately spare (chat + table + canvas) rather than chasing SaaS-grade chrome.
- **Licensing hygiene.** Public repo, no LICENSE file, framework built partly in an employment
  context with an employer-adjacent vertical (Ivotion) in-tree: get the IP/ownership story and an
  explicit license decision (proprietary, BUSL, open-core split) settled **before** commercial
  launch, not after a competitor forks or an employer lawyer asks.

---

## Condensed recommendation

Keep the codebase; it is a sound skeleton with cataloged, fixable debt. Execute in this order:
**(0)** pay the SIM-56..59 debt test-first, **(1)** build the engine statistics subsystem and prove
it against M/M/c closed forms, **(2)** grow the MCP schema to stations/distributions/routing with a
real validate→experiment→patch loop, **(3)** ship the generic `TopologyProblem` + `optimize` tool —
then you have a revenue-capable pro product on the MCP channel at ~2 months, and the Avalonia
BYO-LLM app becomes an additive consumer head rather than the critical path. The two decisions to
make now, because they shape everything: the **license/IP posture** of the public repo, and whether
the primary go-to-market is **pro-first (MCP, BYO-key natural)** or **consumer-first (bundled key
economics required)**.
