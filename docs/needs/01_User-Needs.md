<!-- Action: reference -->
<!-- META
Status: DRAFT v0.1
Date: 2026-08-25
Accountable owner: Matthias Gruber
Scope: SimOpt as a commercial product. Covers the simulation-optimization engine, its MCP head,
       and the desktop application. Excludes the FMT/GridWorld research track (SIM-47..54), which
       shares the repository but not the product.
Reviewers: (none yet — owner review pending)
Classification: Internal / commercial-confidential
Basis: docs/commercial/2026-08-25-architecture-readiness-review.md,
       docs/commercial/2026-08-25-business-strategy.md,
       owner decisions 2026-08-25 (licensing / beachhead / form factor / ambition)
-->

> **DRAFT.** These needs are not signed off. Everything derived from them — system requirements,
> MVP scope, build slices — is provisional until the owner reviews this document.

# SimOpt — User Needs

## What the system is

SimOpt lets a person who will never learn a simulation tool get a defensible answer to a
capacity, throughput or staffing question. The user describes a real system in ordinary language —
"we run two CNC cells, parts arrive about every four minutes, setup takes 20 to 40 minutes" — and
SimOpt builds a discrete-event model of it, shows it running, quantifies the answer with confidence
intervals, and searches for a better configuration. The user never writes code and never learns a
modelling notation. The intelligence that turns language into a model is a large language model the
**user supplies and controls**; the thing that guarantees the numbers are right is SimOpt's own
validated engine.

The product is therefore not model *generation* — that is commoditising. The product is the
**trust and packaging layer** around it: an engine that cannot silently be wrong, evidence that
survives scrutiny in a meeting, and a path from question to answer measured in hours rather than
weeks.

## Vision

> **Describe your system over coffee. Walk into the 2 pm meeting with an animated model,
> confidence intervals, and an optimised configuration.**

Three commitments follow from that sentence, and every need below serves one of them:

1. **Conversation is the only interface that is required.** Not the only one available — but no
   user is ever forced into a modelling language, a form grid, or a script to get an answer.
2. **The numbers are defensible or they are not shipped.** Every result carries replications and
   confidence intervals by default. The engine is validated against closed-form results that a
   sceptical reader can check independently.
3. **The user owns their intelligence and their data.** A locally hosted model is the default, so
   a factory's layout never leaves the building unless the user chooses otherwise. Commercial
   providers are available to anyone who wants more capability, but nobody is required to hold an
   account with anyone to get an answer.

### Strategic frame (owner decisions, 2026-08-25)

| Decision | Choice | Consequence for these needs |
|---|---|---|
| Licensing | Open-core. Engine under **FSL-1.1-ALv2** (free for any purpose except competing; converts to Apache-2.0 after two years); application proprietary and in a separate private repository. Decided 2026-08-25. | Needs must be separable into a free engine tier and a paid product tier. The engine being free *commercially* means the paid value has to live in the application layer. |
| Model access | **Local-first.** A locally hosted model is the default path; commercial providers are supported but opt-in. Decided 2026-08-25 (D-02). | No need may assume the user holds an API key. Confidentiality (UN-021) is promoted from an option to a headline capability, and model capability — not onboarding friction — becomes the risk to manage. |
| Beachhead | General discrete manufacturing, sold **through independent operations consultants** to SMB manufacturers. | The Consultant is the primary buyer persona; the Plant Engineer is the primary *user*. |
| Form factor | One engine, two heads: an MCP endpoint and a desktop application. Usable **with or without the UI, with or without MCP**. | No need may assume a UI, and no need may assume an MCP client. Theme G exists for this. |
| Ambition | Prove it earns money at all, first. | Needs are marked for MVP or later; time-to-first-sale outranks completeness everywhere. |

---

## Chapter 1 — Who this is for

Roles nest. A need served for an outer role is served for every role inside it. Listed
outermost first.

### R1 · Operations Decision-Maker
A plant manager, clinic administrator or operations director who **reads the answer and acts on
it**. Does not build models and never will. Judges the output on whether it survives being
questioned by a colleague. Cares about: is this credible, what does it assume, what does it cost
me if it's wrong. **Never touches the software** in the MVP — receives its output.

### R2 · Plant / Operations Engineer
An industrial or process engineer inside an SMB manufacturer. Understands their production system
deeply, understands queueing intuitively, has no simulation training and no budget for a €6,000/seat
tool. Has a real question — a second shift, a buffer size, a bottleneck they suspect but cannot
prove. **The primary user.** Will use the desktop application. Does not have an LLM API key today
and is not motivated to obtain one.

