<!-- Action: await-user-decision -->
<!-- Tracked-by: SIM-105, SIM-65, SIM-110, SIM-117 -->
# SIM-105 first pass is done; next is SIM-65 step 1, then visualization slice V1

This repository is public. The colleague and his repositories stay unnamed here. Everything
specific to him lives in the gitignored `tmp/` on WSL only (see *Private material*).

## Session 2026-11-03 (WSL): what landed

- **SIM-105 intake.** His platform already embeds SimOpt at a pinned commit, and he sent a review
  request addressed to MG. Knowledge was split by MG's rule ("generic knowledge which we could also
  have gotten from literature into public agent fleet, too specific things into cfg"). Two generic
  files are in `docs/staged/`; the private files are in `tmp/`. cfg inbox items to integrate both
  are filed, as are pdp items for the second app.
- **We contribute** (MG: "post issues to his repo where you found or will find any. we contribute.
  he is a friend"). 31 issues are on his repo: #1-#17 frontend and flow, #18-#29 his SimOpt host,
  #30 the SimOpt fixes (plus a comment that `fff06dd` will move his frozen `line` reference),
  #31 answers to his questions 1-5, 8 and 9, posted on MG's order. MG answers 6, 7, 10 and 11, and
  the licence question, to him personally.
- **Engine, test-first:** SIM-106 (`bb835a1`, follow-up `cb53d09`), SIM-115 and SIM-116 (`fff06dd`).
  The suite is **981 passed, 1 skipped**. SIM-121's Ivotion cancellation test is flaky under load
  (2 of 7 full runs).
- **Also done:** SIM-104, and the monthly `docs/decisions.md` review (archive created, 2026-11-03
  entry added). MG accepted the priorities for SIM-107..121.

## Explicit orders not yet delivered (MG's words)

- *"…we want the knowledge in our knowledge base and the visualization of simopt on that same level
  in general."* The knowledge is staged; cfg still has to integrate it. For the visualization there
  is a plan (`docs/plans/2026-11-03-sim-105-visualization-uplift.md`) and items SIM-107..114, but
  **nothing is built yet**. Next is SIM-110 (V1, honest live map).
- *"later on we want to combine forces with him to create a solution dedicated to ivoclar
  requirements and use cases."* This belongs to the ivoclar project, not this repository. It has
  not started.

## Next concrete actions, in order

1. Present the four decisions below (Q-SFM9, Q-PRIVHOME, Q-FMT, Q-VALID), after re-verifying each premise; first those in `docs/pending-sim-122-continuation.md`.
2. ~~SIM-65 step 1~~ SIM-65 and SIM-66 are done (2026-11-25); their briefs are in `docs/plans/`.
3. SIM-110 (visualization V1). Its first engine seam is `Instrumentation.ObserveThroughput` /
   `ObserveTimeInSystem`.
4. SIM-117 (SIM-106 follow-ups), then SIM-118 (empirical bootstrap distribution, multi-item delay),
   which both feed SIM-65.
5. Any engine change that moves template results gets a comment on his repo, because he freezes
   reference runs against a pinned SimOpt commit.

## Private material (WSL only, gitignored, never commit, never copy into this repo)

**Swept on 2026-11-19 (SIM-123).** The fleet's 14-day `tmp-sweep.sh` deleted all of `tmp/`, including
`tmp/PRIVATE-INDEX.md`, the intake note and the host review with the §8 answer drafts. cfg CFG-1112
tracks a restore from backup. Surviving copies of the private knowledge files, the visualization gap
analysis, the demo crawl and the SIM-122 hand-off are in `~/cfg-agent-fleet/cross-project/simopt-private/`.
His repo was re-cloned under `tmp/` on 2026-11-24 (the path is in the private SIM-122 file). The issues
we posted are on his repo, #1-#31.

## Open decisions (ask each with AskUserQuestion; recommended option first)

1. **Q-SFM9: Section 9 of the generic shopfloor-management file.** Premise: the home queue (sign-offs,
   decisions, escalations, overdue, due today) and the team-health columns in
   `docs/staged/shopfloor-management-and-governance.md` §9 closely mirror the demo's own design
   (the writer agent's note).
   - **Keep it public (Recommended):** it is ordinary dashboard practice, phrased generically.
   - **Move it to the private file** before cfg publishes.
2. **Q-PRIVHOME: Home of the private colleague material.** Premise: his fictional example company is a
   dental-paste maker (private knowledge file, see `tmp/PRIVATE-INDEX.md`); MG ruled cfg on 2026-11-03
   (`docs/decisions.md`).
   - **Keep cfg (Recommended):** it is his private work with synthetic data, not employer content.
   - **Route through the ivoclar project** instead.
3. **Q-FMT: The FMT Gridworld decision entry.** Premise: the decisions review kept it live although SIM-61
   says crucible forked the FMT logic to Python, while SIM-47..54 are still open and
   `src/SimOpt.GridWorld` exists.
   - **Keep it live until SIM-47..54 are triaged (Recommended).**
   - **Archive its ".NET over Python" and placement parts now.**
4. **Q-VALID: "Validation lives in tests, cheaper than schema validation"** (`docs/decisions.md`,
   Agent-Driven Positioning) against D-02's load-bearing validation layer (UN-008) and SIM-65/66's
   published schema with three-layer validation.
   - **Supersede it with the SIM-65 validation design (Recommended).**
   - **Keep both, and state where each applies.**

## Recovery notes

- `dotnet` is at `~/.dotnet/dotnet`, not on PATH: `export DOTNET_ROOT=$HOME/.dotnet PATH=$HOME/.dotnet:$PATH`.
- Run anything that builds a fresh tree through `tmux-launch.sh`; it takes more than 120 s.
