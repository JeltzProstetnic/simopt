# SIM-67: models outlive the server

**Status:** done 2026-11-25 (night, unattended, cfg caretaking, restart #4), both steps in one commit.
Measured: a clinic saved at version 2 and reopened by another server gives byte-identical experiment
results at the same seed. Suite 1331 passed, 1 skipped; Python 23 passed. Q-STORE (D4) is MG's.
**Serves:** UN-005 (*a model can be saved, reopened, and shared with someone else who can run it; it
outlives the process and the tool version that created it*). Models live in the server's memory
(`ModelRegistry`) and die with it; the Python package (SIM-130) starts a server per `with` block, so this
bites at once there.

## Decisions (unattended choices; MG may prefer otherwise)

**D1. A saved model is its topology file, nothing else.** `save_model` writes one version's topology as
SIM-65 JSON (`schema_version` "1", indented), the same document `create_model` takes, so the file is
portable, readable, diffable and runs on any later server that reads schema v1. The version history stays
in the session: a version worth keeping is saved as its own file.

**D2. Three tools.** `save_model(model_id, path, version?, overwrite?)` writes the file and answers
`{model_id, version, path, bytes}`; `load_model(path)` reads a file and registers it exactly as
`create_model` would (the same checks, the same answer, a faulty file refused with every fault);
`export_model(model_id, version?)` returns the topology inline, for a client that does not share the
server's disk.

**D3. Saving does not overwrite by accident.** An existing file is refused unless `overwrite` is true, and
the path must end in `.json`. The server writes where its caller asks: it is the caller's own agent on
the caller's machine, as for every other file that agent writes.

**D4. Not built: an automatic store.** Writing every model to a directory and reloading it when the
server starts ("re-hydrate on reconnect" in the backlog entry) would change what a plain server does on
disk. It is left to MG (Q-STORE in the handover); D1-D3 already let a model outlive the process.

## Steps

1. The three tools, test-first: a saved and reloaded model gives the same results as the original at the
   same seed; refusals for an existing file, a path without `.json`, an unknown model or version, a
   missing file and a faulty model.
2. The Python package: `Model.save(path)`, `Model.export()`, `Server.load_model(path)`.
