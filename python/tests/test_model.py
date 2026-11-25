"""SIM-130 step 2 (D4, D5, D7), without a server: the topology builder writes exactly the schema's JSON,
and results become one plain row per scenario and metric, or per searched point."""
import json

import pytest

from simopt import Experiment, Optimization, Topology, exponential, triangular, uniform, lognormal, gamma, constant, empirical


def test_the_builder_writes_the_schemas_json():
    clinic = (Topology("clinic", seed=7)
              .source("arrivals", arrival=exponential(5))
              .queue("triage_q")
              .station("triage", servers=3, service=triangular(3, 5, 10))
              .queue("exam_q", discipline="fifo")
              .station("exam", servers=8, service=triangular(20, 30, 40))
              .sink("exit")
              .connect("arrivals", "triage_q", "triage", "exam_q", "exam", "exit")
              .metric("wait_exam", "wait_time", "exam_q")
              .metric("los", "cycle_time"))

    assert json.loads(json.dumps(clinic)) == {
        "schema_version": "1", "name": "clinic", "seed": 7,
        "nodes": [
            {"id": "arrivals", "type": "source", "params": {"arrival": {"type": "exponential", "mean": 5}}},
            {"id": "triage_q", "type": "queue"},
            {"id": "triage", "type": "station", "params": {"servers": 3, "service": {"type": "triangular", "min": 3, "mode": 5, "max": 10}}},
            {"id": "exam_q", "type": "queue", "params": {"discipline": "fifo"}},
            {"id": "exam", "type": "station", "params": {"servers": 8, "service": {"type": "triangular", "min": 20, "mode": 30, "max": 40}}},
            {"id": "exit", "type": "sink"}],
        "connections": [{"from": "arrivals", "to": "triage_q"}, {"from": "triage_q", "to": "triage"},
                        {"from": "triage", "to": "exam_q"}, {"from": "exam_q", "to": "exam"}, {"from": "exam", "to": "exit"}],
        "metrics": [{"id": "wait_exam", "kind": "wait_time", "node": "exam_q"}, {"id": "los", "kind": "cycle_time"}],
    }


def test_a_weighted_connection_and_the_other_nodes():
    t = (Topology("t").delay("d", duration=uniform(1, 2)).router("r", policy="probabilistic")
         .connect("r", "a", weight=0.3).connect("r", "b", weight=0.7))
    assert t["nodes"] == [{"id": "d", "type": "delay", "params": {"duration": {"type": "uniform", "min": 1, "max": 2}}},
                          {"id": "r", "type": "router", "params": {"policy": "probabilistic"}}]
    assert t["connections"] == [{"from": "r", "to": "a", "weight": 0.3}, {"from": "r", "to": "b", "weight": 0.7}]
    with pytest.raises(ValueError):
        Topology("t").connect("a", "b", "c", weight=0.5)


def test_the_distributions_omit_what_is_not_given():
    assert exponential(rate=2) == {"type": "exponential", "rate": 2}
    assert lognormal(mean=10, stddev=2) == {"type": "lognormal", "mean": 10, "stddev": 2}
    assert lognormal(mu=1, sigma=0.5) == {"type": "lognormal", "mu": 1, "sigma": 0.5}
    assert gamma(k=2, theta=3) == {"type": "gamma", "k": 2, "theta": 3}
    assert constant(4) == {"type": "constant", "value": 4}
    assert empirical(0, 10, [0.2, 0.3, 0.5]) == {"type": "empirical", "min": 0, "max": 10, "probabilities": [0.2, 0.3, 0.5]}


EXPERIMENT = {
    "model_id": "m1", "replications": 10, "summary": "flu raises the wait",
    "scenarios": [
        {"name": "baseline", "metrics": {"wait_exam": {"mean": 3.2, "ci95": [1.0, 5.4]}, "los": {"mean": 50.0}}},
        {"name": "flu", "metrics": {"wait_exam": {"mean": 9.1, "ci95": [6.0, 12.2]}, "los": {"mean": 61.0, "ci95": [58.0, 64.0]}},
         "vs_baseline": {"wait_exam": {"diff": 5.9, "ci95": [4.0, 7.8], "significant": True}}},
        {"name": "broken", "errors": ["Patch: no node 'x'."]},
    ],
}


def test_an_experiment_is_one_row_per_scenario_and_metric():
    rows = Experiment(EXPERIMENT).rows()

    assert rows == [
        {"scenario": "baseline", "metric": "wait_exam", "mean": 3.2, "ci_low": 1.0, "ci_high": 5.4,
         "diff": None, "diff_ci_low": None, "diff_ci_high": None, "significant": None},
        {"scenario": "baseline", "metric": "los", "mean": 50.0, "ci_low": None, "ci_high": None,
         "diff": None, "diff_ci_low": None, "diff_ci_high": None, "significant": None},
        {"scenario": "flu", "metric": "wait_exam", "mean": 9.1, "ci_low": 6.0, "ci_high": 12.2,
         "diff": 5.9, "diff_ci_low": 4.0, "diff_ci_high": 7.8, "significant": True},
        {"scenario": "flu", "metric": "los", "mean": 61.0, "ci_low": 58.0, "ci_high": 64.0,
         "diff": None, "diff_ci_low": None, "diff_ci_high": None, "significant": None},
    ]
    assert Experiment(EXPERIMENT).errors == {"broken": ["Patch: no node 'x'."]}
    assert Experiment(EXPERIMENT).summary == "flu raises the wait"


OPTIMIZATION = {
    "strategy": "grid", "evaluations": 9, "summary": "exam.servers = 7 is the best",
    "best": {"point": {"exam.servers": 7}, "objective": 7, "feasible": True, "metrics": {"wait_exam": {"mean": 3.18, "ci95": [0.92, 5.43]}}},
    "top": [{"point": {"exam.servers": 7}, "objective": 7, "feasible": True},
            {"point": {"exam.servers": 8}, "objective": 8, "feasible": True}],
}


def test_a_search_is_one_row_per_point_it_ranks():
    found = Optimization(OPTIMIZATION)

    assert found.rows() == [{"exam.servers": 7, "objective": 7, "feasible": True},
                            {"exam.servers": 8, "objective": 8, "feasible": True}]
    assert found.best["point"] == {"exam.servers": 7}
    assert found.summary.startswith("exam.servers = 7")


def test_rows_become_a_pandas_frame():
    pandas = pytest.importorskip("pandas")
    frame = Experiment(EXPERIMENT).frame()
    assert isinstance(frame, pandas.DataFrame)
    assert list(frame.columns[:5]) == ["scenario", "metric", "mean", "ci_low", "ci_high"]
    assert frame.loc[(frame.scenario == "flu") & (frame.metric == "wait_exam"), "diff"].item() == 5.9
    assert list(Optimization(OPTIMIZATION).frame()["exam.servers"]) == [7, 8]


def test_a_frame_keeps_the_answers_provenance():
    # SIM-86: provenance travels with the figures, also once they are a DataFrame
    pytest.importorskip("pandas")
    answer = {**EXPERIMENT, "model_sha256": "ab" * 32, "seed": 7, "assumed": {"warmup": "a tenth of the duration"}}

    frame = Experiment(answer).frame()

    assert frame.attrs["provenance"]["model_sha256"] == "ab" * 32
    assert frame.attrs["provenance"]["assumed"] == {"warmup": "a tenth of the duration"}
    assert "scenarios" not in frame.attrs["provenance"]
