using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using SimOpt.Ivotion;
using SimOpt.Visualization.ViewModels;
using Xunit;

namespace SimOpt.Tests.Visualization;

public class IvotionOptimizationViewModelTests
{
    // ─── Helpers ───────────────────────────────────────────────────────────

    private static IvotionOptimizationViewModel MakeVm(IIvotionOptimizationEngine engine)
        => new IvotionOptimizationViewModel(engine, a => a());

    private static IvotionSolution SampleSolution(int[]? p = null)
        => new IvotionSolution(p ?? new[] { 1, 2, 2, 1, 1, 15 });

    private sealed class FakeEngine : IIvotionOptimizationEngine
    {
        public List<IvotionFitnessSample> Samples { get; init; } = new();
        public IvotionOptimizationResult Result { get; set; } =
            new IvotionOptimizationResult(null, null, 0, 0, false);
        public IvotionOptimizationSettings? LastSettings { get; private set; }
        public int InvocationCount { get; private set; }

        public Task<IvotionOptimizationResult> RunAsync(
            IvotionOptimizationSettings settings,
            IProgress<IvotionFitnessSample>? progress,
            CancellationToken ct)
        {
            InvocationCount++;
            LastSettings = settings;
            foreach (var s in Samples) progress?.Report(s);
            return Task.FromResult(Result);
        }
    }

    // ─── Default state ─────────────────────────────────────────────────────

    [Fact]
    public void Defaults_MatchLockedInDecisions()
    {
        var vm = MakeVm(new FakeEngine());

        vm.OperatorWage.Should().Be(32.0);
        vm.SelectedStrategy.Should().Be(IvotionStrategyKind.Evolutionary);
        vm.SelectedObjective.Should().Be(IvotionObjective.MaximizeThroughput);
        vm.Iterations.Should().BePositive();
        vm.PopulationSize.Should().BePositive();
        vm.IsRunning.Should().BeFalse();
        vm.FitnessHistory.Should().BeEmpty();
        vm.BestSolution.Should().BeNull();
    }