### R3 · Independent Operations Consultant
Solo or small practice, ex-industry, advising SMB manufacturers. Knows a simulation study wins and
justifies engagements but has never justified the licence cost or the modelling weeks. Turns a
three-week study into two days. **The primary buyer** — pays with their own money, decides in one
sitting, and brings repeat usage across clients. Comfortable with an API key. Needs the output to
look like their deliverable, not ours.

### R4 · Technical Integrator
A developer, data scientist or AI engineer who drives SimOpt from their own agent environment
(Claude Code, Cursor, a script, a pipeline) with no UI at all. Values the MCP head, reproducibility,
and headless operation. Smallest population, earliest to adopt, and the cheapest to serve — reaches
the product through channels the others don't.

### R5 · Court-Appointed Expert (Gerichtlich beeideter Sachverständiger) — *added 2026-08-25*
An expert witness who must demonstrate, to a court or an opposing expert, how a computer system
behaved when it interacted with the real world. **This is the role the product was originally built
for** (see `docs/decisions.md` D-05), and it was absent from this document only because nobody had
written down why the tool exists.

Their bar is higher than every other role's, on a narrow axis: the result must survive adversarial
scrutiny. Not "is it useful" but "can it be reproduced by someone hostile to the conclusion,
months later, on different hardware, and can every assumption behind it be named". They do not need
breadth of features. They need the parts that are true to be provably true.

Serving this role costs almost nothing extra, because it demands the same properties the commercial
product already needs — correctness, reproducibility, stated assumptions — only enforced harder.

### R6 · Researcher / Educator
Uses the free engine tier for teaching or publication. Generates no revenue directly and is served
last, deliberately — but supplies the citation lineage and the credibility that the commercial
claim rests on. The dissertation heritage makes this role structurally cheap to serve.

**Non-users, explicitly:** enterprise procurement, regulated-validation buyers, and anyone needing
3D fidelity or material-handling libraries. They are AnyLogic/FlexSim/Simio customers and SimOpt
does not compete for them.

---

## Chapter 2 — Conversational Model Construction

*Turning a spoken description of a real system into a runnable model, without the user learning
anything.*

### UN-001: Natural-Language Model Construction
**As a** Plant Engineer, **I want** to describe my production system in ordinary language and get a
runnable model of it, **so that** I can start answering my question without learning a modelling
tool.
- A free-text description containing arrivals, workstations, capacities and durations yields a
  complete model in one exchange.
- Ambiguity is resolved by asking me, not by silent assumption.
- The vocabulary I use is mine ("cell", "line", "shift"), not the tool's.
- **System boundary:** does not own the language model that performs the interpretation (UN-020).

### UN-002: Model Read-Back and Confirmation
**As a** Plant Engineer, **I want** the system to tell me plainly what model it built before I rely
on it, **so that** I can catch a misunderstanding before it becomes a wrong decision.
- The built model is described back in the same plain language, listing every parameter assumed.
- Anything inferred rather than stated by me is marked as inferred.
- I can correct any element conversationally and see the correction reflected.
- Extends UN-001. **This is the trust hinge of the whole product** — an LLM that silently builds the
  wrong model destroys the value proposition with exactly the persona we sell to.

### UN-003: Conversational Model Refinement
**As a** Plant Engineer, **I want** to change my model by saying what changed, **so that** I can
explore without rebuilding.
- Incremental changes preserve the rest of the model and its history.
- Earlier versions remain addressable so I can compare "what we had" with "what we're proposing".
- Consumes UN-001 output.

### UN-004: Structured Model Authoring Without Conversation
**As a** Technical Integrator, **I want** to define a model in an explicit, versioned, documented
structure, **so that** I can generate, diff, review and version-control it without an LLM in the
loop.
- The structure is the same one the conversational path produces — there is exactly one model
  representation, not a parallel one.
- Its schema is published and machine-readable, so a client can self-correct against it.
- Shared with UN-001; this is the same capability approached from the other side.

### UN-005: Model Persistence and Portability
**As a** Consultant, **I want** models to survive being closed, moved and reopened, **so that** a
client engagement is a file I own rather than a session I lose.
- A model can be saved, reopened, and shared with someone else who can run it.
- A model outlives the process that created it and the tool version that created it.

---

## Chapter 3 — Model Trust and Validation

*Making the engine something a professional can stake their reputation on.*

