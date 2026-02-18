using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Engine;
using SimOpt.Mathematics.Stochastics.Interfaces;
using SimOpt.Basics.Datastructures.Geometry;
using SimOpt.Simulation.Interfaces;

namespace SimOpt.Simulation.Templates
{
    [Serializable]
    public class SimpleServer : Server<SimpleEntity, SimpleEntity>
    {
        #region ctor

        public SimpleServer() : base() { }

        public SimpleServer(IModel model,
                      IDistribution<double> machiningTime,
                      string id = "",
                      string name = "",
                      Func<List<SimpleEntity>, SimpleEntity> createProduct = null, 
                      IDistribution<double> timeToFailure = null,
                      IDistribution<double> timeToRecover = null,
                      Func<SimpleEntity, bool> checkMaterialUsable = null,
                      Func<List<SimpleEntity>, bool> checkMaterialComplete = null, 
                      List<string> states = null,
                      List<Tuple<string, string>> transitions = null,
                      string initialState = null,
                      Point initialPosition = null,
                      IResourceManager manager = null,
                      IEntity currentHolder = null,
                      double autoStartDelay = double.NaN)
            : base(model, machiningTime, id, name, createProduct, timeToFailure, timeToRecover, checkMaterialUsable, checkMaterialComplete, 
                   states, transitions, initialState, initialPosition, manager, currentHolder, autoStartDelay)
        { }

        public SimpleServer(IModel model,
                      int seedID,
                      IDistribution<double> machiningTime,
                      string id = "",
                      string name = "",
                      Func<List<SimpleEntity>, SimpleEntity> createProduct = null, 
                      IDistribution<double> timeToFailure = null,
                      IDistribution<double> timeToRecover = null,
                      Func<SimpleEntity, bool> checkMaterialUsable = null,
                      Func<List<SimpleEntity>, bool> checkMaterialComplete = null, 
                      List<string> states = null,
                      List<Tuple<string, string>> transitions = null,
                      string initialState = null,
                      Point initialPosition = null,
                      IResourceManager manager = null,
                      IEntity currentHolder = null,
                      double autoStartDelay = double.NaN)
            : base(model, seedID, machiningTime, id, name, createProduct, timeToFailure, timeToRecover, checkMaterialUsable, checkMaterialComplete,
                   states, transitions, initialState, initialPosition, manager, currentHolder, autoStartDelay)
        { }

        public SimpleServer(IModel model,
                      Func<List<SimpleEntity>, double> machiningTimeDelegate,
                      string id = "",
                      string name = "",
                      Func<List<SimpleEntity>, SimpleEntity> createProduct = null,
                      Func<SimpleEntity, bool> checkMaterialUsable = null,
                      Func<List<SimpleEntity>, bool> checkMaterialComplete = null,
                      List<string> states = null,
                      List<Tuple<string, string>> transitions = null,
                      string initialState = null,
                      Point initialPosition = null,
                      IResourceManager manager = null,
                      IEntity currentHolder = null,
                      double autoStartDelay = double.NaN)
            : base(model, machiningTimeDelegate, id, name, createProduct, checkMaterialUsable, checkMaterialComplete, states, transitions,
                   initialState, initialPosition, manager, currentHolder, autoStartDelay)
        { }

        public SimpleServer(IModel model,
                      int seedID,
                      Func<List<SimpleEntity>, double> machiningTimeDelegate,
                      string id = "",
                      string name = "",
                      Func<List<SimpleEntity>, SimpleEntity> createProduct = null,
                      Func<SimpleEntity, bool> checkMaterialUsable = null,
                      Func<List<SimpleEntity>, bool> checkMaterialComplete = null,
                      List<string> states = null,
                      List<Tuple<string, string>> transitions = null,
                      string initialState = null,
                      Point initialPosition = null,
                      IResourceManager manager = null,
                      IEntity currentHolder = null,
                      double autoStartDelay = double.NaN)
            : base(model, seedID, machiningTimeDelegate, id, name, createProduct, checkMaterialUsable, checkMaterialComplete, states, transitions,
                   initialState, initialPosition, manager, currentHolder, autoStartDelay)
        { }

        #endregion
    }
}
