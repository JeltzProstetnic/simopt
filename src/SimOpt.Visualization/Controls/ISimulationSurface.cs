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
