using System;
using System.Collections.Generic;

namespace SimOpt.GridWorld.Environment.Topologies;

public class CubicTopology : ITopology<Coord3D>
{
    private static readonly string[] Names =
        ["Stay", "North", "South", "East", "West", "Up", "Down"];
    private static readonly Coord3D[] Deltas =
    [
        new(0, 0, 0), new(0, -1, 0), new(0, 1, 0),
        new(1, 0, 0), new(-1, 0, 0), new(0, 0, 1), new(0, 0, -1)
    ];

    public int Width { get; }
    public int Height { get; }
    public int Depth { get; }
    public int ActionCount => 7;

    public CubicTopology(int width, int height, int depth)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (depth <= 0) throw new ArgumentOutOfRangeException(nameof(depth));
        Width = width;
        Height = height;
        Depth = depth;
    }

    public string ActionName(int actionId)
    {
        if ((uint)actionId >= (uint)Names.Length)
            throw new ArgumentOutOfRangeException(nameof(actionId), $"Must be 0..{ActionCount - 1}");
        return Names[actionId];
    }

    public Coord3D Step(Coord3D from, int actionId)
    {
        if ((uint)actionId >= (uint)Deltas.Length)
            throw new ArgumentOutOfRangeException(nameof(actionId), $"Must be 0..{ActionCount - 1}");
        return from + Deltas[actionId];
    }

    public IEnumerable<Coord3D> Neighbors(Coord3D coord)
    {
        for (int i = 1; i < Deltas.Length; i++)
        {
            var n = coord + Deltas[i];
            if (n.X >= 0 && n.X < Width && n.Y >= 0 && n.Y < Height
                && n.Z >= 0 && n.Z < Depth)
                yield return n;
        }
    }

    public IEnumerable<Coord3D> Neighborhood(Coord3D center, int radius)
    {
        for (int dx = -radius; dx <= radius; dx++)
            for (int dy = -radius; dy <= radius; dy++)
                for (int dz = -radius; dz <= radius; dz++)
                    yield return new Coord3D(center.X + dx, center.Y + dy, center.Z + dz);
    }

    public double Distance(Coord3D a, Coord3D b) =>
        Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) + Math.Abs(a.Z - b.Z);
}
