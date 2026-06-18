task: true
file: docs/pending-glass-demo.md
backlog: SIM-55
description: |
  FSIM-03 is COMPLETE — glass digital twin + optimizer extracted to private furkansim (pushed); simopt is now framework-only (pushed). Open follow-ups:
  (1) SIM-55: decide the Ivotion defaults test — `IvotionOptimizationViewModelTests.Defaults_MatchLockedInDecisions` fails because the Ivotion VM defaults `MinimizeCostPerPiece` but the test expects `MaximizeThroughput`. Pick which side is correct and fix. (Pre-existing, unrelated to glass.)
  (2) furkansim app needs a VISUAL run-confirmation (twin renders + "Apply to Viz" works) — do this in a FURKANSIM session: `af furkansim`, then `dotnet run --project src/Furkansim.App` (export PATH="$HOME/.dotnet:$PATH"). See ~/furkansim/next-session-task.md (now repointed to Phase B / FSIM-10 calibration).
  (3) cfg-agent-fleet has an UNPUSHED commit 1f3a821 (furkansim registry/dashboard/inbox) — a cfg session must push it. furkansim fleet rotate-session.sh wiring still open (cfg inbox registration item).
