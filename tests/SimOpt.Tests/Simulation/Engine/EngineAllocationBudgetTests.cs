using System;
using FluentAssertions;
using SimOpt.Benchmarks;
using Xunit;
using Xunit.Abstractions;

namespace SimOpt.Tests.Simulation.Engine;

/// <summary>
/// SIM-129's target per event, as far as a test can hold it. Time per event depends on the machine
/// and its load, so it is measured by the harness (<c>benchmarks/SimOpt.Benchmarks</c>) and recorded in
/// <c>docs/plans/2026-11-25-sim-129-speed-per-event.md</c>; bytes allocated per event do not, so they
/// are held here. Before SIM-129 these cases allocated 940 to 1,720 bytes per event.
/// </summary>
public class EngineAllocationBudgetTests
{
    private readonly ITestOutputHelper output;

    public EngineAllocationBudgetTests(ITestOutputHelper output) => this.output = output;

    [Theory]
    [InlineData("mm1", 260)]
    [InlineData("mmc", 260)]
    [InlineData("jackson", 260)]
    [InlineData("mg1-breakdowns", 260)]
    [InlineData("mm1-overload", 260)]
    [InlineData("process-mm1", 520)]
    [InlineData("process-stock", 480)]
    [InlineData("process-node-mmc", 440)]   // SIM-126 schema slice 6: 392 measured when added
    public void BytesAllocatedPerEvent_StayWithinBudget(string name, int budget)
    {
        BenchmarkCases.Run(name, 0.05, 2, 1);   // first-call caches

        long before = GC.GetAllocatedBytesForCurrentThread();
        CaseOutcome outcome = BenchmarkCases.Run(name, 0.1, 3, 20_260_826);
        double perEvent = (double)(GC.GetAllocatedBytesForCurrentThread() - before) / outcome.Events;

        output.WriteLine($"{name}: {perEvent:F0} bytes per event over {outcome.Events} events (budget {budget})");
        perEvent.Should().BeLessThanOrEqualTo(budget);
    }
}
