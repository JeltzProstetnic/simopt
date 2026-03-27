using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Basics.Datastructures.Geometry;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Entities;

namespace SimOpt.Simulation.Templates
{
    /// <summary>
    /// Non-generic sink template that permanently removes <see cref="SimpleEntity"/> instances from the simulation.
    /// Wraps the generic <see cref="Sink{TEntity}"/> with <see cref="SimpleEntity"/> to simplify common use cases.
    /// </summary>
    [Serializable]
    public class SimpleSink : Sink<SimpleEntity>
    {
        #region ctor

        /// <summary>
        /// Creates an uninitialized instance. <c>Initialize</c> must be called before the sink can be used.
        /// </summary>
        public SimpleSink() : base() { }

        /// <summary>
        /// Creates a sink bound to the given model.
        /// </summary>
        /// <param name="model">The model this sink belongs to.</param>
        /// <param name="id">Unique identifier for this entity.</param>
        /// <param name="name">Human-readable name for this entity.</param>
        /// <param name="log">When <see langword="true"/>, entity arrivals are written to the simulation log.</param>
        /// <param name="initialPosition">The initial spatial position of this entity.</param>
        public SimpleSink(IModel model, string id = "", string name = "", bool log = false, Point initialPosition = null)
            : base(model, id, name, log, initialPosition)
        { }

        #endregion
    }
}
