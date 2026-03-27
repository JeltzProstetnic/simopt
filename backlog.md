# SimOpt Backlog

## P1 — High Priority

- [x] **SIM-01** Fix EventScheduler.Remove last event (size: S) — Phase 3
- [x] **SIM-02** Fix RandomStrategy same-seed-every-iteration (size: S) — Phase 3
- [x] **SIM-03** Fix Vector.Equals NaN Z comparison bug (size: S) — Phase 3
- [x] **SIM-04** Implement PSO (Particle Swarm Optimization) as IStrategy (size: L) — Phase 4: ParticleSwarmOptimization with IParticleSwarmConfiguration, 11 tests
- [x] **SIM-05** Fix EventScheduler.Remove duplicate-priority edge case (size: M) — Phase 4: clone Priority keys + reverse index for identity-based removal, 3 tests

## P2 — Medium Priority

- [x] **SIM-06** Expand test coverage: Buffer template (size: M) — Phase 4
- [x] **SIM-07** Expand test coverage: Server template (size: L) — Phase 4 (integration tests deferred: ConnectTo type constraints)
- [x] **SIM-08** Expand test coverage: Conveyor template (size: M) — Phase 4
- [x] **SIM-09** Expand test coverage: more distributions (size: M) — Phase 4: Gaussian, NegExponential, Constant
- [x] **SIM-10** Expand test coverage: graph/pathfinding algorithms (size: L) — Phase 4: Dijkstra, Floyd-Warshall, adjacency
- [x] **SIM-11** Expand test coverage: matrix decompositions (size: M) — Phase 4: Cholesky, LU, QR roundtrip
- [x] **SIM-12** Add XML doc comments to public APIs (size: L) — Phase 5: IStrategy, ISolution, IProblem, EventScheduler, Priority, PSO, templates

## P2.5 — Platform (new)

- [x] **SIM-18** Avalonia 2D visualization — SQSS demo with live rendering, entity animations, speed controls
- [x] **SIM-19** MCP server scaffold — create_model, run_simulation, get_status, list_templates tools
- [ ] **SIM-20** MCP server integration testing — verify tools work end-to-end with Claude Code
- [x] **SIM-21** Agent fleet knowledge file — Phase 5: simopt-ops.md with detection patterns, workflow, presets, constraints
- [x] **SIM-22** 3D visualization research — Phase 5: Raylib-cs for prototype, Stride for production
- [x] **SIM-23** Generalize visualization — Phase 5: VizTopology JSON, auto-layout, topology selector, 3 presets

## P3 — Low Priority / Future

- [ ] **SIM-13** Nullable annotation cleanup (size: XL) — ~1690 warnings, systematic conversion
- [x] **SIM-14** PCA/KPCA tests (size: M) — Phase 5: 47 tests (transform/revert, variance, kernels)
- [x] **SIM-15** ROC curve analysis tests (size: M) — Phase 5: 28 tests (AUC, thresholds, DeLong compare)
- [x] **SIM-16** Example project modernization (size: L) — Phase 5: SDK-style csproj, SimOpt.* namespaces, console apps
- [x] **SIM-24** Fix CholeskyDecomposition.Solve() forward substitution bug — Phase 5
- [x] **SIM-25** Fix EigenvalueDecomposition null guard (throws wrong exception) — Phase 5
- [ ] **SIM-17** NuGet packaging (size: M) — publish core libraries as packages (deprioritized)
- [x] **SIM-26** Factory showcase Phase B: professional rendering (gradients, shadows, conveyor animation, SCADA look) (size: L) — Phase B: SCADA palette, SimpleRejectServer, 5 connection patterns, 4 headless tests
- [x] **SIM-27** Factory showcase Phase C: detachable controls, multi-monitor, keyboard shortcuts (size: M)
- [ ] **SIM-28** Factory showcase Phase D: optimization showcase (before/after, fitness chart, sensitivity heatmap) (size: L)
- [ ] **SIM-29** Ivoclar dental manufacturing showcase (size: L)
- [ ] **SIM-30** Icon/graphics library for domain-specific visuals (size: L)
- [x] **SIM-31** Statistics panel + polish: utilization bars per station, throughput, WIP, bottleneck highlighting, detachable stats window, S-key toggle (size: L)
- [x] **SIM-32** Realistic factory floor renderer: toggle between schematic and realistic 2D view (size: XL) — concrete floor, metallic machines, rack buffers, dock bays, belt conveyors, iso entities, R-key toggle
- [ ] **SIM-33** Realistic renderer v2: organic factory layout (non-grid), realistic node sizes, building features (walls, doors, corridors, pillars), walkways between stations (size: L)
- [ ] **SIM-34** Human agent entity: mobile worker that walks between stations, repairs damaged machines, unsticks conveyors, pulls items from machines/belts. Extends simulation primitives with pathfinding + task queue (size: XL)
