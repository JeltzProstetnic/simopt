# Simulation run report

Rendered from the run record `clinic-run.json` by SimOpt.McpServer 1.0.0+ff2c874 (`report_run`). The figures are the recorded ones, to four significant digits.

## Provenance

| | |
|---|---|
| Record | `clinic-run.json`, simopt-run/1 |
| Record hash (SHA-256) | `349f929430a72fa19cb24d74b443baa664c1648114813d0bf87ce5e6264811b9` |
| Record integrity | Intact: the record still hashes to its record_sha256. |
| Model | clinic, version 1 |
| Model hash (SHA-256) | `29fc47074d8f13fafe1b369b4d7c5d6aad028e5a739b64015d3e0c4931eb94c0` |
| Model integrity | Intact: the model in the record hashes to the model_sha256 the result names. |
| Tool call | `run_experiment` |
| Recorded with | SimOpt.McpServer 1.0.0+ff2c874 |

## Run settings

| | |
|---|---|
| Run length | 540 |
| Warm-up | 54 |
| Replications | 5 |
| Base seed | 7 |
| Confidence | 95 % |
| Metrics asked for | all |

Replication r runs at a seed derived from the base seed (ReplicationRunner.SeedFor), so the same base seed gives the same replications on any machine.

## Scenario: baseline

| Metric | Mean | 95 % interval |
|---|---|---|
| wait_exam | 1.18 | [0.3678, 1.993] |
| los | 31.01 | [30.15, 31.88] |

## Scenario: flu

Made from the model by: `set arrivals.arrival.mean = 4`

| Metric | Mean | 95 % interval |
|---|---|---|
| wait_exam | 4.331 | [1.762, 6.9] |
| los | 34.49 | [31.85, 37.12] |

| Metric | Difference from baseline | 95 % interval | Significant |
|---|---|---|---|
| wait_exam | 3.151 | [1.251, 5.05] | yes |
| los | 3.475 | [1.348, 5.603] | yes |

## What the run assumed

Values the run used without being given them, as its result lists them (UN-035):

| Value | Taken as |
|---|---|
| warmup | a tenth of the duration: 54 |
| seed | the model's: 7 |
| exam_q.capacity | unlimited |
| exam_q.discipline | fifo |

## What the model does not represent

The limits of schema v1, as recorded with the run (UN-035):

- Items move between connected nodes in no time; travel, transport and handling take time only where a delay node or a delay step is placed.
- Stations never break down, and their servers work without shifts, breaks or set-ups.
- Arrivals keep one distribution for the whole run: no daily pattern and no peak hours.
- All items are alike: no item types, attributes or priorities; a queue serves first in first out or last in first out, and a router never looks at the item.
- A station's servers are identical and serve one item at a time each, and a station shares nothing with another; what activities share (staff, bays) is a resource that process steps seize, one unit per step.
- A process has no loops or conditions, and an item holds a unit only within one process node; it frees every unit it still holds when it leaves.
- An item waits in its queue until served and never leaves early, except at a seize or take step with a patience; an item a full queue refuses is lost, and counted.
- Every replication starts empty and idle; only the warm-up keeps that start out of the figures.

## How to repeat it

1. With the SimOpt MCP server: call `reproduce_run` with the path of this record. It runs the recorded call on the recorded model, from the record alone, and says whether every figure came out the same.
2. In Python: `simopt.Server().reproduce_run("clinic-run.json")`.
3. The model alone: the record's `model` object is the schema v1 document; saved as a file, `load_model` opens it, and its SHA-256 is the model hash above.