### UN-006: Correct Stochastic Behaviour
**As a** Consultant, **I want** the random behaviour in the model to be mathematically correct,
**so that** an answer I hand a client is not wrong for a reason I could never have detected.
- Every probability distribution offered produces samples matching its definition, demonstrably.
- The correctness claim is backed by tests a sceptic can read and re-run.
- **System boundary:** does not cover whether the user's *chosen* distribution matches their
  reality — that is UN-008.

### UN-007: Independently Verifiable Accuracy
**As a** Consultant, **I want** the engine's results checked against known analytical answers,
**so that** I can point a doubting client at evidence rather than an assurance.
- For systems with textbook closed-form solutions, simulated results agree within stated
  statistical tolerance.
- The comparison is part of the product's routine verification, not a one-off marketing exercise.
- Extends UN-006. This is simultaneously a quality gate and the strongest marketing asset available.

### UN-008: Pre-Run Model Sanity Checking
**As a** Plant Engineer, **I want** to be warned about a structurally or physically implausible
model before I spend time on results, **so that** I don't act on output from a broken model.
- Structural faults (unreachable stations, no exit, disconnected flow) are reported before running.
- Overloaded stations — where demand exceeds capacity and queues grow without limit — are flagged
  with the offending station named.
- Every problem reported carries a suggested correction, not just a diagnosis.
- All problems are reported at once, not one per attempt.

### UN-009: Reproducibility
**As a** Technical Integrator, **I want** the same model and the same seed to produce the same
results, **so that** I can trust a comparison, reproduce a finding, and debug a discrepancy.
- Identical inputs give identical outputs, across sessions, machines and process restarts.
- Any run can be reproduced later from what is stored with it.

### UN-010: Stated Assumptions
**As an** Operations Decision-Maker, **I want** every result to state what it assumed, **so that**
I can judge how much weight it carries in a decision.
- Assumptions, warm-up handling, replication count and model version accompany any reported number.
- A result separated from its assumptions is not a deliverable.
- Consumes UN-002 output.

---

## Chapter 4 — Experimentation and Evidence

*Turning a model that runs into evidence that persuades.*

### UN-011: Operational Performance Measures
**As a** Plant Engineer, **I want** the measures I actually argue about — waiting time, throughput,
queue length, utilisation, time in system — **so that** the output speaks in the language of my
problem.
- Measures are available for the system as a whole and per station.
- Measures are available whether or not any user interface is running.
- **System boundary:** owns operational measures; financial translation is UN-016.

### UN-012: Statistical Confidence
**As a** Consultant, **I want** results expressed with confidence intervals from multiple
replications, **so that** I can distinguish a real difference from simulation noise.
- Multiple replications run by default, not as an expert option.
- Every reported measure carries an interval and the number of replications behind it.
- Start-up transients are excluded from steady-state measures.
- Extends UN-011. Single-sample answers are how a confident wrong recommendation gets made.

### UN-013: Scenario Comparison
**As a** Consultant, **I want** to compare several configurations side by side, **so that** I can
show a client the options rather than assert a conclusion.
- Multiple variants are evaluated together and reported as a comparison.
- Whether a difference between variants is statistically meaningful is stated, not left to the eye.

### UN-014: Question-Shaped Answers
**As an** Operations Decision-Maker, **I want** the answer to my actual question in a sentence,
**so that** I can act without interpreting a statistics table.
- The headline finding is stated in plain language with its uncertainty attached.
- Detail is available underneath for anyone who wants to interrogate it.
- Consumes UN-012 output.

---

## Chapter 5 — Optimisation and Recommendation

*The "opt" in SimOpt — the part no free alternative offers.*

### UN-015: Automated Configuration Search
**As a** Plant Engineer, **I want** the system to find a good configuration itself, **so that** I
get a recommendation rather than a simulator I have to drive by hand.
- I state what I can change and within what limits, and what "better" means.
- The search reports the best configuration found, credible alternatives, and how hard it looked.
- The search respects constraints I set — budget, floor space, headcount — and never proposes a
  configuration that violates them.
- Search is available on any model, including one built conversationally — I never write code to
  make my model optimisable.

### UN-016: Decision Framing in Business Terms
**As an** Operations Decision-Maker, **I want** the recommendation expressed in money and payback,
**so that** I can take it to whoever approves spending.
- Operational improvements convert to cost and benefit using figures I supply.
- The comparison is against the current configuration, not against an abstract optimum.
- Consumes UN-015 output. **Later than MVP** — the operational answer must be trustworthy first.

