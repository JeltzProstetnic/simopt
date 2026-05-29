using System;
using System.Collections.Generic;

namespace SimOpt.GridWorld.Environment.Topologies;

public class RectangularTopology : ITopology<Coord2D>
{
    private static readonly string[] Names = ["Stay", "North", "South", "East", "West"];
    private static readonly Coord2D[] Deltas =
    [
        new(0, 0), new(0, -1), new(0, 1), new(1, 0), new(-1, 0)
    ];

    public int Width { get; }
    public int Height { get; }
    public int ActionCount => 5;

    public RectangularTopology(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public string ActionName(int actionId) => Names[actionId];

    public Coord2D Step(Coord2D from, int actionId) => from + Deltas[actionId];

    public IEnumerable<Coord2D> Neighbors(Coord2D coord)
    {
        for (int i = 1; i < Deltas.Length; i++)
        {
            var n = coord + Deltas[i];
            if (n.X >= 0 && n.X < Width && n.Y >= 0 && n.Y < Height)
                yield return n;
        }
    }

    public IEnumerable<Coord2D> Neighborhood(Coord2D center, int radius)
    {
        for (int dx = -radius; dx <= radius; dx++)
            for (int dy = -radius; dy <= radius; dy++)
                yield return new Coord2D(center.X + dx, center.Y + dy);
    }

    public double Distance(Coord2D a, Coord2D b) =>
        Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
}
