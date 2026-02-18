using System;
using System.Collections.Generic;
using MatthiasToolbox.Basics.Exceptions;
using MatthiasToolbox.Basics.Datastructures.Geometry;
using MatthiasToolbox.Mathematics.Stochastics.Distributions;
using MatthiasToolbox.Simulation.Engine;
using MatthiasToolbox.Simulation.Interfaces;
using MatthiasToolbox.Simulation.Tools;
using MatthiasToolbox.Basics.Interfaces;

namespace MatthiasToolbox.Simulation.Entities
{
    /// <summary>
    /// A simulation object which can use and provide random distributions.
    /// Caution: This class has no own OnInitialized method. 
    /// 
    /// The stochastic entity class is the recommended base entity for all simulation objects in any stochastic simulation. It is also 
    /// based on "AEntity" and extends the abstract class with positioning as in the simple entity. Additionally it implements the ISeedSource 
    /// interface to allow instances to be used to initialize a Random<T> object. The stochastic entity and all derived classes provide the 
    /// seed identifier mechanism as described in chapter 4.3.3. The SeedID property is protected from inadvertent change and has to be 
    /// provided to the constructor together with the model. There is a parameterless constructor as well, mainly for deserialization purposes. 
    /// If it is used, the entity has to be initialized manually, in which case the seed id must be set prior to the model, or by using one of 
    /// the initialization methods.
    /// 
    /// If the stochastic entity or any derived class is initialized using a SeedID, that seed id is combined with the model seed using 
    /// exclusive or (XOR) to create a reproducible seed value for the instance.
    /// </summary>
    /// <remarks>rc</remarks>
    [Serializable]
    public class StochasticEntity : Entity, IPosition<Point>, IAttachable, ISeedSource, IEntity
    {
        #region over

        /// <summary>
        /// reset the initial position and all random generators
        /// </summary>
        public override void Reset()
        {
            currentPosition = initialPosition;

            if (Model.SeedChange)
            {
                if (!seedIDSet)
                    this.seed = Model.SeedGenerator.Next();
                else
                    this.seed = Model.GetRandomSeedFor((int)seedID);

                seedGenerator.Reset((int)this.seed);

                foreach (IRandom rnd in randomGenerators)
                    rnd.Reset(seedGenerator.Next(), Model.Antithetic, Model.NonStochasticMode);

                OnSeedChanged();
            }
            else
            {
                foreach (IRandom rnd in randomGenerators)
                    rnd.Reset(Model.Antithetic, Model.NonStochasticMode);
            }
            OnReset();
        }

        public override bool Load(ModelState state)
        {
            bool result = base.Load(state);

            seed = state.GetValue<int?>(Identifier, "seed");
            seedID = state.GetValue<int?>(Identifier, "seedID");
            seedIDSet = state.GetValue<bool>(Identifier, "seedIDSet");
            seedGenerator = state.GetValue<UniformIntegerDistribution>(Identifier, "seedGenerator");
            seedInitialized = state.GetValue<bool>(Identifier, "seedInitialized");
            positionInitialized = state.GetValue<bool>(Identifier, "positionInitialized");
            initialPosition = state.GetValue<Point>(Identifier, "initialPosition");
            currentPosition = state.GetValue<Point>(Identifier, "currentPosition");
            IsAttached = state.GetValue<bool>(Identifier, "IsAttached");
            randomGenerators = state.GetValue<List<IRandom>>(Identifier, "randomGenerators");
            
            string deserializedContainerID = state.GetValue<string>(Identifier, "ContainerID");
            Container = Model.GetEntity<IContainer>(deserializedContainerID);

            return result;
        }

