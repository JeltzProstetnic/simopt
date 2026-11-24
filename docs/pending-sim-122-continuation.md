<!-- Action: await-user-decision -->
<!-- Work: stage=findings-written; status=blocked; next=MG answers Q-PRIV-RULE and Q-PRIO; then SIM-65 step 1; approved=2026-11-24 MG (Q-PIN-COMMENT, SIM-124) -->
<!-- Tracked-by: SIM-122, SIM-123, SIM-124, SIM-125 -->
# SIM-122 is answered and SIM-124 is fixed; MG's decisions come next, then SIM-65 step 1

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

- **Q-PRIO**: priorities of the new items (SIM-124 is already done). **Recommended: accept as proposed**: SIM-122 P2,
  SIM-123 P1 (private material was lost), SIM-124 P2, SIM-125 P2. The alternative is to set them
  yourself.
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
- **Q-PROC**: a process-style authoring layer (C# iterators or `async` processes over the event engine,
  as SimPy models are written).
  - **Defer until SIM-65 ships (Recommended).**
  - **Plan it now** as its own item.
  - **Never.**
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
4. Then the SIM-105 order: SIM-65 step 1 (`ParamValue`), then SIM-110.
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
