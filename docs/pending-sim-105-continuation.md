<!-- Action: reference -->
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

1. ~~Present the four decisions below~~ **All four DECIDED 2026-11-25 by MG** as recommended (`docs/decisions.md`, 2026-11-25 morning).
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

## Decisions

Q-SFM9, Q-PRIVHOME, Q-FMT and Q-VALID were decided on 2026-11-25, all as recommended; see `docs/decisions.md`.

## Recovery notes

- `dotnet` is at `~/.dotnet/dotnet`, not on PATH: `export DOTNET_ROOT=$HOME/.dotnet PATH=$HOME/.dotnet:$PATH`.
- Run anything that builds a fresh tree through `tmux-launch.sh`; it takes more than 120 s.
