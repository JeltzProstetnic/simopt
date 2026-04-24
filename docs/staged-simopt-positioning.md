# Staged patch for ~/.claude/knowledge/simopt-ops.md

**Source:** SimOpt project decision, 2026-04-24. See `docs/decisions.md` →
"Agent-Driven Sim-Opt Positioning" for full rationale.

**Cross-project boundary:** staged here per SimOpt's CLAUDE.md rule
("SimOpt owns simulation expertise — author here, stage to tmp/staged-*.md,
hand off to cfg-agent-fleet for integration"). A cfg-agent-fleet session
should copy the block below into `~/.claude/knowledge/simopt-ops.md`.

---

## Integration instructions

Add a new **"Product Positioning"** section near the top of
`~/.claude/knowledge/simopt-ops.md`, directly after the "Load when:" line
and before "Detection Patterns". The section should read exactly as below.

---

## Product Positioning — Agent-Driven Sim-Opt

SimOpt is aimed at **non-experts using Claude Code or agent fleet with
this skill** to build preliminary or even final sim-opt systems. Deployment
modes include production use, research pre-studies, and throwaway what-if
analysis.

**This means:**

- **Do not propose import schemas or ETL layers.** When a user has data
  (CSV, spreadsheet, PDF, photograph of a whiteboard, spoken numbers in a
  meeting), YOU read it and call the builder directly. No user-facing
  "import screen", no schema contract — those re-introduce the expert
  configuration burden we exist to eliminate.
- **Prefer conversational parameterization over configuration UI.** The
  user says "four operators at packing"; you change `operators_pack` in
  the solution vector. The UI is for *seeing* results and *steering*
  (start/stop/apply-to-viz), not for entering parameter matrices.
- **Recognize domain patterns and reach for domain classes.** When a user
  mentions batching, fan-out, rejection, or any recurring physical
  behavior, map it to an existing template (e.g., `RolandPrinter`,
  `SimpleRejectServer`) or build a new one with tests. Don't just bolt
  params onto `SimpleServer`.
- **Test-driven even for throwaway models.** A failing test is the
  fastest way to validate that a spontaneously-assembled topology runs
  correctly. Tests also serve as evidence of what was built when the
  session ends. TDD is not an expert-only discipline here — write one
  test per topology stage, minimum.
- **The MCP server is the primary surface for agent automation**
  (SIM-19/20). When the user hands you a system description from outside
  the simopt repo, prefer MCP tool calls over direct file edits where
  possible.

**What is NOT negotiable regardless of positioning:**
- Discrete-event only (no ODE or continuous-time).
- Server→Buffer and Buffer→Server wiring patterns from the cookbook below.
- `createProduct: m => m[0]` for any pass-through server feeding a Buffer.

---

## Also: update test count

The old file says "Run tests (551)" in the Build Commands section. Current
count post-SIM-35 is **594 passing / 1 skipped**. Update the comment.

Old:
```bash
# Run tests (551)
~/.dotnet/dotnet test ~/simopt/tests/SimOpt.Tests/SimOpt.Tests.csproj
```

New:
```bash
# Run tests (594 pass / 1 skip as of SIM-35)
~/.dotnet/dotnet test ~/simopt/tests/SimOpt.Tests/SimOpt.Tests.csproj
```

---

## Also: mention the Ivotion preset in the Presets table

The VizTopology preset table in simopt-ops.md is already missing the
`IvotionPacking()` preset (added in a recent SimOpt session but not yet
integrated into the fleet knowledge file). Add this row:

| Ivotion Packing | `VizTopology.IvotionPacking()` | Source → Inspect → Buf → Roland (batch) → Pack → Label → SSB → Ship. Real Ivoclar production cell. |

---

## Integration note for the cfg-agent-fleet session

After applying these changes, delete this staged file from the SimOpt
project (`tmp/staged-simopt-positioning.md`). The existing
`tmp/staged-simopt-ops-patch.md` in the same directory covers a separate
set of earlier patches — leave it alone unless those are also integrated
during the same cfg session.
