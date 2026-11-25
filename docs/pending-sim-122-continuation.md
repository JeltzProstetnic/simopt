<!-- Action: await-user-decision -->
<!-- Work: stage=building; status=open; next=SIM-110 step 4 (canvas on the schema builder via VizSchema, SimulationModel deleted, File-Open with validation on the nodes, layout block, warm-up state; look at it on screen with MG), then step 5 and SIM-86 slice C; approved=2026-11-25 MG (all 17 open decisions) -->
<!-- Tracked-by: SIM-122, SIM-123, SIM-124, SIM-125, SIM-126, SIM-110, SIM-93, SIM-86 -->
# SimPy-gap section: SIM-124..132, SIM-126 slices 1-6, SIM-65, SIM-66, SIM-68, SIM-69, SIM-59, SIM-130, SIM-67 and SIM-118 done; SIM-126's schema, SIM-110's rest and SIM-93's defaults wait for MG (Q-PROC-SCHEMA, Q-VIEW-PATH, Q-IVO-STATS)

This repository is public. The colleague and his repository stay unnamed here; his specifics are in
the private hand-off (see *Private material*).

## Session 2026-11-24 (WSL, game mode: no GPU, CPU light)

- **The answer.** SimPy has neither an optimiser nor statistics. On a breakdown-heavy M/G/1 case with
  100 replications, current SimOpt and SimPy give the same interval (±1.9 % and ±2.0 %), while
  `b171eb8`, the commit downstream builds still pin, gives ±16.7 % with a +10 % bias. The cause is
  SIM-115 (one repair draw per replication), plus SIM-106's lost items, and both were fixed on
  2026-11-03. Brief, harness and raw results: `docs/plans/2026-11-24-sim-122-simpy-evaluation.md`
  and `docs/plans/2026-11-24-sim-122/`.
- **New engine items:** SIM-124 (every entity stays in `Model.Items`, live heap +1.7 MB per
  replication, measured) and SIM-125 (`SeedID` by default). Two ideas were folded in: SIM-118 item 4
  (a stock primitive) and SIM-117 (zero-delay scheduling).
- **Found:** the fleet's 14-day `tmp-sweep.sh` deleted all of `tmp/` on 2026-11-19, including
  SIM-105 private files that existed nowhere else (SIM-123). cfg placed the new private file the same
  night and tracks the backup check as CFG-1112.
