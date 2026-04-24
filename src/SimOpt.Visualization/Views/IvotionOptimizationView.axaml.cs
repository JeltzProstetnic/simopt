using System;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using SimOpt.Ivotion;
using SimOpt.Visualization.ViewModels;

namespace SimOpt.Visualization.Views;

public partial class IvotionOptimizationView : UserControl
{
    private IvotionOptimizationViewModel? _vm;
    private ScottPlot.Avalonia.AvaPlot? _plot;

    public IvotionOptimizationView()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) =>
        {
            _plot = this.FindControl<ScottPlot.Avalonia.AvaPlot>("FitnessPlot");
            SubscribeToViewModel();
            RefreshPlot();
        };
        DataContextChanged += (_, _) =>
        {
            SubscribeToViewModel();
            RefreshPlot();
        };
    }

    private void SubscribeToViewModel()
    {
        if (_vm is not null)
            _vm.FitnessHistory.CollectionChanged -= OnFitnessHistoryChanged;

        _vm = DataContext as IvotionOptimizationViewModel;

        if (_vm is not null)
            _vm.FitnessHistory.CollectionChanged += OnFitnessHistoryChanged;
    }

    private void OnFitnessHistoryChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (Dispatcher.UIThread.CheckAccess()) RefreshPlot();
        else Dispatcher.UIThread.Post(RefreshPlot);
    }

    private void RefreshPlot()
    {
        if (_plot is null || _vm is null) return;

        _plot.Plot.Clear();
        int count = _vm.FitnessHistory.Count;
        if (count > 0)
        {
            double[] xs = Enumerable.Range(1, count).Select(i => (double)i).ToArray();
            double[] ys = _vm.FitnessHistory.ToArray();
            var line = _plot.Plot.Add.Scatter(xs, ys);
            line.LineWidth = 2;
            line.MarkerSize = 4;

            // Tight left edge at iteration 1; small margin on right. Y gets a
            // data-padded auto-fit so flat runs don't hug the top border.
            double yMin = ys.Min();
            double yMax = ys.Max();
            double yPad = Math.Max(Math.Abs(yMax - yMin) * 0.15, Math.Abs(yMax) * 0.02 + 0.01);
            _plot.Plot.Axes.SetLimits(left: 1, right: count + 0.5,
                                      bottom: yMin - yPad, top: yMax + yPad);
        }
        else
        {
            _plot.Plot.Axes.SetLimits(left: 0, right: 1, bottom: 0, top: 1);
        }
        _plot.Plot.Title("Best-so-far fitness");
        _plot.Plot.XLabel("Iteration");
        _plot.Plot.YLabel("Fitness");
        _plot.Refresh();
    }
}

/// <summary>Bool → opacity (enabled = 1.0, disabled = 0.45).</summary>
public sealed class BoolToOpacityConverter : IValueConverter
{
    public static readonly BoolToOpacityConverter Instance = new();
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => (value is bool b && b) ? 1.0 : 0.45;
    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class IvotionSolutionToSummaryConverter : IValueConverter
{
    public static readonly IvotionSolutionToSummaryConverter Instance = new();
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is not IvotionSolution s) return "—";
        return
            $"Rolands         : {s.RolandCount}\n" +
            $"Ops inspect     : {s.OperatorsInspect}\n" +
            $"Ops pack        : {s.OperatorsPack}\n" +
            $"Ops label       : {s.OperatorsLabel}\n" +
            $"Ops SSB         : {s.OperatorsSsb}\n" +
            $"Batch size      : {s.RolandBatchSize}";
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class IvotionKpisToSummaryConverter : IValueConverter
{
    public static readonly IvotionKpisToSummaryConverter Instance = new();
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is not IvotionKpis k) return "—";
        return
            $"Throughput/hr   : {k.ThroughputPerHour,7:F1}\n" +
            $"Total cost/hr   : ${k.TotalCostPerHour,7:F2}\n" +
            $"Labor hrs/hr    : {k.LaborHoursPerSimHour,7:F1}\n" +
            $"Floor space m²  : {k.FloorSpaceM2,7:F1}\n" +
            $"Cost/piece      : ${k.CostPerPiece,7:F2}";
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => throw new NotSupportedException();
}
