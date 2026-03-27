using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace SimOpt.Visualization.Controls;

/// <summary>
/// Detachable statistics panel. Receives StatsSnapshot updates from the canvas.
/// </summary>
public class StatsPanel : Control
{
    private StatsSnapshot? _snap;

    private static readonly IBrush Bg = new SolidColorBrush(Color.FromRgb(18, 18, 30));
    private static readonly IBrush White = Brushes.White;
    private static readonly IBrush Dim = new SolidColorBrush(Color.FromRgb(140, 140, 176));
    private static readonly IBrush BarBg = new SolidColorBrush(Color.FromRgb(35, 38, 50));
    private static readonly IBrush BarLow = new SolidColorBrush(Color.FromRgb(60, 180, 100));
    private static readonly IBrush BarMed = new SolidColorBrush(Color.FromRgb(200, 170, 40));
    private static readonly IBrush BarHigh = new SolidColorBrush(Color.FromRgb(210, 60, 50));
    private static readonly IBrush BarWip = new SolidColorBrush(Color.FromRgb(70, 140, 210));
    private static readonly IBrush BottleneckClr = new SolidColorBrush(Color.FromRgb(210, 60, 50));
    private static readonly Typeface Tf = new("Inter, Segoe UI, sans-serif");
    private static readonly Typeface TfBold = new("Inter, Segoe UI, sans-serif", FontStyle.Normal, FontWeight.Bold);
    private static readonly Typeface TfMono = new("Cascadia Mono, Consolas, monospace");

    public void Update(StatsSnapshot snap)
    {
        _snap = snap;
        InvalidateVisual();
    }

    public override void Render(DrawingContext ctx)
    {
        base.Render(ctx);
        var b = Bounds;
        ctx.DrawRectangle(Bg, null, new Rect(0, 0, b.Width, b.Height));

        if (_snap == null)
        {
            Txt(ctx, "Waiting for data...", b.Width / 2, b.Height / 2, 12, Dim);
            return;
        }

        double y = 16;
        double rowH = 20;
        double barX = b.Width * 0.5;
        double barW = b.Width * 0.35;

        Txt(ctx, "Server Utilization", b.Width / 2, y, 11, White, true);
        y += rowH;

        // Find bottleneck
        double maxUtil = 0;
        string bottleneck = "";
        foreach (var s in _snap.Servers)
            if (s.Utilization > maxUtil) { maxUtil = s.Utilization; bottleneck = s.Name; }

        foreach (var srv in _snap.Servers)
        {
            string name = srv.Name.Length > 12 ? srv.Name[..12] : srv.Name;
            bool isBn = srv.Name == bottleneck && maxUtil > 0.5;

            if (isBn) Txt(ctx, "▶", 6, y, 8, BottleneckClr);
            Txt(ctx, name, 16, y, 9, Dim, false, leftAlign: true);

            ctx.DrawRectangle(BarBg, null, new Rect(barX, y - 5, barW, 10), 3, 3);
            IBrush clr = srv.Utilization < 0.6 ? BarLow : srv.Utilization < 0.85 ? BarMed : BarHigh;
            if (srv.Utilization > 0.01)
                ctx.DrawRectangle(clr, null, new Rect(barX, y - 5, barW * Math.Min(1, srv.Utilization), 10), 3, 3);
            Txt(ctx, $"{srv.Utilization * 100:F0}%", barX + barW + 6, y, 8, Dim, false, TfMono, leftAlign: true);

            y += rowH;
        }

        y += 8;
        Txt(ctx, "WIP (Buffers)", b.Width / 2, y, 11, White, true);
        y += rowH;

        foreach (var buf in _snap.Buffers)
        {
            string name = buf.Name.Length > 12 ? buf.Name[..12] : buf.Name;
            double fill = buf.Capacity > 0 ? (double)buf.Count / buf.Capacity : 0;

            Txt(ctx, name, 16, y, 9, Dim, false, leftAlign: true);

            ctx.DrawRectangle(BarBg, null, new Rect(barX, y - 5, barW, 10), 3, 3);
            IBrush wipClr = fill > 0.8 ? BarHigh : BarWip;
            if (fill > 0.01)
                ctx.DrawRectangle(wipClr, null, new Rect(barX, y - 5, barW * Math.Min(1, fill), 10), 3, 3);
            Txt(ctx, $"{buf.Count}", barX + barW + 6, y, 8, Dim, false, TfMono, leftAlign: true);

            y += rowH;
        }

        y += 12;
        string tput = _snap.Throughput > 0 ? $"{_snap.Throughput:F1}/t" : "—";
        Txt(ctx, $"Throughput: {tput}", b.Width / 2, y, 10, Dim);
        y += rowH;
        Txt(ctx, $"Shipped: {_snap.SinkTotal}", b.Width / 2, y, 10, Dim);
        y += rowH;
        Txt(ctx, $"t = {_snap.Time:F1}", b.Width / 2, y, 9, Dim, false, TfMono);
    }

    private static void Txt(DrawingContext ctx, string text, double x, double y,
        double size, IBrush brush, bool bold = false, Typeface? tf = null, bool leftAlign = false)
    {
        var fmt = new FormattedText(text, CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight, tf ?? (bold ? TfBold : Tf), size, brush);
        double drawX = leftAlign ? x : x - fmt.Width / 2;
        ctx.DrawText(fmt, new Point(drawX, y - fmt.Height / 2));
    }
}
