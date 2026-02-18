using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Simulation.Engine;
using SimOpt.Basics.Datastructures.Geometry;
using SimOpt.Simulation.Interfaces;
using SimOpt.Basics.Exceptions;
using SimOpt.Simulation.Tools;
using SimOpt.Basics.Interfaces;

namespace SimOpt.Simulation.Entities
{
    /// <summary>
    /// A simulation object which can be used as a resource.
    /// 
    /// The resource entity inherits from stochastic entity and implements IResource, additionally. It holds a reference to a resource 
    /// manager and to its initial and current holders. Furthermore there is a boolean property "Free" which indicates if the resource 
    /// is currently in use. The "Free" property as well as the "CurrentHolder" property have get and set accessors. The set accessors
    /// are meant to be used by the resource manager exclusively. If you manually set the "Free" flag or change the current holder, the 
    /// simulation may become corrupted. Instead of using the "Free" flag, use the "Release" method to indicate to the resource manager 
    /// when you do not need the resource object any longer.
    /// </summary>
    /// <remarks>beta</remarks>
    [Serializable]
    public class ResourceEntity : StochasticEntity, IResource, IPosition<Point>, ISeedSource, IEntity
    {
        #region over

        /// <summary>
        /// resets initial holder and free flag
        /// </summary>
        public override void Reset()
        {
            if (initialHolder != null)
            {
                CurrentHolder = initialHolder;
                Free = false;
            }
            else
            {
                Free = true;
            }
            base.Reset();
        }

        #endregion
        #region virt

        public virtual void OnInitialized() { }

        public virtual void OnResourceManagerInitialized() { }

        public virtual void OnBeforeRelease() { }

        public virtual void OnAfterRelease() { }

        #endregion
        #region cvar

        private bool resourceManagerInitialized;
        private IResourceManager resourceManager;
        private IEntity initialHolder;

        #endregion
        #region prop

        /// <summary>
        /// The resource manager responsible for this resource
        /// must be set in the constructor. (contact matthias if 
        /// you want to set it later)
        /// </summary>
        public IResourceManager ResourceManager { get { return resourceManager; } }

        /// <summary>
        /// CAUTION: DO NOT SET THIS MANUALLY IF A RESOURCE MANAGER IS USED!
        /// </summary>
        public bool Free { get; set; }

        /// <summary>
        /// CAUTION: DO NOT SET THIS MANUALLY IF A RESOURCE MANAGER IS USED!
        /// </summary>
        public IEntity CurrentHolder { get; set; }

        #endregion
        #region ctor

        public ResourceEntity() : base() { }

        public ResourceEntity(IModel model, string id = "", string name = "", Point initialPosition = null, IResourceManager manager = null, IEntity currentHolder = null) 
            : base(model, id, name, initialPosition) 
        {
            // init resource manager
            if (manager != null) resourceManager = manager;
            else resourceManager = model.DefaultResourceManager;
            resourceManager.Manage(this);
            resourceManagerInitialized = true;

            // init holder
            if (currentHolder != null)
            {
                this.CurrentHolder = currentHolder;
                initialHolder = currentHolder;
                Free = false;
            }
            else
            {
                Free = true;
            }

            // call virtuals
            if (resourceManagerInitialized) OnResourceManagerInitialized();
            OnInitialized();
        }

        public ResourceEntity(IModel model, int seedID, string id = "", string name = "", Point initialPosition = null, IResourceManager manager = null, IEntity currentHolder = null) 
            : base(model, seedID, id, name, initialPosition) 
        {
            if (resourceManager != null)
            {
                this.resourceManager = manager;
                manager.Manage(this);
                resourceManagerInitialized = true;
            }
            if (currentHolder != null)
            {
                this.CurrentHolder = currentHolder;
                initialHolder = currentHolder;
                Free = false;
            }
            else
            {
                Free = true;
            }
            if (resourceManagerInitialized) OnResourceManagerInitialized();
            OnInitialized();
        }

        #endregion
        #region init

        public override void Initialize(IModel model, IEntityInitializationParams parameters)
        {
            IResourceEntityInitializationParams initParams = parameters as IResourceEntityInitializationParams;
            if (initParams == null) throw new ArgumentException("You must use an IResourceEntityInitializationParams instance to initialize a ResourceEntity.");
            Initialize(model, initParams);
        }

        public void Initialize(IModel model, IResourceEntityInitializationParams parameters)
        {
            base.Initialize(model, parameters);
            if (parameters.Manager != null) InitializeResourceManager(parameters.Manager, parameters.CurrentHolder);
            OnInitialized();
        }

        //[Obsolete]
        //public void InitializeEntity(IModel model, string id = "", string name = "", IResourceManager manager = null, IEntity currentHolder = null)
        //{
        //    base.Initialize(model, new StochasticEntityInitializationParams(id, name));
        //    if (manager != null) InitializeResourceManager(manager, currentHolder);
        //    OnInitialized();
        //}

        private void InitializeResourceManager(IResourceManager manager, IEntity currentHolder = null) 
        {
            if (this.resourceManager != null) throw new InitializationException("The resource manager for this instance was already defined!");
            if (manager != null)
            {
                this.resourceManager = manager;
                manager.Manage(this);
                resourceManagerInitialized = true;
            }
            if (currentHolder != null)
            {
                this.CurrentHolder = currentHolder;
                initialHolder = currentHolder;
                Free = false;
            }
            else
            {
                Free = true;
            }
            if (resourceManagerInitialized) OnResourceManagerInitialized();
        }

        #endregion
        #region impl

        /// <summary>
        /// Indicate to the resource manager that this 
        /// item is free again.
        /// </summary>
        public void Release() 
        {
            OnBeforeRelease();
            Free = true;
            resourceManager.Update();
            OnAfterRelease();
        }

        #endregion
    }
}