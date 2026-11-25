"""SimOpt from Python: build a model as topology JSON, validate it, run replicated experiments and
searches through the SimOpt MCP server, and get the results as plain data ready for pandas (SIM-130).

    from simopt import Server, Topology, exponential, triangular

    clinic = (Topology("clinic", seed=7)
              .source("arrivals", arrival=exponential(5))
              .queue("exam_q").station("exam", servers=8, service=triangular(20, 30, 40))
              .sink("exit").connect("arrivals", "exam_q", "exam", "exit")
              .metric("wait_exam", "wait_time", "exam_q"))
    with Server() as server:
        model = server.create_model(clinic)
        frame = model.run_experiment(duration=540, warmup=60).frame()
"""
from ._client import Server, ServerError, SimOptError
from ._results import Experiment, Model, Optimization
from ._topology import Topology, constant, empirical, exponential, gamma, lognormal, triangular, uniform
from . import steps

__all__ = ["Server", "ServerError", "SimOptError", "Model", "Experiment", "Optimization", "Topology", "steps",
           "exponential", "triangular", "uniform", "lognormal", "gamma", "constant", "empirical"]
__version__ = "0.1.0"
