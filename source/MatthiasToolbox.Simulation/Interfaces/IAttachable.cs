using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Simulation.Engine;
using MatthiasToolbox.Basics.Interfaces;
using MatthiasToolbox.Basics.Datastructures.Geometry;

namespace MatthiasToolbox.Simulation.Interfaces
{
    public interface IAttachable : IEntity, IPosition<Point>
    {
        /// <summary>
        /// the IContainer to which this 
        /// instance is attached
        /// </summary>
        IContainer Container { get; set; }

        /// <summary>
        /// flag indicating if this instance
        /// is currently attached to a container
        /// </summary>
        bool IsAttached { get; set; }
    }
}