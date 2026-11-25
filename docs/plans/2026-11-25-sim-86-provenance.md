# SIM-86: result provenance and assumption record

2026-11-25, restart #6 (unattended, cfg caretaking). Serves UN-033 (independently reproducible results),
UN-034 (complete provenance), UN-035 (declared assumptions). Leaves the hook for SIM-88 / UN-036.

## What `run_experiment` and `optimize` already carried (SIM-66, SIM-69)

| UN-034 asks for | Before SIM-86 |
|---|---|
| model version | yes, but only as the server's version number, which means nothing to a third party without that server's registry |
| seed | yes (base seed; replication r runs at `ReplicationRunner.SeedFor(seed, r)`) |
| replication count | yes |
| warm-up handling | yes (the length; the default "a tenth of the duration" was not marked as a default) |
| tool version | yes (`SimOpt.McpServer 1.0.0+<commit>`) |
| every parameter value | **no**: neither the values nor anything that tied the result to them |
| exportable for an expert report | **no**: the answer is JSON for an agent |
| UN-035: inferred vs supplied | **no**: nothing said which values were defaults |
| a scenario's definition | **no**: only its name, not the patch that made it |

## Slice A (done, test-first)

- **`model_sha256`** on both results: the SHA-256 of the version's schema v1 document, exactly the
  bytes `save_model` writes, so `sha256sum clinic.json` on the saved file equals it. That ties every
  figure to every parameter value without sending the model in each answer (UN-023 keeps results
  small), and it is the hook SIM-88 needs: a stored record's integrity check starts from this hash.
- **The document is written with line feeds only** (`ModelDocument`, `NewLine = "\n"`). On Windows,
  .NET's indented JSON used `\r\n`, so the same model would have hashed differently there than here,
  against UN-033's "different hardware, a different operating system". `save_model`'s files change
  only on Windows (one byte per line).
- **`assumed`**: every value the run used without being given it, with the value used. Call arguments
  by name (`warmup`: "a tenth of the duration", `seed`: "the model's", `replications`: "10",
  `replications_per_eval`: "5"), `metrics`: "the default set; the model declares none", and every node
  parameter left to the catalogue by its patch path (`triage_q.capacity`: "unlimited",
  `triage_q.discipline`: "fifo", `s.servers`: "1", `in.arrival`: "exponential, mean 1"). A parameter
  given by its older alternative (`service_time`, `mean_interval`) counts as given. Absent when nothing
  was assumed. `optimize` leaves its decision variables out: they were searched, not assumed.
- **Each scenario carries its `patch`**; the baseline has none.
- `replications` (run_experiment) and `replications_per_eval` (optimize) are now optional in the tool
  signature, so the server can tell a given 10 from a default 10. Same defaults as before.

Measured: the clinic answer (3 metrics, warm-up given) is 683 characters, 1206 with a flu scenario.
Tests: `tests/SimOpt.Tests/McpServer/ProvenanceTests.cs` (8). Suite 1361 -> 1369 passed, 1 skipped;
Python package 23 passed.

Not changed: no template's results moved (nothing in the engine was touched), so his frozen reference
runs are unaffected and no comment on his issue #30 is needed.

## Slice B (done, test-first): a run record a third party can repeat

- `run_experiment(..., record: "run.json")` writes one file, format `simopt-run/1`: `model` (the schema
  v1 document), `call` (duration, replications, warm-up and seed resolved, the metrics asked for, the
  scenarios with their patches), `result` (the answer as returned), and `record_sha256` over all of it.
  The answer gains `record: {path, record_sha256}`. The path is checked before anything runs: it must end
  in `.json`, its folder must exist, and an existing file is never replaced (no overwrite flag: a record
  is evidence).
- `reproduce_run(path)` repeats a record on any server from the record alone and reports
  `record_intact` (the content still hashes to `record_sha256`), `model_intact` (the model still hashes
  to the answer's `model_sha256`), `identical` (every recorded figure came out the same), `differences`
  ("baseline los: recorded 39.2 [..], now 38.2 [..]"), the tool versions then and now, and a summary
  that says when a difference may come from a different tool version.
- The hash is taken through `JsonNode` on both sides, indented with line feeds, without the
  `record_sha256` field; numbers keep their written text, so re-serialising the parsed file gives the
  same bytes. A forged figure and a changed model parameter are both caught (tests).
- This is the hook for SIM-88, not SIM-88 itself: a hash in the file shows the file is consistent, not
  that nobody rewrote file and hash together. "Without trusting the storer" (UN-036) needs the hash held
  somewhere else (a signature or a timestamping service), which is SIM-88's job.
- Python: `Model.run_experiment(..., record=path)`, `Server.reproduce_run(path)`, and
  `Experiment.provenance` (the answer without its figures).

Tests: `RunRecordTests` (9), one Python acceptance test. Suite 1369 -> 1378 passed, 1 skipped; Python
23 -> 24 passed.

## The limits of schema v1 (done, test-first)

UN-035 also asks the tool to say what it did not model. `SchemaCatalog.Limits` states seven things a
schema v1 model does not represent (no travel time without a delay node; no breakdowns, shifts, breaks
or set-ups; stationary arrivals; all items alike, no priorities, a router never looks at the item;
identical servers, one item each, nothing shared between stations; no reneging, a refused item is lost
and counted; every replication starts empty and idle, only the warm-up removes that). Each was checked
against the builder. `get_schema` publishes them as `limits`, and every run record carries them inside
its hash. They are not in each agent answer, to keep answers small (UN-023). Tests: one in
`SchemaCatalogTests`, one in `RunRecordTests`; suite 1378 -> 1380 passed, 1 skipped.

## Still open in SIM-86

- **Slice C, the form for an expert report** (UN-034): a human-readable rendering of a record
  (provenance table, assumptions, results with intervals). Its language and format are MG's call
  (Q-PROV-REPORT).
- **Python:** `Experiment.frame().attrs["provenance"]`, so provenance stays with a pandas frame.
- **`optimize` records:** the same record for a search (its call, every evaluated point); not built yet.
- **Known gap:** a document without `seed` is run with 42 (`TopologyDefinition.Seed` is a plain int),
  and that default cannot be told apart from a given 42, so it is not listed in `assumed`. Making the
  seed nullable changes the schema's C# contract; not done unattended.
