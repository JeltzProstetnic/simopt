using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Simulation.Entities;
using MatthiasToolbox.Simulation.Engine;
using MatthiasToolbox.Mathematics.Stochastics.Interfaces;
using MatthiasToolbox.Basics.Datastructures.Geometry;
using MatthiasToolbox.Simulation.Interfaces;

namespace MatthiasToolbox.Simulation.Templates
{
    /// <summary>
    /// non generic wrapper
    /// </summary>
    [Serializable]
    public class SimpleSource : Source<SimpleEntity>
    {
        #region ctor

        /// <summary>
        /// Creates an uninitialized instance. Initialize AND 
        /// InitializeSource will have to be called before the
        /// source can be used.
        /// </summary>
        public SimpleSource() : base() { }

        /// <summary>
        /// CAUTION: The starttime of the model must be initialized 
        /// if autostart is used.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="interval"></param>
        /// <param name="autoStartDelay"></param>
        /// <param name="generator"></param>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="states"></param>
        /// <param name="transitions"></param>
        /// <param name="initialState"></param>
        /// <param name="initialPosition"></param>
        /// <param name="manager"></param>
        /// <param name="currentHolder"></param>
        public SimpleSource(IModel model,
                      IDistribution<double> interval,
                      Func<SimpleEntity> generator,
                      double autoStartDelay = double.NaN,
                      Priority entityCreationEventPriority = null,
                      string id = "",
                      string name = "",
                      List<string> states = null,
                      List<Tuple<string, string>> transitions = null,
                      string initialState = null,
                      Point initialPosition = null,
                      IResourceManager manager = null,
                      IEntity currentHolder = null)
            : base(model, interval, generator, autoStartDelay, entityCreationEventPriority, id, name, states, transitions, initialState, initialPosition, manager, currentHolder)
        { }



        /// <summary>
        /// CAUTION: The starttime of the model must be initialized 
        /// if autostart is used.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="seedID"></param>
        /// <param name="interval"></param>
        /// <param name="autoStartDelay"></param>
        /// <param name="generator"></param>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="states"></param>
        /// <param name="transitions"></param>
        /// <param name="initialState"></param>
        /// <param name="initialPosition"></param>
        /// <param name="manager"></param>
        /// <param name="currentHolder"></param>
        public SimpleSource(IModel model,
                      int seedID,
                      IDistribution<double> interval,
                      Func<SimpleEntity> generator,
                      double autoStartDelay = double.NaN,
                      Priority entityCreationEventPriority = null,
                      string id = "",
                      string name = "",
                      List<string> states = null,
                      List<Tuple<string, string>> transitions = null,
                      string initialState = null,
                      Point initialPosition = null,
                      IResourceManager manager = null,
                      IEntity currentHolder = null)
            : base(model, seedID, interval, generator, autoStartDelay, entityCreationEventPriority, id, name, states, transitions, initialState, initialPosition, manager, currentHolder)
        { }

        /// <summary>
        /// CAUTION: The starttime of the model must be initialized 
        /// if autostart is used.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="interval"></param>
        /// <param name="autoStartDelay"></param>
        /// <param name="generator"></param>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="states"></param>
        /// <param name="transitions"></param>
        /// <param name="initialState"></param>
        /// <param name="initialPosition"></param>
        /// <param name="manager"></param>
        /// <param name="currentHolder"></param>
        public SimpleSource(IModel model,
                      IDistribution<double> interval,
                      double autoStartDelay = double.NaN,
                      Priority entityCreationEventPriority = null,
                      string id = "",
                      string name = "",
                      List<string> states = null,
                      List<Tuple<string, string>> transitions = null,
                      string initialState = null,
                      Point initialPosition = null,
                      IResourceManager manager = null,
                      IEntity currentHolder = null)
            : base(model, interval, autoStartDelay, entityCreationEventPriority, id, name, states, transitions, initialState, initialPosition, manager, currentHolder)
        { }



        /// <summary>
        /// CAUTION: The starttime of the model must be initialized 
        /// if autostart is used.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="seedID"></param>
        /// <param name="interval"></param>
        /// <param name="autoStartDelay"></param>
        /// <param name="generator"></param>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="states"></param>
        /// <param name="transitions"></param>
        /// <param name="initialState"></param>
        /// <param name="initialPosition"></param>
        /// <param name="manager"></param>
        /// <param name="currentHolder"></param>
        public SimpleSource(IModel model,
                      int seedID,
                      IDistribution<double> interval,
                      double autoStartDelay = double.NaN,
                      Priority entityCreationEventPriority = null,
                      string id = "",
                      string name = "",
                      List<string> states = null,
                      List<Tuple<string, string>> transitions = null,
                      string initialState = null,
                      Point initialPosition = null,
                      IResourceManager manager = null,
                      IEntity currentHolder = null)
            : base(model, seedID, interval, autoStartDelay, entityCreationEventPriority, id, name, states, transitions, initialState, initialPosition, manager, currentHolder)
        { }

        #endregion
    }
}
