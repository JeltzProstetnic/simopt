using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Simulation.Engine;
using MatthiasToolbox.Basics.Datastructures.Geometry;
using MatthiasToolbox.Basics.Interfaces;

namespace MatthiasToolbox.Simulation.Interfaces
{
    public interface IContainer : IPosition<Point>, IEntity
    {
        /// <summary>
        /// the currently attached objects
        /// </summary>
        IEnumerable<IAttachable> ContainedObjects { get; }

        /// <summary>
        /// the position of the given object relative
        /// to the container position
        /// </summary>
        /// <param name="containedObject"></param>
        /// <returns></returns>
        Point RelativePositionOf(IAttachable containedObject);

        /// <summary>
        /// the position of the given object
        /// </summary>
        /// <param name="containedObject"></param>
        /// <returns></returns>
        Point AbsolutePositionOf(IAttachable containedObject);
    }
}