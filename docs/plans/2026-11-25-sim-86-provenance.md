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

## Still open in SIM-86

- **Slice B, a run record a third party can re-run** (UN-033, UN-034): `run_experiment` with a
  `record` path writes one JSON file holding the model document, the call with every default resolved,
  the answer, and a SHA-256 over the record's content (SIM-88's hook); a `reproduce_run` tool re-runs a
  record and reports "identical" or the first figure that differs. Low regret: machine format, opt-in.
- **Slice C, the form for an expert report** (UN-034): a human-readable rendering of a record
  (provenance table, assumptions, results with intervals). Its language and format are MG's call
  (Q-PROV-REPORT).
- **The standing simplifications of schema v1** (UN-035: "what it did not model"): moves between nodes
  take no time unless a delay node is placed; stations never fail; no shifts, breaks or priorities;
  each replication starts empty and idle. These belong in the record and in `get_schema`, not in every
  agent answer.
- **Python:** `Experiment.frame().attrs["provenance"]`, so provenance stays with a pandas frame.
- **Known gap:** a document without `seed` is run with 42 (`TopologyDefinition.Seed` is a plain int),
  and that default cannot be told apart from a given 42, so it is not listed in `assumed`. Making the
  seed nullable changes the schema's C# contract; not done unattended.
