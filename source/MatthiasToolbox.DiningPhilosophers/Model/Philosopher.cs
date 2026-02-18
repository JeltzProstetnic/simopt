using System;
using System.Collections.Generic;
using MatthiasToolbox.Basics.Datastructures.StateMachine;
using MatthiasToolbox.Mathematics.Stochastics.Distributions;
using MatthiasToolbox.Simulation;
using MatthiasToolbox.Simulation.Engine;
using MatthiasToolbox.Simulation.Entities;
using MatthiasToolbox.Simulation.Events;

namespace MatthiasToolbox.DiningPhilosophers.Model
{
    /// <summary>
    /// The philosopher class is a state machine entity with the states Undefined, Meditating, Hungry, Eating and Stuffed. It carries two 
    /// pseudo random number generators (PRNGs) to govern the transitions from Meditating to Hungry and from Eating to Stuffed. Concurrent 
    /// access is managed simply by checking the state of the philosopher to the right, who has precedence to the one to the left. 
    /// 
    /// Due to the way resource management is implemented, the experiment would usually never result in a deadlock. But by adding a delay 
    /// to the process of picking up a chopstick, a deadlock becomes possible. This was done on purpose to demonstrate the capability of the 
    /// framework in managing such deadlocks. The deadlock is easily discovered by checking the other four philosophers whenever a philosopher
    /// gets access to  his left chopstick. This will cause the philosopher to stop the simulation, but it would easily be possible to implement 
    /// some deadlock resolution instead, so that the simulation can continue.
    /// </summary>
    public class Philosopher : StateMachineEntity
    {
        #region over

        /// <summary>
        /// Reset to initial state.
        /// </summary>
        public override void OnReset()
        {
            base.OnReset();
            HungryScheduledTime = default(DateTime);
            FullScheduledTime = default(DateTime);
            LeftChopStickReady = false;
            RightChopStickReady = false;
            ScheduleEmptyStomach();
            // TODO: check if resources are released correctly
        }

        /// <summary>
        /// React to state changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="transition"></param>
        public override void OnStateTransition(StateMachineEntity sender, Transition transition)
        {
            base.OnStateTransition(sender, transition);
            if (Model.LoggingEnabled)
            {
                this.Log<SIM_INFO>(transition.SourceState.Name + " -> " + transition.TargetState.Name, Model);
                Logger.Dispatch();
            }
            switch ((StatesEnum)transition.TargetState.ID)
            {
                case StatesEnum.Hungry:
                    ResourceManager.Seize<Chopstick>(this, LeftChopstickReceived, CheckLeftChopstick);
                    break;
                case StatesEnum.Stuffed:
                    leftChopstick.Release();
                    rightChopstick.Release();
                    LeftChopStickReady = false;
                    RightChopStickReady = false;
                    StateMachine.SwitchState((int)StatesEnum.Meditating); // no time needed to lay down the chopsticks -> immediate transition
                    ScheduleEmptyStomach();
                    break;
            }
        }

        #endregion
        #region enum

        /// <summary>
        /// enumeration for the possible states
        /// </summary>
        public enum StatesEnum
        {
            /// <summary>
            /// state unknown
            /// </summary>
            Undefined, 

            /// <summary>
            /// meditating
            /// </summary>
            Meditating, 

            /// <summary>
            /// hungry
            /// </summary>
            Hungry, 

            /// <summary>
            /// currently eating
            /// </summary>
            Eating, 

            /// <summary>
            /// finished eating
            /// </summary>
            Stuffed
        }

        #endregion
        #region cvar

        private int index;
        
        private Random<int> rndHungry;
        private Random<int> rndStuffed;

        private Chopstick leftChopstick;
        private Chopstick rightChopstick;

        private UnaryEvent<Philosopher> LeftChopStickPickedUpEvent;
        private TimeSpan chopStickTakeUpTime = new TimeSpan(2, 0, 0); // 2 hours

        #endregion
        #region prop

        public int Index
        {
            get { return index; }
        }

        /// <summary>
        /// returns the current state of the philosopher
        /// </summary>
        public StatesEnum State
        {
            get
            {
                return (StatesEnum)StateMachine.CurrentState.ID;
            }
        }

        public DateTime HungryScheduledTime { get; set; }

        public DateTime FullScheduledTime { get; set; }

        public bool LeftChopStickReady { get; set; }

        public bool RightChopStickReady { get; set; }

        #endregion
        #region ctor

        /// <summary>
        /// creates a philosopher with the initial state "Meditating"
        /// </summary>
        /// <param name="model"></param>
        /// <param name="index"></param>
        public Philosopher(IModel model, int index) : this(model, "Meditating", index) { }

