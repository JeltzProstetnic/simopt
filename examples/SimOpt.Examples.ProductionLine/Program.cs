using SimOpt.Mathematics.Stochastics.Distributions;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Enum;
using SimOpt.Simulation.Templates;

// ---------------------------------------------------------------------------
// Generic 5-stage serial production line — scaffold for the production-
// simulation PoC. Flow mirrors a typical base-mass line:
//
//   OrderRelease -> MaterialStore -> Prep -> IntermediateBuffer -> Process -> Completed
//   (source)        (buffer)        (server) (buffer)             (server)   (sink)
//
// Demonstrates the two things the PoC needs first:
//   * per-stage transparency (orders / WIP at each step)
//   * bottleneck detection (where WIP builds up)
//
// Next steps to grow this from a scaffold into the PoC:
//   1. Replace the random source with a data-ingestion adapter that reads a
//      daily export (Artikel, Auftrag, Vorgang, Standort) and positions real
//      orders at their current stage (= "digital shadow").
//   2. Add per-stage cycle-time distributions from real data for what-if runs.
//   3. Wire the model into SimOpt.Visualization for the live 2D view.
//   4. Hand the model to SimOpt.Optimization (EA/SA/PSO) for stage 3.
// ---------------------------------------------------------------------------

const double horizonMinutes = 480.0;   // one 8-hour shift
const double stepMinutes = 0.1;

var model = new Model("ProductionLine", seed: 42, DateTime.MinValue);

int orderId = 0;
SimpleEntity NewOrder()
{
    orderId++;
    return new SimpleEntity(model, $"Auftrag-{orderId}", $"Auftrag-{orderId}");
}

// Stage 1 — raw-material release (orders arrive ~every 2.5 min)
var release = new SimpleSource(model, new GaussianDistribution(2.5, 0.4), NewOrder, name: "OrderRelease");

// Stage 2/3 — material store buffer feeding the prep server
var materialStore = new SimpleBuffer(model, QueueRule.FIFO, name: "MaterialStore", maxCapacity: 10000);
var prep = new SimpleServer(model, new GaussianDistribution(2.0, 0.3), name: "Prep", createProduct: m => m[0]);
prep.AutoContinue = true;

// Stage 4/5 — intermediate buffer feeding the (slower) process server -> bottleneck
var intermediate = new SimpleBuffer(model, QueueRule.FIFO, name: "IntermediateBuffer", maxCapacity: 10000);
var process = new SimpleServer(model, new GaussianDistribution(3.0, 0.5), name: "Process", createProduct: m => m[0]);
process.AutoContinue = true;

// Stage 6 — completion / handover
var completed = new SimpleSink(model, name: "Completed");

// Wire the line: release -> materialStore -> prep -> intermediate -> process -> completed
release.ConnectTo(materialStore);
prep.ConnectTo(materialStore);
materialStore.ItemReceivedEvent.AddHandler((s, i) => { if (prep.Idle) prep.Start(); });
intermediate.ConnectTo(prep);
process.ConnectTo(intermediate);
intermediate.ItemReceivedEvent.AddHandler((s, i) => { if (process.Idle) process.Start(); });
completed.ConnectTo(process);

// Track peak WIP per buffer — the bottleneck indicator
int maxStore = 0, maxIntermediate = 0;
materialStore.ItemReceivedEvent.AddHandler((s, i) => { if (materialStore.Count > maxStore) maxStore = materialStore.Count; });
intermediate.ItemReceivedEvent.AddHandler((s, i) => { if (intermediate.Count > maxIntermediate) maxIntermediate = intermediate.Count; });

// Run
release.Start();
int steps = 0;
while (model.CurrentTime < horizonMinutes && steps < 1_000_000)
{
    model.Step(stepMinutes);
    steps++;
}

// Per-stage transparency report
Console.WriteLine("=== Production Line — generic 5-stage scaffold ===");
Console.WriteLine($"Horizon: {model.CurrentTime:F0} min   Orders released: {orderId}");
Console.WriteLine();
Console.WriteLine($"  [1] OrderRelease         released : {orderId}");
Console.WriteLine($"  [2] MaterialStore        WIP now / peak : {materialStore.Count} / {maxStore}");
Console.WriteLine($"  [3] Prep (server)");
Console.WriteLine($"  [4] IntermediateBuffer   WIP now / peak : {intermediate.Count} / {maxIntermediate}");
Console.WriteLine($"  [5] Process (server)");
Console.WriteLine($"  [6] Completed (sink)     finished : {completed.Count}");
Console.WriteLine();

string bottleneck = maxIntermediate >= maxStore
    ? "Process (stage 5) — orders pile up in the intermediate buffer"
    : "Prep (stage 3) — orders pile up in the material store";
Console.WriteLine($"Bottleneck (peak WIP build-up): {bottleneck}");
Console.WriteLine($"  MaterialStore peak={maxStore}, IntermediateBuffer peak={maxIntermediate}");
