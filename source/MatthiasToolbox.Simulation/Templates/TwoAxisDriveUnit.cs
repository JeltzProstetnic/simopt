using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Simulation.Entities;
using MatthiasToolbox.Simulation.Engine;
using MatthiasToolbox.Basics.Datastructures.Geometry;
using MatthiasToolbox.Simulation.Interfaces;
using MatthiasToolbox.Simulation.Tools;
using MatthiasToolbox.Simulation.Network;
using MatthiasToolbox.Simulation.Events;

namespace MatthiasToolbox.Simulation.Templates
{
    [Serializable]
    public class TwoAxisDriveUnit : TaskMachineEntity, IMovable
    {
        #region cvar

        private bool stopFlag = false;

        private List<MobileEntity> mobileEntities = new List<MobileEntity>();
        private List<Point> origins = new List<Point>();
        private List<Vector> axes = new List<Vector>();

        private Queue<ISimulationNode> pathQueue;

        #endregion
        #region prop

        public Point CurrentPosition
        {
            get
            {
                return new Point(mobileEntities[0].GetAbsolutePosition().X, mobileEntities[1].GetAbsolutePosition().Y);
            }
        }

        public IEnumerable<Vector> Axes { get { return axes; } }

        public BinaryEvent<TwoAxisDriveUnit, ISimulationNode> ArrivalEvent { get; private set; }

        public BinaryEvent<MobileEntity, ISimulationNode> ArrivalEventX { get { return mobileEntities[0].ArrivalEvent; } }

        public BinaryEvent<MobileEntity, ISimulationNode> ArrivalEventY { get { return mobileEntities[1].ArrivalEvent; } }

        #region IMovable

        public BinaryEvent<IMovable, Point> StoppedEvent { get; private set; }

        #endregion

        #endregion
        #region ctor

        public TwoAxisDriveUnit() : base() 
        {
            ArrivalEvent = new BinaryEvent<TwoAxisDriveUnit, ISimulationNode>(EntityName + ".Arrived");
            StoppedEvent = new BinaryEvent<IMovable, Point>(EntityName + ".Stopped");
        }

        public TwoAxisDriveUnit(IModel model, 
                                 string id = "", 
                                 string name = "",
                                 Action<Task> notifyTaskFinished = null,
                                 Action<Task> notifyTaskStarted = null,
                                 Action<Task> notifyTaskStartFailed = null,
                                 List<string> states = null, 
                                 List<Tuple<string, string>> transitions = null,
                                 string initialState = "", 
                                 IResourceManager manager = null, 
                                 IEntity currentHolder = null)
            : base(model, id, name, notifyTaskFinished, notifyTaskStarted, notifyTaskStartFailed, states, transitions, initialState, null, manager, currentHolder) 
        {
            ArrivalEvent = new BinaryEvent<TwoAxisDriveUnit, ISimulationNode>(EntityName + ".Arrived");
            StoppedEvent = new BinaryEvent<IMovable, Point>(EntityName + ".Stopped");
        }

        public TwoAxisDriveUnit(IModel model,
                                 int seedID,
                                 string id = "",
                                 string name = "",
                                 Action<Task> notifyTaskFinished = null,
                                 Action<Task> notifyTaskStarted = null,
                                 Action<Task> notifyTaskStartFailed = null, 
                                 List<string> states = null,
                                 List<Tuple<string, string>> transitions = null,
                                 string initialState = "",
                                 IResourceManager manager = null,
                                 IEntity currentHolder = null)
            : base(model, seedID, id, name, notifyTaskFinished, notifyTaskStarted, notifyTaskStartFailed, states, transitions, initialState, null, manager, currentHolder)
        {
            ArrivalEvent = new BinaryEvent<TwoAxisDriveUnit, ISimulationNode>(EntityName + ".Arrived");
            StoppedEvent = new BinaryEvent<IMovable, Point>(EntityName + ".Stopped");
        }

        #endregion
        #region init

