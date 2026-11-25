using System;
using System.Collections.Generic;
using System.Threading;
using FluentAssertions;
using SimOpt.Logging;
using Xunit;

namespace SimOpt.Tests.Logging;

/// <summary>
/// SIM-134: the test host aborted about once in 15 full suite runs. Captured with --blame-crash:
/// <c>KeyNotFoundException</c> on the static logger's dispatcher thread, at <c>loggers[logger]</c>,
/// for <c>LoggerQueueTests.SilentLogger</c>. Delivery read the registered loggers' keys and then
/// looked each one up, so a logger removed in between made the lookup throw, and an exception on a
/// background thread ends the process. A logger that throws ended it the same way. Delivery now walks
/// the registered pairs, so nothing is looked up, and a logger's exception stays with that logger.
/// The synchronous path (<c>AutoDispatch</c>) shares the delivery code and fails on the caller's
/// thread instead, which is what makes the defect testable without killing the host. Switching
/// <c>AutoDispatch</c> on and off again also left the restarted dispatcher dead.
/// </summary>
[Collection(StaticLoggerCollection.Name)]
public class LoggerDeliveryTests
{
    private sealed class RemovingLogger : ILogger
    {
        public ILogger? Other { get; set; }
        public int Received;
        public bool LoggingEnabled { get; set; } = true;
        public void Log(DateTime timeStamp, object sender, Type messageClass, string message, Dictionary<string, object> data)
        {
            Received++;
            if (Other != null) Logger.Remove(Other);
        }
        public void ShutdownLogger() { }
    }

    private sealed class ThrowingLogger : ILogger
    {
        public bool LoggingEnabled { get; set; } = true;
        public void Log(DateTime timeStamp, object sender, Type messageClass, string message, Dictionary<string, object> data)
            => throw new InvalidOperationException("SIM-134: a logger that fails");
        public void ShutdownLogger() { }
    }

    private sealed class CountingLogger : ILogger
    {
        public int Received;
        public bool LoggingEnabled { get; set; } = true;
        public void Log(DateTime timeStamp, object sender, Type messageClass, string message, Dictionary<string, object> data)
            => Interlocked.Increment(ref Received);
        public void ShutdownLogger() { }
    }

    private static void Synchronously(Action body)
    {
        bool auto = Logger.AutoDispatch;
        Logger.AutoDispatch = true;
        try { body(); }
        finally { Logger.AutoDispatch = auto; }
    }

    [Fact]
    public void ALoggerRemovedDuringDelivery_IsNotLookedUp()
    {
        // Each removes the other when it logs, so whichever is delivered to first removes the second
        // before its turn, in either enumeration order.
        var a = new RemovingLogger();
        var b = new RemovingLogger();
        a.Other = b;
        b.Other = a;
        Synchronously(() =>
        {
            Logger.Add(a);
            Logger.Add(b);
            try
            {
                Action log = () => Logger.Log("SIM-134 probe");

                log.Should().NotThrow();
                (a.Received + b.Received).Should().BeGreaterThan(0);
            }
            finally
            {
                Logger.Remove(a);
                Logger.Remove(b);
            }
        });
    }

    [Fact]
    public void AThrowingLogger_StopsNeitherTheOthersNorTheCaller()
    {
        var bad = new ThrowingLogger();
        var good = new CountingLogger();
        Synchronously(() =>
        {
            Logger.Add(bad);
            Logger.Add(good);
            try
            {
                Action log = () => Logger.Log("SIM-134 probe");

                log.Should().NotThrow();
                good.Received.Should().Be(1);
            }
            finally
            {
                Logger.Remove(bad);
                Logger.Remove(good);
            }
        });
    }

    [Fact]
    public void AfterAutoDispatchWasSwitchedOnAndOff_TheDispatcherStillDelivers()
    {
        // Switching AutoDispatch on stopped the dispatcher and left its finish request set, so the
        // thread started when it was switched off again ended at its first wake-up.
        Synchronously(() => { });
        var good = new CountingLogger();
        Logger.Add(good);
        try
        {
            Logger.Log("SIM-134 probe");

            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (Volatile.Read(ref good.Received) < 1 && DateTime.UtcNow < deadline) Thread.Sleep(10);

            Volatile.Read(ref good.Received).Should().Be(1);
        }
        finally { Logger.Remove(good); }
    }

    [Fact]
    public void OnTheDispatcherThread_AThrowingLogger_DoesNotEndTheProcess()
    {
        // Before the fix this did not fail: it aborted the test host, as the suite did.
        var bad = new ThrowingLogger();
        var good = new CountingLogger();
        Logger.AutoDispatch.Should().BeFalse("the queued path is the default");
        Logger.Add(bad);
        Logger.Add(good);
        try
        {
            for (int i = 0; i < 10; i++) Logger.Log("SIM-134 probe " + i);

            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (Volatile.Read(ref good.Received) < 10 && DateTime.UtcNow < deadline) Thread.Sleep(10);

            Volatile.Read(ref good.Received).Should().Be(10);
        }
        finally
        {
            Logger.Remove(bad);
            Logger.Remove(good);
        }
    }
}
