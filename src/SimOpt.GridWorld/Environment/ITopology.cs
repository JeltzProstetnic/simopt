using System;
using System.Collections.Generic;

namespace SimOpt.GridWorld.Environment;

public interface ITopology<TCoord> where TCoord : struct, IEquatable<TCoord>
{
    int ActionCount { get; }
    string ActionName(int actionId);

    /// <summary>
    /// Returns the coordinate reached by taking the given action. The result may be
    /// out of bounds — callers must check Grid.InBounds before using the result.
    /// </summary>
    TCoord Step(TCoord from, int actionId);

    /// <summary>Returns only in-bounds neighbors of the given coordinate.</summary>
    IEnumerable<TCoord> Neighbors(TCoord coord);

    /// <summary>
    /// Returns all coordinates within the given radius, including out-of-bounds ones.
    /// Used for observation construction — out-of-bounds cells map to Wall.
    /// </summary>
    IEnumerable<TCoord> Neighborhood(TCoord center, int radius);

    double Distance(TCoord a, TCoord b);
}
