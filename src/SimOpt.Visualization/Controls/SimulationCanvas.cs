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
/// Phase B: professional industrial/SCADA rendering with gradients, shadows,
/// conveyor animation, glow effects, legend, and scale bar.
/// </summary>
public enum RenderMode { Schematic, Realistic }

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
    private RenderMode _renderMode = RenderMode.Schematic;
    private readonly Random _floorRng = new(12345); // deterministic floor tiles

    public RenderMode Mode
    {
        get => _renderMode;
        set { _renderMode = value; InvalidateVisual(); }
    }

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
    private double _lastWidth;
    private double _lastHeight;

    // Node dimensions (from AutoLayout)
    private const double NW = AutoLayout.NodeWidth;
    private const double NH = AutoLayout.NodeHeight;

    // Statistics tracking
    private readonly Dictionary<string, double> _busyTime = new();
    private readonly Dictionary<string, double> _lastBusyChange = new();
    private readonly Dictionary<string, bool> _wasBusy = new();
    private bool _showStats = true;
    private bool _statsDetached;

    #region Industrial Color Palette

    // Background
    private static readonly IBrush BgBrush = new SolidColorBrush(Color.FromRgb(18, 18, 28));

    // Text
    private static readonly IBrush White = Brushes.White;
    private static readonly IBrush Dim = new SolidColorBrush(Color.FromRgb(140, 140, 176));

    // Grid
    private static readonly IBrush GridBrush = new SolidColorBrush(Color.FromRgb(28, 28, 42));
    private static readonly Pen GridPen = new(GridBrush, 0.5);

    // Shadows
    private static readonly IBrush ShadowBrush = new SolidColorBrush(Color.FromArgb(90, 0, 0, 0));

    // Node border
    private static readonly IBrush NodeBorder = new SolidColorBrush(Color.FromRgb(74, 106, 154));
    private static readonly Pen NPen = new(NodeBorder, 1.5);

    // Arrow / connection
    private static readonly IBrush ArrowClr = new SolidColorBrush(Color.FromRgb(58, 64, 96));
    private static readonly Pen APen = new(ArrowClr, 2);

    // Conveyor
    private static readonly Pen ConveyorPen = new(new SolidColorBrush(Color.FromRgb(42, 48, 72)), 6);
    private static readonly Pen ChevronPen = new(new SolidColorBrush(Color.FromRgb(74, 90, 120)), 1.5);

    // Progress bar
    private static readonly IBrush ProgBg = new SolidColorBrush(Color.FromRgb(36, 36, 50));
    private static readonly IBrush ProgFill = new SolidColorBrush(Color.FromRgb(60, 130, 200));

    // Scale bar
    private static readonly IBrush ScaleBarClr = new SolidColorBrush(Color.FromRgb(100, 100, 130));
    private static readonly Pen ScaleBarPen = new(ScaleBarClr, 1);

    // Legend
    private static readonly IBrush LegendBg = new SolidColorBrush(Color.FromArgb(200, 22, 22, 36));
    private static readonly Pen LegendBorderPen = new(new SolidColorBrush(Color.FromRgb(50, 55, 80)), 1);

    // Default entity dot color (green)
    private static readonly Color DefaultDotColor = Color.FromRgb(90, 218, 112);

    // Glow colors
    private static readonly Color GlowAmber = Color.FromArgb(35, 255, 180, 40);
    private static readonly Color GlowRed = Color.FromArgb(35, 220, 50, 40);
    private static readonly Color GlowGreen = Color.FromArgb(25, 80, 220, 100);

    #endregion

    #region Gradient Brushes

    private static IBrush MakeGrad(byte r1, byte g1, byte b1, byte r2, byte g2, byte b2) =>
        new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Color.FromRgb(r1, g1, b1), 0),
                new GradientStop(Color.FromRgb(r2, g2, b2), 1),
            }
        };

    private static IBrush MakeGradFromHex(string hex)
    {
        var c = Color.Parse(hex);
        byte lr = (byte)Math.Min(255, c.R + 30);
        byte lg = (byte)Math.Min(255, c.G + 30);
        byte lb = (byte)Math.Min(255, c.B + 30);
        byte dr = (byte)Math.Max(0, c.R - 40);
        byte dg = (byte)Math.Max(0, c.G - 40);
        byte db = (byte)Math.Max(0, c.B - 40);
        return MakeGrad(lr, lg, lb, dr, dg, db);
    }

    // Source: green
    private static readonly IBrush SourceGrad = MakeGrad(61, 158, 86, 42, 122, 60);
    // Buffer empty: dark steel
    private static readonly IBrush BufferEmptyGrad = MakeGrad(42, 48, 72, 30, 34, 54);
    // Buffer fill: blue
    private static readonly IBrush BufferFillGrad = MakeGrad(59, 141, 196, 42, 107, 154);
    // Buffer critical: red
    private static readonly IBrush BufferCritGrad = MakeGrad(208, 72, 56, 168, 52, 40);
    // Server idle: cool gray
    private static readonly IBrush ServerIdleGrad = MakeGrad(58, 58, 82, 42, 42, 60);
    // Server busy: amber
    private static readonly IBrush ServerBusyGrad = MakeGrad(212, 160, 40, 184, 136, 24);
    // Server damaged: red
    private static readonly IBrush ServerDmgGrad = MakeGrad(200, 48, 48, 160, 32, 32);
    // Sink: burgundy
    private static readonly IBrush SinkGrad = MakeGrad(160, 56, 56, 122, 40, 40);

    // Status dot colors
    private static readonly IBrush DotIdle = new SolidColorBrush(Color.FromRgb(80, 200, 110));
    private static readonly IBrush DotBusy = new SolidColorBrush(Color.FromRgb(230, 170, 30));
    private static readonly IBrush DotDmg = new SolidColorBrush(Color.FromRgb(220, 50, 50));

    #endregion

    #region Typefaces

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
        _busyTime.Clear();
        _lastBusyChange.Clear();
        _wasBusy.Clear();
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

        UpdateUtilization();
        UpdateDots();
        InvalidateVisual();
    }

    private void UpdateUtilization()
    {
        if (_sim == null) return;
        double now = _sim.CurrentTime;
        foreach (var ns in _nodeStates)
        {
            if (ns.Type != "server") continue;
            bool busy = ns.Working;
            if (!_busyTime.ContainsKey(ns.Id))
            {
                _busyTime[ns.Id] = 0;
                _lastBusyChange[ns.Id] = 0;
                _wasBusy[ns.Id] = false;
            }
            bool was = _wasBusy[ns.Id];
            if (was && !busy)
            {
                _busyTime[ns.Id] += now - _lastBusyChange[ns.Id];
                _lastBusyChange[ns.Id] = now;
            }
            else if (!was && busy)
            {
                _lastBusyChange[ns.Id] = now;
            }
            _wasBusy[ns.Id] = busy;
        }
    }

    private double GetUtilization(string nodeId)
    {
        if (_sim == null || _sim.CurrentTime < 0.1) return 0;
        double busy = _busyTime.GetValueOrDefault(nodeId, 0);
        if (_wasBusy.GetValueOrDefault(nodeId, false))
            busy += _sim.CurrentTime - _lastBusyChange.GetValueOrDefault(nodeId, 0);
        return busy / _sim.CurrentTime;
    }

    public bool ShowStats
    {
        get => _showStats;
        set { _showStats = value; InvalidateVisual(); }
    }

    public bool StatsDetached
    {
        get => _statsDetached;
        set { _statsDetached = value; InvalidateVisual(); }
    }

    /// <summary>
    /// Returns current stats for the detached stats window.
    /// </summary>
    public StatsSnapshot GetStatsSnapshot()
    {
        var snap = new StatsSnapshot { Time = _sim?.CurrentTime ?? 0, Throughput = _throughput };
        foreach (var ns in _nodeStates)
        {
            if (ns.Type == "server")
            {
                var nodeDef = _topology?.Nodes.FirstOrDefault(n => n.Id == ns.Id);
                snap.Servers.Add(new ServerStat
                {
                    Name = nodeDef?.Label?.Split('\n')[0] ?? ns.Id,
                    Utilization = GetUtilization(ns.Id),
                    Working = ns.Working,
                    Damaged = ns.Damaged
                });
            }
            else if (ns.Type == "buffer")
            {
                var nodeDef = _topology?.Nodes.FirstOrDefault(n => n.Id == ns.Id);
                snap.Buffers.Add(new BufferStat
                {
                    Name = nodeDef?.Label?.Split('\n')[0] ?? ns.Id,
                    Count = ns.Count,
                    Capacity = ns.Capacity < int.MaxValue ? ns.Capacity : 100
                });
            }
            else if (ns.Type == "sink")
            {
                snap.SinkTotal += ns.Count;
            }
        }
        return snap;
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
                // Determine entity color from the source node's color hint
                var fromNode = _topology.Nodes.FirstOrDefault(n => n.Id == conn.From);
                Color dotColor = DefaultDotColor;
                if (fromNode?.Color != null)
                {
                    try { dotColor = Color.Parse(fromNode.Color); } catch { }
                }

                _dots.Add(new AnimDot
                {
                    FromId = conn.From,
                    ToId = conn.To,
                    Progress = 0.0,
                    Speed = 0.06 + _rng.NextDouble() * 0.03,
                    DotColor = dotColor
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

        // Recompute layout only on actual window resize
        if (Math.Abs(b.Width - _lastWidth) > 1 || Math.Abs(b.Height - _lastHeight) > 1)
        {
            _positions = AutoLayout.Compute(_topology, b.Width, b.Height - 60);
            _lastWidth = b.Width;
            _lastHeight = b.Height;
        }

        // Compute scale for physical layouts
        double scale = AutoLayout.ComputeScale(_topology, b.Width, b.Height - 60);
        bool isPhysical = _topology.Nodes.Any(n => n.HasPhysicalPosition);

        if (_renderMode == RenderMode.Realistic && isPhysical)
            RenderRealistic(ctx, b, scale);
        else
            RenderSchematic(ctx, b, scale, isPhysical);

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

        // Scale bar (physical layouts only)
        if (isPhysical)
            DrawScaleBar(ctx, b, scale);

        // Legend
        DrawLegend(ctx, b, isPhysical);

        // Statistics panel (only when not detached)
        if (_showStats && !_statsDetached && _sim != null && _sim.CurrentTime > 0.5)
            DrawStatsPanel(ctx, b);
    }

    #region Node Rendering

    private void DrawNode(DrawingContext ctx, Rect r, VizNode def, NodeState? state)
    {
        // Custom gradient from node definition color
        IBrush? customGrad = null;
        if (def.Color != null)
        {
            try { customGrad = MakeGradFromHex(def.Color); } catch { }
        }

        string label = def.Label ?? def.Id;
        double fontSize = Math.Min(11, Math.Max(7, r.Height / 6));

        switch (def.Type.ToLowerInvariant())
        {
            case "source":
                DrawShadow(ctx, r, 8);
                DrawGlow(ctx, r, GlowGreen, (byte)(20 + 8 * Math.Sin(_frame * 0.12)));
                ctx.DrawRectangle(customGrad ?? SourceGrad, NPen, r, 8, 8);
                DrawLabel(ctx, label, r, fontSize, White);
                if (_frame % 8 < 4)
                    ctx.DrawEllipse(DotIdle, null, new Point(r.Right - 8, r.Top + 8), 3, 3);
                break;

            case "buffer":
                DrawShadow(ctx, r, 4);
                ctx.DrawRectangle(BufferEmptyGrad, NPen, r, 4, 4);
                int cnt = state?.Count ?? 0;
                int cap = state?.Capacity ?? 1;
                double fill = cap > 0 ? (double)cnt / cap : 0;
                if (fill > 0)
                {
                    var br = fill > 0.8 ? BufferCritGrad : (customGrad ?? BufferFillGrad);
                    double fh = (r.Height - 4) * Math.Min(1.0, fill);
                    ctx.DrawRectangle(br, null, new Rect(r.X + 2, r.Bottom - 2 - fh, r.Width - 4, fh), 2, 2);
                }
                DrawLabel(ctx, label, r, fontSize, White);
                string capTxt = cap < int.MaxValue ? $"{cnt}/{cap}" : $"{cnt}";
                Txt(ctx, capTxt, r.Center.X, r.Bottom - 8, Math.Max(7, fontSize - 2), White, false, TfMono);
                break;

            case "server":
                bool busy = state?.Working ?? false;
                bool dmg = state?.Damaged ?? false;
                DrawShadow(ctx, r, 8);
                if (busy)
                    DrawGlow(ctx, r, GlowAmber, (byte)(30 + 15 * Math.Sin(_frame * 0.15)));
                if (dmg)
                    DrawGlow(ctx, r, GlowRed, (byte)(35 + 12 * Math.Sin(_frame * 0.2)));
                IBrush bg = dmg ? ServerDmgGrad : busy ? (customGrad ?? ServerBusyGrad) : ServerIdleGrad;
                ctx.DrawRectangle(bg, NPen, r, 8, 8);
                DrawLabel(ctx, label, r, fontSize, White);
                // Status dot
                var dotBrush = dmg ? DotDmg : busy ? DotBusy : DotIdle;
                ctx.DrawEllipse(dotBrush, null, new Point(r.Right - 8, r.Top + 8), 4, 4);
                break;

            case "sink":
                DrawShadow(ctx, r, 8);
                ctx.DrawRectangle(customGrad ?? SinkGrad, NPen, r, 8, 8);
                DrawLabel(ctx, label, r, fontSize, White);
                Txt(ctx, $"{state?.Count ?? 0}", r.Center.X, r.Bottom - 8, Math.Max(8, fontSize), White, true, TfMono);
                break;

            default:
                DrawShadow(ctx, r, 6);
                ctx.DrawRectangle(customGrad ?? ServerIdleGrad, NPen, r, 6, 6);
                DrawLabel(ctx, label, r, fontSize, White);
                break;
        }
    }

    private static void DrawShadow(DrawingContext ctx, Rect r, double cornerRadius)
    {
        var shadowRect = new Rect(r.X + 3, r.Y + 3, r.Width, r.Height);
        ctx.DrawRectangle(ShadowBrush, null, shadowRect, cornerRadius, cornerRadius);
    }

    private static void DrawGlow(DrawingContext ctx, Rect r, Color glowColor, byte alpha)
    {
        var color = Color.FromArgb(alpha, glowColor.R, glowColor.G, glowColor.B);
        var glowBrush = new SolidColorBrush(color);
        var glowRect = r.Inflate(6);
        ctx.DrawRectangle(glowBrush, null, glowRect, 14, 14);
    }

    /// <summary>
    /// Draw multi-line label centered in a node rectangle.
    /// </summary>
    private static void DrawLabel(DrawingContext ctx, string label, Rect r, double fontSize, IBrush brush)
    {
        var lines = label.Split('\n');
        double lineH = fontSize + 2;
        double startY = r.Center.Y - (lines.Length * lineH) / 2 + lineH / 2;
        bool first = true;
        foreach (var line in lines)
        {
            Txt(ctx, line.Trim(), r.Center.X, startY, fontSize, brush, first);
            startY += lineH;
            first = false;
        }
    }

    #endregion

    #region Conveyor Rendering

    private void DrawConveyor(DrawingContext ctx, double x1, double y1, double x2, double y2)
    {
        // Belt: thick line
        ctx.DrawLine(ConveyorPen, new Point(x1, y1), new Point(x2, y2));

        // Animated chevrons along the belt
        double dx = x2 - x1, dy = y2 - y1;
        double dist = Math.Sqrt(dx * dx + dy * dy);
        if (dist < 20) return;
        double nx = dx / dist, ny = dy / dist;
        double perpX = -ny, perpY = nx;

        double spacing = 16;
        double offset = (_frame * 1.2) % spacing;
        for (double t = offset; t < dist - 4; t += spacing)
        {
            double cx = x1 + nx * t;
            double cy = y1 + ny * t;
            // Small chevron pointing in direction of flow
            double sz = 3.5;
            ctx.DrawLine(ChevronPen,
                new Point(cx - nx * sz + perpX * sz, cy - ny * sz + perpY * sz),
                new Point(cx + nx * 1, cy + ny * 1));
            ctx.DrawLine(ChevronPen,
                new Point(cx - nx * sz - perpX * sz, cy - ny * sz - perpY * sz),
                new Point(cx + nx * 1, cy + ny * 1));
        }

        // Small arrowhead at the end
        double a = Math.Atan2(dy, dx);
        var endPen = new Pen(new SolidColorBrush(Color.FromRgb(80, 96, 130)), 1.5);
        ctx.DrawLine(endPen, new Point(x2, y2), new Point(x2 - 7 * Math.Cos(a - 0.4), y2 - 7 * Math.Sin(a - 0.4)));
        ctx.DrawLine(endPen, new Point(x2, y2), new Point(x2 - 7 * Math.Cos(a + 0.4), y2 - 7 * Math.Sin(a + 0.4)));
    }

    #endregion

    #region Floor Grid

    private void DrawFloorGrid(DrawingContext ctx, Rect bounds, double scale)
    {
        double gridStep = 5.0 * scale; // 5 meters in pixels
        if (gridStep < 20) gridStep = 10.0 * scale;
        if (gridStep < 10) return; // too dense

        for (double x = 60; x < bounds.Width - 60; x += gridStep)
            ctx.DrawLine(GridPen, new Point(x, 60), new Point(x, bounds.Height - 55));
        for (double y = 60; y < bounds.Height - 55; y += gridStep)
            ctx.DrawLine(GridPen, new Point(60, y), new Point(bounds.Width - 60, y));
    }

    #endregion

    #region Legend

    private void DrawLegend(DrawingContext ctx, Rect bounds, bool showScale)
    {
        double lw = 145;
        double lh = 148;
        double lx = bounds.Width - lw - 12;
        double ly = 58;

        // Panel
        ctx.DrawRectangle(LegendBg, LegendBorderPen, new Rect(lx, ly, lw, lh), 6, 6);

        // Title
        Txt(ctx, "Legend", lx + lw / 2, ly + 14, 10, White, true);

        double ey = ly + 30;
        double step = 17;
        double swatchX = lx + 12;
        double labelX = lx + 32;

        // Source
        ctx.DrawRectangle(SourceGrad, null, new Rect(swatchX, ey - 5, 14, 10), 3, 3);
        Txt(ctx, "Source", labelX, ey, 9, Dim, false, leftAlign: true);
        ey += step;

        // Buffer
        ctx.DrawRectangle(BufferFillGrad, null, new Rect(swatchX, ey - 5, 14, 10), 2, 2);
        Txt(ctx, "Buffer", labelX, ey, 9, Dim, false, leftAlign: true);
        ey += step;

        // Server (busy)
        ctx.DrawRectangle(ServerBusyGrad, null, new Rect(swatchX, ey - 5, 14, 10), 3, 3);
        Txt(ctx, "Server (busy)", labelX, ey, 9, Dim, false, leftAlign: true);
        ey += step;

        // Sink
        ctx.DrawRectangle(SinkGrad, null, new Rect(swatchX, ey - 5, 14, 10), 3, 3);
        Txt(ctx, "Sink", labelX, ey, 9, Dim, false, leftAlign: true);
        ey += step;

        // Status: idle
        ctx.DrawEllipse(DotIdle, null, new Point(swatchX + 7, ey), 4, 4);
        Txt(ctx, "Idle", labelX, ey, 9, Dim, false, leftAlign: true);
        ey += step;

        // Status: working
        ctx.DrawEllipse(DotBusy, null, new Point(swatchX + 7, ey), 4, 4);
        Txt(ctx, "Working", labelX, ey, 9, Dim, false, leftAlign: true);
        ey += step;

        // Entity
        DrawDiamond(ctx, new Point(swatchX + 7, ey), 4,
            new SolidColorBrush(DefaultDotColor), null);
        Txt(ctx, "Entity", labelX, ey, 9, Dim, false, leftAlign: true);
    }

    #endregion

    #region Scale Bar

    private void DrawScaleBar(DrawingContext ctx, Rect bounds, double scale)
    {
        // Choose bar length that fits nicely
        double barMeters = 10;
        double barPx = barMeters * scale;
        if (barPx > bounds.Width * 0.25) { barMeters = 5; barPx = barMeters * scale; }
        if (barPx < 30) { barMeters = 20; barPx = barMeters * scale; }

        double bx = 20;
        double by = bounds.Height - 52;

        // Main line
        ctx.DrawLine(ScaleBarPen, new Point(bx, by), new Point(bx + barPx, by));
        // End ticks
        ctx.DrawLine(ScaleBarPen, new Point(bx, by - 4), new Point(bx, by + 4));
        ctx.DrawLine(ScaleBarPen, new Point(bx + barPx, by - 4), new Point(bx + barPx, by + 4));
        // Label
        Txt(ctx, $"{barMeters:F0} m", bx + barPx / 2, by - 10, 9, ScaleBarClr);
    }

    #endregion

    #region Render Dispatch

    private void RenderSchematic(DrawingContext ctx, Rect b, double scale, bool isPhysical)
    {
        if (isPhysical) DrawFloorGrid(ctx, b, scale);

        foreach (var conn in _topology!.Connections)
        {
            if (!GetConnectionEndpoints(conn, scale, out var x1, out var y1, out var x2, out var y2)) continue;
            if (isPhysical) DrawConveyor(ctx, x1, y1, x2, y2);
            else Arrow(ctx, x1, y1, x2, y2);
        }

        foreach (var dot in _dots)
        {
            if (!_positions.TryGetValue(dot.FromId, out var fp) || !_positions.TryGetValue(dot.ToId, out var tp)) continue;
            double x = fp.X + (tp.X - fp.X) * dot.Progress;
            double y = fp.Y + (tp.Y - fp.Y) * dot.Progress + Math.Sin(dot.Progress * Math.PI * 2) * 3;
            DrawDiamond(ctx, new Point(x, y), 5, new SolidColorBrush(dot.DotColor), null);
        }

        foreach (var node in _topology.Nodes)
        {
            if (!_positions.TryGetValue(node.Id, out var pos)) continue;
            var state = _nodeStates.FirstOrDefault(n => n.Id == node.Id);
            var sz = AutoLayout.GetNodeSize(node, scale);
            var rect = new Rect(pos.X - sz.Width / 2, pos.Y - sz.Height / 2, sz.Width, sz.Height);
            DrawNode(ctx, rect, node, state);
        }

        if (isPhysical) DrawScaleBar(ctx, b, scale);
        DrawLegend(ctx, b, isPhysical);
    }

    private bool GetConnectionEndpoints(VizConnection conn, double scale,
        out double x1, out double y1, out double x2, out double y2)
    {
        x1 = y1 = x2 = y2 = 0;
        if (!_positions.TryGetValue(conn.From, out var p1) || !_positions.TryGetValue(conn.To, out var p2)) return false;
        var fromNode = _topology!.Nodes.FirstOrDefault(n => n.Id == conn.From);
        var toNode = _topology.Nodes.FirstOrDefault(n => n.Id == conn.To);
        var sz1 = fromNode != null ? AutoLayout.GetNodeSize(fromNode, scale) : new Size(NW, NH);
        var sz2 = toNode != null ? AutoLayout.GetNodeSize(toNode, scale) : new Size(NW, NH);
        double dx = p2.X - p1.X, dy = p2.Y - p1.Y;
        double dist = Math.Sqrt(dx * dx + dy * dy);
        if (dist < 1) return false;
        double nx = dx / dist, ny = dy / dist;
        x1 = p1.X + nx * sz1.Width / 2; y1 = p1.Y + ny * sz1.Height / 2;
        x2 = p2.X - nx * sz2.Width / 2; y2 = p2.Y - ny * sz2.Height / 2;
        return true;
    }

    #endregion

    #region Statistics Panel

    private static readonly IBrush StatsBg = new SolidColorBrush(Color.FromArgb(210, 18, 18, 30));
    private static readonly Pen StatsBorderPen = new(new SolidColorBrush(Color.FromRgb(50, 55, 80)), 1);
    private static readonly IBrush StatsBarBg = new SolidColorBrush(Color.FromRgb(35, 38, 50));
    private static readonly IBrush StatsBarLow = new SolidColorBrush(Color.FromRgb(60, 180, 100));
    private static readonly IBrush StatsBarMed = new SolidColorBrush(Color.FromRgb(200, 170, 40));
    private static readonly IBrush StatsBarHigh = new SolidColorBrush(Color.FromRgb(210, 60, 50));
    private static readonly IBrush StatsBarWip = new SolidColorBrush(Color.FromRgb(70, 140, 210));
    private static readonly IBrush BottleneckHighlight = new SolidColorBrush(Color.FromArgb(40, 210, 60, 50));

    private void DrawStatsPanel(DrawingContext ctx, Rect bounds)
    {
        // Collect server utilizations
        var servers = _nodeStates.Where(n => n.Type == "server").ToList();
        var buffers = _nodeStates.Where(n => n.Type == "buffer").ToList();
        int sinkTotal = _nodeStates.Where(n => n.Type == "sink").Sum(n => n.Count);

        int rowCount = servers.Count + buffers.Count + 3; // +3 for headers and throughput
        double rowH = 16;
        double panelH = Math.Min(rowCount * rowH + 30, bounds.Height * 0.6);
        double panelW = 190;
        double px = 8;
        double py = 58;

        ctx.DrawRectangle(StatsBg, StatsBorderPen, new Rect(px, py, panelW, panelH), 6, 6);
        Txt(ctx, "Statistics", px + panelW / 2, py + 12, 10, White, true);

        double y = py + 26;
        double barX = px + 90;
        double barW = panelW - 100;

        // Find bottleneck (highest utilization server)
        string bottleneckId = "";
        double maxUtil = 0;
        foreach (var srv in servers)
        {
            double u = GetUtilization(srv.Id);
            if (u > maxUtil) { maxUtil = u; bottleneckId = srv.Id; }
        }

        // Server utilizations
        Txt(ctx, "Utilization", px + panelW / 2, y, 8, Dim, true);
        y += rowH;

        foreach (var srv in servers)
        {
            double util = GetUtilization(srv.Id);
            var nodeDef = _topology!.Nodes.FirstOrDefault(n => n.Id == srv.Id);
            string name = nodeDef?.Label?.Split('\n')[0] ?? srv.Id;
            if (name.Length > 10) name = name[..10];

            // Bottleneck highlight
            bool isBottleneck = srv.Id == bottleneckId && maxUtil > 0.5;
            if (isBottleneck)
                Txt(ctx, "▶", px + 4, y, 7, StatsBarHigh);

            Txt(ctx, name, px + 14, y, 8, Dim, false, leftAlign: true);

            // Bar
            ctx.DrawRectangle(StatsBarBg, null, new Rect(barX, y - 4, barW, 8), 2, 2);
            IBrush barClr = util < 0.6 ? StatsBarLow : util < 0.85 ? StatsBarMed : StatsBarHigh;
            if (util > 0.01)
                ctx.DrawRectangle(barClr, null, new Rect(barX, y - 4, barW * Math.Min(1, util), 8), 2, 2);
            Txt(ctx, $"{util * 100:F0}%", barX + barW + 8, y, 7, Dim, false, TfMono, leftAlign: true);

            y += rowH;
        }

        // WIP (buffer levels)
        y += 4;
        Txt(ctx, "WIP (buffers)", px + panelW / 2, y, 8, Dim, true);
        y += rowH;

        foreach (var buf in buffers)
        {
            var nodeDef = _topology!.Nodes.FirstOrDefault(n => n.Id == buf.Id);
            string name = nodeDef?.Label?.Split('\n')[0] ?? buf.Id;
            if (name.Length > 10) name = name[..10];

            int cap = buf.Capacity < int.MaxValue ? buf.Capacity : 100;
            double fill = cap > 0 ? (double)buf.Count / cap : 0;

            Txt(ctx, name, px + 14, y, 8, Dim, false, leftAlign: true);

            ctx.DrawRectangle(StatsBarBg, null, new Rect(barX, y - 4, barW, 8), 2, 2);
            IBrush wipClr = fill > 0.8 ? StatsBarHigh : StatsBarWip;
            if (fill > 0.01)
                ctx.DrawRectangle(wipClr, null, new Rect(barX, y - 4, barW * Math.Min(1, fill), 8), 2, 2);
            Txt(ctx, $"{buf.Count}", barX + barW + 8, y, 7, Dim, false, TfMono, leftAlign: true);

            y += rowH;
        }

        // Throughput
        y += 4;
        string tputStr = _throughput > 0 ? $"{_throughput:F1}/t" : "—";
        Txt(ctx, $"Throughput: {tputStr}  |  Shipped: {sinkTotal}", px + panelW / 2, y, 8, Dim);
    }

    #endregion

    #region Realistic Renderer

    // Realistic palette
    private static readonly IBrush RFloorBase = new SolidColorBrush(Color.FromRgb(58, 58, 62));
    private static readonly Pen RJointPen = new(new SolidColorBrush(Color.FromRgb(42, 42, 46)), 0.5);
    private static readonly Pen RBeltRailPen = new(new SolidColorBrush(Color.FromRgb(90, 95, 105)), 2);
    private static readonly IBrush RBeltSurface = new SolidColorBrush(Color.FromRgb(35, 35, 40));
    private static readonly Pen RRollerPen = new(new SolidColorBrush(Color.FromRgb(75, 78, 85)), 1.5);
    private static readonly Pen RHazardPen = new(new SolidColorBrush(Color.FromArgb(60, 255, 200, 0)), 2) { DashStyle = DashStyle.Dash };
    private static readonly IBrush RMachineCasing = MakeGrad(120, 125, 135, 70, 72, 78);
    private static readonly IBrush RMachineInset = new SolidColorBrush(Color.FromRgb(40, 42, 48));
    private static readonly Pen RMachineBorder = new(new SolidColorBrush(Color.FromRgb(100, 105, 115)), 1.5);
    private static readonly IBrush RDockBay = MakeGrad(80, 85, 92, 55, 58, 64);
    private static readonly IBrush RDockOpen = new SolidColorBrush(Color.FromRgb(30, 30, 35));
    private static readonly Pen RDockBorder = new(new SolidColorBrush(Color.FromRgb(90, 95, 100)), 1);
    private static readonly IBrush RRackSlotEmpty = new SolidColorBrush(Color.FromRgb(45, 48, 55));
    private static readonly IBrush RRackSlotFull = new SolidColorBrush(Color.FromRgb(70, 130, 180));
    private static readonly Pen RRackBorder = new(new SolidColorBrush(Color.FromRgb(80, 85, 95)), 1);
    private static readonly IBrush RLedGreen = new SolidColorBrush(Color.FromRgb(40, 220, 80));
    private static readonly IBrush RLedAmber = new SolidColorBrush(Color.FromRgb(240, 180, 30));
    private static readonly IBrush RLedRed = new SolidColorBrush(Color.FromRgb(230, 50, 40));
    private static readonly IBrush RLedOff = new SolidColorBrush(Color.FromRgb(50, 50, 55));
    private static readonly IBrush RVignette = new SolidColorBrush(Color.FromArgb(60, 0, 0, 0));

    private void RenderRealistic(DrawingContext ctx, Rect b, double scale)
    {
        DrawConcreteFloor(ctx, b, scale);
        DrawHazardMarkings(ctx, scale);

        // Conveyors (behind nodes)
        foreach (var conn in _topology!.Connections)
        {
            if (!GetConnectionEndpoints(conn, scale, out var x1, out var y1, out var x2, out var y2)) continue;
            DrawRealisticBelt(ctx, x1, y1, x2, y2);
        }

        // Entities as 3D boxes
        foreach (var dot in _dots)
        {
            if (!_positions.TryGetValue(dot.FromId, out var fp) || !_positions.TryGetValue(dot.ToId, out var tp)) continue;
            double x = fp.X + (tp.X - fp.X) * dot.Progress;
            double y = fp.Y + (tp.Y - fp.Y) * dot.Progress;
            DrawIsoBox(ctx, new Point(x, y), 7, dot.DotColor);
        }

        // Nodes
        foreach (var node in _topology.Nodes)
        {
            if (!_positions.TryGetValue(node.Id, out var pos)) continue;
            var state = _nodeStates.FirstOrDefault(n => n.Id == node.Id);
            var sz = AutoLayout.GetNodeSize(node, scale);
            var rect = new Rect(pos.X - sz.Width / 2, pos.Y - sz.Height / 2, sz.Width, sz.Height);
            DrawRealisticNode(ctx, rect, node, state);
        }

        // Vignette overlay (darkens edges)
        DrawVignette(ctx, b);
        DrawScaleBar(ctx, b, scale);
    }

    private void DrawConcreteFloor(DrawingContext ctx, Rect b, double scale)
    {
        ctx.DrawRectangle(RFloorBase, null, new Rect(0, 0, b.Width, b.Height));

        // Concrete slabs — deterministic per-tile variation
        double tileSize = Math.Max(30, 2.0 * scale); // ~2m tiles
        var rng = new Random(12345); // reset each frame for determinism
        for (double ty = 0; ty < b.Height; ty += tileSize)
        {
            for (double tx = 0; tx < b.Width; tx += tileSize)
            {
                int v = rng.Next(-8, 9);
                byte c = (byte)Math.Clamp(58 + v, 40, 75);
                var tileBrush = new SolidColorBrush(Color.FromRgb(c, c, (byte)(c + 2)));
                ctx.DrawRectangle(tileBrush, null, new Rect(tx + 0.5, ty + 0.5, tileSize - 1, tileSize - 1));
            }
        }

        // Expansion joints
        for (double ty = 0; ty < b.Height; ty += tileSize)
            ctx.DrawLine(RJointPen, new Point(0, ty), new Point(b.Width, ty));
        for (double tx = 0; tx < b.Width; tx += tileSize)
            ctx.DrawLine(RJointPen, new Point(tx, 0), new Point(tx, b.Height));
    }

    private void DrawHazardMarkings(DrawingContext ctx, double scale)
    {
        foreach (var node in _topology!.Nodes)
        {
            if (!node.HasPhysicalPosition || !_positions.TryGetValue(node.Id, out var pos)) continue;
            if (node.Type != "server") continue;
            var sz = AutoLayout.GetNodeSize(node, scale);
            var r = new Rect(pos.X - sz.Width / 2 - 4, pos.Y - sz.Height / 2 - 4, sz.Width + 8, sz.Height + 8);
            ctx.DrawRectangle(null, RHazardPen, r, 2, 2);
        }
    }

    private void DrawRealisticBelt(DrawingContext ctx, double x1, double y1, double x2, double y2)
    {
        double dx = x2 - x1, dy = y2 - y1;
        double dist = Math.Sqrt(dx * dx + dy * dy);
        if (dist < 5) return;
        double nx = dx / dist, ny = dy / dist;
        double px = -ny, py = nx;
        double hw = 4; // half-width of belt

        // Belt surface
        var geo = new StreamGeometry();
        using (var gc = geo.Open())
        {
            gc.BeginFigure(new Point(x1 + px * hw, y1 + py * hw), true);
            gc.LineTo(new Point(x2 + px * hw, y2 + py * hw));
            gc.LineTo(new Point(x2 - px * hw, y2 - py * hw));
            gc.LineTo(new Point(x1 - px * hw, y1 - py * hw));
            gc.EndFigure(true);
        }
        ctx.DrawGeometry(RBeltSurface, null, geo);

        // Side rails
        ctx.DrawLine(RBeltRailPen, new Point(x1 + px * hw, y1 + py * hw), new Point(x2 + px * hw, y2 + py * hw));
        ctx.DrawLine(RBeltRailPen, new Point(x1 - px * hw, y1 - py * hw), new Point(x2 - px * hw, y2 - py * hw));

        // Animated rollers
        double rollerSpacing = 12;
        double offset = (_frame * 1.0) % rollerSpacing;
        for (double t = offset; t < dist; t += rollerSpacing)
        {
            double rx = x1 + nx * t, ry = y1 + ny * t;
            ctx.DrawLine(RRollerPen,
                new Point(rx + px * (hw - 1), ry + py * (hw - 1)),
                new Point(rx - px * (hw - 1), ry - py * (hw - 1)));
        }
    }

    private void DrawRealisticNode(DrawingContext ctx, Rect r, VizNode def, NodeState? state)
    {
        string label = def.Label ?? def.Id;
        double fontSize = Math.Min(11, Math.Max(7, r.Height / 6));

        switch (def.Type.ToLowerInvariant())
        {
            case "source":
                DrawDockNode(ctx, r, label, fontSize, true, _frame);
                break;
            case "sink":
                DrawDockNode(ctx, r, label, fontSize, false, _frame);
                Txt(ctx, $"{state?.Count ?? 0}", r.Center.X, r.Bottom - fontSize - 2,
                    Math.Max(8, fontSize), White, true, TfMono);
                break;
            case "buffer":
                DrawRackNode(ctx, r, label, fontSize, state);
                break;
            case "server":
                DrawMachineNode(ctx, r, label, fontSize, def, state);
                break;
            default:
                DrawMachineNode(ctx, r, label, fontSize, def, state);
                break;
        }
    }

    private void DrawMachineNode(DrawingContext ctx, Rect r, string label, double fontSize,
        VizNode def, NodeState? state)
    {
        bool busy = state?.Working ?? false;
        bool dmg = state?.Damaged ?? false;

        // Shadow
        ctx.DrawRectangle(ShadowBrush, null, new Rect(r.X + 2, r.Y + 2, r.Width, r.Height), 4, 4);

        // Busy glow
        if (busy)
        {
            byte ga = (byte)(20 + 12 * Math.Sin(_frame * 0.15));
            ctx.DrawRectangle(new SolidColorBrush(Color.FromArgb(ga, 255, 180, 40)), null, r.Inflate(5), 8, 8);
        }

        // Outer casing — metallic gradient
        IBrush casing = RMachineCasing;
        if (def.Color != null)
            try { casing = MakeGradFromHex(def.Color); } catch { }
        ctx.DrawRectangle(casing, RMachineBorder, r, 4, 4);

        // Inner work surface
        var inset = new Rect(r.X + 3, r.Y + 3, r.Width - 6, r.Height - 6);
        ctx.DrawRectangle(RMachineInset, null, inset, 2, 2);

        // Control panel (small strip at bottom)
        double panelH = Math.Min(8, r.Height * 0.15);
        var panel = new Rect(r.X + 3, r.Bottom - panelH - 3, r.Width - 6, panelH);
        ctx.DrawRectangle(new SolidColorBrush(Color.FromRgb(30, 32, 38)), null, panel, 1, 1);

        // LEDs on control panel
        double ledY = panel.Center.Y;
        double ledX = panel.X + 6;
        double ledR = Math.Min(2.5, panelH / 3);
        // Power LED
        ctx.DrawEllipse(RLedGreen, null, new Point(ledX, ledY), ledR, ledR);
        ledX += ledR * 3;
        // Status LED
        IBrush statusLed = dmg ? RLedRed : busy ? RLedAmber : RLedOff;
        ctx.DrawEllipse(statusLed, null, new Point(ledX, ledY), ledR, ledR);
        ledX += ledR * 3;
        // Activity LED (blinks when busy)
        IBrush actLed = busy && _frame % 6 < 3 ? RLedAmber : RLedOff;
        ctx.DrawEllipse(actLed, null, new Point(ledX, ledY), ledR, ledR);

        // Label
        DrawLabel(ctx, label, new Rect(r.X, r.Y, r.Width, r.Height - panelH - 4), fontSize, White);
    }

    private static void DrawDockNode(DrawingContext ctx, Rect r, string label, double fontSize,
        bool isSource, int frame)
    {
        // Shadow
        ctx.DrawRectangle(ShadowBrush, null, new Rect(r.X + 2, r.Y + 2, r.Width, r.Height), 3, 3);

        // Bay
        ctx.DrawRectangle(RDockBay, RDockBorder, r, 3, 3);

        // Open end (darker strip at top for source, bottom for sink)
        double openH = Math.Min(6, r.Height * 0.12);
        var openRect = isSource
            ? new Rect(r.X + 2, r.Y + 2, r.Width - 4, openH)
            : new Rect(r.X + 2, r.Bottom - openH - 2, r.Width - 4, openH);
        ctx.DrawRectangle(RDockOpen, null, openRect, 1, 1);

        // Truck bed / interior hatching
        var interior = new Rect(r.X + 4, r.Y + openH + 4, r.Width - 8, r.Height - openH - 8);
        ctx.DrawRectangle(new SolidColorBrush(Color.FromRgb(50, 52, 58)), null, interior, 2, 2);
        var hatchPen = new Pen(new SolidColorBrush(Color.FromRgb(60, 62, 68)), 0.5);
        for (double hy = interior.Y + 4; hy < interior.Bottom - 2; hy += 5)
            ctx.DrawLine(hatchPen, new Point(interior.X + 2, hy), new Point(interior.Right - 2, hy));

        // Flashing indicator for source
        if (isSource && frame % 10 < 5)
        {
            var ledBrush = new SolidColorBrush(Color.FromArgb(180, 40, 220, 80));
            ctx.DrawEllipse(ledBrush, null, new Point(r.Right - 6, r.Top + 6), 3, 3);
        }

        DrawLabel(ctx, label, r, fontSize, White);
    }

    private void DrawRackNode(DrawingContext ctx, Rect r, string label, double fontSize, NodeState? state)
    {
        // Shadow
        ctx.DrawRectangle(ShadowBrush, null, new Rect(r.X + 2, r.Y + 2, r.Width, r.Height), 3, 3);

        // Rack body
        ctx.DrawRectangle(new SolidColorBrush(Color.FromRgb(55, 58, 65)), RRackBorder, r, 3, 3);

        int cnt = state?.Count ?? 0;
        int cap = state?.Capacity ?? 1;
        if (cap == int.MaxValue) cap = 20;

        // Compute grid: aim for ~square slots
        double slotSize = Math.Min(r.Width / 6, r.Height / 8);
        if (slotSize < 4) slotSize = 4;
        int cols = Math.Max(1, (int)((r.Width - 6) / (slotSize + 2)));
        int rows = Math.Max(1, (int)((r.Height - fontSize - 10) / (slotSize + 2)));
        int totalSlots = Math.Min(cols * rows, cap);

        double gridW = cols * (slotSize + 2) - 2;
        double gridH = rows * (slotSize + 2) - 2;
        double startX = r.X + (r.Width - gridW) / 2;
        double startY = r.Y + 4;

        int filled = Math.Min(cnt, totalSlots);
        int slotIdx = 0;
        for (int row = rows - 1; row >= 0; row--)
        {
            for (int col = 0; col < cols; col++)
            {
                if (slotIdx >= totalSlots) break;
                double sx = startX + col * (slotSize + 2);
                double sy = startY + row * (slotSize + 2);
                bool isFull = slotIdx < filled;
                var slotBrush = isFull ? RRackSlotFull : RRackSlotEmpty;
                ctx.DrawRectangle(slotBrush, null, new Rect(sx, sy, slotSize, slotSize), 1, 1);
                slotIdx++;
            }
        }

        // Count text
        string capTxt = cap < 100 ? $"{cnt}/{cap}" : $"{cnt}";
        Txt(ctx, capTxt, r.Center.X, r.Bottom - 6, Math.Max(7, fontSize - 1), White, false, TfMono);
    }

    private static void DrawIsoBox(DrawingContext ctx, Point p, double size, Color baseColor)
    {
        byte dr = (byte)Math.Max(0, baseColor.R - 50);
        byte dg = (byte)Math.Max(0, baseColor.G - 50);
        byte db = (byte)Math.Max(0, baseColor.B - 50);
        byte sr = (byte)Math.Max(0, baseColor.R - 30);
        byte sg = (byte)Math.Max(0, baseColor.G - 30);
        byte sb = (byte)Math.Max(0, baseColor.B - 30);
        double s = size * 0.5;

        // Top face (brightest)
        var top = new StreamGeometry();
        using (var gc = top.Open())
        {
            gc.BeginFigure(new Point(p.X, p.Y - s * 1.2), true);
            gc.LineTo(new Point(p.X + s, p.Y - s * 0.4));
            gc.LineTo(new Point(p.X, p.Y + s * 0.2));
            gc.LineTo(new Point(p.X - s, p.Y - s * 0.4));
            gc.EndFigure(true);
        }
        ctx.DrawGeometry(new SolidColorBrush(baseColor), null, top);

        // Right face
        var right = new StreamGeometry();
        using (var gc = right.Open())
        {
            gc.BeginFigure(new Point(p.X, p.Y + s * 0.2), true);
            gc.LineTo(new Point(p.X + s, p.Y - s * 0.4));
            gc.LineTo(new Point(p.X + s, p.Y + s * 0.3));
            gc.LineTo(new Point(p.X, p.Y + s * 0.9));
            gc.EndFigure(true);
        }
        ctx.DrawGeometry(new SolidColorBrush(Color.FromRgb(sr, sg, sb)), null, right);

        // Left face (darkest)
        var left = new StreamGeometry();
        using (var gc = left.Open())
        {
            gc.BeginFigure(new Point(p.X, p.Y + s * 0.2), true);
            gc.LineTo(new Point(p.X - s, p.Y - s * 0.4));
            gc.LineTo(new Point(p.X - s, p.Y + s * 0.3));
            gc.LineTo(new Point(p.X, p.Y + s * 0.9));
            gc.EndFigure(true);
        }
        ctx.DrawGeometry(new SolidColorBrush(Color.FromRgb(dr, dg, db)), null, left);
    }

    private static void DrawVignette(DrawingContext ctx, Rect b)
    {
        double v = 40;
        // Top
        ctx.DrawRectangle(new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops = { new GradientStop(Color.FromArgb(80, 0, 0, 0), 0), new GradientStop(Colors.Transparent, 1) }
        }, null, new Rect(0, 0, b.Width, v));
        // Bottom
        ctx.DrawRectangle(new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            GradientStops = { new GradientStop(Color.FromArgb(80, 0, 0, 0), 0), new GradientStop(Colors.Transparent, 1) }
        }, null, new Rect(0, b.Height - v, b.Width, v));
        // Left
        ctx.DrawRectangle(new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
            GradientStops = { new GradientStop(Color.FromArgb(60, 0, 0, 0), 0), new GradientStop(Colors.Transparent, 1) }
        }, null, new Rect(0, 0, v, b.Height));
        // Right
        ctx.DrawRectangle(new LinearGradientBrush
        {
            StartPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            GradientStops = { new GradientStop(Color.FromArgb(60, 0, 0, 0), 0), new GradientStop(Colors.Transparent, 1) }
        }, null, new Rect(b.Width - v, 0, v, b.Height));
    }

    #endregion

    #region Helpers

    private static void Arrow(DrawingContext ctx, double x1, double y1, double x2, double y2)
    {
        ctx.DrawLine(APen, new Point(x1, y1), new Point(x2, y2));
        double a = Math.Atan2(y2 - y1, x2 - x1);
        ctx.DrawLine(APen, new Point(x2, y2), new Point(x2 - 8 * Math.Cos(a - 0.4), y2 - 8 * Math.Sin(a - 0.4)));
        ctx.DrawLine(APen, new Point(x2, y2), new Point(x2 - 8 * Math.Cos(a + 0.4), y2 - 8 * Math.Sin(a + 0.4)));
    }

    private static void DrawDiamond(DrawingContext ctx, Point center, double size, IBrush fill, Pen? stroke)
    {
        var geometry = new StreamGeometry();
        using (var gc = geometry.Open())
        {
            gc.BeginFigure(new Point(center.X, center.Y - size), true);
            gc.LineTo(new Point(center.X + size, center.Y));
            gc.LineTo(new Point(center.X, center.Y + size));
            gc.LineTo(new Point(center.X - size, center.Y));
            gc.EndFigure(true);
        }
        ctx.DrawGeometry(fill, stroke, geometry);
    }

    private static void Txt(DrawingContext ctx, string text, double x, double y,
        double size, IBrush brush, bool bold = false, Typeface? tf = null, bool leftAlign = false)
    {
        var fmt = new FormattedText(text, CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight, tf ?? (bold ? TfBold : TfNorm), size, brush);
        double drawX = leftAlign ? x : x - fmt.Width / 2;
        ctx.DrawText(fmt, new Point(drawX, y - fmt.Height / 2));
    }

    #endregion

    private class AnimDot
    {
        public string FromId { get; set; } = "";
        public string ToId { get; set; } = "";
        public double Progress { get; set; }
        public double Speed { get; set; }
        public Color DotColor { get; set; } = Color.FromRgb(120, 220, 140);
    }
}

public class StatsSnapshot
{
    public double Time { get; set; }
    public double Throughput { get; set; }
    public int SinkTotal { get; set; }
    public List<ServerStat> Servers { get; set; } = new();
    public List<BufferStat> Buffers { get; set; } = new();
}

public class ServerStat
{
    public string Name { get; set; } = "";
    public double Utilization { get; set; }
    public bool Working { get; set; }
    public bool Damaged { get; set; }
}

public class BufferStat
{
    public string Name { get; set; } = "";
    public int Count { get; set; }
    public int Capacity { get; set; }
}