### UN-017: Sensitivity and Robustness
**As a** Consultant, **I want** to know which assumptions the recommendation actually depends on,
**so that** I can defend it when a client disputes an input.
- The inputs that most affect the outcome are identified.
- Whether the recommendation survives plausible variation in those inputs is stated.
- **Later than MVP.**

---

## Chapter 6 — Seeing It Run

*The artefact that makes the meeting go differently.*

### UN-018: Live Visual Simulation
**As a** Consultant, **I want** to show the model running, **so that** the room believes the model
before it argues about the numbers.
- The flow is watchable in real time, with speed control and pause.
- Congestion and bottlenecks are visually obvious without reading a table.
- Any model the user built is watchable — including one built conversationally.
- **System boundary:** presentation only; it never computes a reported measure (UN-011 owns those).

### UN-019: Shareable Deliverables
**As a** Consultant, **I want** to export the model, results and visuals as something I can hand
over, **so that** SimOpt output becomes my deliverable.
- Results and figures export in forms that go into a report or a slide deck.
- The output can carry my identity rather than the tool's.
- Extends UN-018. The white-label aspect is what justifies the Consultant price tier.

---

## Chapter 7 — Provider Independence and Confidentiality

*The user owns their intelligence and their data.*

### UN-020: Bring Your Own Language Model
**As a** Consultant, **I want** to connect the language model I already pay for, **so that** I am
not paying twice and not locked to a vendor's choice.
- Major commercial providers and locally-hosted models are all usable.
- Switching provider does not change what the product can do.
- Credentials are stored with the protection the operating system provides, never in plain files.

### UN-021: Local-Only Operation
**As a** Plant Engineer, **I want** the option that nothing about my factory leaves the building,
**so that** I can use this on systems I am not permitted to describe to an external service.
- A locally-hosted model path is fully supported, not a degraded fallback.
- What is transmitted, to whom, and when is stated plainly before anything is sent.
- Simulation and optimisation themselves never require a network connection.
- Extends UN-020. In EU manufacturing this is a selling point, not merely compliance.

### UN-022: Usable Without Owning a Model Subscription
**As a** Plant Engineer, **I want** to evaluate and use the product without first obtaining an API
key, **so that** the first five minutes don't require an account with a company I've never dealt
with.
- There is a path to a first useful result that does not begin with obtaining a credential.
- **RESOLVED 2026-08-25 (D-02): the keyless path is the default, not the fallback.** A locally
  hosted model requires no credential at all, so this need and UN-020 no longer compete. The cost
  moves from onboarding friction to model capability, which is addressed by UN-002's read-back
  loop and UN-008's validation rather than by asking the user for a key.

---

## Chapter 8 — Access Without the Application

*The second head. No need in this document may assume a user interface.*

### UN-023: Agent-Driven Operation
**As a** Technical Integrator, **I want** to drive the whole capability from my own agent
environment, **so that** SimOpt becomes a tool my assistant uses rather than an application I visit.
- Model construction, validation, experimentation and optimisation are all reachable without the
  desktop application.
- The capability exposed is the same one the application uses — not a reduced subset.
- Results returned are compact enough to be practical inside an agent's limited context.

### UN-024: Headless and Scripted Operation
**As a** Technical Integrator, **I want** to run models unattended, **so that** I can put simulation
into a pipeline, a batch study or a regression suite.
- Runs complete with no interactive session and no display.
- Results are emitted in a form other software can consume.
- Extends UN-023.

### UN-025: Capability Parity Across Heads
**As a** Consultant, **I want** what I learn in one entry point to hold in the other, **so that**
moving between them costs me nothing.
- A model built in one head opens and behaves identically in the other.
- No capability exists in only one head without that being a stated, deliberate exception.
- **This is the structural expression of the owner's "with and without UI, with and without MCP"
  requirement** and constrains the architecture more than any other need here.

---

## Chapter 9 — Getting In and Paying

*From download to revenue, for someone who has never heard of us.*

### UN-026: Frictionless First Result
**As a** Plant Engineer, **I want** to reach a first meaningful result within minutes of downloading,
**so that** I find out whether this is worth my attention before I've spent an evening on it.
- Installation is unremarkable on a normal corporate laptop and does not trip security warnings.
- A worked example is reachable without supplying anything.
- The path from install to first answer requires no documentation.

### UN-027: Guided Competence
**As a** Plant Engineer, **I want** to be taught what I need exactly when I need it, **so that** I
can interpret results correctly without a course in simulation.
- Concepts that affect interpretation — replication, warm-up, confidence — are explained where they
  appear, in the user's terms.
