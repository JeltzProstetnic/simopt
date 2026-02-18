using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Simulation.Interfaces;
using MatthiasToolbox.Basics.Interfaces;

namespace MatthiasToolbox.Simulation.Engine
{
    /// <summary>
    /// Interface for a simulation object. This has to be implemented
    /// whenever the requirements for it include one of the following:
    ///     - must be findable through the model
    ///     - must be serialized with the model
    ///     - uses multiple random distributions
    ///     - is associated with a path network
    ///     - should appear on the visualization
    /// 
    /// So usually only dynamic entities like products which appear in
    /// a simulation but do nothing will NOT implement this.
    /// </summary>
    /// <remarks>beta</remarks>
    public interface IEntity : IResettable, IIdentifiable
    {
        /// <summary>
        /// assign a name
        /// uniqueness is not strictly required
        /// </summary>
        String EntityName { get; set; }

        /// <summary>
        /// the simulation model to which this object belongs
        /// </summary>
        IModel Model { get; set; }
    }
}
