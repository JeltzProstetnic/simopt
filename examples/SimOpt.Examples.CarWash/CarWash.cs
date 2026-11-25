// SIM-126: SimPy's classic car wash, written as SimOpt processes and extended so it uses every process
// feature: a wash bay pool (Resource), drivers who give up when their patience runs out (AnyOf, which
// withdraws the lost request), a soap tank each wash draws from (Stock), a supplier who reacts when the
// tank runs low (Wait.Next on the tank's LevelChangedEvent, with a filter), named random streams per
// process, and replications with confidence intervals through ReplicationRunner.
using System.Collections.Generic;
using SimOpt.Mathematics.Stochastics.Distributions;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Processes;
using SimOpt.Simulation.Statistics;
using SimOpt.Simulation.Templates;

namespace SimOpt.Examples.CarWash
{
    public static class CarWash
    {
        /// <summary>The figures of a car wash; times in minutes.</summary>
        public sealed class Settings
        {
            public int Bays { get; init; } = 2;
            public double MeanGap { get; init; } = 3;           // between arriving cars, exponential
            public double WashMin { get; init; } = 4;
            public double WashMax { get; init; } = 6;
            public double PatienceMin { get; init; } = 5;       // how long a driver waits for a bay
            public double PatienceMax { get; init; } = 20;
            public double TankSize { get; init; } = 40;         // soap, one unit per wash
            public double RefillAt { get; init; } = 5;
            public double DeliveryMin { get; init; } = 30;
            public double DeliveryMax { get; init; } = 60;
            public double OpeningHours { get; init; } = 600;    // one day
        }

        // the statistics' ids in the ReplicationResult
        public const string GaveUp = "gave-up";
        public const string WaitForBay = "wait-for-bay";
        public const string BayUtilization = "bays.stat." + Instrumentation.Utilization;
        public const string SoapEmpty = "soap.stat." + Instrumentation.StockEmpty;

        /// <summary>Runs <paramref name="replications"/> days, each starting empty with a full tank.</summary>
        public static ReplicationResult Run(Settings s, int replications, int baseSeed)
            => new ReplicationRunner(Build(s), runLength: s.OpeningHours).Run(replications, baseSeed);

        public static Model Build(Settings s)
        {
            var model = new Model("car wash", 1, 0d);
            var bays = new Resource(model, capacity: s.Bays, id: "bays", name: "bays");
            var soap = new Stock(model, initialLevel: s.TankSize, capacity: s.TankSize, id: "soap", name: "soap");
            var gaveUp = new TallyCollector(model, "share of drivers who gave up", id: GaveUp, name: GaveUp);
            var waitForBay = new TallyCollector(model, "wait for a bay (min)", id: WaitForBay, name: WaitForBay);
            Instrumentation.ObserveResourceUtilization(bays);
            Instrumentation.ObserveStockEmpty(soap);

            IEnumerable<Wait> Car(Process car, double patience, double washTime)
            {
                double arrived = car.Now;
                Wait bay = bays.Request();
                var first = Wait.AnyOf(bay, car.Timeout(patience));
                yield return first;
                if (first.Winner != bay)
                {
                    gaveUp.Observe(1);           // the lost request has left the queue
                    yield break;
                }
                gaveUp.Observe(0);
                waitForBay.Observe(car.Now - arrived);
                yield return soap.TakeWait(1);   // waits in the bay while the tank is empty
                yield return car.Timeout(washTime);
                bays.Release();
            }

            IEnumerable<Wait> Arrivals(Process p)
            {
                // drawn here, not in each car: a stream per car would cost two generators per car
                Random<double> gap = p.Random("gap", Exponential(1d / s.MeanGap));
                Random<double> patience = p.Random("patience", Uniform(s.PatienceMin, s.PatienceMax));
                Random<double> wash = p.Random("wash", Uniform(s.WashMin, s.WashMax));
                while (true)
                {
                    yield return p.Timeout(gap.Next());
                    double myPatience = patience.Next(), myWash = wash.Next();
                    p.Model.StartProcess(car => Car(car, myPatience, myWash));
                }
            }

            IEnumerable<Wait> Supplier(Process p)
            {
                Random<double> delivery = p.Random("delivery", Uniform(s.DeliveryMin, s.DeliveryMax));
                while (true)
                {
                    if (soap.Level > s.RefillAt)
                        yield return Wait.Next(soap.LevelChangedEvent, (_, level) => level <= s.RefillAt);
                    yield return p.Timeout(delivery.Next());
                    yield return soap.PutWait(s.TankSize - soap.Level);
                }
            }

            model.StartProcess(Arrivals, id: "arrivals");
            model.StartProcess(Supplier, id: "supplier");
            return model;
        }

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
    }
}