- Guidance is offered by the assistant in context, not deferred to a manual.

### UN-028: Free Tier That Is Genuinely Useful
**As a** Researcher, **I want** a free capability that solves real problems, **so that** I can
teach with it, publish with it, and recommend it honestly.
- The free tier answers a real question end to end, with a clear and honestly-stated limit.
- Its limit is size or throughput of use, never correctness or credibility.
- Serves R5, and is simultaneously the primary acquisition channel for R2 and R3.

### UN-029: Straightforward Commercial Purchase
**As a** Consultant, **I want** to buy this in one sitting with a card, **so that** no procurement
process stands between me and using it on Monday.
- Purchase completes without a sales conversation, a quote or an invoice cycle.
- Tax handling is correct for a European seller selling internationally, without the buyer or the
  seller managing it per jurisdiction.
- Licence terms for commercial client work are unambiguous.

### UN-030: Someone Answers
**As a** Consultant, **I want** a human response when I hit a wall, **so that** I can risk a client
engagement on this.
- A support route exists with a stated response expectation.
- Deliberately minimal at this stage: an address and a norm, nothing more.

---

## Chapter 9b — Evidentiary Defensibility — *added 2026-08-25*

*What a result must carry to be usable as evidence rather than merely as advice. Serves R5;
everything here also strengthens the commercial product, which is why it is cheap to build.*

### UN-033: Independently Reproducible Results — *added 2026-08-25*
**As a** Court-Appointed Expert, **I want** any result to be reproducible by a third party from
what I hand them, **so that** it can be relied on when someone is paid to discredit it.
- A run is reproducible from its stored inputs alone — model, seed, version, parameters — by
  someone who was not present when it was produced.
- Reproduction on different hardware, a different operating system and a later date yields
  identical figures, not merely similar ones.
- **System boundary:** does not own whether the *model* corresponds to reality — that is the
  expert's professional judgement and must never be implied by the tool.
- Strengthens UN-009. Where UN-009 asks for determinism as engineering hygiene, this asks for it
  as an evidentiary property, which is a materially stronger obligation.

### UN-034: Complete Provenance of a Result — *added 2026-08-25*
**As a** Court-Appointed Expert, **I want** every reported figure to carry the full chain that
produced it, **so that** I can answer "where does that number come from" without reconstructing it.
- Model version, seed, replication count, warm-up handling, tool version and every parameter value
  travel with the result rather than beside it.
- The chain is exportable in a form that goes into an expert report.
- Extends UN-010.

### UN-035: Declared Model Assumptions and Limits — *added 2026-08-25*
**As a** Court-Appointed Expert, **I want** the tool to state plainly what it did not model,
**so that** I do not have to defend a claim the tool never made.
- Simplifications, distributional assumptions and anything inferred rather than supplied are
  reported explicitly alongside results.
- Where a question is outside what the model can answer, the tool says so rather than answering it.
- Consumes UN-002 and UN-008 output.

### UN-036: Tamper-Evident Result Records — *added 2026-08-25*
**As a** Court-Appointed Expert, **I want** a stored result to be detectably unaltered,
**so that** the record I produced and the record submitted are demonstrably the same.
- A stored run carries an integrity check over its inputs and outputs.
- Alteration after the fact is detectable without trusting the person who stored it.
- **Later than MVP** — worth stating now so the record format is designed with the hook in place
  rather than retrofitted.

## Chapter 10 — Questions That Were Carried as Needs

*Recorded here rather than resolved, so they could not be silently decided by implementation.
**Both were decided by the owner on 2026-08-25** — see `docs/decisions.md` D-01 and D-02. They
are kept, with their resolutions, because the reasoning is what future sessions need.*

### UN-031: Licensing Instrument — **DECIDED 2026-08-25**

**Resolution: FSL-1.1-ALv2 on the engine, proprietary on the application.** `LICENSE.md` is in
place. Note the correction recorded in D-01: FSL permits *any purpose except a Competing Use*, so
commercial and consultant use of the engine are free and the revenue must come from the closed
application layer. Remaining obligation: confirm dissertation-era provenance carries no
institutional claim (SIM-73).

### UN-032: Model-Access Economics — **DECIDED 2026-08-25**

