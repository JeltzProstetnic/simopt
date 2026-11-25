// SIM-129: the models the speed harness times and EngineFingerprintTests pins. The first five are the
// analytic battery's systems (tests/.../AnalyticBatteryTests.cs), built the same way; mg1-breakdowns is
// the SIM-122 case; the two process cases exercise the process layer (SIM-126), the stock (SIM-127)
// and events at the current instant (SIM-128); mm1-overload has a queue that grows all run. Each case
// hashes every number it computes, so a change that moves one result in any replication changes its
// fingerprint.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using SimOpt.Basics.Utilities;
using SimOpt.Mathematics.Stochastics.Distributions;
using SimOpt.Mathematics.Stochastics.Interfaces;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Enum;
using SimOpt.Simulation.Processes;
using SimOpt.Simulation.Statistics;
using SimOpt.Simulation.Templates;
using SimRandom = SimOpt.Simulation.Engine.Random<double>;

namespace SimOpt.Benchmarks
{
    /// <summary>What one case produced: the events it ran and a hash of every result it computed.</summary>
    public sealed record CaseOutcome(string Name, int Replications, long Events, string Fingerprint);

    public static class BenchmarkCases
    {
        public static readonly IReadOnlyList<string> Names = new[]
        {
            "mm1", "md1", "mg1u", "mmc", "jackson", "mg1-breakdowns", "process-mm1", "process-stock", "mm1-overload",
        };

        /// <summary>Runs one case.</summary>
        /// <param name="scale">Multiplies run length and warm-up; 1 is the battery's length.</param>
        public static CaseOutcome Run(string name, double scale, int replications, int baseSeed)
        {
            return name switch
            {
                "mm1" => MM1(scale, replications, baseSeed),
                "md1" => MG1(name, new ConstantDoubleDistribution(1.0, initialize: false), scale, replications, baseSeed),
                "mg1u" => MG1(name, Uniform(0.5, 1.5), scale, replications, baseSeed),
                "mmc" => MMc(scale, replications, baseSeed),
                "jackson" => Jackson(scale, replications, baseSeed),
                "mg1-breakdowns" => MG1Breakdowns(scale, replications, baseSeed),
                "process-mm1" => ProcessMM1(scale, replications, baseSeed),
                "process-stock" => ProcessStock(scale, replications, baseSeed),
                "mm1-overload" => MM1Overload(scale, replications, baseSeed),
                _ => throw new ArgumentException($"Unknown case '{name}'.", nameof(name)),
            };
        }

        // ── shared construction, as in the analytic battery ─────────────────────────

        private static NegExponentialDistribution Exponential(double rate)
        {
            var d = new NegExponentialDistribution();
            d.Configure(rate);
            return d;
        }

        private static UniformDoubleDistribution Uniform(double min, double max)
        {
            var d = new UniformDoubleDistribution();
            d.Configure(min, max);
            return d;
        }

        private static SimpleSource PoissonArrivals(Model model, double lambda, string tag)
        {
            int n = 0;
            return new SimpleSource(model, StableHash.Of(tag + ":src"), Exponential(lambda),
                () => new SimpleEntity(model, $"E{++n}", $"E{n}"), autoStartDelay: 0d, id: "src", name: "src");
        }

        private static SimpleBuffer UnboundedQueue(Model model, string id)
            => new SimpleBuffer(model, QueueRule.FIFO, id: id, name: id, maxCapacity: int.MaxValue);

        private static SimpleServer PassThroughServer(Model model, IDistribution<double> service, string id, string tag)
            => new SimpleServer(model, StableHash.Of($"{tag}:{id}"), service, id: id, name: id, createProduct: m => m[0])
               { AutoContinue = true };

        /// <summary>Runs the replications through <see cref="ReplicationRunner"/> and hashes its result.</summary>
        private static CaseOutcome Replicate(string name, Model model, double runLength, double warmup,
                                             int replications, int baseSeed)
        {
            long events = 0;
            var perReplication = new List<long>();
            model.SimulationFinished += (_, _) => { events += model.EventCounter; perReplication.Add(model.EventCounter); };

            ReplicationResult result = new ReplicationRunner(model, runLength, warmup).Run(replications, baseSeed);

            var hash = new Fingerprint();
            hash.Add(name).Add(result.Replications).Add(result.RunLength).Add(result.WarmupTime);
            foreach (int seed in result.ReplicationSeeds) hash.Add(seed);
            foreach (MetricSummary m in result.Metrics.OrderBy(m => m.StatisticId, StringComparer.Ordinal))
            {
                hash.Add(m.StatisticId).Add(m.Replications).Add(m.Mean).Add(m.StdDev).Add(m.HalfWidth ?? double.NaN);
                foreach (double x in m.ReplicateEstimates) hash.Add(x);
            }
            foreach (long e in perReplication) hash.Add(e);
            return new CaseOutcome(name, replications, events, hash.Hex());
        }

