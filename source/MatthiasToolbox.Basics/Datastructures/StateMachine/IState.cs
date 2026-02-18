using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Basics.Datastructures.StateMachine
{
    /// <summary>
    /// Interface for a state in a state machine.
    /// </summary>
    /// <remarks>final</remarks>
    public interface IState : IEquatable<IState>
    {
        /// <summary>
        /// unique name of this state
        /// </summary>
        string Name { get; set; }
        
        /// <summary>
        /// unique id of this state
        /// </summary>
        int ID { get; set; }

        /// <summary>
        /// Find out if a certain transition is allowed.
        /// </summary>
        /// <param name="toState"></param>
        /// <returns>true if a transition to toState is allowed</returns>
        bool TransitionAllowed(IState toState);

        /// <summary>
        /// add target state to the allowed targets list
        /// </summary>
        /// <param name="target"></param>
        /// <returns>true if the state was new</returns>
        bool AllowTransition(IState toState);

        /// <summary>
        /// remove target state from the allowed targets list
        /// </summary>
        /// <param name="target"></param>
        /// <returns>true if the state was existent</returns>
        bool ForbidTransition(IState toState);
    }
}
