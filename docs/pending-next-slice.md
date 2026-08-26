<!-- Action: present -->
<!-- Tracked-by: SIM-65, SIM-66 -->
# Slice 1 is closed. Slice 2 is next, and it is the one the product cannot ship without.

Slice 1 (SIM-63 statistics subsystem, SIM-64 analytic battery, SIM-81 RNG conformance) is done.
The engine now measures correctly, is checked against closed-form theory on every commit, and its
default generator is the algorithm it claims to be. **Nothing in the engine is known-wrong.**

## What is next

Both remaining Slice 2 items are **P0** and neither has been started:

| Item | What | Why it blocks |
|---|---|---|
| **SIM-65** | Topology schema v1 — multi-capacity stations, a distribution object (exponential/triangular/uniform/lognormal/gamma/constant/empirical), routers, declared metrics, published JSON Schema | Today's schema has four node types, exponential only, single-capacity servers. **It cannot express the product's own flagship example.** Serves UN-001, UN-004. |
| **SIM-66** | MCP tools `validate_model` / `patch_model` / `run_experiment` — three-layer validation, all errors returned at once, scenario arrays in one call, result payloads under ~400 tokens | This is where SIM-63's statistics finally reach a user. Serves UN-003, UN-008, UN-013, UN-023. |

SIM-65 comes first: SIM-66's `validate_model` validates against the schema SIM-65 defines.

## Read before starting

- `docs/needs/01_User-Needs.md` — the UN document, per the standing rule.
- `docs/commercial/2026-08-25-architecture-readiness-review.md` §C carries the MCP tool design.
- `docs/plans/2026-08-26-sim-64-analytic-battery.md` — if SIM-65 adds distributions, **each new
  sampler needs a goodness-of-fit test in `GoodnessOfFitTests` against its own fully specified
  CDF**. The battery cannot see a wrong sampler shape; that file can, and the instrument already
  exists.

## Also open, ruled on 2026-08-26 but not started

- **SIM-92** (P1) visualization computes five unsound statistics — replace all five with reads of
  the engine collectors, which now exist.
- **SIM-93** (P1) Ivotion KPIs — owner ruled **fix, do not retire**, accepting that the tab is
  currently unreachable from `MainWindow.axaml`.
- **SIM-98** (P2) the other three random sources have never been checked against any reference.
- **SIM-96** (P2) nightly analytic gate — and note it now has a second reason to exist: the
  old-vs-new RNG comparison left an unresolved question about cross-stream correlation that only a
  coverage meta-test can settle.
