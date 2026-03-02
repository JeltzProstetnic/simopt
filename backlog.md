# SimOpt Backlog

## P1 — High Priority

- [ ] **SIM-01** Fix EventScheduler.Remove last event (size: S) — **DONE Phase 3**
- [ ] **SIM-02** Fix RandomStrategy same-seed-every-iteration (size: S) — **DONE Phase 3**
- [ ] **SIM-03** Fix Vector.Equals NaN Z comparison bug (size: S) — **DONE Phase 3**
- [ ] **SIM-04** Implement PSO (Particle Swarm Optimization) as IStrategy (size: L) — SwarmingAlgorithm is marked [Obsolete], needs proper implementation
- [ ] **SIM-05** Fix EventScheduler.Remove duplicate-priority edge case (size: M) — Remove only removes by priority key, not by identity; two events at same time+priority are indistinguishable

## P2 — Medium Priority

- [ ] **SIM-06** Expand test coverage: Buffer template (size: M) — FIFO/LIFO/priority/random selection, capacity, rejection
- [ ] **SIM-07** Expand test coverage: Server template (size: L) — state machine, failure/recovery, auto-restart
- [ ] **SIM-08** Expand test coverage: Conveyor template (size: M) — indexed sections, item attachment
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
