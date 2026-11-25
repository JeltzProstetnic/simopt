<!-- Action: await-user-decision -->
<!-- Work: stage=findings-written; status=blocked; next=SIM-65 step 2 (node vocabulary), then steps 3-6, SIM-126's schema part and SIM-110, while MG owes Q-PRIV-RULE and Q-PRIO; approved=2026-11-24 MG (Q-PIN-COMMENT, SIM-124) -->
<!-- Tracked-by: SIM-122, SIM-123, SIM-124, SIM-125 -->
# SimPy-gap section: SIM-124, 125, 127, 128, 129, 131 done, SIM-126 slices 1-6 done, SIM-65 step 1 done; next SIM-65 step 2

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

## Decisions for MG (stable IDs; recommended option first)

- **Q-PRIO**: priorities of the two items still unset (SIM-124 is done, SIM-125 is P0 since 2026-11-25).
  **Recommended: accept as proposed**: SIM-122 P2, SIM-123 P1 (private material was lost). The
  alternative is to set them yourself.
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
   **SIM-65 step 1 done the same night** (`6637d1c`, `ParamValue`; suite 1091 passed, 1 skipped).
   **Next, in order:** SIM-65 step 2 (node vocabulary, `docs/pending-sim-65-continuation.md`), then SIM-126's schema part,
   SIM-110 and SIM-130. Older plan, kept for reference: SIM-126's remaining slices (waiting on a template's event, a random-draw helper per
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
