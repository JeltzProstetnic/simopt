using System;

namespace SimOpt.GridWorld.Environment;

public class Grid
{
    private readonly CellType[,] _cells;

    public int Width { get; }
    public int Height { get; }

    public Grid(int width, int height)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

        Width = width;
        Height = height;
        _cells = new CellType[width, height];
    }

    public CellType this[int x, int y]
    {
        get => _cells[x, y];
        set => _cells[x, y] = value;
    }

    public bool InBounds(int x, int y) =>
        x >= 0 && x < Width && y >= 0 && y < Height;
}
