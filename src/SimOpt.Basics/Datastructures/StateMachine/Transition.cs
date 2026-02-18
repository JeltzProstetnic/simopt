using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimOpt.Basics.Datastructures.StateMachine
{
    [Serializable]
    public class Transition : Transition<State>
    {
        public Transition(State fromState, State toState) : base(fromState, toState) { }
    }

    public class Transition<T> where T : IState
    {
        public readonly T SourceState;
        public readonly T TargetState;
        public Transition(T fromState, T toState)
        {
            SourceState = fromState;
            TargetState = toState;
        }
    }
}
