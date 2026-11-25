# SIM-130: a clean way in from Python

**Status:** done 2026-11-25 (night, unattended, cfg caretaking, restart #4): `7565faf`, `986d073` and the
README commit; 22 pytest tests, the acceptance ones against the real server.
**Serves:** MG's SIM-122 conclusion (2026-11-25): SimOpt wins as simulation-optimisation with sound statistics
that agents and Python users can drive, provided Python can reach it cleanly. UN-001/004 (a model stated
as data), UN-012/013 (replicated results with intervals), UN-015 (search).
**Builds on:** SIM-65 (the topology schema), SIM-66 (`validate_model`, `patch_model`, `run_experiment`),
SIM-69 and SIM-68 (`optimize`).

## The ask

MG, backlog SIM-130 (P0): *a thin Python package that builds a model as SIM-65 topology JSON, validates and
runs it through the MCP tools of SIM-66 (`validate_model`, `run_experiment`, and the optimiser of SIM-69),
and returns results as plain data ready for pandas. In-process pythonnet stays a documented alternative.*

## Measured before designing

A spike (Python, no dependencies) spawned `dotnet SimOpt.McpServer.dll` and spoke to it over stdio:
newline-delimited JSON-RPC 2.0; `initialize` with protocol `2025-06-18` is answered with the same
version; `tools/list` gives `optimize, run_experiment, patch_model, run_simulation, list_models,
validate_model, get_schema, list_templates, create_model, get_status`; a tool's answer is one text content
holding the tool's JSON, and a refusal is `{"error": ..., "validation": {...}}` in that text.

## Decisions (unattended choices; MG may prefer otherwise)

**D1. Where and what.** `python/` in this repository: `pyproject.toml`, the package `simopt`, Python 3.10
or later, **no required dependency**; pandas is optional (`simopt[pandas]`).

**D2. Transport: a small client in the package, not the `mcp` SDK.** About a hundred lines of synchronous
JSON-RPC over the server's stdio. The SDK would add an async API and a dependency tree to a package whose
whole job is ten tool calls; the protocol surface used (initialize, initialized, tools/call) is small and
pinned by the spike above and by the package's own tests against the real server.

**D3. Finding the server.** `Server(command=None)`: the environment variable `SIMOPT_SERVER` (a `.dll`,
run with `dotnet`, or an executable), else this repository's build output
(`src/SimOpt.McpServer/bin/{Release,Debug}/net9.0/SimOpt.McpServer.dll`, found from the package's own
location), with `dotnet` from `DOTNET_ROOT`, `~/.dotnet` or `PATH`. Otherwise an error that says how to
build it. Shipping the server inside a wheel is a packaging question for later.

**D4. The API mirrors the tools.** `Server` (a context manager owning the process) has `validate`,
`create_model`, `schema`, `models`; a `Model` (its `id` and `version`) has `patch`, `run_experiment`,
`optimize`, `run`. Keyword arguments carry the tools' own names (`replications_per_eval`,
`max_evaluations`, ...), so the tool descriptions document the package too. One rule set: the package
checks nothing the server checks (SIM-66's decision).

**D5. Building a topology.** Plain dicts are the contract. A small `Topology` builder (`source`, `queue`,
`station`, `delay`, `router`, `sink`, `connect`, `metric`) and distribution helpers (`exponential`,
`triangular`, `uniform`, `lognormal`, `gamma`, `constant`, `empirical`) return exactly that dict, so a
user can mix them with hand-written JSON.

**D6. Errors are exceptions, not data.** A tool answer with `error` raises `SimOptError` with `.message`
and `.validation` (the report, every fault at once). A server that exits or stops answering raises
`ServerError` with the end of its stderr.

**D7. Results as plain data.** Each result keeps the tool's answer as `.raw`. `Experiment.rows()` gives one
dict per scenario and metric (`scenario, metric, mean, ci_low, ci_high`, and `diff, diff_ci_low,
diff_ci_high, significant` against the baseline); `Optimization.rows()` one per top point (the variables
as columns, `objective`, `feasible`). `.frame()` turns rows into a pandas DataFrame, importing pandas only
then.

## Steps (test-first, a commit each)

1. The client and `Server` (D2, D3, D6): handshake, a tool call, a refusal raised, a dead server raised;
   unit tests against a fake server script, so they need no .NET.
2. `Topology` builder and distributions (D5); `Model`, `run_experiment` and `optimize` with rows and
   frames (D4, D7). **Acceptance**, against the real server: the clinic built in Python, a flu scenario
   compared with the baseline into a DataFrame, and `optimize` answering 7 exam rooms.
3. README section and the pythonnet alternative documented.

## Progress and measured numbers

| step | commit | tests |
|---|---|---|
| 1 client, `Server`, errors, finding the server | `7565faf` | 10 |
| 2 builder, `Model`, rows and frames; acceptance on the real server | `986d073` | 22 |
| 3 README, pythonnet verified | this commit | 22 |

- **A stale Release build answered without the newer tools** (`Unknown tool: 'validate_model'`), because the
  package preferred Release over Debug. It now starts the newest of the two builds (D3, revised).
- **pythonnet, measured:** loaded into the .NET 9 runtime that the server's build names, the first JSON call
  fails inside the MCP SDK's type initialiser: the build carries `System.Text.Json` 10.0.3 and the runtime's
  own version 9 is loaded instead (the normal host resolves this through `deps.json`; pythonnet's loader does
  not). Loaded into the .NET 10.0.2 runtime with a three-line runtime config, `CreateModel` and a 10-replication
  `RunExperiment` work in process (clinic wait 2.05, 95 % interval [1.12, 2.99]). The recipe is in
  `python/README.md`.

## Outside this item

A wheel that carries the server binary, continuous integration for the Python tests, async use, and the
processes of SIM-126 (they are C# iterators; their schema part is separate).