    [Fact]
    public void Strategies_HasRandomAndEvolutionaryEnabled_PsoAndSweepDisabled()
    {
        var vm = MakeVm(new FakeEngine());

        vm.Strategies.Should().HaveCount(4);
        vm.Strategies.Single(s => s.Kind == IvotionStrategyKind.Random).IsEnabled.Should().BeTrue();
        vm.Strategies.Single(s => s.Kind == IvotionStrategyKind.Evolutionary).IsEnabled.Should().BeTrue();
        vm.Strategies.Single(s => s.Kind == IvotionStrategyKind.ParticleSwarm).IsEnabled.Should().BeFalse();
        vm.Strategies.Single(s => s.Kind == IvotionStrategyKind.Sweep).IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void Objectives_ListsAllFiveObjectives()
    {
        var vm = MakeVm(new FakeEngine());

        vm.Objectives.Should().Contain(IvotionObjective.MaximizeThroughput);
        vm.Objectives.Should().Contain(IvotionObjective.MinimizeTotalCost);
        vm.Objectives.Should().Contain(IvotionObjective.MinimizeCostPerPiece);
        vm.Objectives.Should().Contain(IvotionObjective.MinimizeLaborHours);
        vm.Objectives.Should().Contain(IvotionObjective.MinimizeFloorSpace);
    }

    // ─── CanExecute gating ─────────────────────────────────────────────────

    [Fact]
    public void RunCommand_Disabled_WhenDisabledStrategySelected()
    {
        var vm = MakeVm(new FakeEngine());
        vm.SelectedStrategy = IvotionStrategyKind.ParticleSwarm;

        vm.RunCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void RunCommand_Disabled_WhenIterationsZero()
    {
        var vm = MakeVm(new FakeEngine());
        vm.Iterations = 0;

        vm.RunCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void StopCommand_OnlyEnabled_WhenRunning()
    {
        var vm = MakeVm(new FakeEngine());

        vm.StopCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ApplyToVizCommand_Disabled_UntilBestSolutionExists()
    {
        var vm = MakeVm(new FakeEngine());

        vm.ApplyToVizCommand.CanExecute(null).Should().BeFalse();
    }

    // ─── Run flow ──────────────────────────────────────────────────────────

    [Fact]
    public async Task RunCommand_PassesSettingsToEngine()
    {
        var engine = new FakeEngine();
        var vm = MakeVm(engine);
        vm.SelectedStrategy = IvotionStrategyKind.Random;
        vm.SelectedObjective = IvotionObjective.MinimizeTotalCost;
        vm.Iterations = 7;
        vm.PopulationSize = 15;
        vm.OperatorWage = 45.5;
        vm.SimDurationMinutes = 90.0;
        vm.Seed = 99;

        await vm.RunCommand.ExecuteAsync(null);

        engine.InvocationCount.Should().Be(1);
        engine.LastSettings.Should().NotBeNull();
        engine.LastSettings!.Strategy.Should().Be(IvotionStrategyKind.Random);
        engine.LastSettings.Objective.Should().Be(IvotionObjective.MinimizeTotalCost);
        engine.LastSettings.Iterations.Should().Be(7);
        engine.LastSettings.PopulationSize.Should().Be(15);
        engine.LastSettings.OperatorWagePerHour.Should().Be(45.5);
        engine.LastSettings.SimDurationMinutes.Should().Be(90.0);
        engine.LastSettings.Seed.Should().Be(99);
    }

    [Fact]
    public async Task RunCommand_AppendsFitnessHistory_FromProgressSamples()
    {
        var sol = SampleSolution();
        sol.Fitness = 7.25;
        sol.HasFitness = true;
        var engine = new FakeEngine
        {
            Samples =
            {
                new IvotionFitnessSample(1, 3.0, sol),
                new IvotionFitnessSample(2, 5.5, sol),
                new IvotionFitnessSample(3, 7.25, sol),
            },
            Result = new IvotionOptimizationResult(sol, null, 3, 12, false),
        };
        var vm = MakeVm(engine);
        vm.SelectedStrategy = IvotionStrategyKind.Random;

        await vm.RunCommand.ExecuteAsync(null);

        vm.FitnessHistory.Should().Equal(3.0, 5.5, 7.25);
        vm.BestFitness.Should().Be(7.25);
    }

    [Fact]
    public async Task RunCommand_FlipsIsRunningBackToFalse_OnCompletion()
    {
        var engine = new FakeEngine { Result = new IvotionOptimizationResult(null, null, 0, 0, false) };
        var vm = MakeVm(engine);

        await vm.RunCommand.ExecuteAsync(null);

        vm.IsRunning.Should().BeFalse();
    }

    [Fact]
    public async Task RunCommand_PopulatesBestSolutionAndKpis_FromResult()
    {
        var sol = SampleSolution(new[] { 2, 3, 3, 2, 2, 20 });
        sol.Fitness = 42.0;
        sol.HasFitness = true;
        var kpis = new IvotionKpis(
            ThroughputPerHour: 42.0,
            TotalCostPerHour: 150.0,
            LaborHoursPerSimHour: 10,
            FloorSpaceM2: 200.0,
            CostPerPiece: 3.57);

        var engine = new FakeEngine
        {
            Result = new IvotionOptimizationResult(sol, kpis, 5, 100, false),
        };
        var vm = MakeVm(engine);

        await vm.RunCommand.ExecuteAsync(null);

        vm.BestSolution.Should().BeSameAs(sol);
        vm.BestKpis.Should().Be(kpis);
        vm.BestFitness.Should().Be(42.0);
        vm.Status.Should().Contain("5 iteration");
    }

    // ─── ApplyToViz event ──────────────────────────────────────────────────

    [Fact]
    public async Task ApplyToVizCommand_Raises_ApplyToVizRequested_WithBestSolution()
    {
        var sol = SampleSolution();
        var engine = new FakeEngine { Result = new IvotionOptimizationResult(sol, null, 1, 0, false) };
        var vm = MakeVm(engine);
        await vm.RunCommand.ExecuteAsync(null);

        IvotionSolution? received = null;
        vm.ApplyToVizRequested += (_, s) => received = s;

        vm.ApplyToVizCommand.Execute(null);

        received.Should().BeSameAs(sol);
    }

    // ─── Stop flow ─────────────────────────────────────────────────────────

    [Fact]
    public async Task StopCommand_SignalsCancellation_ToEngine()
    {
        var tcs = new TaskCompletionSource<IvotionOptimizationResult>();
        CancellationToken observedCt = default;
        var engine = new CapturingEngine(tcs, ct => observedCt = ct);
        var vm = MakeVm(engine);

        var runTask = vm.RunCommand.ExecuteAsync(null);

        // Run command is in-flight; Stop should fire cancellation.
        vm.StopCommand.Execute(null);
        tcs.SetResult(new IvotionOptimizationResult(null, null, 0, 0, true));
        await runTask;

        observedCt.IsCancellationRequested.Should().BeTrue();
        vm.IsRunning.Should().BeFalse();
    }

    private sealed class CapturingEngine : IIvotionOptimizationEngine
    {
        private readonly TaskCompletionSource<IvotionOptimizationResult> _tcs;
        private readonly Action<CancellationToken> _onRun;

        public CapturingEngine(
            TaskCompletionSource<IvotionOptimizationResult> tcs,
            Action<CancellationToken> onRun)
        {
            _tcs = tcs;
            _onRun = onRun;
        }

        public Task<IvotionOptimizationResult> RunAsync(
            IvotionOptimizationSettings settings,
            IProgress<IvotionFitnessSample>? progress,
            CancellationToken ct)
        {
            _onRun(ct);
            return _tcs.Task;
        }
    }
}
