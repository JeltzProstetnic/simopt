using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using SimOpt.Visualization.Models;

namespace SimOpt.Visualization.Controls;

/// <summary>
/// Custom Avalonia control that renders a simulation network in 2D.
/// Nodes represent simulation entities (Source, Buffer, Server, Sink).
/// Shows real-time state: queue fill level, server busy/idle, throughput.
/// </summary>
public class SimulationCanvas : Control
{
    private SqssDemo? _demo;
    private DispatcherTimer? _timer;
    private bool _running;
    private int _stepCount;

    // Layout constants
    private const double NodeWidth = 120;
    private const double NodeHeight = 60;
    private const double NodeSpacing = 80;
    private const double TopMargin = 80;
    private const double LeftMargin = 60;

    // Colors
    private static readonly IBrush BackgroundBrush = new SolidColorBrush(Color.FromRgb(30, 30, 40));
    private static readonly IBrush NodeBorderBrush = new SolidColorBrush(Color.FromRgb(100, 140, 200));
    private static readonly IBrush SourceBrush = new SolidColorBrush(Color.FromRgb(60, 160, 80));
    private static readonly IBrush QueueBrush = new SolidColorBrush(Color.FromRgb(60, 120, 180));
    private static readonly IBrush ServerIdleBrush = new SolidColorBrush(Color.FromRgb(80, 80, 100));
    private static readonly IBrush ServerBusyBrush = new SolidColorBrush(Color.FromRgb(200, 140, 40));
    private static readonly IBrush SinkBrush = new SolidColorBrush(Color.FromRgb(160, 60, 60));
    private static readonly IBrush TextBrush = Brushes.White;
    private static readonly IBrush DimTextBrush = new SolidColorBrush(Color.FromRgb(160, 160, 180));
    private static readonly IBrush ArrowBrush = new SolidColorBrush(Color.FromRgb(100, 100, 120));
    private static readonly IBrush QueueFillBrush = new SolidColorBrush(Color.FromRgb(80, 160, 220));
    private static readonly IBrush QueueEmptyBrush = new SolidColorBrush(Color.FromRgb(40, 50, 70));

    private static readonly Pen NodePen = new(NodeBorderBrush, 2);
    private static readonly Pen ArrowPen = new(ArrowBrush, 2);

    private static readonly Typeface TypefaceNormal = new("Inter, Segoe UI, sans-serif");
    private static readonly Typeface TypefaceBold = new("Inter, Segoe UI, sans-serif", FontStyle.Normal, FontWeight.Bold);

