using System.Collections.Generic;
using FluentAssertions;
using SimOpt.Visualization.Controls;
using SimOpt.Visualization.Models;
using SimOpt.Visualization.ViewModels;
using Xunit;

namespace SimOpt.Tests.Visualization;

/// <summary>
/// SIM-107 (SIM-110 slice V1): Space pauses and resumes the run it paused. Before the fix it called
/// Stop, and the next Space rebuilt the model from t = 0, so "pause" restarted the run.
/// </summary>
public class PlaybackTests
{
    /// <summary>Records what the view model asks of the canvas; a run ends when <see cref="Finish"/> is called.</summary>
    private sealed class FakeSurface : ISimulationSurface
    {
        private bool running, live;

        public List<string> Calls { get; } = new();
        public int Starts { get; private set; }
        public string? Loaded { get; private set; }

        public int SpeedMs { get; set; }
        public bool ShowStats { get; set; }
        public bool StatsDetached { get; set; }
        public RenderMode Mode { get; set; }
        public bool CanResume => live && !running;

        public void StartSimulation(VizTopology topology, double duration = 200, int speedMs = 30)
        {
            Calls.Add("start " + topology.Name);
            Starts++;
            Loaded = topology.Name;
            running = live = true;
        }

        public void LoadTopologyPaused(VizTopology topology, double duration = 200, int speedMs = 30)
        {
            Calls.Add("load " + topology.Name);
            Loaded = topology.Name;
            live = true;
            running = false;
        }

        public void PauseSimulation() { Calls.Add("pause"); running = false; }

        public void StopSimulation() { Calls.Add("stop"); running = live = false; }

        public bool ResumeSimulation()
        {
            Calls.Add("resume");
            if (!CanResume) return false;
            running = true;
            return true;
        }

        public StatsSnapshot GetStatsSnapshot() => new();

        public event System.EventHandler? RunFinished;

        public void Finish()
        {
            running = live = false;
            RunFinished?.Invoke(this, System.EventArgs.Empty);
        }
    }

    private static (MainWindowViewModel Vm, FakeSurface Surface) Started()
    {
        var surface = new FakeSurface();
        var vm = new MainWindowViewModel { Canvas = surface };
        vm.PlayPauseCommand.Execute(null);
        return (vm, surface);
    }

    [Fact]
    public void Space_while_running_pauses_without_ending_the_run()
    {
        var (vm, surface) = Started();

        vm.PlayPauseCommand.Execute(null);

        surface.Calls.Should().Equal("start " + VizTopology.Sqss().Name, "pause");
        vm.IsRunning.Should().BeFalse();
        vm.IsPaused.Should().BeTrue();
    }

    [Fact]
    public void Space_again_resumes_the_same_run()
    {
        var (vm, surface) = Started();

        vm.PlayPauseCommand.Execute(null);
        vm.PlayPauseCommand.Execute(null);

        surface.Starts.Should().Be(1, "resuming must not rebuild the model");
        surface.Calls[^1].Should().Be("resume");
        vm.IsRunning.Should().BeTrue();
        vm.IsPaused.Should().BeFalse();
    }

    [Fact]
    public void After_stop_space_starts_a_fresh_run()
    {
        var (vm, surface) = Started();

        vm.StopCommand.Execute(null);
        vm.PlayPauseCommand.Execute(null);

        surface.Starts.Should().Be(2);
        vm.IsRunning.Should().BeTrue();
        vm.IsPaused.Should().BeFalse();
    }

    [Fact]
    public void A_stop_while_paused_ends_the_run()
    {
        var (vm, surface) = Started();

        vm.PlayPauseCommand.Execute(null);
        vm.StopCommand.Execute(null);

        surface.Calls[^1].Should().Be("stop");
        vm.IsPaused.Should().BeFalse();
        vm.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void Choosing_another_model_while_paused_starts_that_model()
    {
        var (vm, surface) = Started();

        vm.PlayPauseCommand.Execute(null);
        vm.SelectedTopology = 2;
        vm.PlayPauseCommand.Execute(null);

        surface.Starts.Should().Be(2);
        surface.Loaded.Should().Be(VizTopology.ProductionLine().Name);
    }

    [Fact]
    public void A_finished_run_starts_again_from_the_beginning()
    {
        var (vm, surface) = Started();

        surface.Finish();
        vm.IsRunning.Should().BeFalse("the canvas reports the end of the run");
        vm.IsPaused.Should().BeFalse();
        vm.PlayPauseCommand.Execute(null);

        surface.Starts.Should().Be(2);
        vm.IsRunning.Should().BeTrue();
    }
}
