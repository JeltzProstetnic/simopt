using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimOpt.Simulation.Engine
{
    /// <summary>
    /// interface for resettable objects
    /// </summary>
    /// <remarks>beta</remarks>
    public interface IResettable
    {
        /// <summary>
        /// Reset the object to its initial state.
        /// Implementations must persist the random 
        /// seed through this call.
        /// </summary>
        void Reset();
    }
}
