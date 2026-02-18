using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Simulation.Exceptions;
using MatthiasToolbox.Mathematics.Stochastics.Distributions;
using MatthiasToolbox.Basics.Exceptions;
using System.Runtime.Serialization;
using MatthiasToolbox.Simulation.Tools;
using MatthiasToolbox.Simulation.Interfaces;

namespace MatthiasToolbox.Simulation.Engine
{
    /// <summary>
    /// This is the recommended base template for all simulation objects. It manages a reference to the corresponding model so that the
    /// model cannot be changed accidentally once it has been set. For entities crossing model boundaries in distributed simulation 
    /// experiments there is an initialization mechanism, which can be used to re-initialize the entity to work with a different 
    /// model. The removal of the entity from the previous model is not done automatically.
    /// 
    /// The model in which the entity will be contained must be provided in the constructor call. An identifier and a name are 
    /// optional. If no identifier is provided, a default identifier including an auto-incrementing number will be created. 
    /// The entity will add itself to the provided model during instantiation. 
    /// 
    /// The abstract method "Reset" enforces that every entity or entity template provides a way to set it back to the initial conditions.
    /// </summary>
    /// <remarks>beta</remarks>
    [Serializable]
    public abstract class Entity : IResettable, IEntity, ISaveable
    {
        #region over

        public override string ToString()
        {
            return EntityName + " (id=" + Identifier + ")";
        }

        #endregion
        #region virt

        public virtual void OnModelInitialized() { }

        #endregion
        #region cvar

        private static int instanceCounter = 0;
        private int instanceNumber;
        
        private string id;
        private bool idSet = false;

        private IModel model;
        private bool modelSet = false;

        private bool initialized;

        #endregion
        #region prop

        /// <summary>
        /// The unique id of this instance.
        /// </summary>
        public string Identifier
        {
            get { return id; }
            set 
            {
                if (idSet) throw new ValueAlreadySetException("ID");
                if (!string.IsNullOrEmpty(value))
                {
                    id = value;
                    idSet = true;
                }
                else
                {
                    throw new ArgumentException("The id may not be null or empty.");
                }
            }
        }

        /// <summary>
        /// The human readable name of this instance. This
        /// is not necessarily unique.
        /// </summary>
        public string EntityName
        {
            get; set;
        }

        /// <summary>
        /// The simulation model which is associated with this entity.
        /// </summary>
        public IModel Model
        {
            get
            {
                return model;
            }
            set
            {
                if (modelSet) throw new ModelAlreadySetException();
                if (value != null)
                {
                    model = value;
                    modelSet = true;
                }
                else
                {
                    throw new NullReferenceException("The model reference may not be set to null.");
                }
            }
        }

        /// <summary>
        /// Returns true if this instance was already initialized with a model.
        /// </summary>
        public virtual bool IsInitialized
        {
            get { return initialized; }
        }

        #endregion
        #region ctor

        /// <summary>
        /// Default constructor. Intended only for
        /// deserialization use.
        /// Caution: this does not set model or id! 
        /// Therefore it also doesn't add this instance 
        /// to a model.
        /// </summary>
        internal Entity() 
        {
            instanceNumber = instanceCounter;
            instanceCounter += 1;
        }

        /// <summary>
        /// Constructor with ID and name. The ID must be unique, 
        /// otherwise an exception will be thrown. If no id is
        /// provided, the id will be set to the value of the 
        /// instance counter (n). If neither id nor name are 
        /// provided, the name will be set to "Entity n" 
        /// where n is the ID. If no name is provided, 
        /// the name will be set to ID.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="id"></param>
        /// <param name="name"></param>
        public Entity(IModel model, string id = "", string name = "") 
        {
            if (model == null) throw new ArgumentException("The provided model must not be null!");
            bool noID = string.IsNullOrEmpty(id);
            this.model = model;
            modelSet = true;

            if (noID) this.id = "Entity " + instanceCounter.ToString();
            else this.id = id;
            idSet = true;
            
            model.AddEntity(this);

            if (string.IsNullOrEmpty(name)) this.EntityName = this.id;
            else this.EntityName = name;

            instanceNumber = instanceCounter;
            instanceCounter += 1;

            initialized = true;
            OnModelInitialized();
        }

        #endregion
        #region init

        public virtual void Initialize(IModel model, IEntityInitializationParams parameters) 
        {
            if (model == null) throw new ArgumentException("The provided model must not be null!");
            if (initialized && model == this.model) throw new InitializationException("This instance was already initialized for the model \"" + model.Name + "\"!");
            this.model = model;
            modelSet = true;

            if (!string.IsNullOrEmpty(parameters.ID))
            {
                this.id = parameters.ID;
                idSet = true;
            }

            if (!String.IsNullOrEmpty(parameters.EntityName)) this.EntityName = parameters.EntityName;
            else this.EntityName = id;

            model.AddEntity(this);

            initialized = true;
            OnModelInitialized();
        }

        //public virtual void InitializeEntity(IModel model, string id = "", string name = "")
        //{
        //    if (model == null) throw new ArgumentException("The provided model must not be null!");
        //    if (initialized && model == this.model) throw new InitializationException("This instance was already initialized for the model \"" + model.Name + "\"!");
        //    this.model = model;
        //    modelSet = true;

        //    if (!string.IsNullOrEmpty(id))
        //    {
        //        this.id = id;
        //        idSet = true;
        //    }

        //    if (!String.IsNullOrEmpty(name)) this.EntityName = name;
        //    else this.EntityName = id;

        //    model.AddEntity(this);

        //    initialized = true;
        //    OnModelInitialized();
        //}

        #endregion
        #region rset

        /// <summary>
        /// Caution: Be aware that there is always 
        /// something to reset in a concrete simulation 
        /// object! Not resetting things is the most
        /// common and also the most difficult to find 
        /// mistake in discrete models where the 
        /// reproducibility fails for no other obvious
        /// reason.
        /// </summary>
        public abstract void Reset();

        #endregion
        #region impl

        public virtual bool Load(ModelState state)
        {
            this.model = state.Model;
            this.id = state.GetValue<string>(Identifier, "ID");
            this.EntityName = state.GetValue<string>(Identifier, "Name");
            instanceCounter = state.GetValue<int>(Identifier, "instanceCounter");
            this.instanceNumber = state.GetValue<int>(Identifier, "instanceNumber");
            this.idSet = state.GetValue<bool>(Identifier, "idSet");
            this.initialized = state.GetValue<bool>(Identifier, "initialized");

            return true;
        }

        public virtual void Save(ModelState state)
        {
            state.AddValue(Identifier, "ID", id);
            state.AddValue(Identifier, "Name", EntityName);
            state.AddValue(Identifier, "instanceCounter", instanceCounter);
            state.AddValue(Identifier, "instanceNumber", instanceNumber);
            state.AddValue(Identifier, "idSet", idSet);
            state.AddValue(Identifier, "initialized", initialized);
        }

        #endregion
    }
}