        public override void Save(ModelState state)
        {
            base.Save(state);

            state.AddValue(Identifier, "seed", seed);
            state.AddValue(Identifier, "seedID", seedID);
            state.AddValue(Identifier, "seedIDSet", seedIDSet);
            state.AddValue(Identifier, "seedGenerator", seedGenerator);
            state.AddValue(Identifier, "seedInitialized", seedInitialized);
            state.AddValue(Identifier, "positionInitialized", positionInitialized);
            state.AddValue(Identifier, "initialPosition", initialPosition);
            state.AddValue(Identifier, "currentPosition", currentPosition);
            state.AddValue(Identifier, "IsAttached", IsAttached);

            state.AddValue(Identifier, "randomGenerators", randomGenerators);

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

        /// <summary>
        /// If the seed is changed during reset, OnSeedChanged will be called before OnReset.
        /// </summary>
        public virtual void OnReset() { }

        public virtual void OnSeedInitialized() { }

        /// <summary>
        /// Occurs before OnReset if the seed is changed during reset.
        /// </summary>
        public virtual void OnSeedChanged() { }

        public virtual void OnPositionInitialized() { }

        #endregion
        #region cvar

        private bool seedInitialized;
        private bool positionInitialized;

        protected Point initialPosition;
        private Point currentPosition;

        private int? seed;
        private int? seedID;
        private bool seedIDSet = false;
        private List<IRandom> randomGenerators;
        private UniformIntegerDistribution seedGenerator;

        #endregion
        #region prop

        #region Stochastics

        /// <summary>
        /// Use this ID to enforce a reproducible seed in
        /// spite of unstable creation order.
        /// </summary>
        public int? SeedID
        {
            get
            {
                return seedID;
            }
            set
            {
                if (seed != null)
                    throw new InvalidOperationException("A seed value was already given to this instance. If you want to use " +
                                                        "identifiable seeds, the seed ID has to be set prior to the model.");
                if (seedIDSet) throw new ValueAlreadySetException("SeedID");
                seedID = value;
                seedIDSet = true;
            }
        }

        public int? Seed
        {
            get
            {
                return seed;
            }
        }

        public UniformIntegerDistribution SeedGenerator
        {
            get
            {
                return seedGenerator;
            }
            set
            {
                seedGenerator = value;
            }
        }

        public IEnumerable<IRandom> RandomGenerators
        {
            get
            {
                return randomGenerators;
            }
        }

        #endregion
        #region IPositionable

        /// <summary>
        /// The current 3D Position of this instance.
        /// </summary>
        public virtual Point Position
        {
            get
            {
                if (IsAttached) return Container.AbsolutePositionOf(this);
                else return currentPosition;
            }
            set { currentPosition = value; }
        }

        #endregion
        #region IAttachable

        public IContainer Container { get; set; }

        public bool IsAttached { get; set; }

        #endregion

        #endregion
        #region ctor

        /// <summary>
        /// Default constructor. Caution: this instance 
        /// must be initialized manually before use!
        /// </summary>
        public StochasticEntity() : base() { }

        /// <summary>
        /// Creates an initialized instance, ready for use.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="initialPosition"></param>
        public StochasticEntity(IModel model,
            string id = "",
            string name = "",
            Point initialPosition = null)
            : base(model, id, name)
        {
            if (initialPosition != null) InitializePosition(initialPosition);
            InitializeSeed();
        }

        /// <summary>
        /// Creates an initialized instance, ready for use.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="initialPosition"></param>
        public StochasticEntity(IModel model,
            int seedID,
            string id = "",
            string name = "",
            Point initialPosition = null)
            : base(model, id, name)
        {
            this.SeedID = seedID;
            if (initialPosition != null) InitializePosition(initialPosition);
            InitializeSeed();
        }

        #endregion
        #region init

        public override void Initialize(IModel model, IEntityInitializationParams parameters)
        {
            IStochasticEntityInitializationParams initParams = parameters as IStochasticEntityInitializationParams;
            if (initParams == null) throw new ArgumentException("You must use an IStochasticEntityInitializationParams instance to initialize a StochasticEntity.");
            Initialize(model, initParams);
        }

        public void Initialize(IModel model, IStochasticEntityInitializationParams parameters)
        {
            if (model != null && model != Model) // re-using entity for different model
            {
                seedInitialized = false;
                positionInitialized = false;
            }
            base.Initialize(model, parameters);
            if (parameters.SeedIdentifier.HasValue) this.SeedID = parameters.SeedIdentifier;
            if (parameters.InitialPosition != null) InitializePosition(parameters.InitialPosition);
            InitializeSeed();
        }

        protected void InitializePosition(Point initialPosition)
        {
            if (positionInitialized) 
                throw new InitializationException("The position for this instance has already been initialized!");

            this.initialPosition = initialPosition;
            this.currentPosition = initialPosition;
            this.positionInitialized = true;

            OnPositionInitialized();
        }

        private void InitializeSeed()
        {
            if (seedInitialized) 
                throw new InitializationException("The seed for this instance has already been initialized!");
            
            if (!seedIDSet)
                this.seed = Model.SeedGenerator.Next();
            else
                this.seed = Model.GetRandomSeedFor((int)seedID);

            this.randomGenerators = new List<IRandom>();
            this.seedGenerator = new UniformIntegerDistribution((int)seed, 0, int.MaxValue);
            this.seedInitialized = true;

            OnSeedInitialized();
        }

        #endregion
        #region impl

        public int GetRandomSeedFor(int seedID)
        {
            return (new System.Random((int)seed ^ seedID)).Next();
        }

        public void AddRandomGenerator(IRandom generator)
        {
            if (!seedInitialized) throw new InitializationException("Random generators may only be used after the seed source is initialized!");
            randomGenerators.Add(generator);
        }

        #endregion
    }
}