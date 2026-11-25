"""SIM-141, nightly: the same network run in Ciw and in SimOpt, every metric compared (docs/plans/2026-11-25-
sim-141-ciw-crosscheck.md). Both networks are out of reach of a closed form; the cases are MG's choice of
2026-11-25 (Q-CIW-CASES):

  entry_loss_tandem  two single-server stations in line, the first queue holding at most 3 waiting, so an
                     arrival finding it full is turned away; lognormal then triangular service. Only the
                     first queue is finite on purpose: a full queue INSIDE a SimOpt network loses the item,
                     where Ciw blocks it at the server upstream, so an internal finite buffer would not be
                     the same system in the two engines.
  rework_network     a two-server gamma station, then a deterministic one, after which 15 % go back to the
                     first queue for rework (non-Jackson: neither service is exponential).

Each engine runs REPLICATIONS independent replications; a metric passes when both 95 % intervals are narrow
(half-width at most 2 % of the mean) and the means differ by at most 3.29 combined standard errors (99.9 %,
the gate of the analytic battery, docs/decisions.md 2026-08-26) plus SimOpt's rounding. Runs only with
SIMOPT_NIGHTLY=1; there a missing Ciw or server fails rather than skips."""
import math
import os

import pytest

if os.environ.get("SIMOPT_NIGHTLY") != "1":
    pytest.skip("nightly only: SIMOPT_NIGHTLY=1 (needs ciw and a built server)", allow_module_level=True)

import ciw  # noqa: E402  (nightly: a missing Ciw is a failure, not a skip)

from ciw_reference import (agreement, ciw_metrics, interval_standard_error, mean_and_standard_error,  # noqa: E402
                           t_975)
from simopt import Server, Topology, constant, exponential, gamma, lognormal, triangular  # noqa: E402

END = 20000.0
WARMUP = 2000.0
REPLICATIONS = 100
NARROW = 0.02
CIW_TAIL = 100.0  # Ciw runs on past the end so that every service started before it has its record

ARRIVAL_MEAN = 1.0

# entry_loss_tandem
TANDEM_Q1_CAPACITY = 3
TANDEM_S1_MU, TANDEM_S1_SIGMA = math.log(0.8) - 0.5 ** 2 / 2, 0.5  # lognormal, mean 0.8
TANDEM_S2 = (0.3, 0.6, 1.2)  # triangular min, mode, max: mean 0.7

# rework_network
REWORK_S1_SERVERS, REWORK_S1_K, REWORK_S1_THETA = 2, 2, 0.6  # gamma, mean 1.2
REWORK_S2 = 0.6  # deterministic
REWORK_SHARE = 0.15

METRICS = [("wait_q1", "wait_time", "q1", ("wait", 1)),
           ("wait_q2", "wait_time", "q2", ("wait", 2)),
           ("util_s1", "utilization", "s1", ("utilization", 1)),
           ("util_s2", "utilization", "s2", ("utilization", 2)),
           ("throughput", "throughput", "exit", ("throughput", None)),
           ("cycle_time", "cycle_time", "exit", ("cycle_time", None))]


def with_metrics(topology):
    for id, kind, node, _ in METRICS:
        topology.metric(id, kind, node)
    return topology


def entry_loss_tandem():
    simopt = with_metrics(
        Topology("entry_loss_tandem", seed=141)
        .source("arrivals", arrival=exponential(ARRIVAL_MEAN))
        .queue("q1", capacity=TANDEM_Q1_CAPACITY)
        .station("s1", servers=1, service=lognormal(mu=TANDEM_S1_MU, sigma=TANDEM_S1_SIGMA))
        .queue("q2").station("s2", servers=1, service=triangular(*TANDEM_S2))
        .sink("exit")
        .connect("arrivals", "q1", "s1", "q2", "s2", "exit"))
    reference = ciw.create_network(
        arrival_distributions=[ciw.dists.Exponential(rate=1 / ARRIVAL_MEAN), None],
        service_distributions=[ciw.dists.Lognormal(mean=TANDEM_S1_MU, sd=TANDEM_S1_SIGMA),
                               ciw.dists.Triangular(*TANDEM_S2)],
        routing=[[0.0, 1.0], [0.0, 0.0]],
        number_of_servers=[1, 1],
        queue_capacities=[TANDEM_Q1_CAPACITY, float("inf")])
    return simopt, reference, {1: 1, 2: 1}


