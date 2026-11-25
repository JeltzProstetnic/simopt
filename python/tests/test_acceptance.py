"""SIM-130 acceptance, against the real SimOpt MCP server: the clinic of the readiness review built in
Python, validated, a flu scenario compared with the baseline into a DataFrame, and the fewest exam rooms
keeping the mean wait under 15 minutes found by one optimize call. Skipped when no server is built."""
import pytest

from simopt import Server, ServerError, SimOptError, Topology, exponential, triangular
from simopt._client import server_command

try:
    server_command()
except ServerError as missing:
    pytest.skip(f"no SimOpt MCP server: {missing}", allow_module_level=True)


def clinic():
    return (Topology("clinic", seed=7)
            .source("arrivals", arrival=exponential(5))
            .queue("triage_q").station("triage", servers=3, service=triangular(3, 5, 10))
            .queue("exam_q").station("exam", servers=8, service=triangular(20, 30, 40))
            .sink("exit")
            .connect("arrivals", "triage_q", "triage", "exam_q", "exam", "exit")
            .metric("wait_exam", "wait_time", "exam_q")
            .metric("util_exam", "utilization", "exam")
            .metric("los", "cycle_time"))


@pytest.fixture(scope="module")
def server():
    with Server(timeout=300) as running:
        yield running


def test_the_clinic_validates(server):
    report = server.validate(clinic())
    assert report["valid"] is True


def test_a_broken_model_is_refused_with_every_fault(server):
    broken = Topology("broken").queue("q").sink("exit").connect("q", "exit")
    with pytest.raises(SimOptError) as refused:
        server.create_model(broken)
    assert any(e["code"] == "NO_SOURCE" for e in refused.value.validation["errors"])


def test_a_flu_scenario_against_the_baseline_becomes_a_frame(server):
    pandas = pytest.importorskip("pandas")
    model = server.create_model(clinic())

    experiment = model.run_experiment(duration=540, warmup=60, replications=10, scenarios=[
        {"name": "flu", "patch": [{"op": "set", "path": "arrivals.arrival.mean", "value": 4}]}])

    frame = experiment.frame()
    assert set(frame.scenario) == {"baseline", "flu"}
    assert set(frame.metric) == {"wait_exam", "util_exam", "los"}
    flu = frame[(frame.scenario == "flu") & (frame.metric == "wait_exam")].iloc[0]
    baseline = frame[(frame.scenario == "baseline") & (frame.metric == "wait_exam")].iloc[0]
    assert flu["mean"] > baseline["mean"], "more patients wait longer"
    assert flu["diff"] == pytest.approx(flu["mean"] - baseline["mean"], abs=0.01)
    assert baseline.ci_low < baseline["mean"] < baseline.ci_high


def test_the_fewest_exam_rooms_keeping_the_wait_under_15_is_seven(server):
    model = server.create_model(clinic())

    found = model.optimize(variables=[{"path": "exam.servers", "type": "integer", "min": 4, "max": 12}],
                           objective={"metric": "exam.servers", "direction": "min",
                                      "constraints": [{"metric": "wait_exam", "op": "<", "value": 15}]},
                           duration=540, warmup=60)

    assert found.best["point"] == {"exam.servers": 7}
    assert found.raw["strategy"] == "grid"
    assert found.rows()[0]["exam.servers"] == 7


def test_a_recorded_search_reproduces_identically_on_another_server(server, tmp_path):
    # SIM-86: optimize writes the same run record as run_experiment
    model = server.create_model(clinic())
    path = tmp_path / "search.json"

    model.optimize(variables=[{"path": "exam.servers", "type": "integer", "min": 6, "max": 8}],
                   objective={"metric": "exam.servers", "direction": "min",
                              "constraints": [{"metric": "wait_exam", "op": "<", "value": 15}]},
                   duration=540, replications_per_eval=3, record=path)

    with Server(timeout=300) as another:
        check = another.reproduce_run(path)
    assert check["identical"] and check["record_intact"] and check["model_intact"], check


def test_a_saved_model_reopens_in_another_server_and_runs_the_same(server, tmp_path):
    # SIM-67: a model outlives the server that built it
    model = server.create_model(clinic())
    model.patch([{"op": "set", "path": "exam.servers", "value": 7}])
    path = tmp_path / "clinic-7.json"

    saved = model.save(path)

    assert saved["version"] == 2 and path.is_file()
    assert model.export()["nodes"][4]["params"]["servers"] == 7
    with Server(timeout=300) as another:
        reopened = another.load_model(path)
        assert reopened.run_experiment(duration=540, warmup=60, replications=5).rows() == \
            model.run_experiment(duration=540, warmup=60, replications=5).rows()
    with pytest.raises(SimOptError, match="overwrite"):
        model.save(path)


def test_a_recorded_run_reproduces_identically_on_another_server(server, tmp_path):
    # SIM-86: a run record holds model, call and answer; any server repeats it from the record alone
    model = server.create_model(clinic())
    path = tmp_path / "run.json"

    experiment = model.run_experiment(duration=540, replications=5, record=path)

    assert experiment.provenance["model_sha256"] == experiment.raw["model_sha256"]
    assert experiment.provenance["assumed"]["warmup"] == "a tenth of the duration"
    assert experiment.raw["record"]["path"] == str(path) and path.is_file()
    with Server(timeout=300) as another:
        check = another.reproduce_run(path)
    assert check["identical"] and check["record_intact"] and check["model_intact"], check


def test_a_recorded_run_renders_as_a_report_in_english_and_german(server, tmp_path):
    # SIM-86 slice C: report_run turns a record into the appendix of an expert report
    model = server.create_model(clinic())
    path = tmp_path / "run.json"
    model.run_experiment(duration=540, replications=5, record=path)

    english = server.report_run(path)
    german = server.report_run(path, language="de")

    assert english["record_intact"] and english["model_intact"], english
    assert english["markdown"].startswith("# Simulation run report")
    assert "| Warm-up | 54 |" in english["markdown"]
    assert german["markdown"].startswith("# Bericht über einen Simulationslauf")


def test_a_patch_makes_a_new_version(server):
    model = server.create_model(clinic())
    assert model.version == 1

    model.patch([{"op": "set", "path": "exam.servers", "value": 7}])

    assert model.version == 2
    assert model.id in server.models()["model_ids"]
