# SimOpt Decisions — Archive

Entries moved out of `docs/decisions.md` at its monthly review because they were superseded or fully
executed. Each is kept verbatim under its original heading and ID, so references elsewhere still
resolve, and the line under each says why it left the live file. Where only part of an entry moved
(a subsection or some bullets), the entry is marked *partial* and the rest of it stays live in
`docs/decisions.md`. Append-only: entries are added here, never edited.

---

## Glass digital-twin → dual-repo split (furkansim)

*Archived at review 2026-11-03 (whole entry).*

**Decided:** 2026-06-18 — after the glass base-mass demo for Furkan Bolat (Value Stream Manager Glass, Ivoclar) was delivered and validated.

- **Dual repo.** `simopt` stays the generic, reusable simulation-optimization framework and is **PUBLIC**. A new repo **`furkansim`** (private, `IvoclarR-D-AIOrg`) holds all Ivoclar/glass-ceramics-specific code, calibration, and customer data. Rationale: keep confidential Ivoclar production data and customer-specific topology out of the public framework; let the framework stay clean and reusable.
- **Confidential data committed into the private repo by design.** Furkan's SAP export (`WBZ_DLZ_Analyse_2026.xlsx`) lives in `furkansim/data/`; calibration reference in `furkansim/docs/`. Never to be mirrored to public `simopt`.
- **Framework dependency = sibling ProjectReference.** furkansim `.csproj` references `../simopt/src/SimOpt.*.csproj` directly (both repos checked out side-by-side). Chosen over git submodule / NuGet packages for solo-dev simplicity and instant rebuilds against framework changes.
- **Calibration must precede in-data work because simopt is public:** the Glass code is extracted from simopt into furkansim *before* calibrating with real station names/timing/operators (calibrating in-place would leak to the public repo).
- **Realism direction:** calibrate sim + optimizer from Furkan's real data (stations, value-stream routings, per-step dwell, ~2.6 orders/day arrivals, ~178 KG batches, DLZ median ≈11 working days), then add the 10 real operators as mobile resources walking/carrying between station clusters, then integrate a real floor plan (background image + geometry/distance source) when Furkan provides it. Full plan: `furkansim/docs/furkan-data-calibration.md`, `furkansim/backlog.md`.

Archived 2026-11-03: executed (FSIM-03). The split was carried out; its standing parts (dual repo, no customer data in public simopt, sibling ProjectReference) are carried by the live FSIM-03 entry.

---

## Agent-Driven Sim-Opt Positioning

*Partial: one subsection moved; the rest of the entry stays live.*

### What this implies for the Ivoclar demo

The colleague's "real data next week" doesn't require a schema. When the
numbers arrive — in whatever form — the agent takes them, updates the
`IvotionTopologyBuilder` baseline constants (or constructs the solution
vector directly), and reruns. The UI shows the result. No import screen.

Archived 2026-11-03: superseded by "Agent-Driven Sim-Opt Positioning" → "No standardized import schemas", which states the same rule without the dated demo.

---

## 2026-06-18 — Glass production-line digital-twin demo

*Partial: two bullets moved; the public-repo content boundary bullet stays live.*

- **Reuse over clone:** the glass optimization reuses the generic EA/Random optimizer;
  only the domain (solution/problem/topology/KPIs) is new. Domain classes were placed in
  the existing SimOpt.Ivotion assembly (namespace SimOpt.Glass) to avoid the broken .slnx
  project-add path — flagged as tech debt (SIM-55).
- **Twin authoring workflow validated end-to-end:** a plain-language line description maps
  to a `VizTopology` factory method that the visualization auto-renders, and the optimizer
  applies its best solution back to the live twin — the "semantics + data → ad-hoc,
  optimizable digital twin" loop.

Archived 2026-11-03: superseded by FSIM-03 (the glass domain classes left SimOpt.Ivotion for furkansim) and executed (the demo was delivered).

---

## 2026-07-05 — Critical-subsystem code review (Opus 4.8, 3 parallel agents)

*Partial: one bullet moved; the rest of the entry stays live.*

- **Fable availability:** the injected agent roster still tags Fable 5 as geo-blocked — that's stale.
  Per MG (2026-07-05) Fable is re-enabled for EU (temporary, revocable); already tracked in the
  cross-project inbox for cfg-agent-fleet to fix the agent description + `fleet-capabilities.md`.
  This review ran on **Opus 4.8** (the reliable fallback; Fable's content-gate has been observed
  flapping). No re-run on Fable — the Opus findings stand.

Archived 2026-11-03: executed (handed to cfg-agent-fleet; a fleet-configuration matter, not a SimOpt decision).

---

## Commercial pivot — the four founding decisions (2026-08-25)

### D-01 · Licensing: FSL-1.1-ALv2 on the engine, proprietary on the app

*Partial: one bullet moved; D-01 itself stays live.*

- **Still owed:** confirm the dissertation-era code provenance carries no institutional claim.
  Austrian universities normally leave dissertation copyright with the author; verify rather than
  assume. Tracked as SIM-73.

Archived 2026-11-03: executed (SIM-85, closed by D-05).

---

## D-05 — SimOpt is an instrument of the Gutachtertätigkeit (2026-08-25)

*Partial: one subsection moved; D-05 itself stays live.*

### Open strategic question — ANSWERED 2026-08-26, see D-06

> **Resolved.** The owner chose *forensic as proving ground, manufacturing as the public face,
> sequenced* — see **D-06** at the end of this file. The comparison below is retained because the
> reasoning is what future sessions need, but the question is closed and must not be re-opened on
> the strength of the forensic origin alone.

The beachhead decision of 2026-08-25 (general discrete manufacturing, sold through independent
consultants) was taken without this information on the table. It is worth revisiting, because on
the criteria that matter to a solo owner with no marketing time, the forensic/Sachverständiger
market scores better on almost every axis:

| | Manufacturing consultants | Sachverständige / forensic |
|---|---|---|
| Owner's standing in the market | none yet | **already a practitioner** |
| Distribution | must be built | existing professional network |
| Willingness to pay | €790/yr is a considered purchase | expert-witness work bills at multiples of that |
| Competition | AnyLogic/FlexSim/Simio are strongest here | **essentially unserved** |
| What the product must be excellent at | breadth of features | **correctness and reproducibility — what we just built** |
| Owner can be customer zero | no | **yes** |

Against it: the market is far smaller, sales are relationship-led rather than product-led, and
"simulation as evidence" has an admissibility literature that would need real work before any
claim is made. It may be the better *wedge* while general manufacturing remains the better
*volume* market — the two are not exclusive, and the engine is the same.

**Do not act on this unilaterally.** It is an owner decision, recorded here so the next session
presents it rather than rediscovering it. Tracked as SIM-87.

Archived 2026-11-03: superseded by D-06.

---

## 2026-08-26 — the analytic battery (SIM-64), and three rulings

### Owner rulings, 2026-08-26

*Partial: two bullets moved; the SIM-93 ruling stays live.*

- **SIM-81 promoted P2 → P1, to be done next.** The battery was built to make replacing the random
  number stream provable rather than merely different, and the evidence is freshest immediately
  after it lands. Doing it before more benchmarks are pinned also means fewer numbers to re-baseline.
- **SIM-95 P2, SIM-92 P1, SIM-94 P3, SIM-96 P2, SIM-97 P3** as proposed.

Archived 2026-11-03: executed (SIM-81 done 2026-08-26; SIM-92, SIM-94, SIM-95, SIM-96 and SIM-97 carry these priorities in backlog.md).
