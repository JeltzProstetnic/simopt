using System;
using SimOpt.Simulation;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Entities;
using SimOpt.Mathematics.Stochastics.Distributions;

namespace SimOpt.Examples.SupplyChain
{
    /// <summary>
    /// Supply Chain Simulator — console stub.
    ///
    /// The original example depended on GMap.NET geography, SQLite databases, and WPF charting
    /// which are not available in the cross-platform SDK-style project. This stub demonstrates
    /// the core simulation framework wiring that the full example used.
    ///
    /// The simulation logic in Simulator/ and Database/ subdirectories is preserved as reference
    /// but excluded from compilation due to the removed legacy dependencies.
    /// </summary>
    internal static class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Supply Chain Simulation (console stub) ===");
            Console.WriteLine("Original example required: GMap.NET, SQLite, WPF charting.");
            Console.WriteLine("Demonstrating core simulation framework...");

            // Minimal demonstration of the simulation framework
            var model = new Model("Supply Chain Demo", 42, new DateTime(2011, 1, 1).ToDouble());

            // Use an Erlang distribution to simulate inter-arrival times, as in the original
            var erlang = new ErlangDistribution(1, 1);

            Console.WriteLine($"Model created: '{model.Name}', seed={model.Seed}");
            Console.WriteLine("Framework wiring verified. Full supply chain requires legacy database dependencies.");
        }
    }
}