**Resolution: local-first, cloud optional.** A locally-hosted model is the default path; commercial
providers stay fully supported but opt-in. This resolves the UN-020/UN-022 conflict in favour of
accessibility and confidentiality, and makes three things requirements rather than options:
schema-constrained decoding so topology output is valid by construction (SIM-83), a first-run
hardware check with a stated floor (SIM-82), and a named reference model measured against a
benchmark before "works with local models" may be claimed (SIM-84).

**UN-022 is therefore no longer in tension with UN-020** — the tension noted in Chapter 7 is
resolved by making the keyless path the default rather than the fallback.

### Original statements, retained

### UN-031 (as originally raised): Licensing Instrument
**As the** owner, **I want** a licensing posture that keeps the engine publicly readable and
citable while preventing a competitor from taking the commercial layer, **so that** the academic
lineage keeps working as credibility without funding a fork.
- Current state: public repository, **no licence file**, therefore all rights reserved by default.
  Nothing has been given away; every option remains open.
- Owner has chosen open-core in principle. The instrument is undecided: a permissive engine licence
  (Apache-2.0) versus a source-available one (FSL/PolyForm) that converts to permissive later.
- **Decision required from the owner before any public launch.** The business review recommends
  against a permissive engine licence; the owner's stated preference is open-core. These are
  reconcilable via FSL but the choice is legally consequential and irreversible in practice.
- Prerequisite: confirm dissertation-era code provenance carries no institutional claim.

### UN-032 (as originally raised): Model-Access Economics
**As the** owner, **I want** a resolution of the tension between bring-your-own-key margin and
non-expert accessibility, **so that** the architecture does not decide it by accident.
- UN-020 (BYO key, ~95% gross margin, no inference cost) directly conflicts with UN-022 (the target
  user has no key and won't get one).
- Options: sell to key-holding personas first (R3, R4) and defer R2; bundle a metered key at a
  premium tier; or lean on local models as the default path.
- **Decision required before the desktop application's onboarding is designed.**

---

## Coverage matrix

| Theme | Needs | Serves | MVP? |
|---|---|---|---|
| 2 · Conversational Model Construction | UN-001..005 | R2, R3, R4 | UN-001,002,003,004 yes · UN-005 yes (cheap) |
| 3 · Model Trust and Validation | UN-006..010 | all | **All yes — this is the critical path** |
| 4 · Experimentation and Evidence | UN-011..014 | R1, R2, R3 | UN-011,012,013 yes · UN-014 yes |
| 5 · Optimisation and Recommendation | UN-015..017 | R1, R2, R3 | UN-015 yes · UN-016,017 later |
| 6 · Seeing It Run | UN-018..019 | R1, R3 | UN-018 yes (largely built) · UN-019 yes (Consultant tier) |
| 7 · Provider Independence | UN-020..022 | R2, R3, R4 | UN-020,021 yes · UN-022 open (UN-032) |
| 8 · Access Without the Application | UN-023..025 | R4, R3 | **All yes — owner requirement** |
| 9 · Getting In and Paying | UN-026..030 | R2, R3, R6 | UN-026,028,029,030 yes · UN-027 partial |
| 9b · Evidentiary Defensibility | UN-033..036 | R5 | UN-033,034,035 yes (cheap — they ride on Slice 0/1) · UN-036 later |
| 10 · Decided Questions | UN-031..032 | owner | Decided 2026-08-25 (D-01, D-02) |

**Baseline against today's codebase** (per the 2026-08-25 architecture review):
UN-018 largely built · UN-001/004 partially built (MCP schema too narrow to express the product's own
examples) · UN-006/009 **actively defective** (SIM-56..59 open) · UN-011/012 **not built at all** —
the engine has no output-statistics subsystem · UN-015 not built generically · everything in
Chapters 7 and 9 unbuilt.

**36 needs total: 1 largely met, 2 partially met, 25 unbuilt, and 8 formerly-defective needs now
repaired** — UN-006, UN-007 (partially), UN-009 and UN-033's determinism basis were closed by
Slice 0 on 2026-08-25 (SIM-56/57/58/62, 694 → 809 tests). The stochastic layer no longer produces
knowingly wrong numbers and the reset path no longer diverges between evaluations.

---

## What happens next

`02_System-Requirements.md`, `03_MVP-Offer.md` and `04_Build-Slices.md` are not yet written. They
are derived documents and would be drafts of a draft. The build sequence currently in force is the
one in `docs/commercial/2026-08-25-architecture-readiness-review.md` §E, tracked in `backlog.md`
under the Commercial Track. Once these needs are reviewed, the slice plan gets cut against need IDs
and the coverage matrix becomes the honest answer to "are we nearly done".
