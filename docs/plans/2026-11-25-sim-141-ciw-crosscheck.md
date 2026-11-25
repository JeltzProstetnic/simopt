# SIM-141: Ciw as an independent cross-check, where no closed form reaches

**Status:** built 2026-11-25 (noon). Decisions: Q-CIW (P3), Q-CIW-WHERE (nightly), Q-CIW-CASES (entry loss and a
non-Jackson network), all in `docs/decisions.md`.

## Why

The analytic battery (SIM-64, SIM-97) checks SimOpt against closed forms: M/M/1, M/D/1, M/M/c, Jackson tandems,
M/M/1/K. A consultant's real network (UN-007) has general service times, rework and finite rooms, and no formula
reaches it. [Ciw](https://github.com/CiwPython/Ciw) (MIT) is a separate discrete-event queueing simulator, written
independently, in another language, with its own random numbers. Two independent engines agreeing on a network
neither can be checked on analytically is the next best evidence to a formula.

## What runs

- `python/tests/ciw_reference.py`: turns Ciw's records into SimOpt's metrics over SimOpt's window, the Student-t
  quantile, the standard error behind a reported interval, and the agreement rule. It needs no Ciw.
- `python/tests/test_ciw_reference.py`: those helpers on hand-made records (11 tests, every commit).
- `python/tests/test_ciw_crosscheck.py`: both networks in both engines, nightly only (`SIMOPT_NIGHTLY=1`; there a
  missing Ciw or server fails rather than skips).
- `.github/workflows/nightly.yml`: installs Python 3.12 and `ciw==3.2.7` (pinned: the reference moves only by a
  deliberate change), builds the MCP server, runs both test files. The step runs even when the .NET gate fails.

## The two networks

| | entry_loss_tandem | rework_network |
|---|---|---|
| arrivals | exponential, mean 1 | exponential, mean 1 |
| station 1 | 1 server, lognormal (mu = ln 0.8 - 0.125, sigma 0.5; mean 0.8) | 2 servers, gamma (k 2, theta 0.6; mean 1.2) |
| queue 1 | at most 3 waiting; an arrival finding it full is turned away | unbounded |
| station 2 | 1 server, triangular (0.3, 0.6, 1.2; mean 0.7) | 1 server, deterministic 0.6 |
| after station 2 | out | 15 % back to queue 1 for rework, 85 % out |

**Why only the first queue is finite.** A full queue inside a SimOpt network refuses the item, which is lost and
counted (`SchemaCatalog`: "an item a full queue refuses is lost"). Ciw blocks instead: the item stays at the server
upstream until there is room (`Node.block_individual`, blocking after service). An internal finite buffer is
therefore a different system in the two engines. An arrival from outside finding a full queue is turned away in
both, which the M/M/1/K battery (SIM-97) already pinned for SimOpt with "capacity = waiting room".

## Metrics, defined alike

Both engines measure the window from the warm-up (2,000) to the end (20,000):

| metric | SimOpt (`Instrumentation`) | Ciw (from `get_all_records()`) |
|---|---|---|
| `wait_q1`, `wait_q2` | recorded when an item leaves its queue, counted from the warm-up | mean `waiting_time` of the services starting in the window, each visit apart |
| `util_s1`, `util_s2` | time average of the busy share | service time inside the window over servers x window |
| `throughput` | items reaching the sink after the warm-up, per time | items leaving the network in the window, per time |
| `cycle_time` | creation to the sink | exit less the item's first arrival, every rework included |

Ciw runs 100 time units past the end, so that every service started before it has its record; nothing after the end
is counted.

## The rule

100 replications per engine. A metric passes when both 95 % intervals are narrow (half-width at most 2 % of the
mean, which stops a noisy engine hiding inside a wide interval) and the means differ by at most 3.29 combined
standard errors (99.9 %, the analytic battery's gate, `docs/decisions.md` 2026-08-26), plus half the last of the
four significant digits SimOpt reports. Twelve comparisons pass together 98.8 % of the time for engines that agree.
SimOpt's standard error is recovered from its reported interval (Student t, n - 1 degrees of freedom, as
`ReplicationRunner` computes it).

## Measured (WSL, 2026-11-25, `ciw` 3.2.7)

Both cases pass, 112 s for the module (Ciw about 0.45 s per replication; SimOpt a few seconds for all 100):

| case | metric | SimOpt | Ciw | z | half-widths |
|---|---|---|---|---|---|
| entry_loss_tandem | wait_q1 | 0.8389 ± 0.0024 | 0.8388 ± 0.0026 | +0.07 | 0.29 % / 0.31 % |
| | wait_q2 | 0.2274 ± 0.0013 | 0.2282 ± 0.0013 | −0.87 | 0.57 % / 0.58 % |
| | util_s1 | 0.7364 ± 0.0010 | 0.7366 ± 0.0010 | −0.29 | 0.13 % / 0.13 % |
| | util_s2 | 0.6442 ± 0.0008 | 0.6444 ± 0.0008 | −0.43 | 0.12 % / 0.13 % |
| | throughput | 0.9203 ± 0.0011 | 0.9207 ± 0.0011 | −0.56 | 0.12 % / 0.12 % |
| | cycle_time | 2.5660 ± 0.0035 | 2.5669 ± 0.0034 | −0.38 | 0.14 % / 0.13 % |
| rework_network | wait_q1 | 0.9265 ± 0.0115 | 0.9298 ± 0.0139 | −0.36 | 1.25 % / 1.49 % |
| | wait_q2 | 0.5421 ± 0.0044 | 0.5449 ± 0.0044 | −0.90 | 0.81 % / 0.80 % |
| | util_s1 | 0.7051 ± 0.0013 | 0.7051 ± 0.0015 | −0.02 | 0.18 % / 0.21 % |
| | util_s2 | 0.7055 ± 0.0010 | 0.7058 ± 0.0012 | −0.36 | 0.14 % / 0.17 % |
| | throughput | 0.9991 ± 0.0011 | 1.0000 ± 0.0016 | −0.88 | 0.11 % / 0.16 % |
| | cycle_time | 3.8460 ± 0.0185 | 3.8509 ± 0.0212 | −0.35 | 0.48 % / 0.55 % |

The tandem turns away 8 % of arrivals (throughput 0.920 against 1 offered). The rework network's throughput
contains the offered rate 1 in both engines. Eleven of twelve z are negative, but the metrics of one network move
together (a run with fewer arrivals has a lower throughput, utilisation and wait at once), so that is not twelve
independent signs, and no |z| exceeds 0.9.

**The gate has teeth.** With SimOpt's station 2 made 2 % slower than Ciw's (triangular 0.306, 0.612, 1.224;
scratch script, not committed), the three metrics that station drives fail and the other three still pass:
wait_q2 z +24.5, util_s2 z +21.0, cycle_time z +16.0; wait_q1 +0.07, util_s1 −0.29, throughput −0.56. The
utilisations' allowed difference is about 0.25 % of the value, so a service-time error of about a third of a
percent at a station would already show.

**Before this change** the nightly workflow ran green on `8265e46` (workflow_dispatch run 45606086447,
3 min 26 s), the baseline MG asked for.
