using System;
using System.Collections.Generic;
using System.Linq;

namespace SimOpt.GridWorld.Environment;

public class Grid<TCoord> where TCoord : struct, IEquatable<TCoord>
{
    private readonly Dictionary<TCoord, CellType> _cells;

    public ITopology<TCoord> Topology { get; }
    public IReadOnlyCollection<TCoord> AllCoords { get; }

    public Grid(ITopology<TCoord> topology, IEnumerable<TCoord> coords)
    {
        Topology = topology ?? throw new ArgumentNullException(nameof(topology));
        var coordList = coords.ToList();
        _cells = new Dictionary<TCoord, CellType>(coordList.Count);
        foreach (var c in coordList)
            _cells[c] = CellType.Empty;
        AllCoords = coordList.AsReadOnly();
    }

    public CellType this[TCoord coord]
    {
        get => _cells.TryGetValue(coord, out var ct) ? ct : throw new KeyNotFoundException($"Coordinate {coord} not in grid");
        set
        {
            if (!_cells.ContainsKey(coord))
                throw new KeyNotFoundException($"Coordinate {coord} not in grid");
            _cells[coord] = value;
        }
    }

    public bool InBounds(TCoord coord) => _cells.ContainsKey(coord);

    public CellType GetOrDefault(TCoord coord, CellType defaultValue = CellType.Wall) =>
        _cells.TryGetValue(coord, out var ct) ? ct : defaultValue;
}

public class Grid2D : Grid<Coord2D>
{
    public int Width { get; }
    public int Height { get; }

    public Grid2D(int width, int height)
        : base(new Topologies.RectangularTopology(width, height),
               GenerateCoords(width, height))
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        Width = width;
        Height = height;
    }

    public CellType this[int x, int y]
    {
        get => this[new Coord2D(x, y)];
        set => this[new Coord2D(x, y)] = value;
    }

    public bool InBounds(int x, int y) => InBounds(new Coord2D(x, y));

    private static IEnumerable<Coord2D> GenerateCoords(int w, int h)
    {
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                yield return new Coord2D(x, y);
    }
}

public class Grid3D : Grid<Coord3D>
{
    public int Width { get; }
    public int Height { get; }
    public int Depth { get; }

    public Grid3D(int width, int height, int depth)
        : base(new Topologies.CubicTopology(width, height, depth),
               GenerateCoords(width, height, depth))
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (depth <= 0) throw new ArgumentOutOfRangeException(nameof(depth));
        Width = width;
        Height = height;
        Depth = depth;
    }

    public CellType this[int x, int y, int z]
    {
        get => this[new Coord3D(x, y, z)];
        set => this[new Coord3D(x, y, z)] = value;
    }

    public bool InBounds(int x, int y, int z) => InBounds(new Coord3D(x, y, z));

    private static IEnumerable<Coord3D> GenerateCoords(int w, int h, int d)
    {
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                for (int z = 0; z < d; z++)
                    yield return new Coord3D(x, y, z);
    }
}

public class HexGrid : Grid<HexCoord>
{
    public int Radius { get; }

    public HexGrid(int radius)
        : base(new Topologies.HexTopology(radius), GenerateCoords(radius))
    {
        if (radius <= 0) throw new ArgumentOutOfRangeException(nameof(radius));
        Radius = radius;
    }

    private static IEnumerable<HexCoord> GenerateCoords(int radius)
    {
        for (int q = -radius; q <= radius; q++)
        {
            int rMin = System.Math.Max(-radius, -q - radius);
            int rMax = System.Math.Min(radius, -q + radius);
            for (int r = rMin; r <= rMax; r++)
                yield return new HexCoord(q, r);
        }
    }
}

public static class Grids
{
    public static Grid2D Rectangular(int width, int height) => new(width, height);
    public static Grid3D Cubic(int width, int height, int depth) => new(width, height, depth);
    public static HexGrid Hexagonal(int radius) => new(radius);
}
