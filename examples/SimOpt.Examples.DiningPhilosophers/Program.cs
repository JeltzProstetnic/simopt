using System;
using SimOpt.Logging;
using SimOpt.Logging.Loggers;

namespace SimOpt.Examples.DiningPhilosophers
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            Logger.Add(new ConsoleLogger());
            Logger.Log("SimOpt.Examples.DiningPhilosophers", "Starting Dining Philosophers simulation...");
            Logger.Dispatch();

            var sim = new Model.Simulation(logEvents: true);
            sim.Run();

            Console.WriteLine("Simulation complete.");

            Logger.Log("Shutting down.");
            Logger.Shutdown(false);
        }
    }
}
