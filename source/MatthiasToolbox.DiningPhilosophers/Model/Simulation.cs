using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Simulation;
using MatthiasToolbox.Simulation.Engine;
using System.Windows.Controls;
using MatthiasToolbox.Logging.Loggers;

namespace MatthiasToolbox.DiningPhilosophers.Model
{
    /// <summary>
    /// This class manages the simulation model for the Dining Philosophers Problem.
    /// It carries a reference to the model instance and has a list of philosophers and chopsticks, 
    /// which will be populated (five instances each) on instantiation. Furthermore it will initialize 
    /// the default resource manager of the default model class.
    /// </summary>
    public class Simulation
    {
        #region cvar

        private List<Philosopher> philosophers;
        private List<Chopstick> chopsticks;

        #endregion
        #region prop

        /// <summary>
        /// The model instance for this simulation experiment.
        /// </summary>
        public MatthiasToolbox.Simulation.Engine.Model Model { get; private set; }

        #endregion
        #region ctor

        /// <summary>
        /// Creates an instance of the dining philosophers problem with five philosophers, 
        /// five chopsticks and the fixed seed 123. The default resource manager is also initialized here.
        /// </summary>
        /// <param name="log"></param>
        /// <param name="logEvents"></param>
        public Simulation(RichTextBox log = null, bool logEvents = true)
        {
            bool logging = log != null;
            if (logging) Simulator.RegisterSimulationLogger(new WPFRichTextBoxLogger(log));

            philosophers = new List<Philosopher>();
            chopsticks = new List<Chopstick>();

            Model = new MatthiasToolbox.Simulation.Engine.Model("Dining Philosophers", 123, DateTime.Now.ToDouble());
            Model.LogEvents = logEvents;
            Model.LogStart = logging;
            Model.LogFinish = logging;

            for (int i = 0; i < 5; i += 1)
            {
                philosophers.Add(new Philosopher(Model, i));
                chopsticks.Add(new Chopstick(Model, i));
            }

            // per default the default resource manager is not initialized.
            // if we do it now, the created chopsticks will be seen as initial
            // resources and will be managed also after a reset.
            Model.DefaultResourceManager.Initialize(); 
        }

        #endregion
        #region impl

        /// <summary>
        /// Execute the simulation experiment.
        /// </summary>
        public void Run()
        {
            Model.Run();
        }

        #endregion
    }
}