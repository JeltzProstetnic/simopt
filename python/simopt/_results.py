"""SIM-130 D4, D7: a model on the server, and results kept as the tool's answer plus plain rows."""


def _frame(rows):
    try:
        import pandas
    except ImportError as missing:
        raise ImportError("frame() needs pandas: pip install pandas (or simopt[pandas]); rows() needs nothing.") from missing
    return pandas.DataFrame(rows)


def _interval(estimate):
    ci = (estimate or {}).get("ci95") or [None, None]
    return ci[0], ci[1]


class Experiment:
    """A run_experiment answer. ``raw`` is the answer as the tool gave it."""

    def __init__(self, raw):
        self.raw = raw

    @property
    def summary(self):
        return self.raw.get("summary", "")

    @property
    def provenance(self):
        """SIM-86: everything in the answer except the figures: version, model_sha256, seed, run length,
        warm-up, replications, confidence, tool, what was assumed, and the record if one was written."""
        return {key: value for key, value in self.raw.items() if key not in ("scenarios", "summary")}

    @property
    def errors(self):
        """The scenarios that could not run, with why."""
        return {s["name"]: s["errors"] for s in self.raw.get("scenarios", []) if s.get("errors")}

    def rows(self):
        """One row per scenario and metric: its mean and 95 % interval, and the difference from the
        baseline with its paired interval where the scenario has one."""
        rows = []
        for scenario in self.raw.get("scenarios", []):
            against = scenario.get("vs_baseline") or {}
            for metric, estimate in (scenario.get("metrics") or {}).items():
                low, high = _interval(estimate)
                difference = against.get(metric)
                diff_low, diff_high = _interval(difference)
                rows.append({"scenario": scenario["name"], "metric": metric, "mean": estimate.get("mean"),
                             "ci_low": low, "ci_high": high,
                             "diff": difference.get("diff") if difference else None,
                             "diff_ci_low": diff_low, "diff_ci_high": diff_high,
                             "significant": difference.get("significant") if difference else None})
        return rows

    def frame(self):
        """rows() as a pandas DataFrame, carrying the answer's provenance in ``attrs["provenance"]``."""
        frame = _frame(self.rows())
        frame.attrs["provenance"] = self.provenance
        return frame


class Optimization:
    """An optimize answer. ``raw`` is the answer as the tool gave it."""

    def __init__(self, raw):
        self.raw = raw

    @property
    def best(self):
        return self.raw.get("best", {})

    @property
    def summary(self):
        return self.raw.get("summary", "")

    def rows(self):
        """One row per point the search ranks, best first: the variables, the objective, feasible."""
        return [{**p.get("point", {}), "objective": p.get("objective"), "feasible": p.get("feasible")}
                for p in self.raw.get("top", [])]

    def frame(self):
        return _frame(self.rows())


def _arguments(**given):
    """The tool's arguments, leaving out those not given so that the server's defaults apply."""
    return {key: value for key, value in given.items() if value is not None}


class Model:
    """A model registered on the server. Its keyword arguments carry the tools' own names."""

    def __init__(self, server, id, version=1, answer=None):
        self.server = server
        self.id = id
        self.version = version
        self.answer = answer

    def patch(self, operations):
        """patch_model: applies the operations as a new version, which later calls use."""
        answer = self.server.call("patch_model", {"model_id": self.id, "patch": list(operations)})
        self.version = answer.get("version", self.version)
        return answer

    def run_experiment(self, duration, replications=None, warmup=None, metrics=None, scenarios=None,
                       version=None, seed=None, record=None):
        """run_experiment; record (SIM-86) names a new .json file for the run record, which
        Server.reproduce_run repeats on any server."""
        return Experiment(self.server.call("run_experiment", _arguments(
            model_id=self.id, duration=duration, replications=replications, warmup=warmup, metrics=metrics,
            scenarios=scenarios, version=version, seed=seed, record=None if record is None else str(record))))

    def optimize(self, variables, objective, duration, warmup=None, replications_per_eval=None,
                 max_evaluations=None, max_seconds=None, strategy=None, seed=None, version=None, record=None):
        """optimize; record (SIM-86) names a new .json file for the run record, not with max_seconds."""
        return Optimization(self.server.call("optimize", _arguments(
            model_id=self.id, variables=list(variables), objective=objective, duration=duration, warmup=warmup,
            replications_per_eval=replications_per_eval, max_evaluations=max_evaluations,
            max_seconds=max_seconds, strategy=strategy, seed=seed, version=version,
            record=None if record is None else str(record))))

    def save(self, path, version=None, overwrite=None):
        """save_model (SIM-67): writes the version (default the latest) as topology JSON; an existing file
        is kept unless overwrite is true."""
        return self.server.call("save_model", _arguments(model_id=self.id, path=str(path), version=version,
                                                         overwrite=overwrite))

    def export(self, version=None):
        """export_model (SIM-67): the version's topology as a dict, the document create_model takes."""
        return self.server.call("export_model", _arguments(model_id=self.id, version=version))["topology"]

    def run(self, duration):
        """run_simulation: one run of the latest version, without replications or intervals."""
        return self.server.call("run_simulation", {"model_id": self.id, "duration": duration})

    def __repr__(self):
        return f"Model({self.id!r}, version {self.version})"
