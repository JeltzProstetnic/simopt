using System;
using System.Collections.Generic;
using SimOpt.Simulation;
using SimOpt.Simulation.Engine;

namespace SimOpt.Examples.DiningPhilosophers.Model
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
        public SimOpt.Simulation.Engine.Model Model { get; private set; }

        #endregion
        #region ctor

        /// <summary>
        /// Creates an instance of the dining philosophers problem with five philosophers,
        /// five chopsticks and the fixed seed 123. The default resource manager is also initialized here.
        /// </summary>
        /// <param name="logEvents"></param>
        public Simulation(bool logEvents = true)
        {
            philosophers = new List<Philosopher>();
            chopsticks = new List<Chopstick>();

            Model = new SimOpt.Simulation.Engine.Model("Dining Philosophers", 123, DateTime.Now.ToDouble());
            Model.LogEvents = logEvents;
            Model.LogStart = false;
            Model.LogFinish = false;

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
