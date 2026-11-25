<!-- MANDATORY at shutdown — fill this in every session, including sessions with nothing in flight.
     Required fields: task: true|false, file: <path>, backlog: <ID|none>, description: <text>
     Optional: resume: <the exact first command or step to run> — carried verbatim, shown in HANDOFF (CFG-948).
     The file: MUST point to a dedicated file (e.g., docs/pending-*.md), NEVER to session-context.md.
     rotate-session.sh extracts this section to next-session-task.md automatically. -->
task: true
file: docs/pending-sim-122-continuation.md
backlog: SIM-126
description: Restart #5 wrote SIM-126's schema brief (Q-PROC-SCHEMA), did SIM-110 in part (layout, pause, engine-measured numbers; rest waits Q-VIEW-PATH), SIM-93 in part (Q-IVO-STATS) and SIM-118. Read MG's answers to Q-PROC-SCHEMA, Q-VIEW-PATH and Q-IVO-STATS first; build what they unblock, otherwise SIM-117 or SIM-86.
resume: Read docs/pending-sim-122-continuation.md (section "restart #5" and the decisions), then check Telegram for MG's answers before choosing the next item.
