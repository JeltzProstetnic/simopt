using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimOpt.Glass;

namespace SimOpt.Visualization.ViewModels;

/// <summary>
/// View-model backing the glass production-line optimization panel.
/// Drives <see cref="IGlassOptimizationEngine"/> on a background thread,
/// marshals progress samples to the UI via the injected dispatch action.
/// Mirrors <see cref="IvotionOptimizationViewModel"/>.
/// </summary>
public partial class GlassOptimizationViewModel : ViewModelBase
{
    private readonly IGlassOptimizationEngine _engine;
    private readonly Action<Action> _uiDispatch;
    private CancellationTokenSource? _cts;

    public GlassOptimizationViewModel()
        : this(new GlassOptimizationEngine(), DefaultDispatch) { }

    public GlassOptimizationViewModel(
        IGlassOptimizationEngine engine,
        Action<Action> uiDispatch)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _uiDispatch = uiDispatch ?? throw new ArgumentNullException(nameof(uiDispatch));

        Objectives = new List<GlassObjective>
        {
            GlassObjective.MaximizeThroughput,
            GlassObjective.MinimizeTotalCost,
            GlassObjective.MinimizeCostPerPiece,
            GlassObjective.MinimizeLaborHours,
        };

        Strategies = new List<GlassStrategyOption>();
        foreach (var k in new[]
                 {
                     GlassStrategyKind.Random,
                     GlassStrategyKind.Evolutionary,
                     GlassStrategyKind.ParticleSwarm,
                     GlassStrategyKind.Sweep,
                 })
        {
            Strategies.Add(new GlassStrategyOption(
                k, GlassStrategyInfo.DisplayName(k), GlassStrategyInfo.IsEnabled(k)));
        }

        FitnessHistory = new ObservableCollection<double>();
    }

    private static void DefaultDispatch(Action a) => Dispatcher.UIThread.Post(a);

    // ─── Strategy / objective selection ────────────────────────────────────

    public List<GlassObjective> Objectives { get; }
    public List<GlassStrategyOption> Strategies { get; }

    [ObservableProperty]
    private GlassObjective _selectedObjective = GlassObjective.MinimizeCostPerPiece;

    [ObservableProperty]
    private GlassStrategyKind _selectedStrategy = GlassStrategyKind.Evolutionary;

    // ─── Numeric parameters ────────────────────────────────────────────────

    [ObservableProperty]
    private double _operatorWage = GlassCostModel.OperatorWagePerHour;

    [ObservableProperty]
    private int _iterations = 60;

    [ObservableProperty]
    private int _populationSize = 12;

    [ObservableProperty]
    private double _simDurationMinutes = 480.0;

    [ObservableProperty]
    private int _seed = 42;

    // ─── Run state ─────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCommand), nameof(StopCommand), nameof(ApplyToVizCommand))]
    private bool _isRunning;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyToVizCommand))]
    private GlassSolution? _bestSolution;

    [ObservableProperty]
    private GlassKpis? _bestKpis;

    [ObservableProperty]
    private double? _bestFitness;

    [ObservableProperty]
    private string _status = "Idle.";

    public ObservableCollection<double> FitnessHistory { get; }

    public event EventHandler<GlassSolution>? ApplyToVizRequested;

    // ─── Commands ──────────────────────────────────────────────────────────

    private bool CanRun() =>
        !IsRunning &&
        GlassStrategyInfo.IsEnabled(SelectedStrategy) &&
        Iterations > 0 && PopulationSize > 0 && OperatorWage >= 0;

    private bool CanStop() => IsRunning;

    private bool CanApply() => !IsRunning && BestSolution is not null;

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task RunAsync()
    {
        _cts = new CancellationTokenSource();
        IsRunning = true;
        FitnessHistory.Clear();
        BestSolution = null;
        BestKpis = null;
        BestFitness = null;
        Status = $"Running {GlassStrategyInfo.DisplayName(SelectedStrategy)}...";

        var settings = new GlassOptimizationSettings
        {
            Strategy = SelectedStrategy,
            Objective = SelectedObjective,
            Iterations = Iterations,
            PopulationSize = PopulationSize,
            OperatorWagePerHour = OperatorWage,
            SimDurationMinutes = SimDurationMinutes,
            Seed = Seed,
        };

        var progress = new GlassDirectProgress<GlassFitnessSample>(OnProgressSample);
        try
        {
            var result = await _engine.RunAsync(settings, progress, _cts.Token);
            _uiDispatch(() => ApplyResult(result));
        }
        catch (OperationCanceledException)
        {
            _uiDispatch(() => Status = "Cancelled.");
        }
        catch (Exception ex)
        {
            _uiDispatch(() => Status = $"Error: {ex.Message}");
        }
        finally
        {
            _uiDispatch(() =>
            {
                IsRunning = false;
                _cts?.Dispose();
                _cts = null;
            });
        }
    }

    [RelayCommand(CanExecute = nameof(CanStop))]
    private void Stop() => _cts?.Cancel();

    [RelayCommand(CanExecute = nameof(CanApply))]
    private void ApplyToViz()
    {
        if (BestSolution is not null)
            ApplyToVizRequested?.Invoke(this, BestSolution);
    }

    // ─── Progress / result handling ────────────────────────────────────────

    private void OnProgressSample(GlassFitnessSample sample)
    {
        _uiDispatch(() =>
        {
            FitnessHistory.Add(sample.BestSoFarFitness);
            BestFitness = sample.BestSoFarFitness;
            BestSolution = sample.BestSoFarSolution;
        });
    }

    private void ApplyResult(GlassOptimizationResult result)
    {
        if (result.BestSolution is not null)
        {
            BestSolution = result.BestSolution;
            BestKpis = result.BestKpis;
            BestFitness = result.BestSolution.Fitness;
        }

        Status = result.WasCancelled
            ? $"Cancelled after {result.TotalIterations} iteration(s)."
            : $"Done — {result.TotalIterations} iteration(s) in {result.ElapsedMilliseconds} ms.";
    }

    // Recompute CanRun when strategy/numeric fields change.
    partial void OnSelectedStrategyChanged(GlassStrategyKind value) => RunCommand.NotifyCanExecuteChanged();
    partial void OnIterationsChanged(int value) => RunCommand.NotifyCanExecuteChanged();
    partial void OnPopulationSizeChanged(int value) => RunCommand.NotifyCanExecuteChanged();
    partial void OnOperatorWageChanged(double value) => RunCommand.NotifyCanExecuteChanged();
}

/// <summary>UI-friendly strategy entry with display name + enabled flag.</summary>
public sealed record GlassStrategyOption(
    GlassStrategyKind Kind,
    string DisplayName,
    bool IsEnabled);

/// <summary>Inline <see cref="IProgress{T}"/> — reports directly, no SyncContext hop.</summary>
internal sealed class GlassDirectProgress<T> : IProgress<T>
{
    private readonly Action<T> _handler;
    public GlassDirectProgress(Action<T> handler) => _handler = handler;
    public void Report(T value) => _handler(value);
}
