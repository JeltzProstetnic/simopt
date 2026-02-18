using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Basics.Datastructures.StateMachine;
using SimOpt.Basics.Datastructures.Geometry;
using SimOpt.Simulation.Interfaces;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Tools;

namespace SimOpt.Simulation.Entities
{
    [Serializable]
    public class MovableEntityInitializationParams : TaskMachineEntityInitializationParams, IMovableEntityInitializationParams
    {
        public MovableEntityInitializationParams(string id = "", 
                                                      string name = "", 
                                                      int? seedID = null, 
                                                      Point initialPosition = null, 

            double vMax = 1, double acceleration = 1, double deceleration = 1, 

                                                      IResourceManager manager = null, 
                                                      IEntity currentHolder = null,
                                                      State initialState = null,
                                                      Action<Task> notifyTaskFinished = null,
                                                      Action<Task> notifyTaskStarted = null,
                                                      Action<Task> notifyTaskStartFailed = null)
            : base(id, name, seedID, initialPosition, manager, currentHolder, initialState, notifyTaskFinished, notifyTaskStarted, notifyTaskStartFailed)
        {
            this.VMax = vMax;
            this.Acceleration = acceleration;
            this.Deceleration = deceleration;
        }

        public MovableEntityInitializationParams(string id = "",
                                                      string name = "",
                                                      int? seedID = null,
                                                      Point initialPosition = null,

            double vMax = 1, double acceleration = 1, double deceleration = 1, 

                                                      IResourceManager manager = null,
                                                      IEntity currentHolder = null,
                                                      string initialState = null, 
                                                      Action<Task> notifyTaskFinished = null,
                                                      Action<Task> notifyTaskStarted = null,
                                                      Action<Task> notifyTaskStartFailed = null)
            : base(id, name, seedID, initialPosition, manager, currentHolder, initialState, notifyTaskFinished, notifyTaskStarted, notifyTaskStartFailed)
        {
            this.VMax = vMax;
            this.Acceleration = acceleration;
            this.Deceleration = deceleration;
        }

        public MovableEntityInitializationParams(string id = "",
                                                      string name = "",
                                                      int? seedID = null,
                                                      Point initialPosition = null,

            double vMax = 1, double acceleration = 1, double deceleration = 1, 

                                                      IResourceManager manager = null,
                                                      IEntity currentHolder = null,
                                                      int? initialState = null, 
                                                      Action<Task> notifyTaskFinished = null,
                                                      Action<Task> notifyTaskStarted = null,
                                                      Action<Task> notifyTaskStartFailed = null)
            : base(id, name, seedID, initialPosition, manager, currentHolder, initialState, notifyTaskFinished, notifyTaskStarted, notifyTaskStartFailed)
        {
            this.VMax = vMax;
            this.Acceleration = acceleration;
            this.Deceleration = deceleration;
        }

        #region IMovableEntityInitializationParams

        public double VMax { get; set; }

        public double Acceleration { get; set; }

        public double Deceleration { get; set; }

        #endregion
    }
}