    public void StartSimulation(int seed = 42, double duration = 200.0, int speedMs = 16)
    {
        _demo = new SqssDemo(seed);
        _demo.Build();
        _stepCount = 0;
        _running = true;

        // Start the source to schedule initial events
        _demo.StartSource();

        // Timer drives the animation — each tick advances simulation by one step
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(speedMs) };
        _timer.Tick += (_, _) => StepAndRender();
        _timer.Start();
    }

    public void StopSimulation()
    {
        _running = false;
        _timer?.Stop();
    }

    private void StepAndRender()
    {
        if (_demo == null || !_running) return;

        if (!_demo.Step())
        {
            _running = false;
            _timer?.Stop();
        }
        _stepCount++;

        InvalidateVisual();
    }

    public override void Render(DrawingContext ctx)
    {
        base.Render(ctx);
        var bounds = Bounds;

        // Background
        ctx.DrawRectangle(BackgroundBrush, null, new Rect(0, 0, bounds.Width, bounds.Height));

        if (_demo == null)
        {
            DrawCenteredText(ctx, "Press Start to begin simulation", bounds.Width / 2, bounds.Height / 2, 18, DimTextBrush);
            return;
        }

        // Compute node positions (horizontal layout)
        double centerY = bounds.Height / 2;
        double totalWidth = 4 * NodeWidth + 3 * NodeSpacing;
        double startX = (bounds.Width - totalWidth) / 2;

        var sourceRect = new Rect(startX, centerY - NodeHeight / 2, NodeWidth, NodeHeight);
        var queueRect = new Rect(startX + NodeWidth + NodeSpacing, centerY - NodeHeight / 2, NodeWidth, NodeHeight);
        var serverRect = new Rect(startX + 2 * (NodeWidth + NodeSpacing), centerY - NodeHeight / 2, NodeWidth, NodeHeight);
        var sinkRect = new Rect(startX + 3 * (NodeWidth + NodeSpacing), centerY - NodeHeight / 2, NodeWidth, NodeHeight);

        // Draw arrows between nodes
        DrawArrow(ctx, sourceRect.Right, centerY, queueRect.Left, centerY);
        DrawArrow(ctx, queueRect.Right, centerY, serverRect.Left, centerY);
        DrawArrow(ctx, serverRect.Right, centerY, sinkRect.Left, centerY);

        // Draw nodes
        DrawSource(ctx, sourceRect);
        DrawQueue(ctx, queueRect);
        DrawServer(ctx, serverRect);
        DrawSink(ctx, sinkRect);

        // Header
        DrawHeader(ctx, bounds.Width);

        // Stats bar at bottom
        DrawStats(ctx, bounds);
    }

    private void DrawSource(DrawingContext ctx, Rect rect)
    {
        ctx.DrawRectangle(SourceBrush, NodePen, rect, 8, 8);
        DrawCenteredText(ctx, "SOURCE", rect.Center.X, rect.Center.Y - 8, 13, TextBrush, true);
        DrawCenteredText(ctx, _demo!.Source.EntityName, rect.Center.X, rect.Center.Y + 10, 10, DimTextBrush);
    }

    private void DrawQueue(DrawingContext ctx, Rect rect)
    {
        // Outer frame
        ctx.DrawRectangle(QueueEmptyBrush, NodePen, rect, 4, 4);

        // Fill level indicator
        int count = _demo!.Queue.Count;
        int max = _demo.Queue.MaxCapacity;
        double fillRatio = max > 0 ? (double)count / max : 0;
        if (fillRatio > 0)
        {
            var fillRect = new Rect(rect.X + 2, rect.Y + 2 + (rect.Height - 4) * (1 - fillRatio),
                rect.Width - 4, (rect.Height - 4) * fillRatio);
            ctx.DrawRectangle(QueueFillBrush, null, fillRect, 2, 2);
        }

        DrawCenteredText(ctx, "QUEUE", rect.Center.X, rect.Center.Y - 8, 13, TextBrush, true);
        DrawCenteredText(ctx, $"{count} / {max}", rect.Center.X, rect.Center.Y + 10, 11, TextBrush);
    }

    private void DrawServer(DrawingContext ctx, Rect rect)
    {
        var brush = _demo!.Server.Working ? ServerBusyBrush : ServerIdleBrush;
        ctx.DrawRectangle(brush, NodePen, rect, 8, 8);

        DrawCenteredText(ctx, "SERVER", rect.Center.X, rect.Center.Y - 8, 13, TextBrush, true);
        string state = _demo.Server.Working ? "BUSY" : (_demo.Server.Damaged ? "DAMAGED" : "IDLE");
        DrawCenteredText(ctx, state, rect.Center.X, rect.Center.Y + 10, 10,
            _demo.Server.Working ? TextBrush : DimTextBrush);
    }

    private void DrawSink(DrawingContext ctx, Rect rect)
    {
        ctx.DrawRectangle(SinkBrush, NodePen, rect, 8, 8);
        DrawCenteredText(ctx, "SINK", rect.Center.X, rect.Center.Y - 8, 13, TextBrush, true);
        DrawCenteredText(ctx, $"{_demo!.Sink.Count} items", rect.Center.X, rect.Center.Y + 10, 10, TextBrush);
    }

    private void DrawHeader(DrawingContext ctx, double width)
    {
        DrawCenteredText(ctx, "SimOpt — SQSS Visualization", width / 2, 25, 20, TextBrush, true);
        string timeStr = _demo != null ? $"t = {_demo.Model.CurrentTime:F2}" : "";
        DrawCenteredText(ctx, timeStr, width / 2, 50, 14, DimTextBrush);
    }

    private void DrawStats(DrawingContext ctx, Rect bounds)
    {
        if (_demo == null) return;

        double y = bounds.Height - 30;
        string status = _running ? "RUNNING" : "STOPPED";
        string stats = $"{status}  |  Events: {_stepCount}  |  Queue: {_demo.Queue.Count}/{_demo.Queue.MaxCapacity}  |  Sink: {_demo.Sink.Count}  |  Server: {(_demo.Server.Working ? "BUSY" : "IDLE")}";
        DrawCenteredText(ctx, stats, bounds.Width / 2, y, 12, DimTextBrush);
    }

    private void DrawArrow(DrawingContext ctx, double x1, double y1, double x2, double y2)
    {
        ctx.DrawLine(ArrowPen, new Point(x1, y1), new Point(x2, y2));

        // Arrowhead
        double angle = Math.Atan2(y2 - y1, x2 - x1);
        double headLen = 10;
        var p1 = new Point(x2 - headLen * Math.Cos(angle - 0.4), y2 - headLen * Math.Sin(angle - 0.4));
        var p2 = new Point(x2 - headLen * Math.Cos(angle + 0.4), y2 - headLen * Math.Sin(angle + 0.4));
        ctx.DrawLine(ArrowPen, new Point(x2, y2), p1);
        ctx.DrawLine(ArrowPen, new Point(x2, y2), p2);
    }

    private static void DrawCenteredText(DrawingContext ctx, string text, double x, double y, double size, IBrush brush, bool bold = false)
    {
        var fmt = new FormattedText(text, System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight, bold ? TypefaceBold : TypefaceNormal, size, brush);
        ctx.DrawText(fmt, new Point(x - fmt.Width / 2, y - fmt.Height / 2));
    }
}
