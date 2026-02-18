using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimOpt.Optimization.Interfaces
{
    /// <summary>
    /// Interface for a tweakable class with a parameterless Tweak 
    /// (inherited from ITweakable) and a parametrized Tweak function.
    /// </summary>
    /// <typeparam name="T">Type of the tweak parameter.</typeparam>
    public interface IParametrizedTweakable<T> : ITweakable
    {
        /// <summary>
        /// Tweak this instance.
        /// </summary>
        /// <param name="argument">Settings for the tweak operation.</param>
        void Tweak(T argument);
    }
}
