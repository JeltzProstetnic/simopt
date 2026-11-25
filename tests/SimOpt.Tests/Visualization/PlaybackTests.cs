using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using SimOpt.Engine.Api.Models;
using SimOpt.Engine.Api.Simulation;
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
            CurrentDocument = VizSchema.ToSchema(topology);
            running = live = true;
        }

        public void LoadTopologyPaused(VizTopology topology, double duration = 200, int speedMs = 30)
        {
            Calls.Add("load " + topology.Name);
            Loaded = topology.Name;
            CurrentDocument = VizSchema.ToSchema(topology);
            live = true;
            running = false;
        }

        /// <summary>What the next document reports; valid unless a test says otherwise.</summary>
        public ValidationReport Report { get; set; } = new();

        public (double Duration, double Warmup)? LastRun { get; private set; }

        public VizDocument? CurrentDocument { get; private set; }

        public ValidationReport LoadDocumentPaused(VizDocument document, double duration, double warmup, int speedMs = 30)
        {
            Calls.Add("load document " + document.Topology.Name);
            Loaded = document.Topology.Name;
            CurrentDocument = document;
            LastRun = (duration, warmup);
            live = Report.Valid;
            running = false;
            return Report;
        }

        public ValidationReport StartDocument(VizDocument document, double duration, double warmup, int speedMs = 30)
        {
            Calls.Add("start document " + document.Topology.Name);
            Starts++;
            Loaded = document.Topology.Name;
            CurrentDocument = document;
            LastRun = (duration, warmup);
            running = live = Report.Valid;
            return Report;
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

    // ── SIM-110 step 4: File → Open and Save ──────────────────────────────────────────

    private static string Saved(VizTopology topology, string folder, string name = "line.json")
    {
        string path = Path.Combine(folder, name);
        File.WriteAllText(path, VizFile.Write(VizSchema.ToSchema(topology)));
        return path;
    }

    [Fact]
    public void An_opened_model_is_shown_paused_and_space_runs_it()
    {
        string folder = Directory.CreateTempSubdirectory("simopt-open-").FullName;
        var surface = new FakeSurface();
        var vm = new MainWindowViewModel { Canvas = surface };
        int presets = vm.TopologyNames.Count;

        vm.OpenFile(Saved(VizTopology.ProductionLine(), folder));

        surface.Calls.Should().Equal("load document " + VizTopology.ProductionLine().Name);
        surface.LastRun.Should().Be((200.0, 0.0), "a model file declares no run length, so the presets' one is used");
        vm.TopologyNames.Should().HaveCount(presets + 1);
        vm.TopologyNames[^1].Should().Contain("line.json");
        vm.SelectedTopology.Should().Be(presets);
        vm.IsPaused.Should().BeTrue();

        vm.PlayPauseCommand.Execute(null);

        surface.Calls[^1].Should().Be("resume");
        surface.Starts.Should().Be(0);
        vm.IsRunning.Should().BeTrue();
        Directory.Delete(folder, true);
    }

    [Fact]
    public void A_run_record_runs_for_its_length_with_its_warm_up()
    {
        string folder = Directory.CreateTempSubdirectory("simopt-open-").FullName;
        string path = Path.Combine(folder, "run.json");
        File.WriteAllText(path, "{\"record\": \"simopt-run/1\", \"model\": " + VizFile.Write(VizSchema.ToSchema(VizTopology.Sqss())) +
                                ", \"call\": {\"duration\": 300, \"warmup\": 50}}");
        var surface = new FakeSurface();
        var vm = new MainWindowViewModel { Canvas = surface };

        vm.OpenFile(path);

        surface.LastRun.Should().Be((300.0, 50.0));
        vm.StatusText.Should().Contain("warm-up 50");
        Directory.Delete(folder, true);
    }

    [Fact]
    public void A_model_with_errors_is_shown_with_them_and_does_not_run()
    {
        string folder = Directory.CreateTempSubdirectory("simopt-open-").FullName;
        var report = new ValidationReport();
        report.Errors.Add(new ValidationIssue("UNKNOWN_NODE_TYPE", "x is unknown", "x"));
        var surface = new FakeSurface { Report = report };
        var vm = new MainWindowViewModel { Canvas = surface };

        vm.OpenFile(Saved(VizTopology.Sqss(), folder));

        vm.IsPaused.Should().BeFalse();
        vm.StatusText.Should().Contain("1 problem");
        vm.PlayPauseCommand.Execute(null);
        surface.Calls[^1].Should().Be("start document " + VizTopology.Sqss().Name);
        vm.IsRunning.Should().BeFalse();
        vm.StatusText.Should().Contain("1 problem");
        Directory.Delete(folder, true);
    }

    [Fact]
    public void A_file_that_is_no_model_changes_nothing()
    {
        string folder = Directory.CreateTempSubdirectory("simopt-open-").FullName;
        string path = Path.Combine(folder, "notes.json");
        File.WriteAllText(path, "{ not json");
        var (vm, surface) = Started();
        int names = vm.TopologyNames.Count, calls = surface.Calls.Count;

        vm.OpenFile(path);

        vm.StatusText.Should().Contain("Cannot open notes.json");
        surface.Calls.Should().HaveCount(calls);
        vm.TopologyNames.Should().HaveCount(names);
        vm.IsRunning.Should().BeTrue("the run on screen goes on");
        Directory.Delete(folder, true);
    }

    [Fact]
    public void A_second_file_replaces_the_first_in_the_list_and_a_preset_still_starts()
    {
        string folder = Directory.CreateTempSubdirectory("simopt-open-").FullName;
        var surface = new FakeSurface();
        var vm = new MainWindowViewModel { Canvas = surface };
        int presets = vm.TopologyNames.Count;

        vm.OpenFile(Saved(VizTopology.Sqss(), folder, "a.json"));
        vm.OpenFile(Saved(VizTopology.ProductionLine(), folder, "b.json"));
        vm.SelectedTopology = 1;
        vm.PlayPauseCommand.Execute(null);

        vm.TopologyNames.Should().HaveCount(presets + 1);
        vm.TopologyNames[^1].Should().Contain("b.json");
        surface.Calls[^1].Should().Be("start " + VizTopology.ParallelServers().Name);
        Directory.Delete(folder, true);
    }

    [Fact]
    public void Save_writes_the_model_on_screen_with_its_layout()
    {
        string folder = Directory.CreateTempSubdirectory("simopt-save-").FullName;
        string path = Path.Combine(folder, "factory.json");
        var surface = new FakeSurface();
        var vm = new MainWindowViewModel { Canvas = surface, SelectedTopology = 3 };
        vm.PlayPauseCommand.Execute(null);

        vm.SaveFile(path);

        VizDocument expected = VizSchema.ToSchema(VizTopology.FactoryFloor());
        VizOpened reopened = VizFile.Read(File.ReadAllText(path));
        ModelDocument.Sha256(reopened.Document!.Topology).Should().Be(ModelDocument.Sha256(expected.Topology));
        reopened.Document.Layout.Should().BeEquivalentTo(expected.Layout);
        vm.StatusText.Should().Contain("Saved factory.json");
        Directory.Delete(folder, true);
    }
}
