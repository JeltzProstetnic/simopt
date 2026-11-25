"""SIM-141: Ciw (MIT, https://github.com/CiwPython/Ciw) as an independent reference for networks no closed form
reaches. This module turns Ciw's records into SimOpt's metrics and judges whether two estimates agree; it
needs no Ciw itself, so its tests run on every commit (test_ciw_reference.py). The cross-check that runs
both engines is test_ciw_crosscheck.py, nightly only.

Each metric is computed over the same window SimOpt uses, from the warm-up to the end of the run:
  wait        per node, the mean wait of the services that START inside the window (SimOpt records a
              wait when the item leaves its queue, Instrumentation.ObserveWaitingTime), each visit apart;
  utilization per node, busy time inside the window over servers x window (a time average);
  throughput  items leaving the network inside the window, per unit of time (a turned-away item never
              leaves it);
  cycle_time  for those items, the exit time less the time the item first arrived (every rework included).
"""
import math
from collections import defaultdict, namedtuple

# Two-sided 99.9 %: the containment gate of the analytic battery (docs/decisions.md, 2026-08-26).
Z_GATE = 3.2905267314919255
_Z_975 = 1.959963984540054


def ciw_metrics(records, servers, warmup, end):
    """SimOpt's metrics from one Ciw run's records (Simulation.get_all_records()), for the window
    [warmup, end]. ``servers`` maps each Ciw node number to its number of servers."""
    first_arrival = {}
    waits = defaultdict(list)
    busy = defaultdict(float)
    exits = []
    for r in records:
        first_arrival[r.id_number] = min(first_arrival.get(r.id_number, math.inf), r.arrival_date)
        if r.record_type != "service":
            continue
        if warmup <= r.service_start_date <= end:
            waits[r.node].append(r.waiting_time)
        busy[r.node] += max(0.0, min(r.service_end_date, end) - max(r.service_start_date, warmup))
        if r.destination == -1 and warmup <= r.exit_date <= end:
            exits.append(r)
    window = end - warmup
    return {
        "wait": {node: _mean(waits[node]) for node in servers},
        "utilization": {node: busy[node] / (count * window) for node, count in servers.items()},
        "throughput": len(exits) / window,
        "cycle_time": _mean([r.exit_date - first_arrival[r.id_number] for r in exits]),
    }


def _mean(values):
    return sum(values) / len(values) if values else math.nan


def t_975(df):
    """The 97.5 % quantile of Student's t with ``df`` degrees of freedom: the Cornish-Fisher expansion
    about the normal quantile (Abramowitz and Stegun 26.7.5), within 1e-4 of the table from 9 up."""
    z, v = _Z_975, float(df)
    g1 = (z ** 3 + z) / 4
    g2 = (5 * z ** 5 + 16 * z ** 3 + 3 * z) / 96
    g3 = (3 * z ** 7 + 19 * z ** 5 + 17 * z ** 3 - 15 * z) / 384
    g4 = (79 * z ** 9 + 776 * z ** 7 + 1482 * z ** 5 - 1920 * z ** 3 - 945 * z) / 92160
    return z + g1 / v + g2 / v ** 2 + g3 / v ** 3 + g4 / v ** 4


def interval_standard_error(low, high, n):
    """The standard error behind a 95 % Student-t interval over ``n`` replications (ReplicationRunner)."""
    return (high - low) / 2 / t_975(n - 1)


def mean_and_standard_error(values):
    n = len(values)
    mean = sum(values) / n
    sd = math.sqrt(sum((v - mean) ** 2 for v in values) / (n - 1))
    return mean, sd / math.sqrt(n)


def last_digit_slack(value, digits=4):
    """Half a unit in the last of the ``digits`` significant digits SimOpt rounds its answers to."""
    if value == 0:
        return 0.0
    return 0.5 * 10 ** (math.floor(math.log10(abs(value))) - digits + 1)


Agreement = namedtuple("Agreement", ["agrees", "z", "difference", "allowed"])


def agreement(simopt_mean, simopt_se, ciw_mean, ciw_se):
    """Two independent estimates agree when they differ by at most Z_GATE combined standard errors, plus
    half the last digit SimOpt rounds to. ``z`` is the plain difference over the combined error."""
    combined = math.sqrt(simopt_se ** 2 + ciw_se ** 2)
    difference = simopt_mean - ciw_mean
    allowed = Z_GATE * combined + last_digit_slack(simopt_mean)
    return Agreement(abs(difference) <= allowed, difference / combined if combined else math.inf, difference,
                     allowed)