- Inbox drained: the SimPy [work] item became SIM-122, the [rule] item became SIM-123 (Q-PRIV-RULE),
  the [fyi] items were read (main's CI is green) and deleted.

## Session 2026-11-25 night, restart #4 (WSL, unattended, cfg caretaking)

- **SIM-68 step 4 and SIM-59 done** (`3f16519`..`9bb647c`, suite 1325 passed, 1 skipped). PSO reads
  `IVectorSolution`; SA ends under any schedule; the EA bred from its WORST members (generation sorted
  ascending) and now breeds from the best; mutation on copies; every solution's moves are seeded
  (`SplitMix64`), and a clone splits its generator because a copied state froze SA (2 distinct proposals in
  300). `optimize` runs `sa`, `ea`, `pso`; `Optimizer.Run` has a search seed apart from the replications'.
- **Measured the four searches** (fixed replications, 15 search seeds, four clinic spaces): SA last on every
  larger space, the EA best or tied at the default budget (170 points: optimum 12/15 against random 2/15).
  So "auto" now runs the EA on spaces larger than the budget (D6 had planned SA). Table in the brief.
- **SIM-130 done** (`7565faf`, `986d073`, `f6ff031`): `python/` holds the `simopt` package (no required
  dependency; its own small MCP client over stdio; `Topology` builder; `Model.run_experiment`/`optimize`;
  results as rows and pandas frames). Acceptance against the real server: the clinic validates, a flu
  scenario becomes a frame, `optimize` answers 7 rooms. pythonnet in process works only on the .NET 10
  runtime (the build's System.Text.Json 10 is shadowed by .NET 9's own), documented in `python/README.md`.
- **SIM-67 done** (`ad30ba2`): `save_model`, `load_model`, `export_model`; a model saved and reopened by
  another server gives byte-identical results; Python `Model.save`/`export`, `Server.load_model`.
  Suite 1331 passed, 1 skipped; Python 23 passed (`env -C python python3 -m pytest -q`).
- **Filed:** SIM-140 (constraints on decision variables, UN-015's headcount limits); a furkansim inbox
  item (its Glass EA uses the default configuration, so its results change, and `GlassSolution` has the
  same wall-clock operators). No comment on his issue #30: his optimiser is his own Python code.

## Session 2026-11-25 early morning, restart #5 (WSL, unattended, cfg caretaking)

- **SIM-126's schema brief** (`95cb253`, `docs/plans/2026-11-25-sim-126-process-schema.md`): processes in the topology
  JSON as `resource` and `stock` nodes plus a `process` node with a closed step list; the car wash written as a document;
  measured on the C# car wash that drawing step values on entry and at the step give the same interval for the 2-3 bay
  difference (common random numbers halve it). Building waits for **Q-PROC-SCHEMA**. Telegram #1678.
- **SIM-110 in part:** SIM-108 cycle-safe layout (`c52c0a1`), SIM-107 real pause through `ISimulationSurface`
  (`0157dd2`; furkansim inbox item for its copy of the Space handler), SIM-92 every number on screen from the engine's
  collectors (`ea287b1`). Not looked at on screen. The rest waits for **Q-VIEW-PATH** (brief
  `docs/plans/2026-11-25-sim-110-one-model-path.md`, `965b058`, `80cfc57`).
- **SIM-93 in part** (`a5f1906`): Ivotion runs end exactly on time and report a run cut short, throughput over the time
  simulated with an optional warm-up, paid against busy labour, configuration figures marked. Run length and warm-up
  defaults wait for **Q-IVO-STATS** (measured options in it).
- **SIM-118 done** (`dbb8ea0`): `BootstrapDistribution`; the buffer's refusal hook and full event documented.
- **Housekeeping** (`81d2bbf`): `docs/pending-commercial-track.md` retired, its standing rules in `docs/decisions.md`.
- Suite 1361 passed, 1 skipped (`dotnet test tests/SimOpt.Tests/SimOpt.Tests.csproj`). Weekly ultra at 23 % when last read.

## Session 2026-11-25 early morning, restart #6 (WSL, unattended, cfg caretaking)

- **SIM-86 slices A and B done** (`b34ebcc`, `fb13866`; brief `docs/plans/2026-11-25-sim-86-provenance.md`):
  results name the model by the SHA-256 of its saved document (`model_sha256`, line feeds only so it is the same on
  every OS), list every value they assumed (`assumed`: warm-up, seed, replications, default metrics, node defaults by
  patch path) and echo each scenario's patch. `run_experiment(record: path)` writes a `simopt-run/1` record and
  `reproduce_run(path)` repeats it anywhere: a forged figure or a changed model is caught. Python follows. Suite 1378
  passed, 1 skipped; Python 24. No template result moved.
- Slice C (the expert-report form) waits for **Q-PROV-REPORT**. The limits of schema v1 (`5de51ce`) and the Python frame
  provenance (`cc25966`) are done too; open in SIM-86: slice C and records for `optimize`.
- **SIM-98 done** (`a0bec8f`): the LCG matches the JDK's `java.util.Random`, the subtractive generator matches Knuth's
  published algorithm (renamed from "Multiplicative"). **R250_521 was grossly biased** (top bit almost never set: 0.4 % of
  the first 1,000 doubles reached 0.5) and is fixed; nothing in SimOpt constructs it. Telegram #1681.
- **SIM-121 done** (`596ec4e`): an Ivotion cancellation always returns a cancelled result; the test no longer uses the clock.
- **SIM-36 done** (`1be72c6`): PSO validated on Sphere and Rosenbrock (five seeds each); `IvotionSolution` is an
  `IVectorSolution`; the Ivotion tab offers "Particle Swarm". Not seen on screen.
- **SIM-86 records for `optimize`** (`6927002`); refused with `max_seconds`.
- **SIM-96 done** (`a07e92a`): `AnalyticNightlyTests` + `.github/workflows/nightly.yml`; Wq 3.974 +- 0.0587, coverage 0.935,
  Welch plot in `docs/plans/2026-11-25-sim-96/`. **First scheduled run: 2026-11-26 02:17 UTC; check it went green**
  (it has not run on GitHub yet; locally the gate takes 150 s).
- **SIM-97 done, finite capacity** (`d689917`): M/M/1/K closed forms; the battery's M/M/1/K agrees on Wq, Lq, busy share,
  throughput and blocking (0.1256 +- 0.0019 vs 0.1260). The priority half moved to SIM-100 (no priority discipline yet).
- **Stopped here, idle by the caretaker's rule:** what is left waits for MG (the decisions below), carries behaviour MG has
  not specified (P1: SIM-38, SIM-70..72, SIM-76, SIM-82..84), or is SIM-94 (P3), whose WinForms/WPF examples cannot be
  looked at while MG is away.
- Suite 1439 passed, 4 skipped (3 nightly); Python 26. Telegram #1680 (SIM-86 + Q-PROV-REPORT), #1681 (SIM-98), #1682
  (Q-ENGINE-MOVES).

## Session 2026-11-25 morning (WSL, MG at the console after restart #6)

- **All 17 open decisions answered**, every one as recommended (`docs/decisions.md`, 2026-11-25 morning): Q-PROC-SCHEMA,
  Q-VIEW-PATH, Q-IVO-STATS, Q-ENGINE-MOVES, Q-PROV-REPORT, Q-AUTO, Q-STORE, Q-SRC-START, Q-PRIO, Q-PRIV-RULE, Q-STAGED,
  Q-CIW (SIM-141), Q-SFM9, Q-PRIVHOME, Q-FMT, Q-VALID, Q-ASYNC. cfg inbox item filed for `inbox-file.sh --attach`
  (Q-PRIV-RULE; simopt's `.claude/CLAUDE.md` rule follows when it ships).
- **SIM-139 done** (`a1a84c5`): first item at the auto-start delay on a fresh model too. **SIM-93 done** (`0a8377e`):
  960 minutes after a 120-minute warm-up; the baseline scores 10.71. Suite 1448 passed, 4 skipped; Python 26.
- **Next, in order:** SIM-126's process schema (P0; brief `docs/plans/2026-11-25-sim-126-process-schema.md`, decided
  shape in `docs/decisions.md`), then SIM-110's rest starting with SIM-70's extraction (brief
  `docs/plans/2026-11-25-sim-110-one-model-path.md`), then SIM-86 slice C (`report_run`, Markdown, English or German).
  SIM-117/95/119 wait for his SIM-122 results. Check the first nightly CI run (2026-11-26 02:17 UTC).

## Session 2026-11-25 morning, after the decisions (WSL, MG at the console)

- **SIM-126 done**, all six schema slices test-first (`120ed99`, `9ad8890`, `6295e50`, `9d47545`, `b12c848`, `a487e2b`):
  `resource`, `stock` and `process` nodes with `delay`, `seize`, `release`, `take`, `put`, `wait_until`; patience with
  outlets and the `share` metric; processes nothing feeds repeat beside the flow; resource offered load and
  `STOCK_RUNS_OUT`; step patch paths; Python `simopt.steps`. Measured: M/M/3 as a process Wq 1.098 +- 0.023 against
  Erlang-C 1.079; Erlang B 0.26711 +- 0.00116 against 0.26841; the brief's car-wash document agrees with the C# example
  on all four figures; 234 ns per event against the station's 205. Every number is in the brief's § Built.
- **SIM-142 found and fixed** (`69476ff`): no topology with two sources could be created (both named their items E1, E2
  ...; `TRIAL_FAILED`). P1 proposed; MG to confirm.
- Suite 1448 -> 1565 passed, 4 skipped; Python 26 -> 30. No template result moved, so nothing for his issue #30.
- **SIM-110 steps 1-3 done** (Q-VIEW-PATH): `80e3942` extracts `SimOpt.Engine.Api` (SIM-70's first half; the server keeps
  the tools, experiments, optimiser and run records); `3b525e0` adds the `normal` family (truncated at zero) and
  `batch` on a one-server station (every item of a batch passed on); `20f7206` adds `VizSchema.ToSchema`, every preset
  as a schema document plus layout. Both paths measured in `VizSchemaTests` (SQSS, Production Line unchanged; Factory
  waste 276 -> 241 because the reject follows the seed; Ivotion shipped 24 -> 354 because the Roland printer lost 14 of
  every 15). The viewer still draws through `SimulationModel` until step 4.
- **Step 4, next:** `SimulationCanvas.StartSimulation(VizTopology)` builds through `VizSchema` + `ModelRegistry.TryBuild`;
  node states and measures come from `ActiveModel` (buffers, servers, sinks, plus the inserted queues/routers, laid out by
  `AutoLayout`); `SimulationModel` is deleted with its tests ported (`SimulationModelMeasureTests`, `PlaybackTests`);
  File -> Open reads a topology JSON (an optional top-level `layout` block the builder ignores) and shows validation
  problems on the nodes; "warming up until t = ..." when the document declares a warm-up. furkansim's `MainWindow`
  keeps compiling (`VizTopology` and the canvas keep their members); file its inbox item with that commit (its glass
  line's numbers move). Suite 1600 passed, 4 skipped.

## Decisions for MG (stable IDs; recommended option first)

- **Q-SIM142-PRIO (open, 2026-11-25):** SIM-142 (no topology with two sources could be created; fixed in `69476ff`) was
  filed at **P1 (recommended)**; the alternative is P2. It is done either way, so the answer only sets its record.

- **Q-ENGINE-MOVES: DECIDED 2026-11-25 by MG at the console:** after his SIM-122 results arrive (`docs/decisions.md`, 2026-11-25 morning).
- **Q-PROV-REPORT: DECIDED 2026-11-25 by MG at the console:** a Markdown appendix via `report_run`, English or German; SIM-86 slice C is unblocked (`docs/decisions.md`, 2026-11-25 morning).
- **Q-STORE: DECIDED 2026-11-25 by MG at the console:** not now; revisit with SIM-71 (`docs/decisions.md`, 2026-11-25 morning).
- **Q-PROC-SCHEMA: DECIDED 2026-11-25 by MG at the console:** the process node with a closed step list; SIM-126's schema build is unblocked (`docs/decisions.md`, 2026-11-25 morning).
- **Q-VIEW-PATH: DECIDED 2026-11-25 by MG at the console:** a shared library, every viewer model through the schema builder; SIM-110's rest starts with SIM-70's extraction (`docs/decisions.md`, 2026-11-25 morning).
- **Q-IVO-STATS: DECIDED 2026-11-25 by MG at the console:** 960 minutes with a 120-minute warm-up; SIM-93's defaults are unblocked (`docs/decisions.md`, 2026-11-25 morning).
- **Q-AUTO: DECIDED 2026-11-25 by MG at the console:** the EA stays (`docs/decisions.md`, 2026-11-25 morning).
- **Q-PRIO: DECIDED 2026-11-25 by MG at the console:** accepted as proposed, set in the backlog (`docs/decisions.md`, 2026-11-25 morning).
- **Q-PIN-COMMENT: DECIDED and DONE.** MG via Telegram 2026-11-24 (#1660, replying to #1659):
  *"inform him."* The drafted comment was posted unchanged on his issue #30 at 22:14:59 UTC
  (comment id 7145993700). Next: watch #30 for his reply or a pin move.
- **Q-PRIV-RULE: DECIDED 2026-11-25 by MG at the console:** attach via cfg; cfg inbox [work] item filed for `inbox-file.sh --attach`; the `.claude/CLAUDE.md` rule and its two conflict edits are applied when it ships (SIM-123 stays open until then) (`docs/decisions.md`, 2026-11-25 morning).
- **Q-SRC-START: DECIDED 2026-11-25 by MG at the console:** first item at t = 0 on both paths; SIM-139 is unblocked (`docs/decisions.md`, 2026-11-25 morning).
- **Q-STAGED: DECIDED 2026-11-25 by MG at the console:** simopt's copies deleted (byte-identical to the fleet's) (`docs/decisions.md`, 2026-11-25 morning).
- **Q-PROC: DECIDED 2026-11-25.** MG made the SimPy gap top priority: process-style modelling is
  SIM-126 (P0) in the new top backlog section, with SIM-125, SIM-127..130 (`docs/decisions.md`, 2026-11-25).
- **Q-CIW: DECIDED 2026-11-25 by MG at the console:** filed as SIM-141 at P3 (`docs/decisions.md`, 2026-11-25 morning).
## Next concrete actions, in order

1. Present the decisions above, then those in `docs/pending-sim-105-continuation.md`.
2. Check his issue #30 for a reply to the 2026-11-24 comment and whether his SimOpt pin moved.
3. ~~SIM-124~~ **done 2026-11-24 on MG's order** ("fix that"): heap flat over replications, suite 987
   passed, 1 skipped.
4. **Top backlog section (MG 2026-11-25, "these are top priority").** Done unattended on 2026-11-25:
   SIM-125 (`59f8121`), SIM-128 (`7096bf5`), SIM-127 (`d91b2c3`), SIM-126 slices 1-3 (`a115f22`, `5810ae5`,
   `0a621f3`: processes, `Resource`, any-of/all-of, interrupts, withdrawal) and SIM-131, found on the way
   (model-level random streams shared one seed after `Reset`; `5810ae5`). Suite 1025 passed, 1 skipped.
   Comments on his issue #30 cover SIM-124, SIM-125 and SIM-126..131.
   **SIM-129 done 2026-11-25 (night, unattended):** `5cb47a1`..`576f156`, results byte-identical, 997 -> 179 ns per
   event, SIM-122 case 5.3 s -> 1.55 s (SimPy 2.77 s); brief `docs/plans/2026-11-25-sim-129-speed-per-event.md`;
   suite 1067 passed, 1 skipped. No comment on his issue #30 needed: no result moved.
   **SIM-126 slices 4-6 done the same night:** `e789e5a` (a process waits for the next raise of a template's event,
   `Wait.Next`), `6a228ef` (named random streams per process, `p.Random(name, distribution)`), `be58575` (the car wash
   example, `examples/SimOpt.Examples.CarWash`); suite 1081 passed, 1 skipped. Left of SIM-126: the SIM-65 schema.
   **SIM-65 step 1 and step 2a done the same night** (`6637d1c` `ParamValue`; `f738868` queue/station aliases,
   arrival/service distributions, discipline; suite 1102 passed, 1 skipped). Step 2b found **SIM-132** (`Delay<T>` holds
   one item and silently drops the rest; P2 proposed, **Q-PRIO** now covers it too).
   **2026-11-25 late night (unattended, cfg caretaking): SIM-132 and all of SIM-65 done** (steps 2b-6 and the
   clinic acceptance test; `e1c9899`..`ccaaf99`; suite 1196 passed, 1 skipped). Found and fixed on the way: SIM-133
   (entity auto-ids raced across threads), SIM-134 (the logger's dispatcher thread killed the test host about one
   run in 15; proven by 60 clean runs), SIM-135 (the MCP builder silently left source/station -> station links
   unwired; D3 corrected in `docs/decisions.md`), SIM-138 (flaky allocation tests; awaiting its 40-run proof).
   Filed, open: SIM-136 (suspected item loss in the visualization builder), SIM-137 (battery fixture NRE, 1 in 60),
   SIM-139 (a source's first item at t = 1 fresh, t = 0 after a reset). No comment on his issue #30 needed: no
   template result moved (a one-server station keeps its id and seed; `Delay` keeps capacity 1 by default).
   **2026-11-25 night (restart #3, unattended): SIM-66 done** in six commits, `1076b00`..(the SIM-66 close commit):
   `validate_model` (every fault at once with code, node and fix; structural lint; offered load per station from
   the traffic equations; a trial run on a copy), versions plus `patch_model`, and `run_experiment` (intervals per
   metric, scenarios at common random numbers with paired significance, flags, provenance; clinic 435 characters,
   with a flu scenario 948). Suite 1272 passed, 1 skipped. Found on the way: a push node wired to two nodes
   duplicates every item (now refused), a repeated connection crashed `create_model`, a full queue drops items
   without a trace, and SIM-139's first item on a fresh model is a phantom the generator never made (backlog).
   No comment on his issue #30 needed: no template result moved, and his platform does not use the MCP builder.
   Brief with all numbers: `docs/plans/2026-11-25-sim-66-validate-patch-experiment.md`.
   **Same night, later: SIM-68 steps 1-3 and SIM-69 done** (`228bcc5`, `28fb063`, `f128c0f`): `TopologyProblem`
   (decision variables as patch paths, common random numbers, a constraint kept only on its whole 95 % interval,
   cache, budget, seeded moves) and the `optimize` tool (auto/grid/random). The clinic's "fewest exam rooms keeping
   the mean wait under 15 minutes" answers 7 in one call. Measured on the way: on the mean, 5 replications passed
   6 rooms that 20 failed, which is why constraints use the interval. Suite 1290 passed, 1 skipped. Brief:
   `docs/plans/2026-11-25-sim-68-69-topology-problem-optimize.md`. SIM-137: a 90-run hunt found nothing (see its
   backlog entry for the count).
   **Restart #4 (same night): SIM-68 step 4 and SIM-59 done**, see the section above.
   **Later in restart #4: SIM-130 and SIM-67 done**, see the section above.
   **Restart #5 (2026-11-25, early morning): SIM-126's schema brief written** (`docs/plans/2026-11-25-sim-126-process-schema.md`;
   Q-PROC-SCHEMA above). It carries one measurement: on the C# car wash, drawing step values on entry and at
   the step give the same interval for the 2-3 bay difference, and common random numbers halve it.
   **Same restart, SIM-110 V1 in part** (test-first): SIM-108 cycle-safe layout (`c52c0a1`), SIM-107 Space pauses and
   resumes (`0157dd2`, furkansim inbox item for its copy), SIM-92 every number on screen is the engine's measure
   (`ea287b1`); suite 1348 passed, 1 skipped. Not yet seen on screen (no window opened while MG is away). The rest
   of SIM-110 (one model path) waits for **Q-VIEW-PATH** (brief `docs/plans/2026-11-25-sim-110-one-model-path.md`).
   **Later in restart #5:** SIM-93 in part (Q-IVO-STATS) and SIM-118 done, see the section above.
   **Restart #6:** SIM-86 slices A, B, the limits and Python provenance, SIM-98 and SIM-121 done, see the section above.
   **Next after restart #6, in order:** read Telegram for MG's answers first. Then whichever of SIM-126's schema,
   SIM-110's rest, SIM-93's defaults, SIM-86 slice C or SIM-95/117/119 his answers unblock. Unblocked meanwhile: SIM-36
   (done), SIM-96 (done), records for `optimize` (done), SIM-97 (done). Left unblocked: SIM-94 (P3, examples, needs the
   GUI examples looked at on screen). The P1 items still open (SIM-38, SIM-70..72, SIM-76, SIM-82..84) carry behaviour MG
   has not specified. SIM-20 still needs a Claude Code session with the server registered.
   Older next list (restart #5): read Telegram for MG's answers first. Then: SIM-126's schema build once Q-PROC-SCHEMA is answered;
   SIM-110's rest once Q-VIEW-PATH is answered (with option one it starts with SIM-70's extraction); SIM-93's defaults once
   Q-IVO-STATS is answered; SIM-140 once Q-PRIO sets it. Unblocked meanwhile, smaller: SIM-117 (P2, SIM-106 follow-ups),
   SIM-86 (P1, check what `run_experiment`'s provenance already covers before building). SIM-20 needs a Claude Code session
   with the server registered, so it waits for MG.
   Older next list (before restart #5): SIM-126's schema part (brief first), SIM-110, SIM-140.
   **Housekeeping DONE 2026-11-25 (restart #5):** `docs/pending-commercial-track.md` deleted; its standing rules
   that still apply are in `docs/decisions.md` (2026-11-25, restart #5).
   Older next list (before SIM-130): SIM-130, SIM-126's schema part, SIM-110, SIM-140.
   Older next list (before step 4): SIM-68 step 4 with SIM-59, then SIM-130, SIM-126's schema part, SIM-110.
   SIM-139 waits for **Q-SRC-START**; SIM-137 stays open until it recurs.
   Older next list (before SIM-69): SIM-137, SIM-69, SIM-126's schema part, SIM-110 and SIM-130.
   Older next list, kept: SIM-66, then SIM-137 and SIM-139, SIM-126's schema part, SIM-69, SIM-110 and SIM-130.
   Older next list, kept: SIM-132, then SIM-65 step 2b, then SIM-126's schema part, SIM-110 and SIM-130. Older plan, kept for reference: SIM-126's remaining slices (waiting on a template's event, a random-draw helper per
   process, an example in `examples/`), then SIM-130 after SIM-65/66. SIM-65 step 1 (`ParamValue`) and SIM-110
   follow, unless MG orders otherwise.
   **Q-ASYNC: DECIDED 2026-11-25 by MG: iterators only.** (Was: the process layer uses C# iterators; an `async` front end over the same waits stays
   possible. **Recommended: iterators only for now**; the alternative is to add `async` as well
   (`docs/plans/2026-11-25-sim-126-process-modelling.md`).
5. When his comparison results arrive, check them against the prediction in the private hand-off.
   SIM-122 closes when his failure-bearing cases show the wider spread at `b171eb8`, or when a rerun at
   `96f979c` or later shows the spreads match.

## Private material

The private hand-off is
`~/cfg-agent-fleet/cross-project/simopt-private/sim-122-confidence-intervals-2026-11-24.md`, committed
by cfg on 2026-11-24 (`5da18997`, CFG-1112; it is byte-identical to the staged
`tmp/staged-sim-122-private.md`). cfg's CFG-1112 also tracks restoring the swept SIM-105 files. The
clone of his repo is under `tmp/` (the path and URL are in the private file). Re-clone it from GitHub
if it has been swept.

## Recovery notes

- `dotnet` lives at `~/.dotnet/dotnet` and is not on PATH:
  `export DOTNET_ROOT=$HOME/.dotnet PATH=$HOME/.dotnet:$PATH`.
- The harness builds in about 5 s and 100 replications run in about 7 s. The `b171eb8` comparison needs
  a worktree (see the brief, *Reproduce*). This session's scratch worktree was removed.
