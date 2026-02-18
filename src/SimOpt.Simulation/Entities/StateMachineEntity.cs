using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Simulation.Tools;
using SimOpt.Basics.Datastructures.StateMachine;
using SimOpt.Simulation.Interfaces;
using SimOpt.Simulation.Engine;
using SimOpt.Basics.Datastructures.Geometry;
using SimOpt.Basics.Interfaces;

namespace SimOpt.Simulation.Entities
{
    /// <summary>
    /// A simulation object with a state machine.
    /// 
    /// The state machine entity is based upon the resource entity and adds state machine functionality using a state machine instance 
    /// from the tools namespace. Besides the StateMachine property, it also provides direct access to the previous and current state 
    /// using properties. Finally, a simulation event to signal state changes is implemented.
    /// 
    /// The possible states and transitions can be configured with strings using the constructors or initialization methods, or directly 
    /// via the state machine property. In theory it is possible to extend or change possible states and transitions dynamically. However, 
    /// this is not recommended as it may cause a lot of confusion. It is also possible to create an entity which contains multiple state
    /// machines. This should also be avoided for the same reason. 
    /// 
    /// If a real world object consists of multiple components which should be represented using state machines, it is better to model 
    /// the components as distinct entities and combine them within a container entity (if necessary). This facilitates inspection and 
    /// debugging of the single components.
    /// </summary>
    /// <remarks>rc</remarks>
    [Serializable]
    public class StateMachineEntity : ResourceEntity, IResource, IPosition<Point>, ISeedSource, IEntity
    {
        #region over

        /// <summary>
        /// Reset this instance
        /// </summary>
        public override void Reset()
        {
            stateMachine.Reset();
            base.Reset();
        }

        #endregion
        #region virt

        public virtual void OnStateMachineInitialized() { }

        public new virtual void OnInitialized() { }

        #endregion
        #region cvar

        private StateMachine stateMachine;
		
		#endregion
		#region prop
		
		/// <summary>
		/// the state machine underlying this object
		/// </summary>
		public StateMachine StateMachine 
        {
			get { return stateMachine; }
		}
		
		/// <summary>
		/// returns null until after the first transition
		/// </summary>
		public State PreviousState {
			get { return stateMachine.PreviousState; }
		}
		
		/// <summary>
		/// the actual state of this object
		/// </summary>
		public State CurrentState { 
			get { return stateMachine.CurrentState; } 
		}
		
		#endregion
		#region ctor
		
        public StateMachineEntity() : base() 
        {
            stateMachine = new StateMachine(this, name: base.EntityName + ".StateMachine");
            stateMachine.TransitionEvent.AddHandler(OnStateTransition);
        }

        public StateMachineEntity(IModel model, 
                                  string id = "", 
                                  string name = "", 
                                  List<string> states = null,
                                  List<Tuple<string, string>> transitions = null,
                                  string initialState = null, 
                                  Point initialPosition = null, 
                                  IResourceManager manager = null, 
                                  IEntity currentHolder = null)
            : base(model, id, name, initialPosition, manager, currentHolder) 
        {
            stateMachine = new StateMachine(this, name: base.EntityName + ".StateMachine");
            stateMachine.TransitionEvent.AddHandler(OnStateTransition);
            if (states != null)
            {
                foreach (string stateName in states)
                    stateMachine.AddState(stateName);
                if (transitions != null)
                {
                    foreach (Tuple<string, string> tuple in transitions)
                        stateMachine.AddTransition(tuple.Item1, tuple.Item2);
                }
                if (!string.IsNullOrEmpty(initialState))
                {
                    stateMachine.Initialize(initialState);
                    OnStateMachineInitialized();
                }
            }
            OnInitialized();
        }

