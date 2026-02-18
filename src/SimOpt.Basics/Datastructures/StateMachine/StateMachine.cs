using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimOpt.Basics.Datastructures.StateMachine
{
    /// <summary>
    /// A simple state machine
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <remarks>beta</remarks>
    [Serializable]
    public class StateMachine<T> : IStateMachine<T>
        where T : IState, new()
    {
        #region cvar

        private StateDictionary<T> stateDictionary;

        protected internal Dictionary<int, T> statesByEnum;
        protected internal Dictionary<string, T> statesByName;

        protected T currentState;
        protected T previousState;

        #endregion
        #region prop

        /// <summary>
        /// states lookup by id or name
        /// </summary>
        public StateDictionary<T> State { get { return stateDictionary; } }

        /// <summary>
        /// the currently active state
        /// </summary>
        public T CurrentState
        {
            get { return currentState; }
        }

        /// <summary>
        /// the previously active state
        /// </summary>
        public T PreviousState
        {
            get { return previousState; }
        }

        #endregion
        #region ctor

        /// <summary>
        /// creates a state machine
        /// </summary>
        public StateMachine()
        {
            statesByEnum = new Dictionary<int, T>();
            statesByName = new Dictionary<string, T>();
            stateDictionary = new StateDictionary<T>(this);
        }

        #endregion
        #region impl

        #region add states

        /// <summary>
        /// Add a new state to this state machine.
        /// Caution: if not previously defined otherwise, 
        /// no transitions to or from this state will be allowed.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public bool AddState(T state)
        {
            if (statesByEnum.ContainsKey(state.ID) || statesByName.ContainsKey(state.Name)) return false;
            statesByEnum[state.ID] = state;
            statesByName[state.Name] = state;
            return true;
        }

        /// <summary>
        /// Add a new state to this state machine.
        /// Caution: by default no transitions to or from
        /// this state will be allowed.
        /// </summary>
        /// <param name="stateEnum"></param>
        /// <param name="name"></param>
        public bool AddState(int id)
        {
            return AddState(id, "State " + id.ToString());
        }

        /// <summary>
        /// Add a new state to this state machine.
        /// Caution: by default no transitions to or from
        /// this state will be allowed.
        /// </summary>
        /// <param name="stateEnum"></param>
        /// <param name="name"></param>
        public bool AddState(int id, string name)
        {
            if (statesByEnum.ContainsKey(id) || statesByName.ContainsKey(name)) return false;
            T state = new T();
            state.ID = id;
            state.Name = name;
            statesByEnum[state.ID] = state;
            statesByName[state.Name] = state;
            return true;
        }

        /// <summary>
        /// Add a new state to this state machine.
        /// CAUTION: this will throw an exception if a state 
        /// with the given name already exists within this instance.
        /// CAUTION: An id will be generated automatically 
        /// (autoincrementing from zero) and returned.
        /// CAUTION: by default no transitions to or 
        /// from this state will be allowed.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public int AddState(string name)
        {
            if (statesByName.ContainsKey(name)) throw new ArgumentException("A state with the name \"" + name + "\" already exists in this state machine.", "name");
            int id = 0;
            while (statesByEnum.ContainsKey(id)) id += 1;
            AddState(id, name);
            return id;
        }

        #endregion
        #region switch state

        /// <summary>
        /// Causes an immediate transition if allowed
        /// </summary>
        /// <param name="toState"></param>
        /// <returns></returns>
        public bool SwitchState(T toState)
        {
            if (currentState.TransitionAllowed(toState))
            {
                previousState = currentState;
                currentState = toState;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Causes an immediate transition if allowed
        /// </summary>
        /// <param name="toState"></param>
        /// <returns></returns>
        public bool SwitchState(int toState)
        {
            return SwitchState(statesByEnum[toState]);
        }

        /// <summary>
        /// Causes an immediate transition if allowed
        /// </summary>
        /// <param name="toState"></param>
        /// <returns></returns>
        public bool SwitchState(string toState)
        {
            return SwitchState(statesByName[toState]);
        }

        #endregion
        #region add transition

        /// <summary>
        /// allows transitions from every state to every other state.
        /// </summary>
        public void AllowAllTransitions() 
        {
            foreach (T state in statesByEnum.Values) 
            {
                foreach (T other in statesByEnum.Values)
                {
                    if (!state.Equals(other)) AddTransition(state, other);
                }
            }
        }

        /// <summary>
        /// add a legal transition
        /// </summary>
        /// <param name="fromState"></param>
        /// <param name="toState"></param>
        /// <returns>false if the transition already existed</returns>
        public bool AddTransition(T fromState, T toState)
        {
            return fromState.AllowTransition(toState);
        }

        /// <summary>
        /// add a legal transition
        /// </summary>
        /// <param name="fromState"></param>
        /// <param name="toState"></param>
        /// <returns>false if the transition already existed</returns>
        public bool AddTransition(int fromState, int toState)
        {
            return AddTransition(statesByEnum[fromState], statesByEnum[toState]);
        }

        /// <summary>
        /// add an allowed transition
        /// </summary>
        /// <param name="fromState"></param>
        /// <param name="toState"></param>
        /// <returns>false if the transition already existed</returns>
        public bool AddTransition(string fromState, string toState)
        {
            return AddTransition(statesByName[fromState], statesByName[toState]);
        }

        #endregion
        #region remove transition

        /// <summary>
        /// Disallows all currently allowed transitions.
        /// </summary>
        public void ForbidAllTransitions()
        {
            foreach (T state in statesByEnum.Values)
            {
                foreach (T other in statesByEnum.Values)
                {
                    RemoveTransition(state, other);
                }
            }
        }

        /// <summary>
        /// remove a transition
        /// </summary>
        /// <param name="fromState"></param>
        /// <param name="toState"></param>
        /// <returns>false if the transition didn't exist</returns>
        public bool RemoveTransition(T fromState, T toState)
        {
            return fromState.ForbidTransition(toState);
        }

        /// <summary>
        /// remove a transition
        /// </summary>
        /// <param name="fromState"></param>
        /// <param name="toState"></param>
        /// <returns>false if the transition didn't exist</returns>
        public bool RemoveTransition(int fromState, int toState)
        {
            return RemoveTransition(statesByEnum[fromState], statesByEnum[toState]);
        }

        /// <summary>
        /// remove a transition
        /// </summary>
        /// <param name="fromState"></param>
        /// <param name="toState"></param>
        /// <returns>false if the transition didn't exist</returns>
        public bool RemoveTransition(string fromState, string toState)
        {
            return RemoveTransition(statesByName[fromState], statesByName[toState]);
        }

        #endregion
        
        #endregion
    }
}
