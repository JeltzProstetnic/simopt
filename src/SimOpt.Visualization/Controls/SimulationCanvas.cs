using System;
using System.Collections.Generic;
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
/// Animated dots represent entities flowing through the network.
/// </summary>
public class SimulationCanvas : Control
{
    private SqssDemo? _demo;
    private DispatcherTimer? _timer;
    private bool _running;
    private int _stepCount;
    private int _speedMs = 30;
    private int _frame;

    // Throughput tracking
    private int _lastSinkCount;
    private double _lastTime;
    private double _throughput;

    // Entity animation — dots moving between nodes
    private readonly List<AnimatedEntity> _entities = new();
    private readonly Random _animRng = new(1);

    // Layout
    private const double NodeWidth = 130;
    private const double NodeHeight = 65;
    private const double NodeSpacing = 100;

    // Colors — dark theme
    private static readonly IBrush BgBrush = new SolidColorBrush(Color.FromRgb(22, 22, 32));
    private static readonly IBrush NodeBorder = new SolidColorBrush(Color.FromRgb(80, 110, 170));
    private static readonly IBrush SourceFill = new SolidColorBrush(Color.FromRgb(45, 140, 65));
    private static readonly IBrush QueueEmpty = new SolidColorBrush(Color.FromRgb(35, 42, 60));
    private static readonly IBrush QueueFill = new SolidColorBrush(Color.FromRgb(60, 140, 200));
    private static readonly IBrush QueueCritical = new SolidColorBrush(Color.FromRgb(200, 80, 60));
    private static readonly IBrush ServerIdle = new SolidColorBrush(Color.FromRgb(55, 55, 75));
    private static readonly IBrush ServerBusy = new SolidColorBrush(Color.FromRgb(200, 150, 30));
    private static readonly IBrush ServerDamaged = new SolidColorBrush(Color.FromRgb(180, 40, 40));
    private static readonly IBrush SinkFill = new SolidColorBrush(Color.FromRgb(140, 50, 50));
    private static readonly IBrush White = Brushes.White;
    private static readonly IBrush Dim = new SolidColorBrush(Color.FromRgb(140, 140, 165));
    private static readonly IBrush ArrowColor = new SolidColorBrush(Color.FromRgb(70, 75, 95));
    private static readonly IBrush EntityDot = new SolidColorBrush(Color.FromRgb(120, 220, 140));
    private static readonly IBrush ProgressBg = new SolidColorBrush(Color.FromRgb(40, 40, 55));
    private static readonly IBrush ProgressFill = new SolidColorBrush(Color.FromRgb(60, 130, 200));

    private static readonly Pen NodePen = new(NodeBorder, 1.5);
    private static readonly Pen ArrowPen = new(ArrowColor, 2);
    private static readonly Pen EntityPen = new(new SolidColorBrush(Color.FromRgb(80, 180, 100)), 1);

    private static readonly Typeface TfNormal = new("Inter, Segoe UI, sans-serif");
    private static readonly Typeface TfBold = new("Inter, Segoe UI, sans-serif", FontStyle.Normal, FontWeight.Bold);
    private static readonly Typeface TfMono = new("Cascadia Mono, Consolas, monospace");

    public int SpeedMs
    {
        get => _speedMs;
        set
        {
            _speedMs = Math.Max(5, Math.Min(200, value));
            if (_timer != null) _timer.Interval = TimeSpan.FromMilliseconds(_speedMs);
        }
    }

    public void StartSimulation(int seed = 42, double duration = 200.0, int speedMs = 30)
    {
        _demo = new SqssDemo(seed) { EndTime = duration };
        _demo.Build();
        _stepCount = 0;
        _frame = 0;
        _lastSinkCount = 0;
        _lastTime = 0;
        _throughput = 0;
        _entities.Clear();
        _speedMs = speedMs;
        _running = true;

        _demo.StartSource();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(_speedMs) };
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
        _frame++;

        // Update throughput every ~1.0 sim time units
        double dt = _demo.Model.CurrentTime - _lastTime;
        if (dt >= 1.0)
        {
            int produced = _demo.Sink.Count - _lastSinkCount;
            _throughput = produced / dt;
            _lastSinkCount = _demo.Sink.Count;
            _lastTime = _demo.Model.CurrentTime;
        }

        // Spawn entity dots based on sink count changes
        UpdateEntityAnimations();

