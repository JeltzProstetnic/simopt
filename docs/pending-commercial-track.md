<!-- Action: act -->
<!-- Tracked-by: SIM-58, SIM-62, SIM-63, SIM-64, SIM-73, SIM-80 -->
# Commercial track — working handoff

Established 2026-08-25. This is the standing work order for autonomous sessions on SimOpt.
It says what to do next and, more importantly, what not to do.

## Read first
1. `docs/needs/01_User-Needs.md` — the vision and the 32 needs everything traces to.
2. `docs/commercial/2026-08-25-architecture-readiness-review.md` — §C is the MCP tool design,
   §E is the slice plan. §A is the honest state of the codebase.
3. `backlog.md`, section **P0 — Commercial Track**.

## Immediate queue

| Order | Item | Why now |
|---|---|---|
| 1 | **SIM-58** DES reset-path fixes | `Delay.Reset` never re-schedules its initial item, so evaluation #1 and #2 of the same model silently diverge — and reset-and-re-run is exactly the loop `IProblem.Evaluate` and the MCP `run_simulation` tool depend on. Optimisation results are untrustworthy until this is closed. |
| 2 | **SIM-62** Stable per-node seeding | `node.Id.GetHashCode()` is randomised per process, so the same topology and seed give different streams after a restart. Breaks UN-009 outright. Small, cheap, blocking. |
| 3 | **SIM-63** Output-statistics subsystem | The largest genuinely new engineering item in the product. No waiting-time tallies, no time-weighted stats, no warm-up, no replications exist today; utilisation is polled in the UI layer and is unreachable from headless runs. Serves UN-011/UN-012. |
| 4 | **SIM-64** Analytic benchmark battery | M/M/1, M/M/c, M/G/1 against closed form as a CI gate. Simultaneously the quality gate and the strongest marketing asset available. Serves UN-007. |

Then Slice 2 (schema v1 + experiment tools) and Slice 3 (`TopologyProblem` + `optimize`), after
which **v0.9 ships on the MCP channel** — no UI code required, and the earliest possible test of
whether anyone actually pays.

## Method — non-negotiable
- **Test-first, always.** Every finding in the 2026-07-05 review is a *hypothesis*; the failing
  test is what confirms it. Two findings this session turned out to be worse than reported and two
  more were not in any review at all — both only surfaced because tests came first.
- **Prefer a deterministic test to a sampled one.** MersenneTwister's defect is a 2⁻³¹ event and no
  sampling loop would ever have gated it; extracting the pure mapping made it a boundary test.
- **Re-run the full suite after every change to a random stream.** Baseline is 790 pass / 1 known
  fail (`IvotionOptimizationViewModelTests.Defaults_MatchLockedInDecisions`, SIM-55) / 1 skip.
- **Commit per backlog item**, with the finding and its confirmation in the message.

## Owner decisions outstanding
These block later slices, not the immediate queue. Do not decide them by implementation.

1. **UN-031 / SIM-73 — licensing instrument.** Repo is public with no LICENSE, so all rights are
   reserved by default and nothing has been given away. Owner chose open-core in principle; the
   instrument is undecided. The business review recommends *against* a permissive engine licence
   and proposes FSL (source-available, converting to Apache-2.0 after two years). Legally
   consequential and irreversible in practice. Blocks public launch.
2. **UN-032 / SIM-80 — model-access economics.** Bring-your-own-key gives ~95% gross margin but
   the primary target user has no API key and won't obtain one. Options: sell to key-holding
   personas first, bundle a metered key at a premium tier, or make local models the default path.
   Blocks the desktop app's onboarding design.

## Do not do
- Do not start UI work before Slice 1 is green. Every week of interface built on wrong numbers is
  negative progress for a product whose only asset is being right.
- Do not touch the FMT/GridWorld track (SIM-47..54). It shares the repository but not the product,
  and crucible has forked that domain logic to Python (SIM-61).
- Do not wire new distributions into the MCP schema ahead of their correctness fixes — that
  industrialises the bug instead of fixing it.
