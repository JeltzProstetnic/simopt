using Xunit;

namespace SimOpt.Tests.Helpers;

/// <summary>
/// SIM-96: a test that costs minutes rather than seconds and runs only in the nightly gate
/// (<c>.github/workflows/nightly.yml</c>), or wherever <c>SIMOPT_NIGHTLY=1</c> is set; elsewhere it is
/// reported as skipped, so the per-commit suite keeps its ten seconds. Pair it with
/// <c>[Trait("Category", "Nightly")]</c>, which the nightly workflow filters on.
/// </summary>
public sealed class NightlyFactAttribute : FactAttribute
{
    public const string Variable = "SIMOPT_NIGHTLY";

    public NightlyFactAttribute()
    {
        if (System.Environment.GetEnvironmentVariable(Variable) != "1")
            Skip = $"nightly gate only: set {Variable}=1";
    }
}
