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
    private double _lastWidth;
    private double _lastHeight;

    // Node dimensions (from AutoLayout)
    private const double NW = AutoLayout.NodeWidth;
    private const double NH = AutoLayout.NodeHeight;

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

        // Draw floor grid for physical layouts
        if (isPhysical)
            DrawFloorGrid(ctx, b, scale);

        // Draw connections (conveyors for factory, arrows for others)
        foreach (var conn in _topology.Connections)
        {
            if (!_positions.TryGetValue(conn.From, out var p1) || !_positions.TryGetValue(conn.To, out var p2))
                continue;
            var fromNode = _topology.Nodes.FirstOrDefault(n => n.Id == conn.From);
            var toNode = _topology.Nodes.FirstOrDefault(n => n.Id == conn.To);
            var sz1 = fromNode != null ? AutoLayout.GetNodeSize(fromNode, scale) : new Size(NW, NH);
            var sz2 = toNode != null ? AutoLayout.GetNodeSize(toNode, scale) : new Size(NW, NH);
            // Connect from edge of source to edge of target
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist < 1) continue;
            double nx = dx / dist, ny = dy / dist;
            double x1 = p1.X + nx * sz1.Width / 2;
            double y1 = p1.Y + ny * sz1.Height / 2;
            double x2 = p2.X - nx * sz2.Width / 2;
            double y2 = p2.Y - ny * sz2.Height / 2;

            if (isPhysical)
                DrawConveyor(ctx, x1, y1, x2, y2);
            else
                Arrow(ctx, x1, y1, x2, y2);
        }

        // Draw entity shapes (colored diamonds)
        foreach (var dot in _dots)
        {
            if (!_positions.TryGetValue(dot.FromId, out var fp) || !_positions.TryGetValue(dot.ToId, out var tp))
                continue;
            double x = fp.X + (tp.X - fp.X) * dot.Progress;
            double y = fp.Y + (tp.Y - fp.Y) * dot.Progress + Math.Sin(dot.Progress * Math.PI * 2) * 3;
            DrawDiamond(ctx, new Point(x, y), 5, new SolidColorBrush(dot.DotColor), null);
        }

        // Draw nodes with shadows, gradients, and glow
        foreach (var node in _topology.Nodes)
        {
            if (!_positions.TryGetValue(node.Id, out var pos)) continue;
            var state = _nodeStates.FirstOrDefault(n => n.Id == node.Id);
            var sz = AutoLayout.GetNodeSize(node, scale);
            var rect = new Rect(pos.X - sz.Width / 2, pos.Y - sz.Height / 2, sz.Width, sz.Height);
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

        // Scale bar (physical layouts only)
        if (isPhysical)
            DrawScaleBar(ctx, b, scale);

        // Legend
        DrawLegend(ctx, b, isPhysical);
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
