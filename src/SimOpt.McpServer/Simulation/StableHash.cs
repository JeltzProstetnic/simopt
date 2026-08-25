using System.Text;

namespace SimOpt.McpServer.Simulation;

/// <summary>
/// A process-stable string hash, used to derive a distinct random seed per node from a single
/// topology seed.
/// </summary>
/// <remarks>
/// <para>
/// SIM-62. The obvious choice, <see cref="string.GetHashCode()"/>, is wrong here and wrong in a
/// way that is invisible in any single session: .NET randomises string hashing per process, so
/// the same topology and the same seed produce different random streams after every restart of
/// the MCP server. Reproducibility (UN-009) then holds only within one session, which is exactly
/// when nobody is checking it.
/// </para>
/// <para>
/// FNV-1a is used because it is fixed by specification, trivially implementable, and identical on
/// every runtime and platform — the properties that matter for a seed. Its distribution quality is
/// irrelevant: the value is only mixed into a seed, never used for bucketing.
/// </para>
/// </remarks>
public static class StableHash
{
    private const uint FnvOffsetBasis = 2166136261;
    private const uint FnvPrime = 16777619;

    /// <summary>
    /// Computes the 32-bit FNV-1a hash of a string over its UTF-8 bytes.
    /// The result is identical across processes, machines and runtime versions.
    /// </summary>
    public static int Of(string value)
    {
        unchecked
        {
            uint hash = FnvOffsetBasis;
            foreach (byte b in Encoding.UTF8.GetBytes(value ?? string.Empty))
            {
                hash ^= b;
                hash *= FnvPrime;
            }
            return (int)hash;
        }
    }
}
