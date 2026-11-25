// SIM-126: a car wash written as processes. Compares two and three bays over 20 replicated days and
// prints every statistic with its 95 % confidence interval.
//   dotnet run --project examples/SimOpt.Examples.CarWash
using System;
using System.Collections.Generic;
using SimOpt.Simulation.Statistics;

namespace SimOpt.Examples.CarWash
{
    internal static class Program
    {
        private static readonly Dictionary<string, string> Labels = new Dictionary<string, string>
        {
            [CarWash.GaveUp] = "drivers who gave up (share)",
            [CarWash.WaitForBay] = "wait for a bay (minutes)",
            [CarWash.BayUtilization] = "bays busy (share of time)",
            [CarWash.SoapEmpty] = "soap tank empty (share of time)",
        };

        private static void Main()
        {
            foreach (int bays in new[] { 2, 3 })
            {
                ReplicationResult day = CarWash.Run(new CarWash.Settings { Bays = bays }, replications: 20, baseSeed: 2026);
                Console.WriteLine($"{bays} bays, {day.Replications} days of {day.RunLength:F0} minutes, mean and 95 % half-width:");
                foreach (MetricSummary m in day.Metrics)
                    Console.WriteLine($"  {Labels[m.StatisticId],-34} {m.Mean,7:F3} ± {m.HalfWidth ?? double.NaN:F3}");
            }
        }
    }
}