        // ── the battery's systems ─────────────────────────────────────────────────

        private static CaseOutcome MM1(double scale, int replications, int baseSeed)
        {
            var model = new Model("mm1", 1, 0d);
            SimpleSource source = PoissonArrivals(model, 0.8, "mm1");
            SimpleBuffer buffer = UnboundedQueue(model, "buf");
            SimpleServer server = PassThroughServer(model, Exponential(1.0), "srv", "mm1");
            var sink = new SimpleSink(model, id: "snk", name: "snk");
            buffer.ConnectTo(source);
            server.ConnectTo(buffer);
            buffer.ItemReceivedEvent.AddHandler((_, _) => { if (server.Idle) server.Start(); });
            sink.ConnectTo(server);
            Instrumentation.ObserveQueueLength(buffer);
            Instrumentation.ObserveWaitingTime(buffer);
            Instrumentation.ObserveUtilization(server);
            return Replicate("mm1", model, 21_000d * scale, 1_000d * scale, replications, baseSeed);
        }

        /// <summary>
        /// An overloaded M/M/1 (λ = 1.2, μ = 1): the queue grows all run, as in the bad
        /// configurations an optimiser visits, so per-item costs that depend on queue length show.
        /// </summary>
        private static CaseOutcome MM1Overload(double scale, int replications, int baseSeed)
        {
            var model = new Model("mm1-overload", 1, 0d);
            SimpleSource source = PoissonArrivals(model, 1.2, "mm1-overload");
            SimpleBuffer buffer = UnboundedQueue(model, "buf");
            SimpleServer server = PassThroughServer(model, Exponential(1.0), "srv", "mm1-overload");
            var sink = new SimpleSink(model, id: "snk", name: "snk");
            buffer.ConnectTo(source);
            server.ConnectTo(buffer);
            buffer.ItemReceivedEvent.AddHandler((_, _) => { if (server.Idle) server.Start(); });
            sink.ConnectTo(server);
            Instrumentation.ObserveQueueLength(buffer);
            Instrumentation.ObserveWaitingTime(buffer);
            Instrumentation.ObserveUtilization(server);
            return Replicate("mm1-overload", model, 20_000d * scale, 1_000d * scale, replications, baseSeed);
        }

        private static CaseOutcome MG1(string tag, IDistribution<double> service, double scale, int replications, int baseSeed)
        {
            var model = new Model(tag, 1, 0d);
            SimpleSource source = PoissonArrivals(model, 0.5, tag);
            SimpleBuffer buffer = UnboundedQueue(model, "buf");
            SimpleServer server = PassThroughServer(model, service, "srv", tag);
            var sink = new SimpleSink(model, id: "snk", name: "snk");
            buffer.ConnectTo(source);
            server.ConnectTo(buffer);
            buffer.ItemReceivedEvent.AddHandler((_, _) => { if (server.Idle) server.Start(); });
            sink.ConnectTo(server);
            Instrumentation.ObserveQueueLength(buffer);
            Instrumentation.ObserveWaitingTime(buffer);
            Instrumentation.ObserveUtilization(server);
            return Replicate(tag, model, 20_200d * scale, 200d * scale, replications, baseSeed);
        }

        private static CaseOutcome MMc(double scale, int replications, int baseSeed)
        {
            var model = new Model("mmc", 1, 0d);
            SimpleSource source = PoissonArrivals(model, 2.4, "mmc");
            SimpleBuffer buffer = UnboundedQueue(model, "buf");
            var sink = new SimpleSink(model, id: "snk", name: "snk");
            buffer.ConnectTo(source);
            var pool = new List<SimpleServer>();
            for (int s = 0; s < 3; s++)
            {
                SimpleServer server = PassThroughServer(model, Exponential(1.0), $"srv{s}", "mmc");
                server.ConnectTo(buffer);
                sink.ConnectTo(server);
                pool.Add(server);
            }
            buffer.ItemReceivedEvent.AddHandler((_, _) => pool.FirstOrDefault(s => s.Idle)?.Start());
            Instrumentation.ObserveQueueLength(buffer);
            Instrumentation.ObserveWaitingTime(buffer);
            foreach (SimpleServer s in pool) Instrumentation.ObserveUtilization(s);
            return Replicate("mmc", model, 21_000d * scale, 1_000d * scale, replications, baseSeed);
        }

