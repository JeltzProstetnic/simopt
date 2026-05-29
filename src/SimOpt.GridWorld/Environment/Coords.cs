using System;

namespace SimOpt.GridWorld.Environment;

public readonly record struct Coord2D(int X, int Y) : IComparable<Coord2D>
{
    public static Coord2D operator +(Coord2D a, Coord2D b) => new(a.X + b.X, a.Y + b.Y);
    public static Coord2D operator -(Coord2D a, Coord2D b) => new(a.X - b.X, a.Y - b.Y);
    public int CompareTo(Coord2D other) { int c = X.CompareTo(other.X); return c != 0 ? c : Y.CompareTo(other.Y); }
}

public readonly record struct Coord3D(int X, int Y, int Z) : IComparable<Coord3D>
{
    public static Coord3D operator +(Coord3D a, Coord3D b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Coord3D operator -(Coord3D a, Coord3D b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public int CompareTo(Coord3D other) { int c = X.CompareTo(other.X); if (c != 0) return c; c = Y.CompareTo(other.Y); return c != 0 ? c : Z.CompareTo(other.Z); }
}

public readonly record struct HexCoord(int Q, int R) : IComparable<HexCoord>
{
    public int S => -Q - R;
    public static HexCoord operator +(HexCoord a, HexCoord b) => new(a.Q + b.Q, a.R + b.R);
    public static HexCoord operator -(HexCoord a, HexCoord b) => new(a.Q - b.Q, a.R - b.R);

    public int DistanceTo(HexCoord other) =>
        (Math.Abs(Q - other.Q) + Math.Abs(R - other.R) + Math.Abs(S - other.S)) / 2;
    public int CompareTo(HexCoord other) { int c = Q.CompareTo(other.Q); return c != 0 ? c : R.CompareTo(other.R); }
}
