using System;
using System.Collections.Generic;

namespace SimOpt.GridWorld.Environment;

public interface ITopology<TCoord> where TCoord : struct, IEquatable<TCoord>
{
    int ActionCount { get; }
    string ActionName(int actionId);
    TCoord Step(TCoord from, int actionId);
    IEnumerable<TCoord> Neighbors(TCoord coord);
    IEnumerable<TCoord> Neighborhood(TCoord center, int radius);
    double Distance(TCoord a, TCoord b);
}
