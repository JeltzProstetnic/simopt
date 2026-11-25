using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimOpt.Engine.Api.Models;
using SimOpt.Ivotion;
using SimOpt.Visualization.Controls;
using SimOpt.Visualization.Models;

namespace SimOpt.Visualization.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public IvotionOptimizationViewModel Optimization { get; }

    public MainWindowViewModel()
    {
        Optimization = new IvotionOptimizationViewModel();
        Optimization.ApplyToVizRequested += OnApplyToVizRequested;
    }

    // SIM-107: the dropdown entry whose model the canvas holds, so Space resumes it only while it is
    // still the one selected; null when nothing resumable is loaded.
    private int? _loadedTopology;

    private void OnApplyToVizRequested(object? sender, IvotionSolution best)
    {
        SelectedTopology = 4; // Ivotion entry in dropdown (display only — we override below)
        if (Canvas == null) return;

        Stop();
        var parametricTopology = VizTopology.IvotionPacking(best);
        Canvas.LoadTopologyPaused(parametricTopology, duration: 200.0, speedMs: Speed);
        _loadedTopology = SelectedTopology;
        IsRunning = false;
        IsPaused = true;
        StatusText =
            $"Loaded optimized: {best.RolandCount}× Roland, " +
            $"ops {best.OperatorsInspect}/{best.OperatorsPack}/{best.OperatorsLabel}/{best.OperatorsSsb}, " +
            $"batch {best.RolandBatchSize}  |  press Space to run";
    }

    [ObservableProperty]
    private string _statusText = "Ready  |  Space=play/pause  -/+=speed  F=fullscreen  D=detach";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRun))]
    private bool _isRunning;

    /// <summary>SIM-107: a run is loaded and paused; Space or Start continues it.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRun))]
    private bool _isPaused;

    /// <summary>A run is going or paused, so Stop has something to end.</summary>
    public bool HasRun => IsRunning || IsPaused;

    [ObservableProperty]
    private int _speed = 30;

    [ObservableProperty]
    private int _selectedTopology = 0;

    [ObservableProperty]
    private bool _controlBarVisible = true;

    [ObservableProperty]
    private bool _isFullscreen;

    [ObservableProperty]
    private bool _isRealistic;

    [ObservableProperty]
    private string _renderModeLabel = "Realistic";

    private ISimulationSurface? _canvas;

    public ISimulationSurface? Canvas
    {
        get => _canvas;
        set
        {
            if (_canvas != null) _canvas.RunFinished -= OnRunFinished;
            _canvas = value;
            if (_canvas != null) _canvas.RunFinished += OnRunFinished;
        }
    }
    public Window? OwnerWindow { get; set; }

    private Window? _controlsWindow;
    private Window? _statsWindow;
    private DispatcherTimer? _statsTimer;
    private WindowState _previousWindowState = WindowState.Normal;

    public ObservableCollection<string> TopologyNames { get; } = new()
    {
        "SQSS (Source-Queue-Server-Sink)",
        "Parallel Servers",
        "Production Line",
        "Factory Floor (Physical Layout)",
        "Ivoclar Ivotion Packing Line"
    };

    // SIM-110 step 4: the file opened last, the entry after the presets
    private const int Presets = 5;
    private VizOpened? _opened;
    private string _openedName = "";

    /// <summary>The run length of a model file that declares none: the presets' one.</summary>
    private const double DefaultDuration = 200.0;

    private VizTopology GetSelectedTopology() => SelectedTopology switch
    {
        1 => VizTopology.ParallelServers(),
        2 => VizTopology.ProductionLine(),
        3 => VizTopology.FactoryFloor(),
        4 => VizTopology.IvotionPacking(),
        _ => VizTopology.Sqss()
    };

    partial void OnSpeedChanged(int value)
    {
        if (Canvas != null) Canvas.SpeedMs = value;
    }

    partial void OnSelectedTopologyChanged(int value)
    {
        if (value != _loadedTopology) _loadedTopology = null;   // another model chosen: start it afresh
    }

    /// <summary>SIM-107: Space. Pauses a running model, otherwise starts or resumes one.</summary>
    [RelayCommand]
    private void PlayPause()
    {
        if (IsRunning) Pause();
        else Start();
    }

    /// <summary>Resumes the paused run of the selected model, or starts the selected model at t = 0.</summary>
    [RelayCommand]
    private void Start()
    {
        if (Canvas == null) return;

        if (_loadedTopology == SelectedTopology && Canvas.CanResume && Canvas.ResumeSimulation())
        {
            IsRunning = true;
            IsPaused = false;
            StatusText = "Running  |  Space=pause  -/+=speed  F=fullscreen";
            return;
        }

        if (SelectedTopology == Presets && _opened?.Document != null)
        {
            ValidationReport report = Canvas.StartDocument(_opened.Document, _opened.Duration ?? DefaultDuration, _opened.Warmup ?? 0d, Speed);
            if (!report.Valid)
            {
                ShowProblems(report);
                return;
            }
            _loadedTopology = SelectedTopology;
            IsRunning = true;
            IsPaused = false;
            StatusText = $"Running: {_openedName}  |  Space=pause  -/+=speed  F=fullscreen";
            return;
        }

        var topology = GetSelectedTopology();
        Canvas.StartSimulation(topology, duration: 200.0, speedMs: Speed);
        _loadedTopology = SelectedTopology;
        IsRunning = true;
        IsPaused = false;
        StatusText = $"Running: {topology.Name}  |  Space=pause  -/+=speed  F=fullscreen";
    }

    /// <summary>
    /// SIM-110 step 4: File → Open. A model file (save_model, export_model, or saved here) or a run record,
    /// which also gives the run's length and warm-up, is shown paused; Space runs it. A model with errors is
    /// drawn with them on its nodes and does not run. A file that is no model changes nothing.
    /// </summary>
    public void OpenFile(string path)
    {
        string name = Path.GetFileName(path);
        VizOpened opened;
        try
        {
            opened = VizFile.Read(File.ReadAllText(path));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            opened = VizOpened.Failed(e.Message);
        }
        if (opened.Document == null)
        {
            StatusText = $"Cannot open {name}: {opened.Error}";
            return;
        }
        if (Canvas == null) return;

        // the canvas ends the run on screen when it loads the file
        _loadedTopology = null;
        IsRunning = false;
        IsPaused = false;
        _opened = opened;
        _openedName = name;
        string entry = $"{(opened.Kind == "run record" ? "Run record" : "File")}: {name}";
        if (TopologyNames.Count > Presets) TopologyNames[Presets] = entry;
        else TopologyNames.Add(entry);
        SelectedTopology = Presets;

        double warmup = opened.Warmup ?? 0d;
        ValidationReport report = Canvas.LoadDocumentPaused(opened.Document, opened.Duration ?? DefaultDuration, warmup, Speed);
        if (!report.Valid)
        {
            ShowProblems(report);
            return;
        }
        _loadedTopology = Presets;
        IsPaused = true;
        string warnings = report.Warnings.Count == 0 ? "" : $", {Plural(report.Warnings.Count, "warning")} marked";
        string run = warmup > 0d ? $", warm-up {warmup:0.###} of {opened.Duration ?? DefaultDuration:0.###}" : "";
        StatusText = $"Opened {name}{run}{warnings}  |  Space=run";
    }

    /// <summary>SIM-110 step 4: File → Save writes the model on screen with its layout, which the MCP head's load_model reads too.</summary>
    public void SaveFile(string path)
    {
        VizDocument? document = Canvas?.CurrentDocument;
        if (document == null)
        {
            StatusText = "Nothing to save: start or open a model first";
            return;
        }
        try
        {
            File.WriteAllText(path, VizFile.Write(document));
            StatusText = $"Saved {Path.GetFileName(path)}";
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            StatusText = $"Cannot save {Path.GetFileName(path)}: {e.Message}";
        }
    }

    private void ShowProblems(ValidationReport report)
    {
        _loadedTopology = null;
        IsRunning = false;
        IsPaused = false;
        StatusText = $"{_openedName}: {Plural(report.Errors.Count + report.Warnings.Count, "problem")}, marked on the nodes; " +
                     "fix the file and open it again";
    }

    private static string Plural(int n, string what) => n == 1 ? $"1 {what}" : $"{n} {what}s";

    [RelayCommand]
    private void Pause()
    {
        if (Canvas == null || !IsRunning) return;
        Canvas.PauseSimulation();
        IsRunning = false;
        IsPaused = true;
        StatusText = "Paused  |  Space=resume  Stop=end the run  -/+=speed";
    }

    [RelayCommand]
    private void Stop()
    {
        Canvas?.StopSimulation();
        _loadedTopology = null;
        IsRunning = false;
        IsPaused = false;
        StatusText = "Stopped  |  Space=start  -/+=speed  F=fullscreen";
    }

    private void OnRunFinished(object? sender, EventArgs e)
    {
        _loadedTopology = null;
        IsRunning = false;
        IsPaused = false;
        StatusText = "Finished  |  Space=run again  -/+=speed  F=fullscreen";
    }

    [RelayCommand]
    private void ToggleStats()
    {
        if (Canvas == null) return;
        Canvas.ShowStats = !Canvas.ShowStats;
    }

    [RelayCommand]
    private void ToggleRenderMode()
    {
        if (Canvas == null) return;
        IsRealistic = !IsRealistic;
        Canvas.Mode = IsRealistic
            ? SimOpt.Visualization.Controls.RenderMode.Realistic
            : SimOpt.Visualization.Controls.RenderMode.Schematic;
        RenderModeLabel = IsRealistic ? "Schematic" : "Realistic";
    }

    [RelayCommand]
    private void Faster() => Speed = Math.Max(5, Speed - 10);

    [RelayCommand]
    private void Slower() => Speed = Math.Min(200, Speed + 10);

    [RelayCommand]
    private void ToggleFullscreen()
    {
        if (OwnerWindow == null) return;

        if (IsFullscreen)
        {
            OwnerWindow.WindowState = _previousWindowState;
            OwnerWindow.SystemDecorations = SystemDecorations.Full;
            ControlBarVisible = _controlsWindow == null; // show bar if not detached
            IsFullscreen = false;
        }
        else
        {
            _previousWindowState = OwnerWindow.WindowState;
            OwnerWindow.WindowState = WindowState.FullScreen;
            OwnerWindow.SystemDecorations = SystemDecorations.BorderOnly;
            ControlBarVisible = false;
            IsFullscreen = true;
        }
    }

    [RelayCommand]
    private void DetachControls()
    {
        if (OwnerWindow == null) return;

        if (_controlsWindow != null)
        {
            // Re-attach: close the floating window, show inline bar
            _controlsWindow.Close();
            _controlsWindow = null;
            ControlBarVisible = true;
            return;
        }

        // Detach: hide inline bar + stats, open floating windows
        ControlBarVisible = false;
        if (Canvas != null) Canvas.StatsDetached = true;
        OpenStatsWindow();

        _controlsWindow = new Window
        {
            Title = "SimOpt Controls",
            Width = 500,
            Height = 80,
            MinWidth = 400,
            MinHeight = 60,
            CanResize = true,
            Background = Avalonia.Media.Brushes.Transparent,
            SystemDecorations = SystemDecorations.Full,
            Topmost = true,
            DataContext = this,
            Content = CreateControlPanel()
        };

        _controlsWindow.Closed += (_, _) =>
        {
            _controlsWindow = null;
            if (!IsFullscreen) ControlBarVisible = true;
            CloseStatsWindow();
        };

        _controlsWindow.Show();
    }

    private void OpenStatsWindow()
    {
        if (_statsWindow != null) return;

        var statsPanel = new StatsPanel();
        _statsWindow = new Window
        {
            Title = "SimOpt Statistics",
            Width = 280,
            Height = 400,
            MinWidth = 220,
            MinHeight = 200,
            CanResize = true,
            Topmost = true,
            Background = new SolidColorBrush(Color.FromRgb(18, 18, 30)),
            Content = statsPanel
        };

        _statsTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _statsTimer.Tick += (_, _) =>
        {
            if (Canvas == null) return;
            var snap = Canvas.GetStatsSnapshot();
            statsPanel.Update(snap);
        };
        _statsTimer.Start();

        _statsWindow.Closed += (_, _) =>
        {
            _statsTimer?.Stop();
            _statsTimer = null;
            _statsWindow = null;
            if (Canvas != null) Canvas.StatsDetached = false;
        };

        _statsWindow.Show();
    }

    private void CloseStatsWindow()
    {
        _statsTimer?.Stop();
        _statsTimer = null;
        _statsWindow?.Close();
        _statsWindow = null;
        if (Canvas != null) Canvas.StatsDetached = false;
    }

    private Avalonia.Controls.Control CreateControlPanel()
    {
        var panel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Spacing = 8,
            Margin = new Avalonia.Thickness(8)
        };

        var combo = new ComboBox { ItemsSource = TopologyNames, Width = 200, FontSize = 12 };
        combo.Bind(ComboBox.SelectedIndexProperty, new Avalonia.Data.Binding("SelectedTopology"));
        combo.Bind(ComboBox.IsEnabledProperty, new Avalonia.Data.Binding("!IsRunning"));

        var startBtn = new Button { Content = "Start", Padding = new Avalonia.Thickness(16, 4), FontSize = 12 };
        startBtn.Bind(Button.CommandProperty, new Avalonia.Data.Binding("StartCommand"));
        startBtn.Bind(Button.IsEnabledProperty, new Avalonia.Data.Binding("!IsRunning"));

        var stopBtn = new Button { Content = "Stop", Padding = new Avalonia.Thickness(16, 4), FontSize = 12 };
        stopBtn.Bind(Button.CommandProperty, new Avalonia.Data.Binding("StopCommand"));
        stopBtn.Bind(Button.IsEnabledProperty, new Avalonia.Data.Binding("HasRun"));

        var fasterBtn = new Button { Content = "-", Padding = new Avalonia.Thickness(6, 4), FontSize = 12 };
        fasterBtn.Bind(Button.CommandProperty, new Avalonia.Data.Binding("FasterCommand"));

        var slowerBtn = new Button { Content = "+", Padding = new Avalonia.Thickness(6, 4), FontSize = 12 };
        slowerBtn.Bind(Button.CommandProperty, new Avalonia.Data.Binding("SlowerCommand"));

        var speedText = new TextBlock
        {
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            FontSize = 11,
            Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(136, 136, 164)),
            Width = 40,
            TextAlignment = Avalonia.Media.TextAlignment.Center
        };
        speedText.Bind(TextBlock.TextProperty, new Avalonia.Data.Binding("Speed") { StringFormat = "{0}ms" });

        var reattachBtn = new Button { Content = "Re-attach", Padding = new Avalonia.Thickness(10, 4), FontSize = 11 };
        reattachBtn.Bind(Button.CommandProperty, new Avalonia.Data.Binding("DetachControlsCommand"));

        panel.Children.Add(combo);
        panel.Children.Add(startBtn);
        panel.Children.Add(stopBtn);
        panel.Children.Add(fasterBtn);
        panel.Children.Add(speedText);
        panel.Children.Add(slowerBtn);
        panel.Children.Add(reattachBtn);

        return new Border
        {
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(28, 28, 42)),
            CornerRadius = new Avalonia.CornerRadius(6),
            Padding = new Avalonia.Thickness(4),
            Child = panel
        };
    }
}
