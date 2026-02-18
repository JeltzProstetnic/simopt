using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Optimization.Interfaces
{
    /// <summary>
    /// Interface for a class with a tweak operation.
    /// </summary>
    public interface ITweakable
    {
        /// <summary>
        /// Tweak this instance.
        /// </summary>
        void Tweak();
    }
}
