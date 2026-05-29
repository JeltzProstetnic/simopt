using System;
using System.Collections.Generic;

namespace SimOpt.GridWorld.Environment.Topologies;

public class HexTopology : ITopology<HexCoord>
{
    private static readonly string[] Names =
        ["Stay", "East", "NorthEast", "NorthWest", "West", "SouthWest", "SouthEast"];
    private static readonly HexCoord[] Deltas =
    [
        new(0, 0), new(1, 0), new(1, -1), new(0, -1),
        new(-1, 0), new(-1, 1), new(0, 1)
    ];

    public int Radius { get; }
    public int ActionCount => 7;

    public HexTopology(int radius)
    {
        Radius = radius;
    }

    public string ActionName(int actionId) => Names[actionId];

    public HexCoord Step(HexCoord from, int actionId) => from + Deltas[actionId];

    public IEnumerable<HexCoord> Neighbors(HexCoord coord)
    {
        for (int i = 1; i < Deltas.Length; i++)
        {
            var n = coord + Deltas[i];
            if (IsInBounds(n))
                yield return n;
        }
    }

    public IEnumerable<HexCoord> Neighborhood(HexCoord center, int radius)
    {
        for (int dq = -radius; dq <= radius; dq++)
        {
            int rMin = Math.Max(-radius, -dq - radius);
            int rMax = Math.Min(radius, -dq + radius);
            for (int dr = rMin; dr <= rMax; dr++)
                yield return new HexCoord(center.Q + dq, center.R + dr);
        }
    }

    public double Distance(HexCoord a, HexCoord b) => a.DistanceTo(b);

    private bool IsInBounds(HexCoord c) =>
        Math.Abs(c.Q) <= Radius && Math.Abs(c.R) <= Radius && Math.Abs(c.S) <= Radius;
}
