<!-- MANDATORY at shutdown — fill this in every session, including sessions with nothing in flight.
     Required fields: task: true|false, file: <path>, backlog: <ID|none>, description: <text>
     Optional: resume: <the exact first command or step to run> — carried verbatim, shown in HANDOFF (CFG-948).
     The file: MUST point to a dedicated file (e.g., docs/pending-*.md), NEVER to session-context.md.
     rotate-session.sh extracts this section to next-session-task.md automatically. -->
task: true
file: docs/pending-sim-122-continuation.md
backlog: SIM-132
description: SIM-132 (give Delay<T> a capacity, default 1, visible refusal, tests), then SIM-65 step 2b (delay node with unlimited capacity, connection weight), then steps 3-6. MG decisions still open: Q-PRIO (now incl. SIM-132), Q-PRIV-RULE, Q-STAGED, Q-CIW, Q-ASYNC, and SIM-105's Q-SFM9, Q-PRIVHOME, Q-FMT, Q-VALID.
resume: Read docs/pending-sim-122-continuation.md and docs/pending-sim-65-continuation.md (Status 2026-11-25), then write SIM-132's failing tests in tests/SimOpt.Tests/Simulation/Templates/.
