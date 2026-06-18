using System;
using System.Collections.Generic;
using SimOpt.Mathematics.Stochastics.Distributions;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Enum;
using SimOpt.Simulation.Templates;

namespace SimOpt.Glass;

/// <summary>
/// Builds a headless glass base-mass production-line simulation from a decision
/// vector. No dependency on SimOpt.Visualization — used by the optimizer to
/// evaluate candidate solutions. A parallel viz-side builder may convert the
/// same GlassSolution into a VizTopology for rendering.
///
/// Line layout:
///   Rohmateriallager (source) → Materialpuffer (buffer)
///     → N parallel "Mischen/Aufbereitung" mixers (5.0 min/batch each)
///   → Zwischenlager (intermediate buffer, capacity = IntermediateBufferCapacity)
///     → N parallel "Weiterverarbeitung" processing servers (6.0 min/batch each)
///   → buffer → Quality server (2.0 / OperatorsQuality)
///   → Packing server (1.5 / OperatorsPacking) → Fertigmeldung (sink)
///
/// Parallel servers follow the FactoryFloor pattern: multiple servers pull from
/// one shared buffer; on buffer.ItemReceivedEvent the first idle server starts.
///
/// Operator parallelism on the quality/packing manual stations is modelled as
/// effective_service_time = base / operator_count. Honest simplification.
/// </summary>
public static class GlassTopologyBuilder
{
    public const double BaseArrivalIntervalMinutes = 2.5;
    public const double BaseMixMinutes = 5.0;
    public const double BaseProcessMinutes = 6.0;
    public const double BaseQualityMinutes = 2.0;
    public const double BasePackingMinutes = 1.5;

    public static GlassTopologyHandles Build(
        GlassSolution solution,
        int seed = 42,
        double? arrivalIntervalMinutes = null)
    {
        ArgumentNullException.ThrowIfNull(solution);

        double arrivalInterval = arrivalIntervalMinutes ?? BaseArrivalIntervalMinutes;
        double mixTime = BaseMixMinutes;
        double processTime = BaseProcessMinutes;
        double qualityTime = BaseQualityMinutes / solution.OperatorsQuality;
        double packingTime = BasePackingMinutes / solution.OperatorsPacking;

        var model = new Model("Glass", seed, DateTime.MinValue);

        int productCounter = 0;
        SimpleEntity MakeEntity()
        {
            productCounter++;
            return new SimpleEntity(model, $"B{productCounter}", $"Batch {productCounter}");
        }

        // Source: Rohmateriallager (raw-material provisioning).
        var source = new SimpleSource(
            model,
            new GaussianDistribution(arrivalInterval, arrivalInterval * 0.15),
            MakeEntity,
            name: "Rohmateriallager");

        // Stage 1: Materialpuffer → N parallel mixers (Mischen / Aufbereitung).
        var bufMix = new SimpleBuffer(model, QueueRule.FIFO, name: "Materialpuffer", maxCapacity: 1000);
        var mixers = new List<SimpleServer>();
        for (int i = 0; i < solution.NumberOfMixers; i++)
            mixers.Add(NewManualServer(model, $"Mischen/Aufbereitung_{i + 1}", mixTime));

        // Stage 2: Zwischenlager (intermediate buffer) → N parallel processing lines.
        var bufWip = new SimpleBuffer(model, QueueRule.FIFO, name: "Zwischenlager",
            maxCapacity: solution.IntermediateBufferCapacity);
        var processingLines = new List<SimpleServer>();
        for (int i = 0; i < solution.NumberOfProcessingLines; i++)
            processingLines.Add(NewManualServer(model, $"Weiterverarbeitung_{i + 1}", processTime));

        // Stage 3: buffer → Quality server.
        var bufQuality = new SimpleBuffer(model, QueueRule.FIFO, name: "QualityQueue", maxCapacity: 1000);
        var quality = NewManualServer(model, "Quality", qualityTime);

        // Stage 4: buffer → Packing server.
        var bufPacking = new SimpleBuffer(model, QueueRule.FIFO, name: "PackingQueue", maxCapacity: 1000);
        var packing = NewManualServer(model, "Packing", packingTime);

        // Sink: Fertigmeldung (completion).
        var done = new SimpleSink(model, name: "Fertigmeldung");

        // ─── Wiring ────────────────────────────────────────────────────────

        // Source → Materialpuffer
        source.ConnectTo(bufMix);

        // Materialpuffer → parallel mixers (shared buffer; first idle mixer starts)
        foreach (var m in mixers)
        {
            m.ConnectTo(bufMix);
            bufWip.ConnectTo(m);
        }
        bufMix.ItemReceivedEvent.AddHandler((_, _) =>
        {
            foreach (var m in mixers)
                if (m.Idle) { m.Start(); break; }
        });

        // Zwischenlager → parallel processing lines (shared buffer)
        foreach (var p in processingLines)
        {
            p.ConnectTo(bufWip);
            bufQuality.ConnectTo(p);
        }
        bufWip.ItemReceivedEvent.AddHandler((_, _) =>
        {
            foreach (var p in processingLines)
                if (p.Idle) { p.Start(); break; }
        });

        // QualityQueue → Quality
        quality.ConnectTo(bufQuality);
        bufQuality.ItemReceivedEvent.AddHandler((_, _) => { if (quality.Idle) quality.Start(); });

        // Quality → PackingQueue
        bufPacking.ConnectTo(quality);

        // PackingQueue → Packing
        packing.ConnectTo(bufPacking);
        bufPacking.ItemReceivedEvent.AddHandler((_, _) => { if (packing.Idle) packing.Start(); });

        // Packing → Fertigmeldung
        done.ConnectTo(packing);

        return new GlassTopologyHandles(
            model,
            solution,
            source,
            done,
            mixers,
            processingLines,
            quality,
            packing,
            new[] { bufMix, bufWip, bufQuality, bufPacking },
            arrivalInterval,
            mixTime,
            processTime,
            qualityTime,
            packingTime);
    }

    private static SimpleServer NewManualServer(Model model, string name, double serviceTime)
    {
        var server = new SimpleServer(
            model,
            new ConstantDoubleDistribution(serviceTime, false),
            name: name,
            // Defensive: under the AutoContinue + ItemReceived double-drive, the
            // finished-event callback can fire with an already-drained material
            // batch (event-ordering edge case the framework exposes for certain
            // single-server constant-time topologies). Return the first item when
            // present; otherwise emit nothing rather than indexing an empty list.
            createProduct: material => material.Count > 0 ? material[0] : null!);
        server.AutoContinue = true;
        return server;
    }
}