        /// <summary>
        /// initial position: absolute
        /// </summary>
        /// <param name="initialPosition">Absolute value</param>
        /// <param name="originX"></param>
        /// <param name="axisX"></param>
        /// <param name="accelerationX"></param>
        /// <param name="decelerationX"></param>
        /// <param name="vMaxX"></param>
        /// <param name="originY"></param>
        /// <param name="axisY"></param>
        /// <param name="accelerationY"></param>
        /// <param name="decelerationY"></param>
        /// <param name="vMaxY"></param>
        public void InitializeDriveUnit(
            Point initialPosition,
            Point originX, 
            Vector axisX, 
            double accelerationX, 
            double decelerationX,
            double vMaxX, 
            Point originY, 
            Vector axisY, 
            double accelerationY, 
            double decelerationY, 
            double vMaxY)
        {
            base.InitializePosition(initialPosition);

            // TODO: schiefe ausrichtung?
            // originX + a * Vector.AsNormalized(axisX) = 

            Node initialX = new Node("initialX", "", null, 1, new Point(initialPosition.X, originX.Y));
            Node initialY = new Node("initialY", "", null, 1, new Point(originY.X, initialPosition.Y));

            MobileEntity meX = new MobileEntity(Model, this.Identifier + ".MobileEntity1", this.Identifier + ".MobileEntity1", initialX, vMaxX, accelerationX, decelerationX, initialPosition: base.initialPosition);
            MobileEntity meY = new MobileEntity(Model, this.Identifier + ".MobileEntity2", this.Identifier + ".MobileEntity2", initialY, vMaxY, accelerationY, decelerationY, initialPosition: base.initialPosition);
            
            mobileEntities.Add(meX);
            mobileEntities.Add(meY);
            
            axes.Add(axisX);
            axes.Add(axisY);

            origins.Add(originX);
            origins.Add(originY);

            // PassThroughEvent = new BinaryCompoundEvent<MobileEntity, ISimulationNode>(this.EntityName + ".PassThroughCompoundEvent", meX.PassThroughEvent, meY.PassThroughEvent);
            ArrivalEvent.AddHandler(InternalArrivedHandler, new Priority(type: Enum.PriorityType.LowLevelAfterOthers));
        }

        #endregion
        #region hand

        private void InternalArrivedHandler(TwoAxisDriveUnit sender, ISimulationNode target)
        {
            if (stopFlag)
            {
                stopFlag = false;
                pathQueue.Clear();
            }
            else 
            {
                if (pathQueue != null && pathQueue.Count > 0) DriveTo(pathQueue.Dequeue());
            }
        }

        #endregion
        #region impl

        /// <summary>
        /// Stops the drive unit as soon as the next target is reached.
        /// </summary>
        public void Stop()
        {
            StopMoving();
        }

        /// <summary>
        /// Stops the drive unit as soon as the next target is reached.
        /// </summary>
        public void StopMoving()
        {
            stopFlag = true;
        }

        public void DriveTo(Point target)
        {
            Node targetX = new Node("", "", null, 1, new Point(target.X, origins[0].Y));
            Node targetY = new Node("", "", null, 1, new Point(origins[1].X, target.Y));

            (mobileEntities[0].CurrentNode as ISimulationNode).ConnectTo(targetX);
            (mobileEntities[1].CurrentNode as ISimulationNode).ConnectTo(targetY);

            mobileEntities[0].DriveTo(targetX);
            mobileEntities[1].DriveTo(targetY);

            double arrivalTime = Math.Max(mobileEntities[0].TimeToTarget, mobileEntities[1].TimeToTarget);
            Model.AddEvent(arrivalTime, ArrivalEvent.GetInstance(this, new Node(position: target)));

            if (pathQueue == null || pathQueue.Count <= 1)
                Model.AddEvent(arrivalTime, StoppedEvent.GetInstance(this, target));
        }

        public void DriveTo(ISimulationNode target)
        {
            Node targetX = new Node("", "", null, 1, new Point(target.Position.X, origins[0].Y));
            Node targetY = new Node("", "", null, 1, new Point(origins[1].X, target.Position.Y));

            (mobileEntities[0].CurrentNode as ISimulationNode).ConnectTo(targetX);
            (mobileEntities[1].CurrentNode as ISimulationNode).ConnectTo(targetY);

            mobileEntities[0].DriveTo(targetX);
            mobileEntities[1].DriveTo(targetY);

            double arrivalTime = Math.Max(mobileEntities[0].TimeToTarget, mobileEntities[1].TimeToTarget);
            Model.AddEvent(arrivalTime, ArrivalEvent.GetInstance(this, target));

            if(pathQueue == null || pathQueue.Count <= 1)
                Model.AddEvent(arrivalTime, StoppedEvent.GetInstance(this, target.Position));
        }

        /// <summary>
        /// Stops at each node.
        /// </summary>
        /// <param name="path"></param>
        public void DriveVia(Path path)
        {
            pathQueue = new Queue<ISimulationNode>(path.SimulationNodes);
            if (path.FirstNode.Position.Equals(CurrentPosition)) pathQueue.Dequeue();
            DriveTo(pathQueue.Dequeue());
        }

        #region IMovable Member

        public void MoveTo(Point position)
        {
            DriveTo(position);
        }

        public void StartMoving(Vector direction)
        {
            throw new NotImplementedException("This entity cannot move without a specified target.");
        }

        #endregion

        #endregion
    }
}