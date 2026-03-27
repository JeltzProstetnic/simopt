using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using SimOpt.Visualization.Models;

namespace SimOpt.Visualization.Controls;

/// <summary>
/// Generic simulation visualization canvas.
/// Renders any VizTopology with auto-layout, animated entity flow, and live state.
/// </summary>
public class SimulationCanvas : Control
{
    private SimulationModel? _sim;
    private VizTopology? _topology;
    private Dictionary<string, Point> _positions = new();
    private DispatcherTimer? _timer;
    private bool _running;
    private int _stepCount;
    private int _frame;
    private int _speedMs = 30;

    // Throughput tracking
    private int _lastSinkTotal;
    private double _lastTime;
    private double _throughput;

    // Entity animations — driven by state changes, not frame count
    private readonly List<AnimDot> _dots = new();
    private readonly Random _rng = new(1);
    private List<NodeState> _nodeStates = new();
    private Dictionary<string, int> _prevCounts = new();
    private Dictionary<string, bool> _prevWorking = new();

    // Node dimensions (from AutoLayout)
    private const double NW = AutoLayout.NodeWidth;
    private const double NH = AutoLayout.NodeHeight;

    #region Colors

    private static readonly IBrush BgBrush = new SolidColorBrush(Color.FromRgb(22, 22, 32));
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
    private static readonly IBrush ArrowClr = new SolidColorBrush(Color.FromRgb(70, 75, 95));
    private static readonly IBrush DotClr = new SolidColorBrush(Color.FromRgb(120, 220, 140));
    private static readonly IBrush ProgBg = new SolidColorBrush(Color.FromRgb(40, 40, 55));
    private static readonly IBrush ProgFill = new SolidColorBrush(Color.FromRgb(60, 130, 200));
    private static readonly IBrush NodeBorder = new SolidColorBrush(Color.FromRgb(80, 110, 170));

    private static readonly Pen NPen = new(NodeBorder, 1.5);
    private static readonly Pen APen = new(ArrowClr, 2);
    private static readonly Pen DPen = new(new SolidColorBrush(Color.FromRgb(80, 180, 100)), 1);

    private static readonly Typeface TfNorm = new("Inter, Segoe UI, sans-serif");
    private static readonly Typeface TfBold = new("Inter, Segoe UI, sans-serif", FontStyle.Normal, FontWeight.Bold);
    private static readonly Typeface TfMono = new("Cascadia Mono, Consolas, monospace");

    #endregion

    public int SpeedMs
    {
        get => _speedMs;
        set
        {
            _speedMs = Math.Max(5, Math.Min(200, value));
            if (_timer != null) _timer.Interval = TimeSpan.FromMilliseconds(_speedMs);
        }
    }