        InvalidateVisual();
    }

    private void UpdateEntityAnimations()
    {
        // Spawn new entities randomly based on simulation activity
        if (_frame % 3 == 0 && _demo!.Queue.Count > 0)
        {
            _entities.Add(new AnimatedEntity
            {
                Segment = 0, // source → queue
                Progress = _animRng.NextDouble() * 0.3,
                Speed = 0.03 + _animRng.NextDouble() * 0.02
            });
        }

        if (_frame % 4 == 0 && _demo.Server.Working)
        {
            _entities.Add(new AnimatedEntity
            {
                Segment = 1, // queue → server
                Progress = _animRng.NextDouble() * 0.2,
                Speed = 0.04 + _animRng.NextDouble() * 0.02
            });
        }

        if (_frame % 5 == 0 && _demo.Sink.Count > _lastSinkCount - 1)
        {
            _entities.Add(new AnimatedEntity
            {
                Segment = 2, // server → sink
                Progress = _animRng.NextDouble() * 0.3,
                Speed = 0.035 + _animRng.NextDouble() * 0.02
            });
        }

        // Advance and remove completed entities
        for (int i = _entities.Count - 1; i >= 0; i--)
        {
            _entities[i].Progress += _entities[i].Speed;
            if (_entities[i].Progress >= 1.0)
                _entities.RemoveAt(i);
        }

        // Cap entity count to prevent memory issues
        while (_entities.Count > 60)
            _entities.RemoveAt(0);
    }

    public override void Render(DrawingContext ctx)
    {
        base.Render(ctx);
        var b = Bounds;

        // Background
        ctx.DrawRectangle(BgBrush, null, new Rect(0, 0, b.Width, b.Height));

        if (_demo == null)
        {
            Text(ctx, "Press Start to begin simulation", b.Width / 2, b.Height / 2, 18, Dim);
            return;
        }

        // Layout — center the 4 nodes horizontally
        double cy = b.Height / 2 - 10;
        double tw = 4 * NodeWidth + 3 * NodeSpacing;
        double sx = (b.Width - tw) / 2;

        var rSource = R(sx, cy, 0);
        var rQueue = R(sx, cy, 1);
        var rServer = R(sx, cy, 2);
        var rSink = R(sx, cy, 3);

        // Arrows
        Arrow(ctx, rSource.Right, cy, rQueue.Left, cy);
        Arrow(ctx, rQueue.Right, cy, rServer.Left, cy);
        Arrow(ctx, rServer.Right, cy, rSink.Left, cy);

        // Entity dots on arrows
        DrawEntities(ctx, rSource, rQueue, rServer, rSink, cy);

        // Nodes
        DrawSourceNode(ctx, rSource);
        DrawQueueNode(ctx, rQueue);
        DrawServerNode(ctx, rServer);
        DrawSinkNode(ctx, rSink);

        // Header
        Text(ctx, "SimOpt — SQSS Visualization", b.Width / 2, 22, 18, White, true);
        Text(ctx, $"t = {_demo.Model.CurrentTime:F1}", b.Width / 2, 46, 13, Dim, false, TfMono);

        // Progress bar
        DrawProgress(ctx, b);

        // Stats
        DrawStats(ctx, b);

        // Speed indicator
        Text(ctx, $"Speed: {_speedMs}ms", b.Width - 70, 22, 10, Dim);
    }

    private static Rect R(double sx, double cy, int index)
    {
        double x = sx + index * (NodeWidth + NodeSpacing);
        return new Rect(x, cy - NodeHeight / 2, NodeWidth, NodeHeight);
    }

    private void DrawSourceNode(DrawingContext ctx, Rect r)
    {
        ctx.DrawRectangle(SourceFill, NodePen, r, 10, 10);
        Text(ctx, "SOURCE", r.Center.X, r.Center.Y - 10, 12, White, true);
        // Pulse indicator when source is active
        if (_frame % 8 < 4)
        {
            var dot = new Rect(r.Right - 16, r.Top + 6, 8, 8);
            ctx.DrawRectangle(EntityDot, null, dot, 4, 4);
        }
    }

    private void DrawQueueNode(DrawingContext ctx, Rect r)
    {
        ctx.DrawRectangle(QueueEmpty, NodePen, r, 6, 6);

        int count = _demo!.Queue.Count;
        int max = _demo.Queue.MaxCapacity;
        double fill = max > 0 ? (double)count / max : 0;

        // Fill bar (bottom-up)
        if (fill > 0)
        {
            var brush = fill > 0.8 ? QueueCritical : QueueFill;
            double fh = (r.Height - 4) * fill;
            var fr = new Rect(r.X + 2, r.Bottom - 2 - fh, r.Width - 4, fh);
            ctx.DrawRectangle(brush, null, fr, 3, 3);
        }

        Text(ctx, "QUEUE", r.Center.X, r.Center.Y - 10, 12, White, true);
        Text(ctx, $"{count} / {max}", r.Center.X, r.Center.Y + 10, 11, White, false, TfMono);
    }

    private void DrawServerNode(DrawingContext ctx, Rect r)
    {
        IBrush bg = _demo!.Server.Damaged ? ServerDamaged
            : _demo.Server.Working ? ServerBusy : ServerIdle;
        ctx.DrawRectangle(bg, NodePen, r, 10, 10);

        Text(ctx, "SERVER", r.Center.X, r.Center.Y - 10, 12, White, true);
        string state = _demo.Server.Damaged ? "DAMAGED"
            : _demo.Server.Working ? "BUSY" : "IDLE";
        var stateColor = _demo.Server.Working ? White : Dim;
        Text(ctx, state, r.Center.X, r.Center.Y + 10, 10, stateColor, false, TfMono);
    }

    private void DrawSinkNode(DrawingContext ctx, Rect r)
    {
        ctx.DrawRectangle(SinkFill, NodePen, r, 10, 10);
        Text(ctx, "SINK", r.Center.X, r.Center.Y - 10, 12, White, true);
        Text(ctx, $"{_demo!.Sink.Count}", r.Center.X, r.Center.Y + 10, 13, White, true, TfMono);
    }

    private void DrawEntities(DrawingContext ctx, Rect src, Rect que, Rect srv, Rect snk, double cy)
    {
        foreach (var e in _entities)
        {
            double x1, x2;
            switch (e.Segment)
            {
                case 0: x1 = src.Right; x2 = que.Left; break;
                case 1: x1 = que.Right; x2 = srv.Left; break;
                case 2: x1 = srv.Right; x2 = snk.Left; break;
                default: continue;
            }

            double x = x1 + (x2 - x1) * e.Progress;
            double y = cy + Math.Sin(e.Progress * Math.PI * 2) * 3; // slight wobble
            ctx.DrawEllipse(EntityDot, EntityPen, new Point(x, y), 4, 4);
        }
    }

    private void DrawProgress(DrawingContext ctx, Rect b)
    {
        if (_demo == null) return;
        double progress = Math.Min(1.0, _demo.Model.CurrentTime / _demo.EndTime);
        double barW = b.Width * 0.6;
        double barH = 6;
        double barX = (b.Width - barW) / 2;
        double barY = b.Height - 50;

        ctx.DrawRectangle(ProgressBg, null, new Rect(barX, barY, barW, barH), 3, 3);
        if (progress > 0)
            ctx.DrawRectangle(ProgressFill, null, new Rect(barX, barY, barW * progress, barH), 3, 3);

        Text(ctx, $"{progress * 100:F0}%", barX + barW + 15, barY + 3, 10, Dim, false, TfMono);
    }

    private void DrawStats(DrawingContext ctx, Rect b)
    {
        if (_demo == null) return;
        double y = b.Height - 25;
        string status = _running ? "RUN" : "END";
        string tput = _throughput > 0 ? $"{_throughput:F1}/t" : "—";
        string stats = $"{status}  |  Steps: {_stepCount}  |  Queue: {_demo.Queue.Count}/{_demo.Queue.MaxCapacity}  |  Produced: {_demo.Sink.Count}  |  Throughput: {tput}";
        Text(ctx, stats, b.Width / 2, y, 11, Dim, false, TfMono);
    }

    private void Arrow(DrawingContext ctx, double x1, double y1, double x2, double y2)
    {
        ctx.DrawLine(ArrowPen, new Point(x1, y1), new Point(x2, y2));
        double a = Math.Atan2(y2 - y1, x2 - x1);
        double hl = 8;
        ctx.DrawLine(ArrowPen, new Point(x2, y2),
            new Point(x2 - hl * Math.Cos(a - 0.4), y2 - hl * Math.Sin(a - 0.4)));
        ctx.DrawLine(ArrowPen, new Point(x2, y2),
            new Point(x2 - hl * Math.Cos(a + 0.4), y2 - hl * Math.Sin(a + 0.4)));
    }

    private static void Text(DrawingContext ctx, string text, double x, double y,
        double size, IBrush brush, bool bold = false, Typeface? tf = null)
    {
        var fmt = new FormattedText(text, CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight, tf ?? (bold ? TfBold : TfNormal), size, brush);
        ctx.DrawText(fmt, new Point(x - fmt.Width / 2, y - fmt.Height / 2));
    }

    private class AnimatedEntity
    {
        public int Segment { get; set; }     // 0=src→que, 1=que→srv, 2=srv→snk
        public double Progress { get; set; }  // 0.0 to 1.0
        public double Speed { get; set; }     // per frame
    }
}