        private static CaseOutcome Jackson(double scale, int replications, int baseSeed)
        {
            var model = new Model("jackson", 1, 0d);
            SimpleSource source = PoissonArrivals(model, 1.0, "jk");
            SimpleBuffer buf1 = UnboundedQueue(model, "buf1");
            SimpleServer srv1 = PassThroughServer(model, Exponential(2.0), "srv1", "jk");
            SimpleBuffer buf2 = UnboundedQueue(model, "buf2");
            SimpleServer srv2 = PassThroughServer(model, Exponential(1.5), "srv2", "jk");
            var sink = new SimpleSink(model, id: "snk", name: "snk");
            buf1.ConnectTo(source);
            srv1.ConnectTo(buf1);
            buf1.ItemReceivedEvent.AddHandler((_, _) => { if (srv1.Idle) srv1.Start(); });
            buf2.ConnectTo(srv1);
            srv2.ConnectTo(buf2);
            buf2.ItemReceivedEvent.AddHandler((_, _) => { if (srv2.Idle) srv2.Start(); });
            sink.ConnectTo(srv2);
            Instrumentation.ObserveQueueLength(buf1);
            Instrumentation.ObserveWaitingTime(buf1);
            Instrumentation.ObserveUtilization(srv1);
            Instrumentation.ObserveQueueLength(buf2);
            Instrumentation.ObserveWaitingTime(buf2);
            Instrumentation.ObserveUtilization(srv2);
            return Replicate("jackson", model, 20_500d * scale, 500d * scale, replications, baseSeed);
        }

        /// <summary>
        /// The SIM-122 case: M/G/1 with operating-time breakdowns, preempt-resume. Arrivals every 60
        /// minutes on average, service 45, time to failure 480 operating minutes, repair 30; a full
        /// run is 360 days with 30 days of warm-up.
        /// </summary>
        private static CaseOutcome MG1Breakdowns(double scale, int replications, int baseSeed)
        {
            const double day = 1440d;
            var model = new Model("mg1-breakdowns", 1, 0d);
            SimpleSource source = PoissonArrivals(model, 1d / 60, "bench");
            SimpleBuffer buffer = UnboundedQueue(model, "buf");
            var server = new SimpleServer(model, StableHash.Of("bench:srv"), Exponential(1d / 45), id: "srv", name: "srv",
                createProduct: m => m[0], timeToFailure: Exponential(1d / 480), timeToRecover: Exponential(1d / 30))
            {
                AutoContinue = true,
                AutoRecover = true,
                AutoRestart = true,
                ContinueProductAfterFailure = true,
            };
            var sink = new SimpleSink(model, id: "snk", name: "snk");
            buffer.ConnectTo(source);
            server.ConnectTo(buffer);
            buffer.ItemReceivedEvent.AddHandler((_, _) => { if (server.Idle) server.Start(); });
            sink.ConnectTo(server);
            Instrumentation.ObserveQueueLength(buffer);
            Instrumentation.ObserveWaitingTime(buffer);
            Instrumentation.ObserveUtilization(server);
            return Replicate("mg1-breakdowns", model, 360 * day * scale, 30 * day * scale, replications, baseSeed);
        }

        // ── the process layer ────────────────────────────────────────────────────

