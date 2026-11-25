using System;
using System.Collections;
using System.Reflection;
using System.Threading;
using FluentAssertions;
using SimOpt.Logging;
using Xunit;

namespace SimOpt.Tests.Logging;

/// <summary>
/// SIM-124, second half: the static logger's dispatcher thread dropped at most one message per
/// wake-up when no logger was registered, and the wake-up signal is an <c>AutoResetEvent</c>, so
/// many enqueues collapse into one wake-up. In a headless run, which registers no logger, the
/// message queue therefore only grew: about 90 messages per replication of the SIM-122 case, 2,635
/// after 30 replications. The dispatcher must drain the queue, delivering or dropping, until it is
/// empty, and only then wait.
/// </summary>
[Collection(StaticLoggerCollection.Name)]
public class LoggerQueueTests
{
    private static int QueueLength()
    {
        var queue = typeof(Logger).GetField("messageQueue", BindingFlags.Static | BindingFlags.NonPublic)!
            .GetValue(null) as ICollection;
        return queue!.Count;
    }

    private sealed class SilentLogger : ILogger
    {
        public bool LoggingEnabled { get; set; } = true;
        public void Log(DateTime timeStamp, object sender, Type messageClass, string message, System.Collections.Generic.Dictionary<string, object> data) { }
        public void ShutdownLogger() { }
    }

    /// <remarks>
    /// SIM-129: a log call with no logger registered no longer queues anything, so the messages
    /// the dispatcher has to drop are the ones queued while a logger was registered and still
    /// waiting when it was removed.
    /// </remarks>
    [Fact]
    public void TheMessageQueueDrains_EvenWithNoLoggerRegistered()
    {
        var logger = new SilentLogger();
        Logger.Add(logger);
        for (int i = 0; i < 2000; i++) Logger.Log("SIM-124 queue probe " + i);
        Logger.Remove(logger);

        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (QueueLength() > 0 && DateTime.UtcNow < deadline) Thread.Sleep(10);

        QueueLength().Should().Be(0, "nothing else ever removes a queued message, so a queue that does not drain grows for the life of the process");
    }
}