        /// <summary>
        /// creates a philosopher with a given initial state
        /// </summary>
        /// <param name="model"></param>
        /// <param name="initialState"></param>
        /// <param name="index"></param>
        public Philosopher(IModel model, string initialState, int index)
             : base(model, index.ToString(), "Philosopher " + index.ToString(),
                    new List<String> { "Undefined", "Meditating", "Hungry", "Eating", "Stuffed" }, null, 
                    initialState)
        {
            rndHungry = new Random<int>(this, new UniformIntegerDistribution(1, 10));
            rndStuffed = new Random<int>(this, new UniformIntegerDistribution(1, 10));
             
            this.index = index;

            // allow transition from "Undefined" to any other state
            StateMachine.AddTransition(0, 1);
            StateMachine.AddTransition(0, 2);
            StateMachine.AddTransition(0, 3);
            StateMachine.AddTransition(0, 4);

            StateMachine.AddTransition(1, 2); // meditating -> hungry
            StateMachine.AddTransition(2, 3); // hungry -> eating
            StateMachine.AddTransition(3, 4); // eating -> stuffed
            StateMachine.AddTransition(4, 1); // stuffed -> meditating

            ScheduleEmptyStomach(); // order sensitive: the above transitions have to be allowed first

            StateMachine.TransitionEvent.Log = true;

            LeftChopStickPickedUpEvent = new UnaryEvent<Philosopher>(this.EntityName + ".LeftChopStickPickedUp");
            LeftChopStickPickedUpEvent.AddHandler(InternalLeftChopStickPickedUpHandler);
        }

        #endregion
        #region impl

        private void ScheduleEmptyStomach()
        {
            HungryScheduledTime = Model.CurrentTime.ToDateTime().AddHours(rndHungry.Next());
            StateMachine.ScheduleTransition(HungryScheduledTime.ToDouble(), (int)StatesEnum.Hungry);
        }

        private void ScheduleFullStomach()
        {
            FullScheduledTime = Model.CurrentTime.ToDateTime().AddHours(rndStuffed.Next());
            StateMachine.ScheduleTransition(FullScheduledTime.ToDouble(), (int)StatesEnum.Stuffed);
        }

        private bool LeftChopstickReceived(Chopstick leftChopstick)
        {
            LeftChopStickReady = true;
            this.leftChopstick = leftChopstick;
            if (Model.LoggingEnabled) this.Log<SIM_INFO>("Left chopstick received. (id " + leftChopstick.Index.ToString() + ")", Model);

            //			// this would immediately return the other chopstick, not leaving the time
            //			// for the other neighbour to pick up his left stick, and so avoid any deadlock
            //			// by giving philosopher 1 priority over philosopher 2 and so on:
            //			// #####################
            // Model.Seize(typeof(Chopstick), this, RightChopstickReceived, CheckRightChopstick);
            //			// #####################
            //			// instead we schedule a delay event (interpret it as the time needed to take
            //			// up the first chopstick):

            int i = 0;
            foreach (Philosopher p in Model.FindEntities<Philosopher>())
            {
                if (p.LeftChopStickReady && !p.RightChopStickReady)
                    i += 1;
            }
            if (i == 5)
            {
                if (Model.LoggingEnabled) this.Log<SIM_WARNING>("DEADLOCK", Model);
                Model.Stop();
            }

            Model.AddEvent(chopStickTakeUpTime.ToDouble(), LeftChopStickPickedUpEvent.GetInstance(this));
            return true;
        }

        private void InternalLeftChopStickPickedUpHandler(Philosopher sender)
        {
            ResourceManager.Seize<Chopstick>(this, RightChopstickReceived, CheckRightChopstick);
        }

        private bool CheckLeftChopstick(Chopstick chopstick)
        {
            if (chopstick.Index == Index)
            {
                // it is the chopstick to the philosopher's left
                return true;
            }
            else
            {
                // the chopstick is not to the philosopher's left
                return false;
            }
        }

        private bool RightChopstickReceived(Chopstick rightChopstick)
        {
            RightChopStickReady = true;
            this.rightChopstick = rightChopstick;
            if (Model.LoggingEnabled) this.Log<SIM_INFO>("Right chopstick received. (id " + rightChopstick.Index.ToString() + ")", Model);
            // it takes no time to pick up the right chopstick:
            StateMachine.SwitchState((int)StatesEnum.Eating);
            ScheduleFullStomach();
            return true;
        }

        private bool CheckRightChopstick(Chopstick chopstick)
        {
            int rightIndex = (Index + 1) % 5;
            if (chopstick.Index == rightIndex)
            {
                 // it is the chopstick to the philosopher's right

                 // the following code wouldn't work because we don't know when his
                 // state transitions happen (we have used instantaneous transition, remember?):
                 // if(p.State == StatesEnum.Hungry) return false;

                 // do not take the chopstick, if the philosopher to my right waits for it.

                Philosopher p = Model.GetEntity<Philosopher>(rightIndex.ToString());
                if (p.HungryScheduledTime == Model.CurrentTime.ToDateTime())
                {
                    if (Model.LoggingEnabled) this.Log<SIM_INFO>("Cannot pick up right chopstick because " + p.EntityName + " needs it.", Model);
                    return false;
                }

                 return true;
            }
            else
            {
                 // the chopstick is not to the philosopher's right
                 return false;
            }
        }

        #endregion
    }
}