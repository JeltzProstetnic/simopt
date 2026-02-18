using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimOpt.Optimization.Strategies.Evolutionary
{
    /// <summary>
    /// Interface for combinable classes. 
    /// </summary>
    /// <typeparam name="T">The type of the combinable class</typeparam>
    public interface ICombinable<T>
    {
        /// <summary>
        /// A combine function which creates an instance in which the 
        /// current and the other instance are somehow combined.
        /// </summary>
        /// <param name="other">The instance to be combined with the this.</param>
        /// <returns>A new instance with combined properties of this and the other one.</returns>
        T CombineWith(T other);
    }
}
