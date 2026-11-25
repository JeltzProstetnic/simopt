using System;
using SimOpt.Visualization.Models;

namespace SimOpt.Visualization.Controls;

/// <summary>
/// What the main view model asks of the canvas that runs and draws a model (SIM-107). A view model
/// test drives a fake through it, so the play, pause and stop logic is testable without a window.
/// </summary>
public interface ISimulationSurface
{
    int SpeedMs { get; set; }
    bool ShowStats { get; set; }
    bool StatsDetached { get; set; }
    RenderMode Mode { get; set; }

    /// <summary>A run is loaded, paused and not finished, so <see cref="ResumeSimulation"/> continues it.</summary>
    bool CanResume { get; }

    /// <summary>Builds the model afresh at t = 0 and runs it.</summary>
    void StartSimulation(VizTopology topology, double duration = 200.0, int speedMs = 30);

    /// <summary>Builds the model afresh at t = 0 and shows it without running it.</summary>
    void LoadTopologyPaused(VizTopology topology, double duration = 200.0, int speedMs = 30);

    /// <summary>
    /// SIM-110 step 4: builds a schema document's model afresh at t = 0 and shows it without running it. Returns
    /// what the check found; with errors the document is drawn with its problems on the nodes and cannot run.
    /// </summary>
    SimOpt.Engine.Api.Models.ValidationReport LoadDocumentPaused(VizDocument document, double duration, double warmup, int speedMs = 30);

    /// <summary>As <see cref="LoadDocumentPaused"/>, and runs it when it has no errors.</summary>
    SimOpt.Engine.Api.Models.ValidationReport StartDocument(VizDocument document, double duration, double warmup, int speedMs = 30);

    /// <summary>The document on screen with its layout, for File → Save; null when none is.</summary>
    VizDocument? CurrentDocument { get; }

    /// <summary>Stops the clock and keeps the model, so the run can be resumed.</summary>
    void PauseSimulation();

    /// <summary>Continues a paused run; false when there is none to continue.</summary>
    bool ResumeSimulation();

    /// <summary>Ends the run; the model stays on screen, and the next start builds it afresh.</summary>
    void StopSimulation();

    StatsSnapshot GetStatsSnapshot();

    /// <summary>Raised when a run reaches its end time.</summary>
    event EventHandler? RunFinished;
}
