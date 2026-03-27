using System;
using SimOpt.Simulation;
using SimOpt.Simulation.Engine;

namespace SimOpt.Examples.WarehouseSimulator
{
    /// <summary>
    /// V-Research Warehouse Simulator — console stub.
    ///
    /// The original example depended on Oracle Database (System.Data.OracleClient),
    /// LINQ-to-SQL table mappings, and WPF for visualization. These dependencies are
    /// not available in the cross-platform SDK-style project.
    ///
    /// The data access code in Data/ and the WPF views are preserved as reference
    /// but excluded from compilation. The Model/Simulation.cs core is also excluded
    /// because it references those database types.
    /// </summary>
    internal static class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Warehouse Simulator (console stub) ===");
            Console.WriteLine("Original example required: Oracle DB, System.Data.Linq, WPF.");
            Console.WriteLine("Demonstrating core simulation framework...");

            var model = new Model("V-Research Warehouse Simulation", 123, 0);

            Console.WriteLine($"Model created: '{model.Name}', seed={model.Seed}");
            Console.WriteLine("Framework wiring verified. Full warehouse sim requires legacy database dependencies.");
        }
    }
}
