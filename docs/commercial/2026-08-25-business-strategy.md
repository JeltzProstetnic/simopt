# SimOpt Commercial Strategy — Finance, Pricing, Go-to-Market

**Date:** 2026-08-25
**Status:** Draft for owner review (produced by Fable subagent; all figures sourced or explicitly flagged as estimates)
**Product premise:** LLM-native desktop sim-opt tool — describe a real system in chat, get a parameterized, validated, optimized discrete-event model. BYO LLM key. Built on the existing SimOpt .NET 9 engine.

---

## A. Market reality check

### Size

The discrete-event simulation software market is ~$1.6–2.2B (2025) growing at ~10–13% CAGR — estimates vary by analyst: [$2.24B in 2025 → $2.53B in 2026, 12.6% CAGR](https://www.openpr.com/news/4519441/industry-report-on-discrete-event-simulation-software-market) vs. [$1.56B (2024) → $1.70B (2025)](https://www.360iresearch.com/library/intelligence/discrete-event-simulation-software). The broader simulation software market is ~$16–20B ([Fortune Business Insights](https://www.fortunebusinessinsights.com/simulation-software-market-102435), [Grand View](https://www.grandviewresearch.com/industry-analysis/simulation-software-market)). Analysts explicitly note the 2025–26 structural shift: vendors embedding AI/ML for "real-time adaptive modeling" — the incumbents see the same opportunity SimOpt targets.

Do not over-read the billions. The addressable slice for a one-person desktop product is the **prosumer/SMB long tail** the incumbents ignore because their sales motion (quote-based, distributor-mediated, training-attached) can't profitably serve it. That slice is plausibly 1–3% of the DES market in revenue but a much larger share of *seats* — tens of thousands of ops managers and consultants who never buy Arena.

### Who pays today, and how much

| Product | Price point | Model | Source |
|---|---|---|---|
| AnyLogic Professional | $12,390–18,990/seat | Perpetual + support renewal; free PLE for education | [checkthat.ai](https://checkthat.ai/brands/anylogic/pricing), [Scribd price list](https://www.scribd.com/document/428955164/Prices-AnyLogic-USD-pdf) |
| AnyLogic Univ. Researcher | $3,550–4,250 | Academic | same |
| Simio | ~$11,244/yr | Subscription, quote-based; free eval edition | cited in [Factible/FlexSim comparison](https://www.factible.io/en/blog/autodesk-flexsim), [GetApp](https://www.getapp.com/industries-software/a/simio/pricing/) |
| FlexSim (Autodesk) | ~$6,000/yr | Subscription only (Autodesk killed perpetual); free student version | [Factible](https://www.factible.io/en/blog/autodesk-flexsim), [Autodesk](https://www.autodesk.com/products/flexsim/overview) |
| Simul8 | $5,499–8,699/yr (Project/Business/Twin tiers) | Subscription; **perpetual licensing discontinued** | [Capterra](https://www.capterra.com/p/114609/SIMUL8-Professional/pricing/), [Simul8 perpetual notice](https://www.simul8.com/software/perpetual) |
| Arena (Rockwell) | Quote-only via distributors; free student edition | Perpetual + maintenance, enterprise motion | [Rockwell buying options](https://www.rockwellautomation.com/en-us/products/software/arena-simulation/buying-options.html) |
| ProModel (BigBear.ai) | Quote-only | Enterprise | vendor site |
| SimPy, Salabim, Ciw (Python), JaamSim (Java) | Free | MIT / Apache OSS, code-first | project repos |

### The shape of the gap

The market is a barbell: **free-but-you-must-code** (SimPy et al.) on one end, **$6k–19k/seat GUI suites with days-of-training** on the other. Between €0 and €5,500/yr there is essentially *nothing* aimed at a non-programmer with a queueing problem. Every incumbent's trend is *away* from this gap (subscription-only, higher prices, enterprise focus — Simul8 and FlexSim both killed perpetual). That's the opening.

Second observation: the incumbents' academic free tiers deliberately farm students who later specify the tool at employers. SimOpt can't win that long game; it must win people who were *never going to become simulation engineers*.

---

## B. Positioning and the wedge

### Where SimOpt cannot win

Feature depth (AnyLogic has 20+ years, material-handling/rail/pedestrian libraries), 3D fidelity (FlexSim), validated pedigree in regulated procurement, installed-base network effects, or enterprise sales. State this plainly and never compete there.

### The honest competitive threat nobody in the incumbent list poses

The real competitor is **ChatGPT/Claude + SimPy, for free**. A technical user can already paste "simulate my clinic with 3 doctors…" into Claude and get a runnable SimPy script. Academic work confirms LLM→simulation generation is an active frontier ([Springer JIM 2025 on LLM-driven production-planning simulation](https://link.springer.com/article/10.1007/s10845-025-02732-z), [LLM digital-twin updating](https://arxiv.org/pdf/2506.12091)). SimOpt's answer must be what raw LLM+SimPy lacks: a **validated engine the LLM cannot silently get wrong**, live visualization the stakeholder meeting can watch, built-in optimization loops, replications/confidence intervals, and zero Python environment friction. The product is the *trust and packaging layer*, not the model generation — generation is commoditizing under our feet.

### The single sharpest wedge

**Time-to-defensible-answer for people who will never learn a simulation tool: "Describe your system over coffee; walk into the 2 pm meeting with an animated model, confidence intervals, and an optimized configuration."**

Defense of this choice:
1. **It's the incumbents' structural blind spot.** Their value = modeling power for trained analysts; their UX assumes weeks of learning. They can bolt on AI copilots (and will), but a copilot inside a $12k tool still requires buying and learning the $12k tool. SimOpt inverts it: chat is the *only* interface.
2. **It monetizes urgency, not capability.** An ops manager with a staffing decision on Friday doesn't comparison-shop features; they pay €29–79 for an answer this week. Urgency-priced products tolerate feature shallowness.
3. **The live 2D visualization is the demo, the marketing asset, and the meeting artifact in one.** None of the free alternatives (SimPy scripts) produce something a plant manager will watch. This is SimOpt's already-built differentiator.
4. **Consultants multiply it.** An independent lean/logistics consultant who can turn a 3-week simulation study into a 2-day one changes their own margin — they'll pay 10× the prosumer price and bring repeat usage.

Sub-wedge to hold in reserve, not lead with: verticals the big tools ignore (clinics, labs, small job shops) — same product, vertical prompt templates.

### The one hard prerequisite

The engine currently has **documented correctness bugs that are disqualifying for a commercial claim**: SIM-56/57 (backlog + `docs/2026-07-05-critical-code-review.md`) include a wrong Triangular-distribution CDF (every triangular draw invalid), RNG range and overflow bugs, and reset-path divergence (SIM-58). A product whose pitch is "trust the engine, not the LLM's improvisation" cannot ship until SIM-56–59 are fixed and covered by statistical tests. This is the top of the commercial critical path, ahead of any UI work.

---

## C. Pricing model

### Licensing status of the repo — material finding

**There is no LICENSE file at `/home/jeltz/simopt/`** (verified 2026-08-25). Under copyright default, the code is **all-rights-reserved** even though the repo is public on GitHub (`JeltzProstetnic/simopt`). Consequences:

- Nobody may legally use, modify, or redistribute it — good for monetization, but "public + unlicensed" is an unstable posture: it invites unlicensed copying in practice and deters legitimate community contribution.
- **Nothing is given away yet. All options remain open.** This is the best possible starting position; do not MIT-license casually.
- One check the owner must do himself: dissertation-era code provenance. If the university or a former employer has any claim on the dissertation codebase, resolve before selling. Austrian universities typically leave dissertation copyright with the author, but verify.

**Recommendation:** dual structure.
- Engine repo → add a source-available license (**Functional Source License (FSL)** or **PolyForm Noncommercial**): free to read, learn, use non-commercially; commercial use requires a paid license. FSL converts to Apache-2.0 after 2 years, which preserves academic goodwill (citable, inspectable — matters for the dissertation lineage) while protecting the product window.
- Desktop app + MCP server + LLM orchestration → closed source, separate private repo. This is where the money is.
- Do **not** open-core with MIT on the engine: the engine *is* the moat's trust layer, and a one-person shop can't outrun forks.

### BYO-key economics — quantified

The user pays inference. A model-building session (construct + parameterize + a few optimization-explained iterations) plausibly consumes 200k–1,500k tokens of Claude/GPT traffic ≈ **$1–10/session at 2026 API prices, paid by the customer**. For SimOpt the marginal COGS per customer is ~zero: no inference bill, no GPU hosting, no per-seat cloud cost. Compare an AI-SaaS that hosts inference: typically 20–40% of revenue goes to compute. SimOpt's gross margin is **~95%** (only the merchant-of-record's ~5% + $0.50 per transaction — [Paddle/Lemon Squeezy fees](https://fungies.io/merchant-of-record-pricing-guide-2026/)). BYO-key also sidesteps the "AI wrapper gets margin-squeezed" failure mode entirely, and supports local models (Ollama/LM Studio) for data-sensitive factories — a genuine selling point in EU manufacturing.

Trade-off to state honestly: BYO-key adds onboarding friction (get an API key) for exactly the non-technical target user. Mitigate with a first-run wizard and Ollama auto-detection; consider a later "managed key" convenience tier (+€10/mo, metered) once volume justifies it.

### Recommended tiers

Desktop app in 2026: the market has moved to subscription (Simul8, FlexSim both dropped perpetual), but *indie* desktop economics still reward a Sublime/JetBrains-style fallback license — it converts subscription-averse engineers. Offer both.

| Tier | Price | What's included |
|---|---|---|
| **Free (Community)** | €0 | Full chat + engine, models up to ~10 nodes, 1 optimization run/day, watermarked exports, non-commercial use. Free tier is the marketing channel — must be genuinely useful. |
| **Pro** | **€29/mo or €290/yr** ($32/$320) | Unlimited model size, all optimizers, replications + CIs, PDF/CSV export, commercial use, both render modes |
| **Consultant** | **€79/mo or €790/yr** ($87/$870) | Pro + white-label/branded reports, scenario compare (before/after), model templates library, priority support |
| **Perpetual-fallback** | **€490 one-time** ($540) | Pro features, version pinned + 12 months of updates (JetBrains model) |
| **Team/Site** | **€1,990/yr** ($2,200) for 5 seats | Later — only when pulled by demand; do not build enterprise features speculatively |

Rationale for the €29 anchor: it's a credit-card decision below any procurement threshold, 20× cheaper than the cheapest incumbent, yet high enough that 100 Pro subscribers = €29k/yr — a meaningful outcome at achievable volume. The Consultant tier is where the real ARPU is; consultants routinely expense €790 without approval. Resist pricing lower: at €9/mo you need 10× the customers for the same money, and the audience is business users, not hobbyists.

---

## D. Financial model — 3-year P&L sketch

### Cost side (annual, EUR)

| Item | Y1 | Y2 | Y3 | Notes |
|---|---|---|---|---|
| AI dev tokens | 2,400 | 2,400 | 2,400 | Claude Max ~€200/mo covers agent development today (Fable currently included). **Risk flag:** if subsidized access ends and development moves to metered API, autonomous agent development at this intensity is realistically €500–2,000/mo — the single most volatile cost line. |
| Website + docs hosting | 200 | 200 | 300 | Static site (Hostinger already in hand), no backend needed for BYO-key |
| Code-signing certificate | 400 | 400 | 400 | OV/EV cert for Windows SmartScreen reputation — non-optional for a downloadable .exe; unsigned installers kill conversion |
| License-key backend | 0–300 | 300 | 300 | Keygen.sh or self-hosted on existing VPS |
| Payment processing | 5% of revenue | 5% | 5% | Merchant of record (Paddle or Lemon Squeezy) handles **all EU/global VAT** — the whole reason to use MoR ([source](https://solopreneurship.eu/reviews/best-payment-processors-digital-products-eu/)) |
| Accountant (AT) | 800 | 1,200 | 1,500 | Einzelunternehmen + Kleinunternehmer keeps this small initially |
| Misc (domain, trademark search, graphics) | 500 | 300 | 300 | |
| **Total fixed** | **~€4,500** | **~€4,800** | **~€5,200** | Excluding owner time and the token-cost risk flag |

Owner time is the real scarce input: at 3–5 h/week × 48 weeks ≈ 150–250 h/yr. Priced at his consulting-equivalent rate (~€150/h) that's €22–37k/yr of opportunity cost — worth stating so the kill criteria in §G mean something.

### Legal entity — recommendation

**Einzelunternehmen (sole trader) + Kleinunternehmerregelung.** Since 2025 Austria's small-business VAT exemption threshold is **€55,000 gross/yr**, with a 10% tolerance band, and — critically for software sold EU-wide — the **EU small-business scheme** now lets Austrian Kleinunternehmer sell VAT-exempt into other member states up to €100,000 EU-wide turnover, registered via FinanzOnline ([USP.gv.at](https://www.usp.gv.at/aktuelles/newsliste/kleinunternehmerregelung-ab-2025.html), [WKO](https://www.wko.at/vlbg/steuern/kleinunternehmerregelung-in-oesterreich-und-der-eu)). In practice the MoR route makes even this moot for B2C: Paddle is the seller of record and owns VAT. GmbH (or FlexKapG) only makes sense above roughly €60–80k sustained profit (liability shielding, 23% CIT vs. progressive income tax) or if the employer conflict (below) requires an arm's-length vehicle. Don't spend €10k capital + €2k/yr GmbH overhead on day one.

**Employer conflict — handle structurally, not morally.** Austrian/Liechtenstein employment law typically requires notifying the employer of a Nebentätigkeit, and R&D employment contracts often contain IP-assignment clauses for inventions "related to the employer's business." Three clean moves: (1) confirm the SimOpt codebase's dissertation provenance predates and is independent of Ivoclar employment — document it; (2) get a short written employer acknowledgment of the side business (generic "simulation software", no dental specifics); (3) **do not make dental manufacturing the public beachhead** — the Ivotion demo material stays internal. Beachhead instead: healthcare *clinics* and generic SMB manufacturing, which use the same queueing math with zero employer overlap. The domain expertise transfers; the conflict doesn't.

### Revenue scenarios (est. — reasoning shown, no false precision)

Assumptions: blended ARPU ≈ €350/yr (mix of Pro/Consultant/perpetual), free→paid conversion 2–4% (typical for prosumer freemium), annual churn 30–35% (prosumer desktop norm).

| Scenario | Y1 | Y2 | Y3 |
|---|---|---|---|
| **Low** (product ships late, no channel catches) | €1–3k (5–10 customers) | €5–10k | €10–15k |
| **Base** (ships mid-Y1, one channel works — likely LinkedIn/YouTube) | €4–8k (15–25 customers) | €18–35k (60–100) | €40–70k (120–200) |
| **High** (HN/viral moment + consultant word-of-mouth) | €12k | €50k | €100–150k |

Milestones against goals: **(i) cover cash costs** ≈ €5k/yr ≈ 15 Pro subscribers — achievable in Y1 base case. **(ii) meaningful side income** (€20k+/yr) ≈ 60–80 customers — Y2 base case. **(iii) replace an R&D-manager salary** (€100k+ gross) ≈ 300+ customers at current ARPU — only the Y3 high case, and honestly **unlikely at 3–5 h/week without either a consultant-channel flywheel or a vertical SaaS pivot at higher ARPU.** Say it plainly: this is a strong side-income candidate and a weak salary-replacement candidate on the stated time budget.

### Unit economics

- **CAC:** near-zero cash, paid in owner hours + agent tokens (content is agent-generated, demos are the product filming itself). Paid acquisition (search ads on "simulation software" — bid up by incumbents) is not viable at €29/mo ARPU; don't try.
- **LTV:** €290/yr ÷ 33% churn ≈ **~€870 Pro**, **~€2,400 Consultant**. With ~95% gross margin, LTV:CAC is excellent *provided CAC stays content-driven*.
- **Payback:** immediate (annual prepay, no cash CAC).

---

## E. Go-to-market — for someone with almost no time

Ranked by effort-to-return under the 3–5 h/week constraint. Core insight: **the product produces its own marketing footage** (animated factory-floor sims), and the agent can produce everything except the owner's face and network.

| Rank | Channel | Why | Owner-hours |
|---|---|---|---|
| 1 | **LinkedIn** (existing profile + ops/manufacturing network) | 2–3 posts/week, each a 30–60 s screen capture of a sim answering a real question ("How many pick stations does this warehouse actually need?"). Agent drafts, owner approves. His R&D-AI-manager credibility is the distribution asset. | ~1 h/wk |
| 2 | **YouTube** (demo + problem-genre videos) | "Simulating an emergency department in 10 minutes by talking" is genuinely novel footage in 2026. Evergreen search traffic for "clinic capacity planning", "warehouse throughput simulation". Agent writes scripts + does screen recording; owner voices or uses cloned/TTS voice. | ~1 h/wk |
| 3 | **SEO content site** | Agent-generated at ~zero marginal owner time: comparison pages ("AnyLogic alternatives", "Simul8 pricing vs"), problem guides, glossary. Compounds slowly; start immediately because of the lag. | ~0 |
| 4 | **Show HN + Reddit** (r/OperationsResearch, r/simulation, r/manufacturing, r/IndustrialEngineering) | One-shot spikes; do Show HN only when onboarding survives 500 simultaneous strangers. Genuine "I built this" posts, not marketing. | bursts |
| 5 | **Academic channel** (dissertation lineage) | A tool paper / demo at **Winter Simulation Conference** or a JOSS-style writeup gives citable legitimacy and reaches simulation lecturers — free tier for teaching seeds future consultants. Slow burn, high credibility per hour. | bursts |
| 6 | **Consultant partnerships** | After first 2–3 consultant customers exist: 20% referral or white-label arrangement. Highest ARPU channel but requires humans-talking-to-humans time — sequence it after product-led proof. | later |
| 7 | Marketplaces (Microsoft Store) | Distribution hygiene, not a channel. Low effort, low return. | ~0 |

### First 90 days of marketing (starts when the MVP demo is recordable, not before)

- **Wk 1–2:** Landing page + waitlist live (agent-built, Hostinger). Record the one canonical 3-min demo video. Set up Paddle/LS sandbox account, LinkedIn content calendar.
- **Wk 3–4:** Begin LinkedIn cadence (2/wk). Publish demo video on YouTube. First 3 SEO articles live.
- **Wk 5–6:** Free beta to first 20 waitlist users; instrument the one metric that matters (chat→completed-model rate). 2 more LinkedIn posts + 1 problem-genre video (clinic staffing).
- **Wk 7–8:** Fix the top 3 beta-onboarding failures. Post a build-in-public retrospective (LinkedIn + relevant subreddit). SEO batch 2 (comparison pages).
- **Wk 9–10:** Turn on payments. Announce launch pricing (founding-user 30% lifetime discount, capped at 50) to waitlist + LinkedIn.
- **Wk 11–12:** **Show HN** with the honest story ("I turned my dissertation DES engine into a talk-to-it simulator"). Warehouse-genre video. Email every beta user personally.
- **Wk 13:** Review against kill-criteria checkpoint 1 (§G).

---

## F. The first paying customer

**Who:** An independent operations/lean consultant, 40–55, ex-industry, solo or 2–3-person practice, somewhere in DACH/UK/US. Advises SMB manufacturers and clinics. Knows simulation studies win engagements but has never justified an AnyLogic license or the modeling time. Found SimOpt via a LinkedIn post shared into his feed or a YouTube search for "warehouse simulation without coding".

**What they pay:** €790/yr Consultant tier (or €79/mo for the first engagement, annualizing once it lands in a client deliverable).

**What must be true for them to pay — worked backwards to the ship list:**

1. *"I described my client's flow line in chat and the model matched reality within tolerance."* → MCP tool-loop must work end-to-end (**SIM-20, currently open, becomes P0**) and the engine must be numerically right (**SIM-56–59 fixed, statistically tested**).
2. *"The animation and stats panel looked professional in the client workshop."* → Already largely built (schematic/realistic renderers, stats panel). Needs: export/branded-report path (subset of SIM-28/SIM-43).
3. *"It ran on my laptop with my OpenAI key in under 10 minutes from download."* → Signed Windows installer, first-run key wizard, Ollama detection. **All new work.**
4. *"When the model was wrong, I could see why and correct it in chat."* → Model-summary read-back ("I built: Source(exp 5 min) → Buffer(15) → 2×Server…") + editable parameters. This trust loop is the make-or-break feature; an LLM that silently builds the wrong model kills the product with exactly this persona.
5. *"Someone answers when I hit a wall."* → An email address and a 48 h response norm. Nothing more at this stage.

**Ship list in priority order:** SIM-56–59 fixes → SIM-20 MCP E2E → embedded chat panel in the Avalonia app (BYO-key, provider-agnostic) → model read-back/confirm loop → signed installer + license keys + Paddle → report export. Everything else on the backlog (realistic renderer v2, human agents, 3D) is post-revenue polish.

---

## G. Risks and kill criteria

### Top risks, ranked by expected damage

1. **Frontier-model commoditization** — "just ask Claude" gets good enough with a generic code-interpreter for the technical half of the audience. *Mitigation:* own the non-technical UX + validated engine + visualization; ship fast; this window is 12–24 months, not 5 years.
2. **Trust failure** — one confidently wrong answer in a customer's real decision. *Mitigation:* SIM-56–59 first; replications with CIs by default (SIM-40); read-back loop; honest "model assumptions" panel in every report.
3. **Incumbent copilots** — AnyLogic/Simio ship chat interfaces. *Mitigation:* they'll bolt chat onto expert tools at expert prices; SimOpt's position (chat-only, €29) stays differentiated. Real risk is their *marketing* drowning the category.
4. **Owner-time collapse** — day job + scientific projects squeeze the 3–5 h/week to zero for a quarter, momentum dies. *Mitigation:* agent-autonomous pipeline for content and dev; kill criteria below prevent zombie mode.
5. **Employer IP/conflict surprise** — unresolved Nebentätigkeit or IP claim surfaces after revenue exists. *Mitigation:* §D structural steps done **before** first sale, not after.
6. **Token-subsidy end** — dev cost line jumps 5–10× if Max-subscription economics change. *Mitigation:* treat every month of current access as build-runway; front-load engine and product work now.

### Kill/pivot checkpoints (measurable, dated from "commercial build start")

| When | Gate | If missed |
|---|---|---|
| **Month 3** | MCP E2E works: a non-owner user builds a correct model of a described system via chat, unassisted, ≥50% of attempts. Engine bugs SIM-56–59 closed. | If the chat→model loop can't hit 50% assisted-free success, the core thesis fails → **pivot** to "agent-assisted consulting tool used by the owner/consultants only" or stop. |
| **Month 6** | 100+ free-tier activations AND ≥10 paying customers (any tier) AND at least one paying customer who was a stranger (not network). | No stranger will pay → distribution thesis broken. One more quarter on a single channel experiment, then stop. |
| **Month 12** | **€500+ MRR-equivalent** (≈ €6k ARR) and churn <50%/yr among the first cohort. | Below this after a year of shipping, the salary-replacement goal is dead and even side-income is marginal vs. owner opportunity cost → wind down to maintenance mode (perpetual licenses only, no new dev) or open-source the lot for academic reputation value. |
| **Month 24** | €2,000+ MRR and a repeatable channel (known cost-per-customer in hours). | Plateau below → hold as €10–20k/yr lifestyle side product deliberately, or sell the asset (micro-acquisition market exists for exactly this profile). |

A clear kill line is cheap insurance for a person whose scarcest asset is hours: the worst outcome isn't failure, it's three years of ambient guilt at €80/month.

---

## One-paragraph verdict

There is a real, durable gap between free-but-code and €6k-per-seat simulation tools, and an LLM-native, BYO-key desktop product is a credible wedge into it — but the wedge is *time-to-defensible-answer for non-experts*, the engine's known math bugs are a commercial blocker before any UI work, the repo's missing LICENSE is an asset to be spent deliberately (FSL the engine, close the app), sell through a merchant of record as an Austrian Kleinunternehmer-Einzelunternehmer, price at €29/€79/mo with a €490 perpetual fallback, market it through LinkedIn + YouTube footage the product generates itself, and hold to the kill gates — €500 MRR by month 12 or this becomes a deliberate hobby, which is also a legitimate outcome as long as it's chosen rather than drifted into.

---

### Sources

- [Fortune Business Insights — Simulation Software Market](https://www.fortunebusinessinsights.com/simulation-software-market-102435)
- [OpenPR — DES market report ($2.24B 2025)](https://www.openpr.com/news/4519441/industry-report-on-discrete-event-simulation-software-market)
- [360iResearch — DES Software Market 2025–2030](https://www.360iresearch.com/library/intelligence/discrete-event-simulation-software)
- [Grand View Research — Simulation Software](https://www.grandviewresearch.com/industry-analysis/simulation-software-market)
- [AnyLogic pricing (checkthat.ai)](https://checkthat.ai/brands/anylogic/pricing) · [AnyLogic USD price list (Scribd)](https://www.scribd.com/document/428955164/Prices-AnyLogic-USD-pdf)
- [Simio pricing (GetApp)](https://www.getapp.com/industries-software/a/simio/pricing/)
- [FlexSim pricing & licensing (Factible)](https://www.factible.io/en/blog/autodesk-flexsim) · [Autodesk FlexSim](https://www.autodesk.com/products/flexsim/overview)
- [Simul8 pricing (Capterra)](https://www.capterra.com/p/114609/SIMUL8-Professional/pricing/) · [Simul8 perpetual discontinuation](https://www.simul8.com/software/perpetual)
- [Arena buying options (Rockwell)](https://www.rockwellautomation.com/en-us/products/software/arena-simulation/buying-options.html)
- [Kleinunternehmerregelung ab 2025 (USP.gv.at)](https://www.usp.gv.at/aktuelles/newsliste/kleinunternehmerregelung-ab-2025.html) · [WKO — KU-Regelung Österreich & EU](https://www.wko.at/vlbg/steuern/kleinunternehmerregelung-in-oesterreich-und-der-eu)
- [MoR fee comparison 2026 (Fungies)](https://fungies.io/merchant-of-record-pricing-guide-2026/) · [EU digital-product payment processors (Solopreneurship.eu)](https://solopreneurship.eu/reviews/best-payment-processors-digital-products-eu/)
- [LLM→simulation model generation, J. Intelligent Manufacturing 2025](https://link.springer.com/article/10.1007/s10845-025-02732-z) · [Continuously Updating Digital Twins with LLMs (arXiv)](https://arxiv.org/pdf/2506.12091)
