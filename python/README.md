# simopt (Python)

Build a SimOpt model in Python, validate it, run replicated experiments and searches, and get the results
as plain rows or pandas DataFrames. The package drives the SimOpt MCP server over its standard input and
output; it has no required dependency (pandas is optional). Design and decisions:
`docs/plans/2026-11-25-sim-130-python-package.md`.

## Install

```bash
dotnet build src/SimOpt.McpServer -c Release   # the server the package starts
pip install -e "python[pandas]"                # from the repository root
```

The package starts the server it finds first: the `.dll` or executable named by `SIMOPT_SERVER`, else the
newest build of `src/SimOpt.McpServer` in this repository. A `.dll` runs with `dotnet` from `DOTNET_ROOT`,
`~/.dotnet` or the `PATH`.

## The clinic, from Python

```python
from simopt import Server, Topology, exponential, triangular

clinic = (Topology("clinic", seed=7)
          .source("arrivals", arrival=exponential(5))
          .queue("triage_q").station("triage", servers=3, service=triangular(3, 5, 10))
          .queue("exam_q").station("exam", servers=8, service=triangular(20, 30, 40))
          .sink("exit")
          .connect("arrivals", "triage_q", "triage", "exam_q", "exam", "exit")
          .metric("wait_exam", "wait_time", "exam_q")
          .metric("los", "cycle_time"))

with Server() as server:
    print(server.validate(clinic)["valid"])          # every fault at once, nothing registered
    model = server.create_model(clinic)

    # ten replications of an 8-hour day after an hour of warm-up, and a flu scenario against it
    experiment = model.run_experiment(duration=540, warmup=60, replications=10, scenarios=[
        {"name": "flu", "patch": [{"op": "set", "path": "arrivals.arrival.mean", "value": 4}]}])
    print(experiment.frame())       # scenario, metric, mean, ci_low, ci_high, diff, diff_ci_low, ...

    # the fewest exam rooms keeping the mean wait under 15 minutes on its whole 95 % interval
    found = model.optimize(variables=[{"path": "exam.servers", "type": "integer", "min": 4, "max": 12}],
                           objective={"metric": "exam.servers", "direction": "min",
                                      "constraints": [{"metric": "wait_exam", "op": "<", "value": 15}]},
                           duration=540, warmup=60)
    print(found.best["point"], found.summary)   # {'exam.servers': 7} ...
```

A topology is a plain dict in the SIM-65 schema; `Topology` only saves typing, and `server.schema()`
returns the node types, parameters, metric kinds and distributions with a JSON Schema.

## What there is

| | |
|---|---|
| `Server(command=None, timeout=3600)` | starts the server; use it in a `with` block. `validate`, `create_model`, `load_model(path)`, `schema`, `models`, and `call(tool, arguments)` for any tool |
| `Model` | `patch(operations)` (a new version), `run_experiment(...)`, `optimize(...)`, `save(path)` and `export()` (the topology as a file or a dict, which `load_model` reopens in any later server), `run(duration)`; keyword arguments carry the MCP tools' own names |
| `Experiment` | `raw` (the tool's answer), `rows()`, `frame()`, `errors`, `summary` |
| `Optimization` | `raw`, `best`, `rows()` (one per ranked point), `frame()`, `summary` |
| `Topology` | `source`, `queue`, `station`, `delay`, `router`, `sink`, `node`, `connect(*path, weight=None)`, `metric` |
| distributions | `exponential`, `triangular`, `uniform`, `lognormal`, `gamma`, `constant`, `empirical` |

A refusal raises `SimOptError`, with `.message` and `.validation` (the model's report); a server that cannot
be found or stops raises `ServerError` with the end of its error output.

## In process, through pythonnet

The same tools can run inside the Python process through [pythonnet](https://pythonnet.github.io/), without
the server process. Verified on 2026-11-25 with pythonnet 3.2.1 and the .NET 10.0.2 runtime. The server's
build carries `System.Text.Json` 10, so it has to be loaded into a .NET 10 runtime: on .NET 9, the
runtime's own version 9 is used instead and the first JSON call fails.

```python
import json, os, sys
from pythonnet import load

BIN = "src/SimOpt.McpServer/bin/Release/net9.0"
# net10.runtimeconfig.json: {"runtimeOptions": {"tfm": "net10.0",
#                            "framework": {"name": "Microsoft.NETCore.App", "version": "10.0.0"}}}
load("coreclr", runtime_config="net10.runtimeconfig.json", dotnet_root=os.path.expanduser("~/.dotnet"))
import clr
sys.path.append(BIN)
clr.AddReference("SimOpt.McpServer")
clr.AddReference("ModelContextProtocol.Core")
from ModelContextProtocol import McpJsonUtilities
from SimOpt.McpServer.Models import TopologyDefinition
from SimOpt.McpServer.Simulation import ModelRegistry
from SimOpt.McpServer.Tools import SimulationTools
from System.Text.Json import JsonSerializer

tools = SimulationTools(ModelRegistry())
topology = JsonSerializer.Deserialize[TopologyDefinition](json.dumps(clinic), McpJsonUtilities.DefaultOptions)
created = json.loads(tools.CreateModel(topology))
answer = json.loads(tools.RunExperiment(created["model_id"], 540.0, 10, 60.0, None, None, None, None))
```

The tools answer with the same JSON strings as over MCP. The package is the simpler route: it needs no
runtime matching, and a crash in the engine cannot take the Python process with it.

## Tests

```bash
cd python && python -m pytest
```

The client's tests use a stand-in server and need no .NET. The acceptance tests run the clinic against the
real server, and are skipped when no server is built.
