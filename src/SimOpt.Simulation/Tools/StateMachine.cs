using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Basics.Datastructures.StateMachine;
using SimOpt.Simulation.Events;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Enum;
using SimOpt.Basics.Exceptions;
using SimOpt.Logging;

namespace SimOpt.Simulation.Tools
{
    /// <summary>
    /// A state machine for discrete simulation.
    /// TODO: implement logging
    /// </summary>
    /// <remarks>beta</remarks>
    [Serializable]
    public class StateMachine : SimOpt.Basics.Datastructures.StateMachine.StateMachine<State>, IResettable
    {
        #region cvar

        private string name;
        private bool initialized;
        private State initialState;
        private StateMachineEntity owner;
        private BinaryEvent<StateMachineEntity, Transition> transitionEvent;

        #endregion
        #region prop

        /// <summary>
        /// The name of this state machine.
        /// </summary>
        public string Name { get { return name; } }

        /// <summary>
        /// the event which fires whenever a state transition occurs
        /// </summary>
        public BinaryEvent<StateMachineEntity, Transition> TransitionEvent { get { return transitionEvent; } }

        /// <summary>
        /// the owner of this state machine
        /// </summary>
        public StateMachineEntity Owner { get { return owner; } }

        /// <summary>
        /// The initial state of this state machine.
        /// This is important for resetting.
        /// </summary>
        public State InitialState { get { return initialState; } }

        #endregion
        #region ctor

        /// <summary>
        /// creates a state machine
        /// </summary>
        /// <param name="name">
        /// Use this to be able to tell between events from 
        /// multiple state machines within one simulation object.
        /// </param>
        /// <param name="owner">
        /// the simulation object which holds this state machine
        /// </param>
        public StateMachine(StateMachineEntity owner, State initialState = null, string name = "") : base()
        {
            this.name = name;
            this.owner = owner;
            if (string.IsNullOrEmpty(name)) name = "UnnamedStateMachine";
            transitionEvent = new BinaryEvent<StateMachineEntity, Transition>(name + ".StateTransitionEvent");
            transitionEvent.AddHandler(InternalTransitionHandler, new Priority(double.MaxValue, PriorityType.LowLevelBeforeOthers));
            if (initialState != null) Initialize(initialState);
        }

        #endregion
        #region init

        /// <summary>
        /// set the initial state of this state machine.
        /// Will throw an exception if the state machine
        /// was already initialized (e.g. by the 
        /// constructor)
        /// </summary>
        /// <param name="initialState"></param>
        public void Initialize(State initialState)
        {
            if (initialized) throw new InitializationException("The state machine \"" + name + "\" was already initialized.");
            this.initialState = initialState;
            base.currentState = initialState;
            initialized = true;
        }

        /// <summary>
        /// set the initial state of this state machine.
        /// Will throw an exception if the state machine
        /// was already initialized (e.g. by the 
        /// constructor)
        /// </summary>
        /// <param name="initialStateID"></param>
        public void Initialize(int initialStateID)
        {
            Initialize(statesByEnum[initialStateID]);
        }

        /// <summary>
        /// set the initial state of this state machine.
        /// Will throw an exception if the state machine
        /// was already initialized (e.g. by the 
        /// constructor)
        /// </summary>
        /// <param name="initialStateName"></param>
        public void Initialize(string initialStateName) 
        {
            Initialize(statesByName[initialStateName]);
        }

        #endregion
        #region impl

        /// <summary>
        /// Actually switch the current state.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void InternalTransitionHandler(StateMachineEntity sender, Transition eventArgs)
        {
            base.SwitchState(eventArgs.TargetState);
        }

        #region schedule transition

        /// <summary>
        /// will check if DateTime t is in the future as required (an exception is thrown otherwise)
        /// and if the transition is allowed (will be ignored otherwise)
        /// </summary>
        /// <param name="pointInTime"></param>
        /// <param name="fromState"></param>
        /// <param name="toState"></param>
        /// <returns>The scheduled event instance or null if the transition is not allowed.</returns>
        public BinaryEventInstance<StateMachineEntity, Transition> ScheduleTransition(double pointInTime, int toState)
        {
            BinaryEventInstance<StateMachineEntity, Transition> result = null;

            if (CurrentState.TransitionAllowed(statesByEnum[toState]))
            {
                result = transitionEvent.GetInstance(owner, new Transition(CurrentState, statesByEnum[toState]));
                owner.Model.AddEventAt(pointInTime, result);
            }

            return result;
        }

