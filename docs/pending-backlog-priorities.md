<!-- Action: await-user-decision -->
<!-- Tracked-by: SIM-90, SIM-92, SIM-93, SIM-94 -->
# Present at startup: six new backlog items need a priority ruling

Six items were filed on 2026-08-26 at **proposed** priorities. The agent proposes; the owner
decides. Present this table and get a ruling before the next session plans its work.

| Item | Proposed | What it is | Argument for the proposed priority | Argument against |
|---|---|---|---|---|
| **SIM-89** | P0 — **already done** | MCP head could not build or run a single model | No decision needed, listed for completeness | — |
| **SIM-91** | P0 — **already done** | First run of a model diverged from every later run | No decision needed, listed for completeness | — |
| **SIM-90** | P1 — **done after this file was written** | Entity identity destroyed at every server | No decision needed. Closed 2026-08-26; the causal story originally filed for it was wrong and is corrected in the backlog entry. | — |
| **SIM-95** | **P2** | `Server`'s failure-continue path resumes with a **null product** — deferred event instances never populate `EventArgs` | It is a genuine wrong-value defect in the engine, in a path with zero behavioural coverage. | Unreachable today from every head, because no failure distributions are wired anywhere. Nothing can hit it until someone models machine breakdowns — which is a plausible near-term feature request. |
| **SIM-92** | **P1** | Visualization computes five unsound statistics, including a bottleneck ranking keyed on a truncated display name | These numbers are what a customer sees in a demo. The engine now has correct ones, so the UI is actively contradicting the engine. | No paying customer exists yet; it is cosmetic until one does. Could be P2. |
| **SIM-93** | **P1** | Ivotion KPIs: a headcount stored in a field named `LaborHoursPerSimHour` and displayed as "Labor hrs/hr"; throughput divides by requested rather than elapsed duration | It is a wrong number shown to an Ivoclar audience under a dimensionally false label. | The Ivotion optimisation tab is currently unreachable from the UI (the tab was removed by the framework-only pivot), so nobody is reading it today. That is an argument for P2 — or for retiring the code. |
| **SIM-94** | **P3** | Example programs teach the wrong statistics (SQSS prints the queue count labelled "Sink received items", ProductionLine's bottleneck tie goes to whichever branch, etc.) | Examples are documentation, and this is what a new user copies. | Nothing depends on them and no user has seen them yet. |

## The one that is genuinely a judgement call

**SIM-93 raises a question beyond its own priority: should the Ivotion optimisation UI be fixed or
retired?** The tab was removed from `MainWindow.axaml` by the framework-only pivot, so
`IvotionOptimizationView` is unreachable while `MainWindowViewModel` still constructs its
view-model. Fixing the KPI labels means maintaining a surface nobody can currently open. Deleting it
means discarding the Ivoclar demo capability that `docs/decisions.md` records as *"the killer
pitch"*. Neither is obviously right and it is not an agent's call.

## Also worth a ruling

**SIM-81** (MersenneTwister is not a Mersenne Twister — no tempering, first 624 outputs are
`System.Random`) is still open at P2. Its own backlog note says it needs the SIM-64 analytic battery
in place first, to prove the change is an improvement rather than merely a difference. That battery
is now half built, so SIM-81 becomes actionable as soon as SIM-64 lands — worth deciding now whether
it moves up.
