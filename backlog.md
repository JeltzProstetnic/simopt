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
- [ ] **SIM-09** Expand test coverage: more distributions (size: M) — Gaussian, NegExponential, Poisson, Erlang
- [ ] **SIM-10** Expand test coverage: graph/pathfinding algorithms (size: L) — Dijkstra, Floyd-Warshall, A*
- [ ] **SIM-11** Expand test coverage: matrix decompositions (size: M) — Cholesky, LU, QR, SVD
- [ ] **SIM-12** Add XML doc comments to public APIs (size: L) — systematic pass over all public types

## P3 — Low Priority / Future

- [ ] **SIM-13** Nullable annotation cleanup (size: XL) — ~1690 warnings, systematic conversion
- [ ] **SIM-14** PCA/KPCA tests (size: M) — transform/revert roundtrip, variance conservation
- [ ] **SIM-15** ROC curve analysis tests (size: M) — AUC calculation, threshold selection
- [ ] **SIM-16** Example project modernization (size: L) — update 6 example apps to current APIs
- [ ] **SIM-17** NuGet packaging (size: M) — publish core libraries as packages
