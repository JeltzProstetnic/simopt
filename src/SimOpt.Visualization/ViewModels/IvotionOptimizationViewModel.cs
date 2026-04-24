using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimOpt.Ivotion;

namespace SimOpt.Visualization.ViewModels;

/// <summary>
/// View-model backing the Ivotion optimization panel (SIM-37 Phase B).
/// Drives <see cref="IIvotionOptimizationEngine"/> on a background thread,
/// marshals progress samples to the UI via the injected dispatch action.
/// </summary>
public partial class IvotionOptimizationViewModel : ViewModelBase
{
    private readonly IIvotionOptimizationEngine _engine;
    private readonly Action<Action> _uiDispatch;
    private CancellationTokenSource? _cts;

    public IvotionOptimizationViewModel()
        : this(new IvotionOptimizationEngine(), DefaultDispatch) { }

    public IvotionOptimizationViewModel(
        IIvotionOptimizationEngine engine,
        Action<Action> uiDispatch)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _uiDispatch = uiDispatch ?? throw new ArgumentNullException(nameof(uiDispatch));

        Objectives = new List<IvotionObjective>
        {
            IvotionObjective.MaximizeThroughput,
            IvotionObjective.MinimizeTotalCost,
            IvotionObjective.MinimizeCostPerPiece,
            IvotionObjective.MinimizeLaborHours,
            IvotionObjective.MinimizeFloorSpace,
        };

        Strategies = new List<IvotionStrategyOption>();
        foreach (var k in new[]
                 {
                     IvotionStrategyKind.Random,
                     IvotionStrategyKind.Evolutionary,
                     IvotionStrategyKind.ParticleSwarm,
                     IvotionStrategyKind.Sweep,
                 })
        {
            Strategies.Add(new IvotionStrategyOption(
                k, IvotionStrategyInfo.DisplayName(k), IvotionStrategyInfo.IsEnabled(k)));
        }

        FitnessHistory = new ObservableCollection<double>();
    }

    private static void DefaultDispatch(Action a) => Dispatcher.UIThread.Post(a);

    // ─── Strategy / objective selection ────────────────────────────────────

    public List<IvotionObjective> Objectives { get; }
    public List<IvotionStrategyOption> Strategies { get; }

    [ObservableProperty]
    private IvotionObjective _selectedObjective = IvotionObjective.MinimizeCostPerPiece;

    [ObservableProperty]
    private IvotionStrategyKind _selectedStrategy = IvotionStrategyKind.Evolutionary;

    // ─── Numeric parameters ────────────────────────────────────────────────

    [ObservableProperty]
    private double _operatorWage = IvotionCostModel.OperatorWagePerHour;

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
    private IvotionSolution? _bestSolution;

    [ObservableProperty]
    private IvotionKpis? _bestKpis;

    [ObservableProperty]
    private double? _bestFitness;

    [ObservableProperty]
    private string _status = "Idle.";

    public ObservableCollection<double> FitnessHistory { get; }

    public event EventHandler<IvotionSolution>? ApplyToVizRequested;

    // ─── Commands ──────────────────────────────────────────────────────────

    private bool CanRun() =>
        !IsRunning &&
        IvotionStrategyInfo.IsEnabled(SelectedStrategy) &&
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
        Status = $"Running {IvotionStrategyInfo.DisplayName(SelectedStrategy)}...";

        var settings = new IvotionOptimizationSettings
        {
            Strategy = SelectedStrategy,
            Objective = SelectedObjective,
            Iterations = Iterations,
            PopulationSize = PopulationSize,
            OperatorWagePerHour = OperatorWage,
            SimDurationMinutes = SimDurationMinutes,
            Seed = Seed,
        };

        var progress = new DirectProgress<IvotionFitnessSample>(OnProgressSample);
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

    private void OnProgressSample(IvotionFitnessSample sample)
    {
        _uiDispatch(() =>
        {
            FitnessHistory.Add(sample.BestSoFarFitness);
            BestFitness = sample.BestSoFarFitness;
            BestSolution = sample.BestSoFarSolution;
        });
    }

    private void ApplyResult(IvotionOptimizationResult result)
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
    partial void OnSelectedStrategyChanged(IvotionStrategyKind value) => RunCommand.NotifyCanExecuteChanged();
    partial void OnIterationsChanged(int value) => RunCommand.NotifyCanExecuteChanged();
    partial void OnPopulationSizeChanged(int value) => RunCommand.NotifyCanExecuteChanged();
    partial void OnOperatorWageChanged(double value) => RunCommand.NotifyCanExecuteChanged();
}

/// <summary>UI-friendly strategy entry with display name + enabled flag.</summary>
public sealed record IvotionStrategyOption(
    IvotionStrategyKind Kind,
    string DisplayName,
    bool IsEnabled);

/// <summary>Inline <see cref="IProgress{T}"/> — reports directly, no SyncContext hop.</summary>
internal sealed class DirectProgress<T> : IProgress<T>
{
    private readonly Action<T> _handler;
    public DirectProgress(Action<T> handler) => _handler = handler;
    public void Report(T value) => _handler(value);
}