        /// <summary>
        /// will check if DateTime t is in the future as required (an exception is thrown otherwise)
        /// and if the transition is allowed (will be ignored otherwise)
        /// </summary>
        /// <param name="pointInTime"></param>
        /// <param name="fromState"></param>
        /// <param name="toState"></param>
        /// <returns>The scheduled event instance or null if the transition is not allowed.</returns>
        public BinaryEventInstance<StateMachineEntity, Transition> ScheduleTransition(double pointInTime, string toState)
        {
            BinaryEventInstance<StateMachineEntity, Transition> result = null;

            if (CurrentState.TransitionAllowed(statesByName[toState]))
            {
                result = transitionEvent.GetInstance(owner, new Transition(CurrentState, statesByName[toState]));
                owner.Model.AddEventAt(pointInTime, result);
            }

            return result;
        }

        /// <summary>
        /// will check if DateTime t is in the future as required (an exception is thrown otherwise)
        /// and if the transition is allowed (will be ignored otherwise)
        /// </summary>
        /// <param name="pointInTime"></param>
        /// <param name="fromState"></param>
        /// <param name="toState"></param>
        /// <returns>The scheduled event instance or null if the transition is not allowed.</returns>
        public BinaryEventInstance<StateMachineEntity, Transition> ScheduleTransition(double pointInTime, State toState)
        {
            BinaryEventInstance<StateMachineEntity, Transition> result = null;

            if (CurrentState.TransitionAllowed(toState))
            {
                result = transitionEvent.GetInstance(owner, new Transition(CurrentState, toState));
                owner.Model.AddEventAt(pointInTime, result);
            }

            return result;
        }

        #endregion
        #region immediate transition

        /// <summary>
        /// Causes an immediate transition (if allowed).
        /// </summary>
        /// <param name="toState"></param>
        /// <param name="callHandlers">
        /// a flag indication if all state transition 
        /// handlers should be called on success.
        /// If set to true, the state will only be switched
        /// immediately before the handlers are invoked.
        /// If set to false, the state will be switched 
        /// immediately after this call.
        /// </param>
        /// <returns></returns>
        public bool SwitchState(State toState, bool callHandlers = true)
        {
            if (CurrentState.TransitionAllowed(toState))
            {
                if (callHandlers)
                {
                    // TODO: Model.ExecuteImmediateEvent(...) - owner.Model.AddImmediateEvent(transitionEvent.GetInstance(owner, new Transition(CurrentState, toState)));
                    transitionEvent.GetInstance(owner, new Transition(CurrentState, toState)).Raise();
                    return true;
                }
                else
                {
                    return base.SwitchState(toState);
                }
            }
            return false;
        }

        /// <summary>
        /// Causes an immediate transition (if allowed) and
        /// calls all handlers of the transition event.
        /// </summary>
        /// <param name="toState"></param>
        /// <returns></returns>
        public new bool SwitchState(State toState)
        {
            return SwitchState(toState, true);
        }

        /// <summary>
        /// Causes an immediate transition if allowed
        /// </summary>
        /// <param name="toState"></param>
        /// <returns></returns>
        public new bool SwitchState(int toState)
        {
            return SwitchState(statesByEnum[toState]);
        }

        /// <summary>
        /// Causes an immediate transition if allowed
        /// </summary>
        /// <param name="toState"></param>
        /// <returns></returns>
        public new bool SwitchState(string toState)
        {
            return SwitchState(statesByName[toState]);
        }

        #endregion

        #endregion
        #region rset

        /// <summary>
        /// reset this state machine to its initial state
        /// </summary>
        public void Reset() 
        {
#if DEBUG
            if (!initialized && this.Owner.Model.LoggingEnabled) 
                this.Log<WARN>("The state machine \"" + name + "\" was reset but never initialized!");
#endif
            base.currentState = initialState;
            base.previousState = null;
        }

        #endregion
    }
}