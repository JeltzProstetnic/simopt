using System;

namespace SimOpt.Tests.Helpers;

/// <summary>
/// SIM-138: allocation measurement that one-off runtime work does not upset. See
/// <see cref="AllocationsTests"/>.
/// </summary>
public static class Allocations
{
    /// <summary>
    /// Runs <paramref name="action"/> once to warm up, then <paramref name="windows"/> times more,
    /// and returns the fewest bytes the calling thread allocated in one of those runs.
    /// </summary>
    public static long Least(Action action, int windows = 5)
    {
        action();
        long least = long.MaxValue;
        for (int w = 0; w < windows; w++)
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            action();
            least = Math.Min(least, GC.GetAllocatedBytesForCurrentThread() - before);
        }
        return least;
    }
}
