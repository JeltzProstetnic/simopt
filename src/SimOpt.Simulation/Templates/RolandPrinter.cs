using System;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Templates;
using SimOpt.Mathematics.Stochastics.Distributions;

namespace SimOpt.Simulation.Templates;

/// <summary>
/// Domain-specific server modeling a Roland VersaUV LEF flatbed UV printer.
/// Accumulates <see cref="BatchSize"/> input pieces, then runs one print cycle
/// of <see cref="CycleTime"/> = BatchSize × <see cref="PerPieceTime"/>.
///
/// Models real LEF behavior: jig is loaded with N pieces, the printer runs
/// the whole bed in one pass, the operator unloads. The bed cannot start the
/// next cycle until the current one finishes — even if more material is queued.
///
/// Emit semantics: one representative entity per completed batch (downstream
/// throughput math = 1 / CycleTime per batch). Per-piece fan-out (releasing
/// all N entities to downstream) is planned for a follow-up; the framework
/// supports it via the EntityCreatedEvent re-emit pattern.
/// </summary>
[Serializable]
public class RolandPrinter : SimpleServer
{
    public int BatchSize { get; }
    public double PerPieceTime { get; }
    public double CycleTime => BatchSize * PerPieceTime;

    public RolandPrinter(IModel model, int batchSize, double perPieceTime,
                         string name = "Roland")
        : base(model,
               new ConstantDoubleDistribution(batchSize * perPieceTime, false),
               name: name,
               createProduct: m => m[0],
               checkMaterialComplete: m => m.Count >= batchSize)
    {
        if (batchSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(batchSize),
                "Batch size must be positive.");
        if (perPieceTime <= 0)
            throw new ArgumentOutOfRangeException(nameof(perPieceTime),
                "Per-piece time must be positive.");

        BatchSize = batchSize;
        PerPieceTime = perPieceTime;
        AutoContinue = true;
    }
}
