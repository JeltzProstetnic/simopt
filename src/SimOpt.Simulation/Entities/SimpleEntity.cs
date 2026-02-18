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
using System.Runtime.Serialization;

namespace SimOpt.Simulation.Entities
{
    /// <summary>
    /// The simple entity class is the most basic implementation of "AEntity". It extends the base class with an optional 2D or 3D position. 
    /// The current position is accessible using the "Position" property and will be reset to the initial position as provided to the 
    /// constructor when the model is reset. This class is meant to provide a very simple, light weight entity template for the 
    /// quick implementation of basic simulation objects without any functionality or with very limited functionality on their own.
    /// </summary>
    [Serializable]
    public class SimpleEntity : Entity, IPosition<Point>, IAttachable
    {
        #region over

        public override void Reset()
        {
            currentPosition = initialPosition;
            OnReset();
        }

        public override string ToString()
        {
            return EntityName + " (id=" + Identifier + ")";
        }

        public override bool Load(ModelState state)
        {
            bool result = base.Load(state);

            positionInitialized = state.GetValue<bool>(Identifier, "positionInitialized");
            initialPosition = state.GetValue<Point>(Identifier, "initialPosition");
            currentPosition = state.GetValue<Point>(Identifier, "currentPosition");
            IsAttached = state.GetValue<bool>(Identifier, "IsAttached");

            string deserializedContainerID = state.GetValue<string>(Identifier, "ContainerID");
            Container = Model.GetEntity<IContainer>(deserializedContainerID);

            return result;
        }

        public override void Save(ModelState state)
        {
            base.Save(state);

            state.AddValue(Identifier, "positionInitialized", positionInitialized);
            state.AddValue(Identifier, "initialPosition", initialPosition);
            state.AddValue(Identifier, "currentPosition", currentPosition);
            state.AddValue(Identifier, "IsAttached", IsAttached);

            if (Container is IIdentifiable)
            {
                state.AddValue(Identifier, "ContainerID", (Container as IIdentifiable).Identifier);
            }
            else
            {
                Logging.Logger.Log<Logging.WARN>(this, "Container must be identifiable to be serialized.");
            }
        }

        #endregion
        #region virt

        public virtual void OnReset() { }

        public virtual void OnPositionInitialized() { }

        #endregion
        #region cvar

        private bool positionInitialized;
        private Point initialPosition;
        private Point currentPosition;

        #endregion
        #region prop

        #region IPositionable

        public Point Position
        {
            get
            {
                if (IsAttached) return Container.AbsolutePositionOf(this);
                else return currentPosition;
            }
            set { currentPosition = value; }
        }

        #endregion
        #region IAttachable<SimpleEntity>

        public IContainer Container { get; set; }

        public bool IsAttached { get; set; }

        #endregion

        #endregion
        #region ctor

        /// <summary>
        /// Empty constructor. CAUTION: this
        /// instance must be initialized with a 
        /// model before it can be used!
        /// </summary>

        public SimpleEntity()
            : base()
        {
        }

        public SimpleEntity(IModel model,
            string id = "",
            string name = "",
            Point initialPosition = null)
            : base(model, id, name)
        {
            if (initialPosition != null) InitializePosition(initialPosition);
        }

        #endregion
        #region init

        public override void Initialize(IModel model, IEntityInitializationParams parameters)
        {
            ISimpleEntityInitializationParams initParams = parameters as ISimpleEntityInitializationParams;
            if (initParams == null) throw new ArgumentException("You must use an ISimpleEntityInitializationParams instance to initialize a SimpleEntity.");
            Initialize(model, initParams);
        }

        public void Initialize(IModel model, ISimpleEntityInitializationParams parameters)
        {
            base.Initialize(model, parameters);
            if (parameters.InitialPosition != null) InitializePosition(parameters.InitialPosition);
        }

        //[Obsolete]
        //public void InitializeEntity(IModel model, string id = "", string name = "", Point initialPosition = null) 
        //{
        //    base.Initialize(model, new EntityInitializationParams(id, name));
        //    if (initialPosition != null) InitializePosition(initialPosition);
        //}

        private void InitializePosition(Point initialPosition)
        {
            if (positionInitialized) throw new InitializationException("The position of this instance has already been initialized!");
            this.initialPosition = initialPosition;
            this.currentPosition = initialPosition;
            positionInitialized = true;
            OnPositionInitialized();
        }

        #endregion
    }
}