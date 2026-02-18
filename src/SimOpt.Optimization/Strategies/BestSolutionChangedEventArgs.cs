using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Optimization.Interfaces;

namespace SimOpt.Optimization.Strategies
{
    /// <summary>
    /// Event arguments containing an old and a new value for a problem solution.
    /// </summary>
    public class BestSolutionChangedEventArgs : EventArgs
    {
        /// <summary>
        /// This may be null if no previous best solution was known.
        /// </summary>
        public ISolution OldValue { get; private set; }

        /// <summary>
        /// The currently best solution found by the strategy.
        /// </summary>
        public ISolution NewValue { get; private set; }

        /// <summary>
        /// default constructor
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        public BestSolutionChangedEventArgs(ISolution oldValue, ISolution newValue)
        {
            this.OldValue = oldValue;
            this.NewValue = newValue;
        }
    }
}
