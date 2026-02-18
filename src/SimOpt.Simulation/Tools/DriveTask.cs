using System;
using SimOpt.Basics.Datastructures.Geometry;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Network;
using SimOpt.Simulation.Interfaces;

namespace SimOpt.Simulation.Tools
{
    /// <summary>
    /// A simple task for a mobile entity.
    /// </summary>
    [Serializable]
    public class DriveTask : Task
    {
        #region over

        /// <summary>
        /// Invokes the mobile entity's drive method.
        /// </summary>
        public override void OnStart()
        {
            if (target != null)
            {
                owner.DriveTo(target);
            }
            else if (path != null)
            {
                owner.DriveVia(path);
            }
            else
            {
                throw new ApplicationException("A drive task was started without a target or path.");
            }
        }

        #endregion
        #region cvar

        private static int instanceCounter = 0;

        private MobileEntity owner;
        private ISimulationNode target;
        private ISimulationPath path;

        #endregion
        #region prop

        /// <summary>
        /// The mobile entity to which this task belongs.
        /// </summary>
        public MobileEntity Owner
        {
            get { return owner; }
            set 
            { 
                owner = value;
                owner.ArrivalEvent.AddHandler(ArrivalHandler, new Engine.Priority(type:Enum.PriorityType.LowLevelBeforeOthers));
            }
        }

        /// <summary>
        /// The target node.
        /// </summary>
        public ISimulationNode Target
        {
            get
            {
                if (target == null && path != null) return path.LastNode;
                else return target;
            }
            set
            {
                target = value;
                path = null;
            }
        }

        /// <summary>
        /// A pre-defined path to drive on.
        /// </summary>
        public ISimulationPath Path
        {
            get { return path; }
            set
            {
                path = value;
                target = null;
            }
        }

        /// <summary>
        /// A point in the simulation model to drive to.
        /// </summary>
        public Point TargetPosition
        {
            get { return target.Position; }
            set
            {
                target = new Node(position: value);
            }
        }

        #endregion
        #region ctor

        /// <summary>
        /// Empty constructor.
        /// </summary>
        public DriveTask() : base("Drive Task " + (instanceCounter++).ToString()) { }

        /// <summary>
        /// Minimal constructor. Doesn't set a target.
        /// </summary>
        /// <param name="owner"></param>
        public DriveTask(MobileEntity owner) : this()
        {
            this.Owner = owner;
        }

        /// <summary>
        /// Creates a drive task using a point as target.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="target"></param>
        public DriveTask(MobileEntity owner, Point target)
            : this(owner)
        {
            this.target = new Node(position: target);
        }

        /// <summary>
        /// Creates a drive task using a node as target.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="target"></param>
        public DriveTask(MobileEntity owner, ISimulationNode target)
            : this(owner)
        {
            this.target = target;
        }

        /// <summary>
        /// Creates a drive task using a path.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="path"></param>
        public DriveTask(MobileEntity owner, ISimulationPath path)
            : this(owner)
        {
            this.path = path;
        }

        #endregion
        #region hand

        private void ArrivalHandler(MobileEntity sender, ISimulationNode target) 
        {
            base.Finish();
        }

        #endregion
    }
}