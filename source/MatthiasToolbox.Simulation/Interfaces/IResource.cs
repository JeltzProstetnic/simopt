using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Simulation.Engine;

namespace MatthiasToolbox.Simulation.Interfaces
{
    public interface IResource : IEntity
    {
        /// <summary>
        /// the simulation object currently holding the resource
        /// caution! do not use the setter from outside the resource manager!!!
        /// </summary>
        IEntity CurrentHolder { get; set; }

        /// <summary>
        /// the resource manager which manages this resource
        /// caution! do not change the resource manager once it was set!
        /// </summary>
        IResourceManager ResourceManager { get; }

        /// <summary>
        /// returns false if the resource is currently in use or 
        /// part of an active reservation process.
        /// caution! do not use the setter from outside the resource manager!!!
        /// </summary>
        bool Free { get; set; }

        /// <summary>
        /// releases the resource after use.
        /// </summary>
        void Release();
    }
}
