using System;

namespace SimOpt.Examples.EmergencyDepartment
{
    internal sealed class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== Emergency Department Simulation ===");

            try
            {
                Console.WriteLine("--- Task A ---");
                var sim1 = new Model.Simulation(124557, 1);
                (sim1.Model as SimOpt.Simulation.Engine.Model)!.LogStart = true;
                sim1.Model.LoggingEnabled = false;
                (sim1.Model as SimOpt.Simulation.Engine.Model)!.LogFinish = true;
                sim1.Model.Start();

                Console.WriteLine("--- Task B ---");
                var sim2 = new Model.Simulation(124557, 2);
                (sim2.Model as SimOpt.Simulation.Engine.Model)!.LogStart = true;
                sim2.Model.LoggingEnabled = false;
                (sim2.Model as SimOpt.Simulation.Engine.Model)!.LogFinish = true;
                sim2.Model.Start();

                Console.WriteLine("--- Task C ---");
                var sim3 = new Model.Simulation(124557, 3);
                (sim3.Model as SimOpt.Simulation.Engine.Model)!.LogStart = true;
                sim3.Model.LoggingEnabled = false;
                (sim3.Model as SimOpt.Simulation.Engine.Model)!.LogFinish = true;
                sim3.Model.Start();

                Console.WriteLine("Simulation complete.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
