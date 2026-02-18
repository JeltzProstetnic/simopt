using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Basics.Datastructures.StateMachine
{
    /// <summary>
    /// A state machine
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <remarks>beta</remarks>
    public interface IStateMachine<T> where T : IState
    {
        #region prop

        /// <summary>
        /// the currenty active state
        /// </summary>
        T CurrentState { get; }

        /// <summary>
        /// the previously active state
        /// </summary>
        T PreviousState { get; }

        #endregion
        #region impl

        #region state factory

        /// <summary>
        /// Add an existing state.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        bool AddState(T state);

        /// <summary>
        /// Add a new state.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        bool AddState(int id);

        /// <summary>
        /// Add a new state.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        bool AddState(int id, string name);

        #endregion
        #region perform transition

        /// <summary>
        /// causes an immediate transition.
        /// </summary>
        /// <param name="toState"></param>
        bool SwitchState(T toState);

        /// <summary>
        /// causes an immediate transition.
        /// </summary>
        /// <param name="toState"></param>
        bool SwitchState(int toState);

        /// <summary>
        /// causes an immediate transition.
        /// </summary>
        /// <param name="toState"></param>
        bool SwitchState(string toState);

        #endregion
        #region allow transitions

        /// <summary>
        /// allow transitions from every state to each other state
        /// </summary>
        void AllowAllTransitions();

        /// <summary>
        /// add a legal transition
        /// </summary>
        /// <param name="fromState"></param>
        /// <param name="toState"></param>
        /// <returns>false if the transition already existed</returns>
        bool AddTransition(T fromState, T toState);

        /// <summary>
        /// add a legal transition
        /// </summary>
        /// <param name="fromState"></param>
        /// <param name="toState"></param>
        /// <returns>false if the transition already existed</returns>
        bool AddTransition(int fromState, int toState);

        /// <summary>
        /// add an allowed transition
        /// </summary>
        /// <param name="fromState"></param>
        /// <param name="toState"></param>
        /// <returns>false if the transition already existed</returns>
        bool AddTransition(string fromState, string toState);

        #endregion
        #region disallow transitions

        /// <summary>
        /// Forbid all transitions.
        /// </summary>
        void ForbidAllTransitions();

        /// <summary>
        /// remove a transition
        /// </summary>
        /// <param name="fromState"></param>
        /// <param name="toState"></param>
        /// <returns>false if the transition didn't exist</returns>
        bool RemoveTransition(T fromState, T toState);

        /// <summary>
        /// remove a transition
        /// </summary>
        /// <param name="fromState"></param>
        /// <param name="toState"></param>
        /// <returns>false if the transition didn't exist</returns>
        bool RemoveTransition(int fromState, int toState);

        /// <summary>
        /// remove a transition
        /// </summary>
        /// <param name="fromState"></param>
        /// <param name="toState"></param>
        /// <returns>false if the transition didn't exist</returns>
        bool RemoveTransition(string fromState, string toState);

        #endregion

        #endregion
    }
}
