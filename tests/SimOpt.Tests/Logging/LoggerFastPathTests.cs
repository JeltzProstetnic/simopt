using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using FluentAssertions;
using SimOpt.Logging;
using SimOpt.Simulation;
using SimOpt.Simulation.Engine;
using Logger = SimOpt.Logging.Logger;
using Xunit;

namespace SimOpt.Tests.Logging;

/// <summary>
/// SIM-129: with no logger registered, a log call built a dictionary, read the wall clock, queued
/// the message and woke the dispatcher thread, only for the dispatcher to drop it; the model-aware
/// variant also built and compiled two expression trees. Templates log on their hot path (an empty
/// buffer warns on every preview), so a headless run paid for messages nobody could read. A log
/// call must now cost nothing while no logger is registered, and a registered logger must still
/// receive every message.
/// </summary>
[Collection(StaticLoggerCollection.Name)]
public class LoggerFastPathTests
{
    private sealed class CollectingLogger : ILogger
    {
        public ConcurrentQueue<string> Messages { get; } = new ConcurrentQueue<string>();
        public bool LoggingEnabled { get; set; } = true;
        public void Log(DateTime timeStamp, object sender, Type messageClass, string message, Dictionary<string, object> data)
            => Messages.Enqueue(message);
        public void ShutdownLogger() { }
    }

    private static long AllocatedBy(Action action)
    {
        action();   // JIT and first-call caches
        long before = GC.GetAllocatedBytesForCurrentThread();
        action();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    [Fact]
    public void WithNoLoggerRegistered_ALogCallAllocatesNothing()
    {
        Logger.IsActive.Should().BeFalse("no test registers a logger outside this collection");
        var sender = new object();

        long bytes = AllocatedBy(() => { for (int i = 0; i < 1000; i++) sender.Log<WARN>("probe"); });

        bytes.Should().Be(0, "a message no logger can receive must not be built");
    }

    [Fact]
    public void WithNoLoggerRegistered_AModelAwareLogCallAllocatesNothing()
    {
        var model = new Model("log-probe", 1, 0d);
        var sender = new object();

        long bytes = AllocatedBy(() => { for (int i = 0; i < 1000; i++) sender.Log<SIM_WARNING>("probe", model); });

        bytes.Should().Be(0, "the model-aware call used to build and compile two expression trees per message");
    }

    [Fact]
    public void ARegisteredLoggerReceivesMessages_AndRemovingItSilencesLoggingAgain()
    {
        var logger = new CollectingLogger();
        Logger.Add(logger);
        try
        {
            Logger.IsActive.Should().BeTrue();
            new object().Log<WARN>("SIM-129 delivered");

            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (!logger.Messages.Contains("SIM-129 delivered") && DateTime.UtcNow < deadline) Thread.Sleep(10);
            logger.Messages.Should().Contain("SIM-129 delivered");
        }
        finally
        {
            Logger.Remove(logger);
        }

        Logger.IsActive.Should().BeFalse();
        var sender = new object();
        AllocatedBy(() => sender.Log<WARN>("after removal")).Should().Be(0);
    }
}

/// <summary>Tests that read or change the static logger's registrations run one at a time.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class StaticLoggerCollection
{
    public const string Name = "static logger";
}
