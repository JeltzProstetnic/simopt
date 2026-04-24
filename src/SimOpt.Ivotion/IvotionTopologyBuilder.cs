using System;
using System.Collections.Generic;
using SimOpt.Mathematics.Stochastics.Distributions;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Enum;
using SimOpt.Simulation.Templates;

namespace SimOpt.Ivotion;

/// <summary>
/// Builds a headless Ivotion packing-line simulation from a decision vector.
/// No dependency on SimOpt.Visualization — used by the optimizer to evaluate
/// candidate solutions. A parallel viz-side builder may convert the same
/// IvotionSolution into a VizTopology for rendering (Phase B).
///
/// Baseline cycle times (minutes) mirror VizTopology.IvotionPacking() until
/// real customer data lands via Phase D1 CSV import:
///   arrival interval : 5.5 min
///   inspect          : 1.0 min  (manual — scaled by operators_inspect)
///   Roland per-piece : 0.4 min  (cycle = 0.4 × batch_size)
///   pack             : 2.0 min  (manual — scaled by operators_pack)
///   label            : 1.2 min  (manual — scaled by operators_label)
///   SSB              : 0.3 min  (manual — scaled by operators_ssb)
///
/// Operator parallelism is modelled as effective_service_time = base / count.
/// Honest simplification until a ResourceManager-backed pool lands in v3.
/// </summary>
public static class IvotionTopologyBuilder
{
    public const double BaseArrivalIntervalMinutes = 5.5;
    public const double BaseInspectMinutes = 1.0;
    public const double BaseRolandPerPieceMinutes = 0.4;
    public const double BasePackMinutes = 2.0;
    public const double BaseLabelMinutes = 1.2;
    public const double BaseSsbMinutes = 0.3;

    public static IvotionTopologyHandles Build(
        IvotionSolution solution,
        int seed = 42,
        double? arrivalIntervalMinutes = null)
    {
        ArgumentNullException.ThrowIfNull(solution);

        double arrivalInterval = arrivalIntervalMinutes ?? BaseArrivalIntervalMinutes;
        double inspectTime = BaseInspectMinutes / solution.OperatorsInspect;
        double packTime = BasePackMinutes / solution.OperatorsPack;
        double labelTime = BaseLabelMinutes / solution.OperatorsLabel;
        double ssbTime = BaseSsbMinutes / solution.OperatorsSsb;

        var model = new Model("Ivotion", seed, DateTime.MinValue);

        int productCounter = 0;
        SimpleEntity MakeEntity()
        {
            productCounter++;
            return new SimpleEntity(model, $"D{productCounter}", $"Denture {productCounter}");
        }

        // Source: denture arrivals
        var source = new SimpleSource(
            model,
            new GaussianDistribution(arrivalInterval, arrivalInterval * 0.15),
            MakeEntity,
            name: "incoming");

        // Stage 1: Inspect
        var buf1 = new SimpleBuffer(model, QueueRule.FIFO, name: "buf1", maxCapacity: 1000);
        var inspect = NewManualServer(model, "inspect", inspectTime);

        // Stage 2: Roland print queue + 1 or 2 Rolands
        var buf2 = new SimpleBuffer(model, QueueRule.FIFO, name: "buf2", maxCapacity: 1000);
        var rolands = new List<RolandPrinter>();
        if (solution.RolandCount == 1)
        {
            rolands.Add(new RolandPrinter(model, solution.RolandBatchSize,
                BaseRolandPerPieceMinutes, name: "roland_a"));
        }
        else
        {
            rolands.Add(new RolandPrinter(model, solution.RolandBatchSize,
                BaseRolandPerPieceMinutes, name: "roland_a"));
            rolands.Add(new RolandPrinter(model, solution.RolandBatchSize,
                BaseRolandPerPieceMinutes, name: "roland_b"));
        }

        // Stage 3: Pack
        var buf3 = new SimpleBuffer(model, QueueRule.FIFO, name: "buf3", maxCapacity: 1000);
        var pack = NewManualServer(model, "pack", packTime);

        // Stage 4: Label
        var buf4 = new SimpleBuffer(model, QueueRule.FIFO, name: "buf4", maxCapacity: 1000);
        var label = NewManualServer(model, "label", labelTime);

        // Stage 5: SSB
        var buf5 = new SimpleBuffer(model, QueueRule.FIFO, name: "buf5", maxCapacity: 1000);
        var ssb = NewManualServer(model, "ssb", ssbTime);

        // Sink
        var shipped = new SimpleSink(model, name: "shipped");

        // Wiring
        source.ConnectTo(buf1);

        inspect.ConnectTo(buf1);
        buf1.ItemReceivedEvent.AddHandler((_, _) => { if (inspect.Idle) inspect.Start(); });

        buf2.ConnectTo(inspect);

        foreach (var r in rolands)
        {
            r.ConnectTo(buf2);
            buf2.ItemReceivedEvent.AddHandler((_, _) => { if (r.Idle) r.Start(); });
            buf3.ConnectTo(r);
        }

        pack.ConnectTo(buf3);
        buf3.ItemReceivedEvent.AddHandler((_, _) => { if (pack.Idle) pack.Start(); });

        buf4.ConnectTo(pack);

        label.ConnectTo(buf4);
        buf4.ItemReceivedEvent.AddHandler((_, _) => { if (label.Idle) label.Start(); });

        buf5.ConnectTo(label);

        ssb.ConnectTo(buf5);
        buf5.ItemReceivedEvent.AddHandler((_, _) => { if (ssb.Idle) ssb.Start(); });

        shipped.ConnectTo(ssb);

        return new IvotionTopologyHandles(
            model,
            solution,
            source,
            shipped,
            rolands,
            new[] { inspect, pack, label, ssb },
            new[] { buf1, buf2, buf3, buf4, buf5 },
            arrivalInterval,
            inspectTime,
            packTime,
            labelTime,
            ssbTime);
    }

    private static SimpleServer NewManualServer(Model model, string name, double serviceTime)
    {
        var server = new SimpleServer(
            model,
            new ConstantDoubleDistribution(serviceTime, false),
            name: name,
            createProduct: material => material[0]);
        server.AutoContinue = true;
        return server;
    }
}