def rework_network():
    simopt = with_metrics(
        Topology("rework_network", seed=141)
        .source("arrivals", arrival=exponential(ARRIVAL_MEAN))
        .queue("q1").station("s1", servers=REWORK_S1_SERVERS, service=gamma(REWORK_S1_K, REWORK_S1_THETA))
        .queue("q2").station("s2", servers=1, service=constant(REWORK_S2))
        .router("inspect", "probabilistic")
        .sink("exit")
        .connect("arrivals", "q1", "s1", "q2", "s2", "inspect")
        .connect("inspect", "exit", weight=1 - REWORK_SHARE)
        .connect("inspect", "q1", weight=REWORK_SHARE))
    reference = ciw.create_network(
        arrival_distributions=[ciw.dists.Exponential(rate=1 / ARRIVAL_MEAN), None],
        service_distributions=[ciw.dists.Gamma(shape=REWORK_S1_K, scale=REWORK_S1_THETA),
                               ciw.dists.Deterministic(REWORK_S2)],
        routing=[[0.0, 1.0], [REWORK_SHARE, 0.0]],
        number_of_servers=[REWORK_S1_SERVERS, 1])
    return simopt, reference, {1: REWORK_S1_SERVERS, 2: 1}


def run_ciw(network, servers):
    """Every metric's replicate values, one Ciw run per replication, seeded 0 .. REPLICATIONS - 1."""
    values = {id: [] for id, *_ in METRICS}
    for replication in range(REPLICATIONS):
        ciw.seed(replication)
        simulation = ciw.Simulation(network)
        simulation.simulate_until_max_time(END + CIW_TAIL)
        measured = ciw_metrics(simulation.get_all_records(), servers, warmup=WARMUP, end=END)
        for id, _, _, (name, node) in METRICS:
            values[id].append(measured[name] if node is None else measured[name][node])
    return values


@pytest.fixture(scope="module")
def server():
    with Server(timeout=600) as running:
        yield running


@pytest.mark.parametrize("case", [entry_loss_tandem, rework_network], ids=lambda case: case.__name__)
def test_simopt_agrees_with_ciw(server, case):
    topology, network, servers = case()
    experiment = server.create_model(topology).run_experiment(duration=END, warmup=WARMUP,
                                                              replications=REPLICATIONS)
    assert not experiment.errors, experiment.errors
    simopt = {row["metric"]: row for row in experiment.rows()}
    reference = run_ciw(network, servers)

    t = t_975(REPLICATIONS - 1)
    lines, failures = [], []
    for id, *_ in METRICS:
        row = simopt[id]
        simopt_se = interval_standard_error(row["ci_low"], row["ci_high"], REPLICATIONS)
        ciw_mean, ciw_se = mean_and_standard_error(reference[id])
        verdict = agreement(row["mean"], simopt_se, ciw_mean, ciw_se)
        widths = (t * simopt_se / abs(row["mean"]), t * ciw_se / abs(ciw_mean))
        line = (f"{id:<11} SimOpt {row['mean']:.4f} +- {t * simopt_se:.4f}   Ciw {ciw_mean:.4f} +- {t * ciw_se:.4f}"
                f"   z {verdict.z:+.2f}   half-widths {widths[0]:.2%} / {widths[1]:.2%}")
        lines.append(line)
        if not verdict.agrees:
            failures.append(f"{line}: differ by {verdict.difference:+.4f}, allowed {verdict.allowed:.4f}")
        if max(widths) > NARROW:
            failures.append(f"{line}: an interval is wider than {NARROW:.0%} of its mean")
    print(f"\n{case.__name__}, {REPLICATIONS} replications of {END:g} ({WARMUP:g} warm-up) per engine:")
    print("\n".join(lines))
    assert not failures, "\n" + "\n".join(failures)