        /// <summary>M/M/1 written as processes (λ = 0.8, μ = 1), as in ResourceTests.</summary>
        private static CaseOutcome ProcessMM1(double scale, int replications, int baseSeed)
        {
            var model = new Model("process-mm1", 11, 0d);
            var server = new Resource(model, id: "server", name: "server");
            var gaps = new SimRandom(model, Exponential(0.8));
            var work = new SimRandom(model, Exponential(1.0));
            var sojourn = new List<double>();
            double warmup = 1_000d * scale, length = 21_000d * scale;

            IEnumerable<Wait> Customer(Process p)
            {
                double arrived = p.Now;
                yield return server.Request();
                yield return p.Timeout(work.Next());
                server.Release();
                if (arrived >= warmup) sojourn.Add(p.Now - arrived);
            }
            IEnumerable<Wait> Arrivals(Process p)
            {
                while (true)
                {
                    yield return p.Timeout(gaps.Next());
                    p.Model.StartProcess(Customer);
                }
            }
            model.StartProcess(Arrivals, id: "arrivals");
            TimeWeightedCollector busy = Instrumentation.ObserveResourceUtilization(server, warmup);

            var hash = new Fingerprint().Add("process-mm1");
            long events = 0;
            for (int r = 0; r < replications; r++)
            {
                sojourn.Clear();
                model.Reset(ReplicationRunner.SeedFor(baseSeed, r));
                busy.WarmupTime = warmup;
                model.Run(length);
                events += model.EventCounter;
                hash.Add(model.EventCounter).Add(sojourn.Count).Add(busy.Estimate(model.CurrentTime));
                foreach (double s in sojourn) hash.Add(s);
            }
            return new CaseOutcome("process-mm1", replications, events, hash.Hex());
        }

        /// <summary>
        /// A producer fills a bounded stock, a consumer takes from it but gives up after a while
        /// (any-of with withdrawal), and a breakdown process interrupts the consumer now and then.
        /// </summary>
        private static CaseOutcome ProcessStock(double scale, int replications, int baseSeed)
        {
            var model = new Model("process-stock", 7, 0d);
            var stock = new Stock(model, initialLevel: 2, capacity: 5, id: "stock", name: "stock");
            var make = new SimRandom(model, Exponential(1.0));
            var use = new SimRandom(model, Exponential(1.1));
            var fail = new SimRandom(model, Exponential(1d / 40));
            int taken = 0, gaveUp = 0, interrupted = 0;
            double length = 20_000d * scale;
            Process consumer = null;

            IEnumerable<Wait> Producer(Process p)
            {
                while (true)
                {
                    yield return p.Timeout(make.Next());
                    yield return stock.PutWait(1);
                }
            }
            IEnumerable<Wait> Consumer(Process p)
            {
                while (true)
                {
                    Wait take = stock.TakeWait(1);
                    var any = Wait.AnyOf(take, p.Timeout(1.5));
                    yield return any;
                    if (p.Interrupted) { interrupted++; continue; }
                    if (any.Winner == take) taken++; else gaveUp++;
                    yield return p.Timeout(use.Next());
                }
            }
            IEnumerable<Wait> Breakdowns(Process p)
            {
                while (true)
                {
                    yield return p.Timeout(fail.Next());
                    consumer.Interrupt();
                }
            }
            model.StartProcess(Producer, id: "producer");
            consumer = model.StartProcess(Consumer, id: "consumer");
            model.StartProcess(Breakdowns, id: "breakdowns");
            TimeWeightedCollector level = Instrumentation.ObserveStockLevel(stock);
            TimeWeightedCollector empty = Instrumentation.ObserveStockEmpty(stock);

            var hash = new Fingerprint().Add("process-stock");
            long events = 0;
            for (int r = 0; r < replications; r++)
            {
                taken = gaveUp = interrupted = 0;
                model.Reset(ReplicationRunner.SeedFor(baseSeed, r));
                model.Run(length);
                events += model.EventCounter;
                hash.Add(model.EventCounter).Add(taken).Add(gaveUp).Add(interrupted).Add(stock.Level)
                    .Add(level.Estimate(model.CurrentTime)).Add(empty.Estimate(model.CurrentTime));
            }
            return new CaseOutcome("process-stock", replications, events, hash.Hex());
        }
    }

    /// <summary>SHA-256 over an exact record of values: doubles by their bits, so equal means byte-identical.</summary>
    public sealed class Fingerprint
    {
        private readonly StringBuilder text = new StringBuilder();

        public Fingerprint Add(string s) { text.Append('s').Append(s.Length).Append(':').Append(s).Append(';'); return this; }
        public Fingerprint Add(long x) { text.Append('i').Append(x).Append(';'); return this; }
        public Fingerprint Add(double x) { text.Append('d').Append(BitConverter.DoubleToInt64Bits(x).ToString("x16")).Append(';'); return this; }

        /// <summary>The first 16 hex digits of the SHA-256 of everything added.</summary>
        public string Hex() => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString())))[..16].ToLowerInvariant();
    }
}
