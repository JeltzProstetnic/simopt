<!-- Action: reference -->
<!-- Tracked-by: SIM-63, SIM-64, SIM-65, SIM-66, SIM-68, SIM-69, SIM-87 -->
# Commercial track — working handoff

Established 2026-08-25. This is the standing work order for autonomous sessions on SimOpt.
It says what to do next and, more importantly, what not to do.

> **Status lives in `backlog.md`, not here.** Everything below is method and rationale. Item
> state (open / in-progress / done) is authoritative in `backlog.md` alone. Demoted to
> `reference` on 2026-08-26 after every item it carried was promoted to the backlog.

## Read first
1. `docs/needs/01_User-Needs.md` — the vision and the 32 needs everything traces to.
2. `docs/commercial/2026-08-25-architecture-readiness-review.md` — §C is the MCP tool design,
   §E is the slice plan. §A is the honest state of the codebase.
3. `backlog.md`, section **P0 — Commercial Track**.

## Slice 0 is CLOSED (2026-08-25)

SIM-56, SIM-57, SIM-58 and SIM-62 are all done, test-first, with zero regressions. The suite went
from 694 to 809 passing. The engine no longer produces knowingly wrong numbers, and reset-and-
re-run is deterministic. **This was the precondition for everything else.**

## Immediate queue

| Order | Item | Why now |
|---|---|---|
| 1 | **SIM-63** Output-statistics subsystem | The largest genuinely new engineering item in the product. No waiting-time tallies, no time-weighted stats, no warm-up, no replications exist today; utilisation is polled in the UI layer (`SimulationCanvas.cs:297-332`) and is unreachable from headless or MCP runs. An operations manager asking "how long do people wait?" currently gets no answer at all. Serves UN-011/UN-012. |
| 2 | **SIM-64** Analytic benchmark battery | M/M/1, M/M/c, M/G/1 against closed form as a CI gate. Simultaneously the quality gate and the strongest marketing asset available — an LLM-written one-off script has no such proof. Serves UN-007. Do it immediately after SIM-63 so the statistics are validated as they land. |
| 3 | **SIM-65 / SIM-66** Schema v1 + experiment tools | Multi-capacity stations, distribution objects, routing, then `validate_model` / `patch_model` / `run_experiment`. Today's schema cannot express the product's own flagship example. |
| 4 | **SIM-68 / SIM-69** `TopologyProblem` + `optimize` | The keystone. Turns every MCP-built model into an optimisable one and kills hand-written per-domain `IProblem`s. |

Then **v0.9 ships on the MCP channel** — no UI code required, and the earliest possible test of
whether anyone actually pays.

**Note the ordering constraint added by D-02 (local-first):** SIM-83 (schema-constrained decoding)
depends on SIM-65, and SIM-84 (reference-model benchmark) is the Month-3 kill gate. Build SIM-84
early enough to fail cheaply rather than discovering at month three that no local model can drive
the tool surface.

## Method — non-negotiable
- **Test-first, always.** Every finding in the 2026-07-05 review is a *hypothesis*; the failing
  test is what confirms it. Two findings this session turned out to be worse than reported and two
  more were not in any review at all — both only surfaced because tests came first.
- **Prefer a deterministic test to a sampled one.** MersenneTwister's defect is a 2⁻³¹ event and no
  sampling loop would ever have gated it; extracting the pure mapping made it a boundary test.
- **Re-run the full suite after every change to a random stream.** Baseline is 790 pass / 1 known
  fail (`IvotionOptimizationViewModelTests.Defaults_MatchLockedInDecisions`, SIM-55) / 1 skip.
- **Commit per backlog item**, with the finding and its confirmation in the message.

## Beachhead — SETTLED 2026-08-26 (SIM-87 closed)

The owner answered: **forensic casework is the proving ground; manufacturing stays the public
face; a deliberate forensic go-to-market is revisited only once the engine has survived real case
use.** Full reasoning in `docs/decisions.md` **D-06**.

Two obligations this places on every future session:

- **No marketing, landing-page, demo or social copy may state or imply that SimOpt output is
  court-admissible** until the admissibility literature has actually been researched. That work is
  unpriced and deliberately unscheduled; it becomes urgent the moment anyone wants to use the word
  "evidence" in public.
- **Do not re-open the beachhead** on the strength of the forensic origin. That argument has been
  made and answered.

## Owner decisions — SETTLED 2026-08-25

Both previously-open questions are decided. See `docs/decisions.md` D-01..D-04.

1. **Licensing (D-01):** FSL-1.1-ALv2 on the engine, proprietary on the app. `LICENSE.md` shipped.
   Note the recorded correction: FSL permits *any purpose except a Competing Use*, so commercial
   and consultant use of the engine is free — the paid value must live in the application layer.
2. **Model access (D-02):** local-first, cloud optional. No need may assume the user holds an API
   key. Spawned SIM-82 (runtime detection + hardware floor), SIM-83 (schema-constrained decoding),
   SIM-84 (reference-model benchmark = the Month-3 kill gate).
3. **Priorities (D-03):** as proposed.
4. **Employer boundary (D-04):** positioning does not lead with dental; `SimOpt.Ivotion` code
   stays public per FSIM-03, which is explicitly *not* reversed.

5. **Legal basis (D-05): CLOSED.** Both previously-outstanding owner actions are resolved. The
   dissertation code is entirely the owner's (SIM-85). The employer-boundary question dissolves
   rather than being satisfied: he holds a separately constituted practice as a **gerichtlich
   beeideter Sachverständiger**, software he creates falls within that Gutachtertätigkeit, and
   commercialisation is an extension of an existing business at his discretion (SIM-79). The
   business review's conflict-of-interest analysis (§D) assumed the ordinary employee-side-project
   structure and is **superseded** — do not re-apply it.

**No owner actions are outstanding.** The only thing waiting on the owner is the SIM-87 decision
above, and that is a strategic choice, not a blocker.

## Do not do
- Do not start UI work before Slice 1 is green. Every week of interface built on wrong numbers is
  negative progress for a product whose only asset is being right.
- Do not touch the FMT/GridWorld track (SIM-47..54). It shares the repository but not the product,
  and crucible has forked that domain logic to Python (SIM-61).
- Do not wire new distributions into the MCP schema ahead of their correctness fixes — that
  industrialises the bug instead of fixing it.
