# SimOpt — Project Rules

## Workflow
- **Solo project** — no PRs. Merge branches directly to main.
- **TDD** — write failing tests first, then implement.
- **SimOpt owns simulation expertise.** Author all simulation-related content (knowledge files, skill patches, wiring docs) here and stage to `tmp/staged-*.md`. Hand off to cfg-agent-fleet only for integration (copying staged content into `~/.claude/` files).

## Project Structure
- **Code:** the `src/` table in the root `CLAUDE.md` (engine, optimization, mathematics, statistics, visualization, MCP server, domain projects).
- `tests/SimOpt.Tests/`: xUnit + FluentAssertions, one folder per `src/` project. The analytic battery (M/M/1, M/D/1, M/M/c, Jackson tandem against closed forms) is `Simulation/Statistics/AnalyticBatteryTests.cs`.
- `docs/needs/01_User-Needs.md`: user needs UN-001..036. Read it before building.
- `docs/commercial/`: architecture readiness review (slice plan, MCP tool design) and business strategy.
- `docs/plans/`: one design brief per backlog item, `YYYY-MM-DD-sim-NN-<topic>.md`, carrying the measured numbers.
- `docs/decisions.md`: owner decisions with rationale (monthly review, see session protocol).
- `docs/pending-*.md`: session handovers. They are tracked, so they are public.
- `tmp/`: gitignored. Third-party private material, such as a colleague's repository for SIM-105, lives only here.

## Build & Test Notes
- `dotnet` (SDK 10.0.102, tests target net9.0) is at `~/.dotnet/dotnet` and is **not on PATH** in Claude Code's Bash or in tmux jobs. Prefix commands with `export DOTNET_ROOT=$HOME/.dotnet PATH=$HOME/.dotnet:$PATH`.
- The full suite takes about 10 s warm (35 s before SIM-129). A fresh worktree's restore + build + test runs past the 120 s tool limit, so launch it through `tmux-launch.sh`.
- Baseline as of 2026-11-25: 1325 passed, 1 skipped. Python package: 22 passed (`env -C python python3 -m pytest -q`).

## Active Roster
- **Agents:** Explore (engine code search across `src/`), Plan (slice design, gap analyses), general-purpose (cross-file reviews and extraction). VoltAgent bundles `voltagent-core-dev`, `voltagent-lang` and `voltagent-qa-sec` are enabled in `.claude/settings.local.json`, so `voltagent-lang:csharp-developer`, `voltagent-lang:dotnet-core-expert`, `voltagent-qa-sec:code-reviewer` and `voltagent-qa-sec:debugger` are available.
- **Skills:** `simopt` (simulation-optimization problem solving), `test-driven-development`, `systematic-debugging`, `verification-before-completion`, `writing-plans`.
- **MCP:** serena (C# symbol navigation), github (`JeltzProstetnic`; this repo is public), context7 (.NET and Avalonia docs).

## Reference (load on demand, not at start)
- MCP catalog: `~/.claude/reference/mcp-catalog.md`
- Serena: `~/.claude/reference/serena.md`
- TDD protocol: `~/.claude/domains/software-development/tdd-protocol.md` (before any code change)
- Fleet SimOpt usage: `~/.claude/knowledge/simopt-ops.md`
- Critical code review: `docs/2026-07-05-critical-code-review.md`
- Analytic reference: `docs/2026-08-26-analytic-reference.md`

## Downstream Consumers
Engine behaviour changes reach these, so check them before changing a template's semantics:
- `~/furkansim` (private): sibling `ProjectReference`.
- `~/crucible`: a Python port of `SimOpt.GridWorld`, no binary dependency.
- A colleague's private platform builds SimOpt at a pinned commit and freezes reference runs on it (SIM-105). A change to a template's results moves those runs, so name it in the commit message.
