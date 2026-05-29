using System;

namespace SimOpt.GridWorld.Environment;

public readonly record struct Coord2D(int X, int Y)
{
    public static Coord2D operator +(Coord2D a, Coord2D b) => new(a.X + b.X, a.Y + b.Y);
    public static Coord2D operator -(Coord2D a, Coord2D b) => new(a.X - b.X, a.Y - b.Y);
}

public readonly record struct Coord3D(int X, int Y, int Z)
{
    public static Coord3D operator +(Coord3D a, Coord3D b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Coord3D operator -(Coord3D a, Coord3D b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
}

public readonly record struct HexCoord(int Q, int R)
{
    public int S => -Q - R;
    public static HexCoord operator +(HexCoord a, HexCoord b) => new(a.Q + b.Q, a.R + b.R);
    public static HexCoord operator -(HexCoord a, HexCoord b) => new(a.Q - b.Q, a.R - b.R);

    public int DistanceTo(HexCoord other) =>
        (Math.Abs(Q - other.Q) + Math.Abs(R - other.R) + Math.Abs(S - other.S)) / 2;
}
