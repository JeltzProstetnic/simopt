<!-- task: true -->
task: true
file: docs/2026-07-05-critical-code-review.md
backlog: SIM-56
description: Fix the critical-review findings test-first, starting with SIM-56 (RNG contract — MersenneTwister `[0,1)` + int.MinValue overflow, R250_521 overrun, LCG NextInteger, exp-family U=0 guards). It is the root cause upstream of SIM-57's exp-family infinities and the Extensions crashes. Then SIM-57 (Triangular/Gamma/Median), SIM-58 (DES reset paths), SIM-59 (optimizer integrity), SIM-60 (low-severity math). Each fix: write the failing test first. Also ask the user whether to retire SIM-55 (glass — obsolete post-FSIM-03).
