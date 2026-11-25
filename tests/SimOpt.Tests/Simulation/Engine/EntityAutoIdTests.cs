using System;
using System.Linq;
using System.Threading;
using FluentAssertions;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Entities;
using Xunit;

namespace SimOpt.Tests.Simulation.Engine;

/// <summary>
/// SIM-133: an entity built without an id is named "Entity n" from a static counter, and the model
/// refuses a second entity with the same id. The counter was read for the id and then bumped with a
/// plain <c>+=</c>, so models built on parallel threads (xUnit runs test classes in parallel; an MCP
/// server can take concurrent create_model calls) lost updates, the counter stepped back, and a model
/// was handed an id it already held: "An item with the same key has already been added. Key: Entity
/// 153412" (LazySeedGeneratorTests, 1 run in 16 of the full suite, 2026-11-25).
/// </summary>
public class EntityAutoIdTests
{
    [Fact]
    public void ModelsBuiltOnParallelThreads_NeverGetADuplicateAutoId()
    {
        const int threads = 8, perThread = 20_000;
        var errors = new Exception?[threads];
        var counts = new int[threads];
        using var start = new ManualResetEventSlim(false);
        var workers = Enumerable.Range(0, threads).Select(t => new Thread(() =>
        {
            var model = new Model("m" + t, 1, 0d);
            start.Wait();
            try
            {
                for (int i = 0; i < perThread; i++) _ = new SimpleEntity(model);
                counts[t] = model.Items.Count;
            }
            catch (Exception e) { errors[t] = e; }
        })).ToList();
        workers.ForEach(w => w.Start());

        start.Set();
        workers.ForEach(w => w.Join());

        errors.Should().OnlyContain(e => e == null);
        counts.Should().OnlyContain(c => c >= perThread);
    }
}
