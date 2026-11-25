"""SIM-126 schema, slice 6: processes from Python. The builder writes resources, stocks and process nodes with
their closed list of steps exactly as the schema reads them, so the brief's car wash comes out as the brief
writes it; against the real server it validates, warns that its soap runs out, and runs."""
import json

import pytest

from simopt import Topology, exponential, steps, uniform

BRIEF_NODES = json.loads("""[
  {"id": "arrivals", "type": "source",   "params": {"arrival": {"type": "exponential", "mean": 3}}},
  {"id": "bays",     "type": "resource", "params": {"capacity": 2}},
  {"id": "soap",     "type": "stock",    "params": {"initial": 40, "capacity": 40}},
  {"id": "wash", "type": "process", "steps": [
    {"type": "seize", "resource": "bays", "patience": {"type": "uniform", "min": 5, "max": 20}, "outlet": "gave_up"},
    {"type": "take",  "stock": "soap", "amount": 1},
    {"id": "washing", "type": "delay", "duration": {"type": "uniform", "min": 4, "max": 6}}
  ]},
  {"id": "supplier", "type": "process", "steps": [
    {"id": "low", "type": "wait_until", "stock": "soap", "at_most": 5},
    {"type": "delay", "duration": {"type": "uniform", "min": 30, "max": 60}},
    {"type": "put", "stock": "soap", "up_to": 40}
  ]},
  {"id": "washed", "type": "sink"},
  {"id": "left",   "type": "sink"}
]""")


def car_wash():
    return (Topology("car wash")
            .source("arrivals", arrival=exponential(3))
            .resource("bays", capacity=2)
            .stock("soap", initial=40, capacity=40)
            .process("wash", [steps.seize("bays", patience=uniform(5, 20), outlet="gave_up"),
                              steps.take("soap", 1),
                              steps.delay(uniform(4, 6), id="washing")])
            .process("supplier", [steps.wait_until("soap", at_most=5, id="low"),
                                  steps.delay(uniform(30, 60)),
                                  steps.put("soap", up_to=40)])
            .sink("washed").sink("left")
            .connect("arrivals", "wash", "washed")
            .connect("wash", "left", outlet="gave_up")
            .metric("gave-up", "share", "left")
            .metric("wait-for-bay", "wait_time", "bays")
            .metric("bays-busy", "utilization", "bays")
            .metric("soap-empty", "empty_share", "soap"))


def test_the_builder_writes_the_briefs_car_wash():
    wash = json.loads(json.dumps(car_wash()))

    assert wash["nodes"] == BRIEF_NODES
    assert wash["connections"] == [{"from": "arrivals", "to": "wash"}, {"from": "wash", "to": "washed"},
                                   {"from": "wash", "to": "left", "outlet": "gave_up"}]
    assert wash["metrics"][0] == {"id": "gave-up", "kind": "share", "node": "left"}


def test_the_steps_omit_what_is_not_given():
    assert steps.seize("crew") == {"type": "seize", "resource": "crew"}
    assert steps.release("crew", id="done") == {"id": "done", "type": "release", "resource": "crew"}
    assert steps.take("parts", uniform(1, 3), patience=exponential(5), outlet="short") == {
        "type": "take", "stock": "parts", "amount": {"type": "uniform", "min": 1, "max": 3},
        "patience": {"type": "exponential", "mean": 5}, "outlet": "short"}
    assert steps.put("bins", 2) == {"type": "put", "stock": "bins", "amount": 2}
    assert steps.wait_until("bins", at_least=10) == {"type": "wait_until", "stock": "bins", "at_least": 10}
    assert Topology("t").resource("crew")["nodes"] == [{"id": "crew", "type": "resource"}]


def test_an_outlet_belongs_to_one_connection():
    with pytest.raises(ValueError):
        Topology("t").connect("a", "b", "c", outlet="gave_up")


def test_the_car_wash_against_the_real_server():
    from simopt import Server, ServerError
    from simopt._client import server_command
    try:
        server_command()
    except ServerError as missing:
        pytest.skip(f"no SimOpt MCP server: {missing}")

    with Server(timeout=300) as server:
        report = server.validate(car_wash())
        model = server.create_model(car_wash())
        rows = model.run_experiment(duration=600, warmup=0, replications=10).rows()

    assert report["valid"] is True
    assert any(w["code"] == "STOCK_RUNS_OUT" and w["node"] == "soap" for w in report["warnings"])
    means = {row["metric"]: row["mean"] for row in rows}
    assert set(means) == {"gave-up", "wait-for-bay", "bays-busy", "soap-empty"}
    assert 0.05 < means["gave-up"] < 0.4
    assert 0.05 < means["soap-empty"] < 0.4
