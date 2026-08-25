<!-- Action: await-user-decision -->
<!-- Tracked-by: SIM-87 -->
# Present this first: the beachhead may be wrong

**One decision is waiting. Nothing is blocked by it — the Slice 1 queue proceeds either way — but
it should be put to the owner before more positioning work is built on the current answer.**

## Why it is open again

On 2026-08-25 the owner chose **general discrete manufacturing, sold through independent
consultants**. Later the same session he disclosed something that was not on the table when that
choice was made (`docs/decisions.md` D-05):

> "Any software I create would be part of my Gutachtertätigkeit. I need to be able to simulate the
> interaction of computer systems with the real world to prove cases, this is why I have that
> product."

So the product's origin use case is **forensic** — expert evidence, as a gerichtlich beeideter
Sachverständiger — and the commercial venture is a derivative of it, not the other way round.

## The comparison

| | Manufacturing consultants | Sachverständige / forensic |
|---|---|---|
| Owner's standing in the market | none yet | **already a practitioner** |
| Distribution | must be built from zero | existing professional network |
| Willingness to pay | €790/yr is a considered purchase | expert-witness work bills at multiples |
| Competition | AnyLogic / FlexSim / Simio strongest here | **essentially unserved** |
| What the product must excel at | breadth of features | **correctness + reproducibility** |
| Owner can be customer zero | no | **yes** |
| Market size | large | small |
| Sales motion | product-led, agent-supportable | relationship-led, needs owner hours |
| Unpriced work | — | simulation-as-evidence admissibility literature |

The decisive asymmetry for a solo owner with a few hours a week: the forensic market needs almost
no distribution to be built, and what it demands is exactly what Slice 0 already delivered. The
manufacturing market needs a channel built from nothing against well-funded incumbents.

The decisive argument against: it is genuinely small, it cannot be sold by an agent writing
LinkedIn posts, and no claim about evidentiary use should be made until its admissibility basis has
been researched properly.

## The three options

**A — Switch the beachhead to forensic/Sachverständige.** Owner is customer zero, so the Month-3
kill gate becomes "does it work for my own casework" rather than "will a stranger pay". Fastest
possible validation loop. Accepts a smaller ceiling.

**B — Keep manufacturing, treat forensic as the proving ground.** Owner uses it on real casework to
harden it; positioning and marketing stay as decided. No repositioning cost, slower feedback.

**C — Forensic as wedge, manufacturing as volume.** Same engine, two positionings, sequenced.
Highest total ceiling; risks doing neither well on a few hours a week.

**Recommendation: C, sequenced as B-then-A** — use forensic casework as the proving ground now
(it costs nothing extra and the owner is already doing that work), keep the manufacturing
positioning as the public face, and revisit a deliberate forensic go-to-market only once the
engine has survived real case use. This defers the repositioning cost until there is evidence for
it, and avoids making an evidentiary claim before its basis exists.

**But this is a positioning and identity call, not a technical one. It is the owner's.**

## What does not change either way

Slice 1 (SIM-63 output statistics, SIM-64 analytic benchmark battery) is the next work under all
three options, and is more clearly correct under all of them than it was yesterday — confidence
intervals and closed-form validation are what a capacity study needs and what an opposing expert
demands. Build it, then ask.

---

## Operational note for the next session — Fable is unavailable until 23:00

The owner reported on **2026-08-25** that the Fable token budget is exhausted. **Do not spawn a
Fable subagent before 23:00 on 2026-08-25.** Use Opus for anything that would otherwise have gone
to Fable, and say so when reporting.

Nothing was lost: both Fable passes this session completed before the budget ran out, and their
full outputs are committed at `docs/commercial/2026-08-25-architecture-readiness-review.md` and
`docs/commercial/2026-08-25-business-strategy.md`. No Fable work is parked mid-flight.
