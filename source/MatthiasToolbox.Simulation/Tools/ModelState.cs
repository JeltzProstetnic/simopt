using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using MatthiasToolbox.Basics.Datastructures.Collections;
using System.Diagnostics;
using MatthiasToolbox.Utilities;
using MatthiasToolbox.Simulation.Engine;
using System.Runtime.CompilerServices;
using MatthiasToolbox.Simulation.Enum;
using MatthiasToolbox.Mathematics.Stochastics.Distributions;
using System.Xml.Serialization;

namespace MatthiasToolbox.Simulation.Tools
{
    [Serializable]
    public class ModelState
    {
        #region over

        public override string ToString()
        {
            return "State data of the model <" + ModelName + ">";
        }

        #endregion
        #region cvar

        [NonSerialized, XmlIgnore]
        private Model _model;

        [NonSerialized, XmlIgnore]
        private bool _isModelLoaded;

        [NonSerialized, XmlIgnore]
        private bool _isEntitiesLoaded;

        #endregion
        #region prop

        public Dictionary<string, object> RootValues { get; set; }
        public Dictionary<string, Dictionary<string, object>> EntityValues { get; set; }

        #region temporary

        public Model Model { get { return _model; } set { _model = value; } }
        public bool IsModelLoaded { get { return _isModelLoaded; } set { _isModelLoaded = value; } }
        public bool IsEntitiesLoaded { get { return _isEntitiesLoaded; } set { _isEntitiesLoaded = value; } }

        #endregion
        #region Model

        public int InstanceCounter {get;set;}
        public string ModelName {get;set;}
        public int Seed {get;set;}
        public bool IsAntithetic {get;set;}
        public bool IsNonStochasticMode {get;set;}
        public double StartTime {get;set;}
        public double CurrentTime {get;set;}
        public double EndingTime {get;set;}
        public double StepSize {get;set;}
        public bool IsSeedChange {get;set;}
        public bool IsReset {get;set;}
        public bool IsLogFinish {get;set;}
        public bool IsLogStart {get;set;}
        public bool IsInitialized {get;set;}
        public ExecutionState CurrentState {get;set;}
        public bool IsRequestStop {get;set;}
        public bool IsRequestPause {get;set;}
        public bool IsRequestInterrupt {get;set;}
        public TimeSpan LastRunDuration {get;set;}
        public DateTime RealStartTime {get;set;}
        public UniformIntegerDistribution SeedGenerator {get;set;}

        #endregion
        #region EventScheduler

        public double TimeOfNextScheduledEvent { get; set; }
        public bool EventSchedulerLogging { get; set; }
        public int EventCounter { get; set; }
        public int HandlerCounter { get; set; }
        public int OrderCounter { get; set; }
        public double EventSchedulerNow { get; set; }
        public bool EventSchedulerProcessing { get; set; }

        #endregion

        #endregion
        #region ctor

        public ModelState()
        {
            RootValues = new Dictionary<string, object>();
            EntityValues = new Dictionary<string, Dictionary<string, object>>();
        }

        public ModelState(string modelName) : this()
        {
            this.ModelName = modelName;
        }

        #endregion
        #region impl

        #region get

        public T GetValue<T>(string name)
        {
            return (T)RootValues[name];
        }

        public T GetValue<T>(string id, string name)
        {
            return (T)EntityValues[id][name];
        }

        #endregion
        #region add

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns>success flag (false if the name was already in the dictionary)</returns>
        public bool AddValue(string name, object value) 
        {
            if (RootValues.ContainsKey(name))
            {
                Logging.Logger.Log<Logging.ERROR>("The key " + name + "already has a value. The new value was discarded.");
                return false;
            }
            else
            {
                RootValues[name] = value;
                return true;
            }
        }

        public void AddOrOverrideValue(string name, object value)
        {
                RootValues[name] = value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns>success flag (false if the name was already in the dictionary)</returns>
        public bool AddValue(string containerID, string name, object value)
        {
            if (!EntityValues.ContainsKey(containerID)) EntityValues[containerID] = new Dictionary<string, object>();

            if (EntityValues[containerID].ContainsKey(name))
            {
                Logging.Logger.Log<Logging.ERROR>("The key " + name + "already has a value. The new value was discarded.");
                return false;
            }
            else
            {
                EntityValues[containerID][name] = value;
                return true;
            }
        }

        public void AddOrOverrideValue(string containerID, string name, object value)
        {
            if (!EntityValues.ContainsKey(containerID)) EntityValues[containerID] = new Dictionary<string, object>();
            EntityValues[containerID][name] = value;
        }

        #endregion

        #endregion
    }
}