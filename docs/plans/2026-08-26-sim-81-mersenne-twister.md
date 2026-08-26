<!-- Action: reference -->
<!-- Tracked-by: SIM-81 -->
<!-- consumed-by: docs/plans/2026-08-26-sim-64-analytic-battery.md (the gate this change was waiting for) -->
# SIM-81 — MersenneTwister is now a Mersenne Twister

Closes SIM-81, promoted to P1 by owner ruling on 2026-08-26 and done immediately after SIM-64
landed, which is the gate it had been waiting on since it was filed.

## What was wrong

Two defects, both in `MersenneTwister`:

1. **The state was seeded from `System.Random.Next()`**, and the read index started at 0 rather than
   624. The twist runs only when the index reaches 624, so **the first 624 outputs were
   `System.Random`'s output verbatim** — the class was a different generator entirely for its first
   block. `Next()` returns `[0, int.MaxValue)`, so bit 31 was never set in any of them.
2. **The tempering transform was absent.** The raw recurrence was returned directly.

## What changed

- The reference initialisation `mt[i] = 1812433253·(mt[i−1] ^ (mt[i−1] >> 30)) + i`, index starting
  at 624 so the first draw twists.
- The reference tempering, applied to the output only — the recurrence state is untouched.
- `NextUInt()` made public. The only conclusive test of a Mersenne Twister is whether its raw words
  match the published vector, and that cannot be asserted through `NextInteger()`, which masks bit
  31 away — hiding precisely the defect being fixed.

Pinned against the canonical seed-5489 vector plus two other seeds, produced by an independent
implementation written from the algorithm rather than from this code. Suite 914 → 922.

## Measured before and after

**The unambiguous defect, and the one that is fully repaired.** KS against Uniform(0,1) on the raw
word divided by 2³², first block only (n = 624, critical value 1.62762 at α = 0.01):

| | √n·D | mean | max |
|---|---|---|---|
| old | **12.4989** | 0.253106 | 0.499645 |
| new | **0.6686** | 0.499036 | 0.996461 |

The old first block was confined to the lower half of the range, exactly as predicted. Bit 31 was
set in **0 of the first 624 draws**; the reference sets it in **302**.

**What did *not* change measurably, stated because overclaiming here would be worse than saying
nothing.** At n = 1,000,000 both generators pass a uniformity test comfortably (√n·D 0.7746 old,
1.0327 new) — the first-block defect is diluted to invisibility in a large sample, so a large-sample
uniformity check is *not* the before/after evidence the backlog entry expected it to be. A
chi-square on the low three bits over 10⁶ draws also passes for both (7.01 against 6.22, df = 7).
Tempering's benefit is 623-dimensional equidistribution, which no 1-dimensional frequency test can
see; it is justified by conformance to the specification, not by anything measured here.

**Effect on the simulator.** Every random stream in the product moved. The full suite passes with
zero failures and zero re-baselined constants — no test anywhere pinned a simulated number tightly
enough to notice, which is itself worth knowing.

Across the analytic battery, mean |error| as a fraction of the reported half-width over its 14
statistics, on four independent seed sets:

| seed set | old | new |
|---|---|---|
| 20260826 | 0.407 | **0.139** |
| 111 | 0.425 | **0.304** |
| 20260827 | 0.453 | 0.479 |
| 987654321 | 0.399 | **0.286** |
| average | 0.421 | 0.302 |

Better on three of four, worse on one. **Both generators pass every assertion on every seed set**,
so this is a conformance fix rather than the repair of a wrong answer, and it should not be
described as one.

One observation worth recording without overstating it: the old generator's figure barely moves
across seed sets (0.399–0.453) while the new one's varies as sampling noise should (0.139–0.479). A
quantity that ought to fluctuate and doesn't is the signature of a systematic component — plausibly
cross-stream correlation, since per-node seeds are derived by XOR from one base seed and the old
generator's first block was a deterministic function of that seed through `System.Random`. Four
seed sets cannot settle it. **SIM-96's coverage meta-test is what would**, and this is now a second
reason to build it.

## Consequences elsewhere

- `UniformMapping.TryMapToInteger` still masks rather than shifts, but the justification changed and
  the comment was rewritten accordingly. It was a defence against this defect; it is now kept
  because masking makes no assumption about which bits carry entropy, and the function serves four
  generators of varying quality.
- Two comments in `RandomSourceContractTests` described the defect as current and were corrected.
  `MersenneTwister_FirstDraws_SpanTheFullUnitInterval` is retained as a cheap regression; the
  conclusive test is now `MersenneTwisterReferenceTests`.
- **The other three generators are unaudited.** `R250_521`,
  `LinearCongruentialGenerator` and `SubtractiveCongruentialGenerator` have never been checked
  against any reference vector, and nothing here says anything about them. They are not the
  default, which is the only reason this is not urgent.