        public StateMachineEntity(IModel model, 
                                  int seedID, 
                                  string id = "",
                                  string name = "", 
                                  List<string> states = null,
                                  List<Tuple<string, string>> transitions = null,
                                  string initialState = null, 
                                  Point initialPosition = null, 
                                  IResourceManager manager = null, 
                                  IEntity currentHolder = null)
            : base(model, seedID, id, name, initialPosition, manager, currentHolder) 
        {
            stateMachine = new StateMachine(this, name: base.EntityName + ".StateMachine");
            stateMachine.TransitionEvent.AddHandler(OnStateTransition);
            if (states != null)
            {
                foreach (string stateName in states)
                    stateMachine.AddState(stateName);
                if (transitions != null)
                {
                    foreach (Tuple<string, string> tuple in transitions)
                        stateMachine.AddTransition(tuple.Item1, tuple.Item2);
                }
                if (!string.IsNullOrEmpty(initialState))
                {
                    stateMachine.Initialize(initialState);
                    OnStateMachineInitialized();
                }
            }
            OnInitialized();
        }

		#endregion
        #region init

        #region model

        public override void Initialize(IModel model, IEntityInitializationParams parameters)
        {
            IStateMachineEntityInitializationParams initParams = parameters as IStateMachineEntityInitializationParams;
            if (initParams == null) throw new ArgumentException("You must use an IStateMachineEntityInitializationParams instance to initialize a StateMachineEntity.");
            Initialize(model, initParams);
        }

        public void Initialize(IModel model, IStateMachineEntityInitializationParams parameters)
        {
            base.Initialize(model, parameters);
            if(parameters.InitialState != null) InitializeStateMachine(parameters.InitialState);
            else if (parameters.InitialStateID.HasValue) InitializeStateMachine(parameters.InitialStateID.Value);
            else if (!string.IsNullOrEmpty(parameters.InitialStateName)) InitializeStateMachine(parameters.InitialStateName);
        }

        //public new void Initialize(IModel model, string id = "", string name = "", IResourceManager manager = null, IEntity currentHolder = null) 
        //{
        //    base.InitializeEntity(model, id, name, manager, currentHolder);
        //    OnInitialized();
        //}

        //public new void Initialize(IModel model, int seedID, string id = "", string name = "", Point initialPosition = null)
        //{
        //    base.InitializeEntity(model, seedID, id, name, initialPosition);
        //    OnInitialized();
        //}

        //public new void Initialize(IModel model, string id = "", string name = "", Point initialPosition = null)
        //{
        //    base.InitializeEntity(model, id, name, initialPosition);
        //    OnInitialized();
        //}

        //public new void Initialize(IModel model, string id = "", string name = "") 
        //{
        //    base.InitializeEntity(model, id, name);
        //    OnInitialized();
        //}

        //public void Initialize(IModel model, string id = "", string name = "", State initialState = null)
        //{
        //    base.InitializeEntity(model, id, name);
        //    if(initialState != null) InitializeStateMachine(initialState);
        //    OnInitialized();
        //}

        //public void Initialize(IModel model, string id = "", string name = "", string initialState = null)
        //{
        //    base.InitializeEntity(model, id, name);
        //    if (!string.IsNullOrEmpty(initialState)) InitializeStateMachine(initialState);
        //    OnInitialized();
        //}

        //public void Initialize(IModel model, string id = "", string name = "", int? initialState = null)
        //{
        //    base.InitializeEntity(model, id, name);
        //    if (initialState != null) InitializeStateMachine((int)initialState);
        //    OnInitialized();
        //}

        #endregion
        #region state machine

        /// <summary>
        /// This has to be called if no initial state was provided for the constructor.
        /// </summary>
        /// <param name="initialState"></param>
        private void InitializeStateMachine(State initialState) 
        {
            stateMachine.Initialize(initialState);
            OnStateMachineInitialized();
        }

        /// <summary>
        /// This has to be called if no initial state was provided for the constructor.
        /// </summary>
        /// <param name="initialState"></param>
        private void InitializeStateMachine(string initialState)
        {
            stateMachine.Initialize(initialState);
            OnStateMachineInitialized();
        }

        /// <summary>
        /// This has to be called if no initial state was provided for the constructor.
        /// </summary>
        /// <param name="initialState"></param>
        private void InitializeStateMachine(int initialState)
        {
            stateMachine.Initialize(initialState);
            OnStateMachineInitialized();
        }

        #endregion

        #endregion
        #region impl

        /// <summary>
		/// default handler template with default user priority
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="transition"></param>
		public virtual void OnStateTransition(StateMachineEntity sender, Transition transition) {}
		
		#endregion
    }
}