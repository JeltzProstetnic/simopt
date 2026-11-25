<!-- Action: await-user-decision -->
<!-- Work: stage=findings-written; status=open; next=read MG's answers, then SIM-117 or SIM-86 while SIM-126 schema, SIM-110 rest and SIM-93 defaults wait for Q-PROC-SCHEMA Q-VIEW-PATH Q-IVO-STATS - MG owes Q-PROC-SCHEMA Q-VIEW-PATH Q-IVO-STATS Q-AUTO Q-STORE Q-SRC-START Q-PRIV-RULE Q-PRIO; approved=2026-11-24 MG (Q-PIN-COMMENT, SIM-124) -->
<!-- Tracked-by: SIM-122, SIM-123, SIM-124, SIM-125, SIM-126, SIM-110, SIM-93 -->
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

## Decisions for MG (stable IDs; recommended option first)

- **Q-STORE** (new 2026-11-25, restart #4, SIM-67 D4): should the server keep every model in a directory and
  reload it when it starts ("re-hydrate on reconnect" in the old backlog text)?
  - **Not now (Recommended):** `save_model`/`load_model` already let a model outlive the process (UN-005);
    a store changes what a plain server writes to disk. Revisit with the desktop app (SIM-71).
  - **Opt-in:** an environment variable `SIMOPT_MODEL_STORE` names the directory; unset, nothing changes.
- **Q-PROC-SCHEMA** (new 2026-11-25, restart #5; brief `docs/plans/2026-11-25-sim-126-process-schema.md`,
  with the car wash written as a document): which shape processes take in the topology schema, the
  contract the MCP tools and the Python package inherit. Building SIM-126's last part waits for this.
  - **Process node with a closed step list (Recommended):** `resource` and `stock` become nodes, and a
    `process` node runs each item through `delay`, `seize`, `release`, `take`, `put` and `wait_until`
    steps; an unfed process repeats its steps beside the flow (the supplier). Fixed with it: no loops or
    conditions inside a process; an item leaving a process frees every unit it holds there (no leak, but no
    holding a unit across nodes); patience on `seize`/`take` sends the item out by a named outlet; step values
    are drawn on entry (measured on the car wash: no worse than at the step); schema version stays "1". Answer
    "yes", or name the piece to change.
  - **Flowchart blocks** (Arena style): `seize`, `release`, `take`, `put` as graph nodes; a unit can be held
    across nodes, but an activity needs three to five nodes and a forgotten release shows up as a deadlock.
  - **Scripted processes:** loops, conditions and variables in JSON; reaches the most models, loses the
    offered-load pre-check, a plain read-back and SIM-83's grammar.
- **Q-VIEW-PATH** (new 2026-11-25, restart #5; brief `docs/plans/2026-11-25-sim-110-one-model-path.md`): how the
  viewer builds its models, the rest of SIM-110 (one model path, JSON and MCP models in the viewer). Its pause,
  cycle-safe layout and engine-measured numbers are done.
  - **A shared library (SIM-70's `SimOpt.Engine.Api`), every viewer model through the schema builder
    (Recommended):** the topology layer moves out of the MCP server (namespaces change, JSON and tools do not);
    SIM-72's reverse mapping is dropped; `VizTopology` presets and
    furkansim's glass line convert to schema documents plus a layout overlay; the schema gains `normal` arrivals
    and a `batch` station for the Roland printer. You will see: reject decisions follow the seed (the same run
    every time, other numbers than today), a queue drawn where a server fed a server directly, and furkansim's
    glass-line numbers move (inbox item).
  - **The viewer references the MCP server project directly:** one line, but the MCP SDK and the host ride into
    the viewer and furkansim's app.
  - **Two paths:** presets keep their own builder, JSON and MCP models use the schema builder; SIM-99's viewer
    half stays open.
- **Q-IVO-STATS** (new 2026-11-25, restart #5, SIM-93): how long the Ivotion optimiser runs each configuration.
  Measured on the baseline (1 printer, batches of 15): the first batch ships at about 84 minutes, then one every ~83,
  a steady ~10.8 pieces/h. A score counts whole batches in its window, so it can be off by one batch: today's
  120-minute run with no warm-up scores 7.5. 20 seeds gave the same score at every setting below except where the
  warm-up ends near a shipment, so replications would not help; run length does. A warm-up setting now exists
  (default 0).
  - **960 minutes (two shifts) with a 120-minute warm-up (Recommended):** the baseline scores 10.71, the same on all
    20 seeds; off by at most one batch, 1.1 pieces/h; each evaluation takes about eight times as long as today.
  - **480 minutes with a 120-minute warm-up:** scores 10.0, off by at most 2.5 pieces/h; four times as long.
  - **Keep 120 minutes, no warm-up,** and label the score as one batch cycle.
- **Q-AUTO** (new 2026-11-25, restart #4): which search "auto" runs when the space is larger than the
  budget. **Recommended: the EA (built, unattended choice):** best or tied at the default budget with the
  best worst case. Alternatives: random search (the step-3 behaviour), or SA as D6 planned (measured last).

- **Q-PRIO**: priorities of the items still unset (SIM-124 is done, SIM-125 is P0 since 2026-11-25).
  **Recommended: accept as proposed**: SIM-122 P2, SIM-123 P1 (private material was lost), SIM-132 P2
  (`Delay<T>` drops items; found 2026-11-25 night, done), and from the late-night run: SIM-133 P2 (done),
  SIM-134 P1 (done), SIM-135 P1 (done), SIM-136 P2, SIM-137 P1, SIM-138 P3, SIM-139 P1, and from restart #4
  SIM-140 P2 (constraints on decision variables). The alternative is to set them yourself.
- **Q-PIN-COMMENT: DECIDED and DONE.** MG via Telegram 2026-11-24 (#1660, replying to #1659):
  *"inform him."* The drafted comment was posted unchanged on his issue #30 at 22:14:59 UTC
  (comment id 7145993700). Next: watch #30 for his reply or a pin move.
- **Q-PRIV-RULE** (SIM-123, Meta-Rules step 2; the analysis subagent ran 2026-11-24). Do not adopt the
  cfg candidate as written: it keeps `tmp/` as the staging place, and the sweep proved that a race.
  - **Recommended:** cfg adds `inbox-file.sh --attach <file>`, which copies into
    `cfg cross-project/attachments/<project>/` (the folder exists and is trackable). After that ships,
    add to `.claude/CLAUDE.md`, Workflow, after "SimOpt owns simulation expertise": *"**Private material
    leaves the same session.** File every note, review or index you write about a colleague's private
    material as a cfg-agent-fleet inbox attachment in the session you write it, never into `tmp/` or
    any other untracked path."* Make two edits that remove the conflicts: in the Workflow bullet,
    "stage to `tmp/staged-*.md`" becomes "stage it to `docs/staged/`" (what commit 51cbf3c already
    did); in Project Structure, "`tmp/`: gitignored. Third-party private material … lives only here."
    becomes "`tmp/`: gitignored and swept after 14 days; it holds only re-obtainable copies of
    third-party material, such as a colleague's repository for SIM-105." Cost: about 45 tokens, loaded
    only in simopt sessions.
  - **Interim, until `--attach` exists:** the same rule, but the note waits in a gitignored path
    outside `tmp/`. It is still a single untracked copy.
  - **The cfg candidate as written.**
- **Q-SRC-START** (SIM-139, new 2026-11-25 late night): with `autoStartDelay: 0` a source makes its first item
  at t = 1 (one interval) on a freshly built model and at t = 0 after `Model.Reset`. Both paths must agree.
  - **First item at t = 0 on both paths (Recommended):** that is what every replicated run and every MCP run
    already does (they reset first), so no replicated result and none of his frozen reference runs move; only a
    caller running a freshly built model without a reset gets one item more.
  - **First item after one interval on both paths,** as SimPy sources usually do: moves every replicated result
    and his frozen runs (a comment on his issue #30 then).
- **Q-STAGED**: `docs/staged/manufacturing-simulation-modelling.md` and
  `shopfloor-management-and-governance.md` now live in the fleet (`global/knowledge/`, CFG-894).
  - **Delete the simopt copies (Recommended):** the fleet copy is canonical and two public copies will
    drift apart.
  - **Keep them** as the record of what simopt staged.
- **Q-PROC: DECIDED 2026-11-25.** MG made the SimPy gap top priority: process-style modelling is
  SIM-126 (P0) in the new top backlog section, with SIM-125, SIM-127..130 (`docs/decisions.md`, 2026-11-25).
- **Q-CIW**: use Ciw (MIT) as an independent cross-check in the analytic battery for networks without
  a closed form.
  - **File it at P3 (Recommended).**
  - **Skip it.**
- **Carried over from `docs/pending-sim-105-continuation.md`**, unchanged and still open:
  **Q-SFM9** (section 9 of the shopfloor file: public or private), **Q-PRIVHOME** (cfg or the ivoclar
  project for the colleague material), **Q-FMT** (the FMT Gridworld decision entry) and **Q-VALID**
  ("validation lives in tests" against SIM-65's validation design).

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
   **Next, in order:** read Telegram for MG's answers first. Then: SIM-126's schema build once Q-PROC-SCHEMA is answered;
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
   **Q-ASYNC** (new): the process layer uses C# iterators; an `async` front end over the same waits stays
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
