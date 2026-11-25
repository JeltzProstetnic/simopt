"""SIM-141: the helpers the nightly Ciw cross-check stands on, checked on hand-made records so that they run
on every commit without Ciw. The fields are those of Ciw's own DataRecord (ciw/data_record.py, 3.2.7)."""
from collections import namedtuple

import pytest

from ciw_reference import agreement, ciw_metrics, interval_standard_error, t_975

Record = namedtuple("Record", ["id_number", "customer_class", "original_customer_class", "node", "arrival_date",
                               "waiting_time", "service_start_date", "service_time", "service_end_date",
                               "time_blocked", "exit_date", "destination", "queue_size_at_arrival",
                               "queue_size_at_departure", "server_id", "record_type"])


def served(id, node, arrived, start, end, destination):
    return Record(id, 0, 0, node, arrived, start - arrived, start, end - start, end, 0.0, end, destination,
                  0, 0, 1, "service")


def rejected(id, node, arrived):
    return Record(id, 0, 0, node, arrived, None, None, None, None, None, arrived, None, 0, 0, None, "rejection")


# A window from 10 to 20 on a single node with one server.
WINDOW = dict(warmup=10.0, end=20.0)


def test_a_wait_counts_when_its_service_starts_inside_the_window_both_ends_included():
    records = [served(1, 1, 5.0, 9.0, 11.0, -1),    # started before the warm-up: not counted
               served(2, 1, 8.0, 10.0, 12.0, -1),   # started at the warm-up: counted, waited 2
               served(3, 1, 16.0, 20.0, 22.0, -1),  # started at the end: counted, waited 4
               served(4, 1, 20.0, 21.0, 23.0, -1)]  # started after the end: not counted
    measured = ciw_metrics(records, servers={1: 1}, **WINDOW)
    assert measured["wait"][1] == pytest.approx(3.0), f"measured {measured['wait'][1]}"


def test_utilisation_clips_each_service_to_the_window_and_divides_by_every_server():
    records = [served(1, 1, 5.0, 8.0, 12.0, -1),    # 2 inside
               served(2, 1, 12.0, 13.0, 16.0, -1),  # 3 inside
               served(3, 1, 18.0, 18.0, 25.0, -1)]  # 2 inside
    one = ciw_metrics(records, servers={1: 1}, **WINDOW)
    two = ciw_metrics(records, servers={1: 2}, **WINDOW)
    assert one["utilization"][1] == pytest.approx(7.0 / 10.0), f"measured {one['utilization'][1]}"
    assert two["utilization"][1] == pytest.approx(7.0 / 20.0), f"measured {two['utilization'][1]}"


def test_throughput_counts_exits_from_the_network_inside_the_window_and_never_a_rejection():
    records = [served(1, 1, 0.0, 1.0, 9.0, -1),     # left before the warm-up
               served(2, 1, 9.0, 9.0, 10.0, -1),    # left at the warm-up: counted
               served(3, 1, 10.0, 11.0, 14.0, 2),   # went on to node 2, not out
               served(3, 2, 14.0, 14.0, 15.0, -1),  # then out: counted
               rejected(4, 1, 15.0),                # turned away: no exit
               served(5, 1, 18.0, 19.0, 21.0, -1)]  # left after the end
    measured = ciw_metrics(records, servers={1: 1, 2: 1}, **WINDOW)
    assert measured["throughput"] == pytest.approx(2 / 10.0), f"measured {measured['throughput']}"


def test_a_cycle_time_runs_from_the_first_arrival_through_every_rework_to_the_exit():
    records = [served(7, 1, 11.0, 11.0, 12.0, 2),
               served(7, 2, 12.0, 13.0, 14.0, 1),   # sent back for rework
               served(7, 1, 14.0, 15.0, 16.0, 2),
               served(7, 2, 16.0, 16.0, 17.0, -1),
               served(8, 1, 12.0, 12.0, 13.0, 2),
               served(8, 2, 13.0, 14.0, 15.0, -1)]
    measured = ciw_metrics(records, servers={1: 1, 2: 1}, **WINDOW)
    assert measured["cycle_time"] == pytest.approx((6.0 + 3.0) / 2), f"measured {measured['cycle_time']}"
    # Every visit's wait counts at its node: node 1 waited 0, 1 and 0.
    assert measured["wait"][1] == pytest.approx(1.0 / 3), f"measured {measured['wait'][1]}"


@pytest.mark.parametrize("df, expected", [(9, 2.2622), (29, 2.0452), (99, 1.9842), (999, 1.9623)])
def test_the_student_t_quantile_matches_the_table(df, expected):
    assert t_975(df) == pytest.approx(expected, abs=2e-4), f"t_975({df}) = {t_975(df)}"


def test_a_95_percent_interval_gives_back_its_standard_error():
    # 100 values with standard error 0.01: the interval is mean +- 1.9842 * 0.01.
    se = interval_standard_error(10.0 - 0.019842, 10.0 + 0.019842, 100)
    assert se == pytest.approx(0.01, rel=1e-3), f"measured {se}"


def test_two_estimates_agree_within_the_combined_error_at_99_9_percent():
    same = agreement(simopt_mean=1.000, simopt_se=0.004, ciw_mean=1.000, ciw_se=0.004)
    near = agreement(simopt_mean=1.017, simopt_se=0.004, ciw_mean=1.000, ciw_se=0.004)  # z = 3.0
    far = agreement(simopt_mean=1.020, simopt_se=0.004, ciw_mean=1.000, ciw_se=0.004)   # z = 3.5
    assert same.agrees and near.agrees, f"z {same.z}, {near.z}"
    assert not far.agrees, f"z {far.z}"
    assert far.z == pytest.approx(0.020 / (0.004 * 2 ** 0.5), rel=1e-3)


def test_the_last_digit_simopt_rounds_to_is_not_held_against_it_and_nothing_more():
    # SimOpt reports four significant digits, so 1.235 may stand for anything from 1.2345 to 1.2355:
    # half a unit (0.0005) is allowed on top of 3.29 combined standard errors (0.000465 here).
    within = agreement(simopt_mean=1.235, simopt_se=0.0001, ciw_mean=1.2344, ciw_se=0.0001)
    beyond = agreement(simopt_mean=1.235, simopt_se=0.0001, ciw_mean=1.2339, ciw_se=0.0001)
    assert within.agrees, f"z {within.z}"
    assert not beyond.agrees, f"z {beyond.z}"
