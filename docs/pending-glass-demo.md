<!-- Action: reference -->
<!-- Tracked-by: SIM-55 -->
# Glass production-line demo — handoff & follow-ups

Built 2026-06-18 (commit 8a6ea35). A live Avalonia **digital twin** of a 5-step glass base-mass line plus an **evolutionary optimizer** (live best-so-far fitness chart) that applies the optimized configuration back to the twin. Run guide: `docs/glass-demo.md`.

## Run the demo (e.g. office Fedora)
```bash
git pull
export PATH="$HOME/.dotnet:$PATH"   # if dotnet not already on PATH
dotnet run --project src/SimOpt.Visualization/SimOpt.Visualization.csproj
```
Topology dropdown defaults to "Glass Production Line (Base Mass)"; **Space** to run; the **"Optimization (Glass)"** tab → set objective/strategy → **Run** → **Apply to Viz**.

## What's where
- Twin topology: `VizTopology.GlassLine()` / `GlassLine(GlassSolution)` in `src/SimOpt.Visualization/Models/VizTopology.cs`
- Optimization domain: `src/SimOpt.Ivotion/Glass*.cs` (namespace `SimOpt.Glass`)
- UI: `ViewModels/GlassOptimizationViewModel.cs`, `Views/GlassOptimizationView.axaml`; tab in `Views/MainWindow.axaml`
- Tests: `tests/SimOpt.Tests/Glass/GlassOptimizationTests.cs` (4/4 pass)
- Headless scaffold: `examples/SimOpt.Examples.ProductionLine` (per-stage + bottleneck report)

## Follow-ups (SIM-55)
- **Pre-existing test failure (NOT from this work):** `IvotionOptimizationViewModelTests.Defaults_MatchLockedInDecisions` — Ivotion VM defaults `MinimizeCostPerPiece` but the test expects `MaximizeThroughput`. Full suite: 698 pass / 1 skip / 1 fail. Decide which side to correct.
- Glass classes live inside the `SimOpt.Ivotion` assembly (namespace `SimOpt.Glass`). Consider extracting to a dedicated `SimOpt.Glass` project — note `dotnet sln` does not handle this `.slnx` cleanly, so it needs a hand-written csproj + a ProjectReference from `SimOpt.Visualization`.
- Optional polish: richer/realistic glass floor layout (cf. SIM-45); live before/after compare canvas (cf. SIM-46).
- Fleet knowledge: `tmp/staged-simopt-viz-twin.md` → integrate into `~/.claude/knowledge/simopt-ops.md` (cfg-agent-fleet inbox item filed).
- Optional local-only: rename the on-screen label "Glass Production Line (Base Mass)" to the customer's preferred term if you want it shown verbatim — keep that change out of public commits.