    /// <summary>
    /// Start a simulation from any topology description.
    /// </summary>
    public void StartSimulation(VizTopology topology, double duration = 200.0, int speedMs = 30)
    {
        _topology = topology;
        _sim = new SimulationModel { EndTime = duration };
        _sim.Build(topology);
        _stepCount = 0;
        _frame = 0;
        _lastSinkTotal = 0;
        _lastTime = 0;
        _throughput = 0;
        _dots.Clear();
        _nodeStates.Clear();
        _speedMs = speedMs;
        _running = true;

        // Compute layout
        _positions = AutoLayout.Compute(topology, Bounds.Width > 0 ? Bounds.Width : 960, Bounds.Height > 0 ? Bounds.Height - 60 : 460);

        _sim.Start();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(_speedMs) };
        _timer.Tick += (_, _) => Tick();
        _timer.Start();
    }

    /// <summary>
    /// Convenience: start with a preset topology.
    /// </summary>
    public void StartSimulation(int seed = 42, double duration = 200.0, int speedMs = 30)
    {
        StartSimulation(VizTopology.Sqss(seed), duration, speedMs);
    }

    public void StopSimulation()
    {
        _running = false;
        _timer?.Stop();
    }

    private void Tick()
    {
        if (_sim == null || !_running) return;

        if (!_sim.Step())
        {
            _running = false;
            _timer?.Stop();
        }
        _stepCount++;
        _frame++;

        _nodeStates = _sim.GetNodeStates();

        // Throughput
        int sinkTotal = _nodeStates.Where(n => n.Type == "sink").Sum(n => n.Count);
        double dt = _sim.CurrentTime - _lastTime;
        if (dt >= 1.0)
        {
            _throughput = (sinkTotal - _lastSinkTotal) / dt;
            _lastSinkTotal = sinkTotal;
            _lastTime = _sim.CurrentTime;
        }

        UpdateDots();
        InvalidateVisual();
    }

    private void UpdateDots()
    {
        if (_topology == null) return;

        // Build current state lookup
        var curCounts = new Dictionary<string, int>();
        var curWorking = new Dictionary<string, bool>();
        foreach (var ns in _nodeStates)
        {
            curCounts[ns.Id] = ns.Count;
            curWorking[ns.Id] = ns.Working;
        }

        // Spawn dots ONLY when state changes indicate actual entity movement
        foreach (var conn in _topology.Connections)
        {
            if (!curCounts.ContainsKey(conn.From) || !curCounts.ContainsKey(conn.To)) continue;
            var fromState = _nodeStates.FirstOrDefault(n => n.Id == conn.From);
            var toState = _nodeStates.FirstOrDefault(n => n.Id == conn.To);
            if (fromState == null || toState == null) continue;

            bool spawn = false;

            // Source → downstream: spawn when downstream count increased
            if (fromState.Type == "source")
            {
                int prevTo = _prevCounts.GetValueOrDefault(conn.To, 0);
                spawn = curCounts[conn.To] > prevTo;
            }
            // Buffer → server: spawn when buffer count decreased (item pulled)
            else if (fromState.Type == "buffer")
            {
                int prevFrom = _prevCounts.GetValueOrDefault(conn.From, 0);
                spawn = curCounts[conn.From] < prevFrom;
            }
            // Server → downstream: spawn when downstream count increased (item finished)
            else if (fromState.Type == "server")
            {
                int prevTo = _prevCounts.GetValueOrDefault(conn.To, 0);
                spawn = curCounts[conn.To] > prevTo;
            }

            if (spawn)
            {
                _dots.Add(new AnimDot
                {
                    FromId = conn.From,
                    ToId = conn.To,
                    Progress = 0.0,
                    Speed = 0.06 + _rng.NextDouble() * 0.03
                });
            }
        }

        // Save current state for next frame diff
        _prevCounts = curCounts;
        _prevWorking = curWorking;

        // Advance and cull
        for (int i = _dots.Count - 1; i >= 0; i--)
        {
            _dots[i].Progress += _dots[i].Speed;
            if (_dots[i].Progress >= 1.0)
                _dots.RemoveAt(i);
        }
        while (_dots.Count > 80) _dots.RemoveAt(0);
    }

    public override void Render(DrawingContext ctx)
    {
        base.Render(ctx);
        var b = Bounds;
        ctx.DrawRectangle(BgBrush, null, new Rect(0, 0, b.Width, b.Height));

        if (_topology == null || _sim == null)
        {
            Txt(ctx, "Press Start to begin simulation", b.Width / 2, b.Height / 2, 18, Dim);
            return;
        }

        // Recompute layout on resize
        if (_frame % 30 == 1)
            _positions = AutoLayout.Compute(_topology, b.Width, b.Height - 60);

        // Draw connections (arrows)
        foreach (var conn in _topology.Connections)
        {
            if (_positions.TryGetValue(conn.From, out var p1) && _positions.TryGetValue(conn.To, out var p2))
                Arrow(ctx, p1.X + NW / 2, p1.Y, p2.X - NW / 2, p2.Y);
        }

        // Draw dots
        foreach (var dot in _dots)
        {
            if (!_positions.TryGetValue(dot.FromId, out var fp) || !_positions.TryGetValue(dot.ToId, out var tp))
                continue;
            double x = (fp.X + NW / 2) + ((tp.X - NW / 2) - (fp.X + NW / 2)) * dot.Progress;
            double y = (fp.Y + tp.Y) / 2 + Math.Sin(dot.Progress * Math.PI * 2) * 4;
            ctx.DrawEllipse(DotClr, DPen, new Point(x, y), 4, 4);
        }

        // Draw nodes
        foreach (var node in _topology.Nodes)
        {
            if (!_positions.TryGetValue(node.Id, out var pos)) continue;
            var state = _nodeStates.FirstOrDefault(n => n.Id == node.Id);
            var rect = new Rect(pos.X - NW / 2, pos.Y - NH / 2, NW, NH);
            DrawNode(ctx, rect, node, state);
        }

        // Header
        Txt(ctx, $"SimOpt — {_topology.Name}", b.Width / 2, 22, 17, White, true);
        Txt(ctx, $"t = {_sim.CurrentTime:F1}", b.Width / 2, 44, 12, Dim, false, TfMono);

        // Progress bar
        double prog = Math.Min(1.0, _sim.CurrentTime / _sim.EndTime);
        double bw = b.Width * 0.5;
        double bx = (b.Width - bw) / 2;
        double by = b.Height - 48;
        ctx.DrawRectangle(ProgBg, null, new Rect(bx, by, bw, 5), 2, 2);
        if (prog > 0) ctx.DrawRectangle(ProgFill, null, new Rect(bx, by, bw * prog, 5), 2, 2);
        Txt(ctx, $"{prog * 100:F0}%", bx + bw + 14, by + 3, 10, Dim, false, TfMono);

        // Stats
        int sinkTotal = _nodeStates.Where(n => n.Type == "sink").Sum(n => n.Count);
        int queueTotal = _nodeStates.Where(n => n.Type == "buffer").Sum(n => n.Count);
        string tput = _throughput > 0 ? $"{_throughput:F1}/t" : "—";
        string stat = $"{(_running ? "RUN" : "END")}  |  Steps: {_stepCount}  |  Queued: {queueTotal}  |  Produced: {sinkTotal}  |  Throughput: {tput}  |  Speed: {_speedMs}ms";
        Txt(ctx, stat, b.Width / 2, b.Height - 25, 11, Dim, false, TfMono);
    }

    private void DrawNode(DrawingContext ctx, Rect r, VizNode def, NodeState? state)
    {
        switch (def.Type.ToLowerInvariant())
        {
            case "source":
                ctx.DrawRectangle(SourceFill, NPen, r, 10, 10);
                Txt(ctx, "SOURCE", r.Center.X, r.Center.Y - 10, 11, White, true);
                Txt(ctx, def.Id, r.Center.X, r.Center.Y + 8, 9, Dim);
                if (_frame % 8 < 4) // pulse
                    ctx.DrawEllipse(DotClr, null, new Point(r.Right - 10, r.Top + 10), 4, 4);
                break;

            case "buffer":
                ctx.DrawRectangle(QueueEmpty, NPen, r, 5, 5);
                int cnt = state?.Count ?? 0;
                int cap = state?.Capacity ?? 1;
                double fill = cap > 0 ? (double)cnt / cap : 0;
                if (fill > 0)
                {
                    var br = fill > 0.8 ? QueueCritical : QueueFill;
                    double fh = (r.Height - 4) * Math.Min(1.0, fill);
                    ctx.DrawRectangle(br, null, new Rect(r.X + 2, r.Bottom - 2 - fh, r.Width - 4, fh), 2, 2);
                }
                Txt(ctx, "BUFFER", r.Center.X, r.Center.Y - 10, 11, White, true);
                Txt(ctx, cap < int.MaxValue ? $"{cnt}/{cap}" : $"{cnt}", r.Center.X, r.Center.Y + 8, 10, White, false, TfMono);
                Txt(ctx, def.Id, r.Center.X, r.Center.Y + 22, 8, Dim);
                break;

            case "server":
                bool busy = state?.Working ?? false;
                bool dmg = state?.Damaged ?? false;
                var bg = dmg ? ServerDamaged : busy ? ServerBusy : ServerIdle;
                ctx.DrawRectangle(bg, NPen, r, 10, 10);
                Txt(ctx, "SERVER", r.Center.X, r.Center.Y - 10, 11, White, true);
                string st = dmg ? "DMG" : busy ? "BUSY" : "IDLE";
                Txt(ctx, st, r.Center.X, r.Center.Y + 8, 10, busy ? White : Dim, false, TfMono);
                Txt(ctx, def.Id, r.Center.X, r.Center.Y + 22, 8, Dim);
                break;

            case "sink":
                ctx.DrawRectangle(SinkFill, NPen, r, 10, 10);
                Txt(ctx, "SINK", r.Center.X, r.Center.Y - 10, 11, White, true);
                Txt(ctx, $"{state?.Count ?? 0}", r.Center.X, r.Center.Y + 8, 13, White, true, TfMono);
                Txt(ctx, def.Id, r.Center.X, r.Center.Y + 22, 8, Dim);
                break;

            default:
                ctx.DrawRectangle(ServerIdle, NPen, r, 6, 6);
                Txt(ctx, def.Type.ToUpperInvariant(), r.Center.X, r.Center.Y - 6, 11, White, true);
                Txt(ctx, def.Id, r.Center.X, r.Center.Y + 10, 9, Dim);
                break;
        }
    }

    private static void Arrow(DrawingContext ctx, double x1, double y1, double x2, double y2)
    {
        ctx.DrawLine(APen, new Point(x1, y1), new Point(x2, y2));
        double a = Math.Atan2(y2 - y1, x2 - x1);
        ctx.DrawLine(APen, new Point(x2, y2), new Point(x2 - 8 * Math.Cos(a - 0.4), y2 - 8 * Math.Sin(a - 0.4)));
        ctx.DrawLine(APen, new Point(x2, y2), new Point(x2 - 8 * Math.Cos(a + 0.4), y2 - 8 * Math.Sin(a + 0.4)));
    }

    private static void Txt(DrawingContext ctx, string text, double x, double y,
        double size, IBrush brush, bool bold = false, Typeface? tf = null)
    {
        var fmt = new FormattedText(text, CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight, tf ?? (bold ? TfBold : TfNorm), size, brush);
        ctx.DrawText(fmt, new Point(x - fmt.Width / 2, y - fmt.Height / 2));
    }

    private class AnimDot
    {
        public string FromId { get; set; } = "";
        public string ToId { get; set; } = "";
        public double Progress { get; set; }
        public double Speed { get; set; }
    }
}
