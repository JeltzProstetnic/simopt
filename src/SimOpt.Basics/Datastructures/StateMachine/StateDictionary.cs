using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimOpt.Basics.Datastructures.StateMachine
{
    [Serializable]
    public class StateDictionary<T> where T : IState, new()
    {
        private StateMachine<T> machine;

        public T this[string key] { get { return machine.statesByName[key]; } }

        public T this[int key] { get { return machine.statesByEnum[key]; } }

        public StateDictionary(StateMachine<T> machine) { this.machine = machine; }
    }
}
