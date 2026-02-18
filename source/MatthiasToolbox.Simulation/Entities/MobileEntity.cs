using System;
using System.Collections.Generic;
using MatthiasToolbox.Basics.Datastructures.Network;
using MatthiasToolbox.Basics.Exceptions;
using MatthiasToolbox.Basics.Datastructures.Geometry;
using MatthiasToolbox.Simulation.Engine;
using MatthiasToolbox.Simulation.Enum;
using MatthiasToolbox.Simulation.Events;
using MatthiasToolbox.Simulation.Interfaces;
using MatthiasToolbox.Simulation.Tools;
using MatthiasToolbox.Simulation.Network;

namespace MatthiasToolbox.Simulation.Entities
{
    // TODO: [10:06:03] AREC Andreas Gruber: mobilentity auf prev node != null abfragen sonst krachts bei der initialisierung
    // TODO: @andi: ist das obige eh schon erledigt oder noch offen? Wenn noch offen, wo im code?

    /// <summary>
    /// The mobile entity is the most complex of the base entities. It inherits TaskMachineEntity and provides support for movement including 
    /// acceleration and deceleration. It can be configured to drive on a network or between absolute positions.
    /// 
    /// The movement of a mobile entity is not approximated via sampling. Instead the mobile object keeps track of the point in time at which 
    /// acceleration was started and calculates its current speed and location based on the speed profile only on request. This is a good
    /// example for a combination of discrete and continuous concepts. By abstaining from discretizing linear movements or other linear 
    /// processes a lot of computing resources can be saved, especially if the simulation is executed without animation. 
    /// 
    /// The mobile entity has no built-in collision handling. Collisions can either be managed by using paths like resources and implementing 
    /// a reservation mechanism or by iterating over all mobile units with collision potential whenever a drive task is started or changed.
    /// </summary>
    [Serializable]
    public class MobileEntity : TaskMachineEntity, IMovable
    {
        #region over

        public override void Reset()
        {
            base.Reset();
            CurrentNode = InitialNode;
            IsOnNode = InitialNode != null;

            nextArrivalInstance = null;
            IsDriving = false;
            PreviousNode = null;
            NextNode = null;
            IsOnPath = false;
            CurrentPath = null;
            IsOnConnection = false;

            DrivingStartTime = double.NaN;
            TimeToVMax = double.NaN;
            DistanceToVMax = double.NaN;
            TimeToDeceleration = double.NaN;
            DistanceToDeceleration = double.NaN;
            TimeAtVMax = double.NaN;
            DistanceAtVMax = double.NaN;
            TimeToTarget = 0;
            DistanceToTarget = 0;

            LastKnownPosition = null;
        }

        public override Point Position
        {
            get
            {
                return GetAbsolutePosition();
            }
            set
            {
                if(IsDriving) throw new InvalidOperationException("The position of a mobile entity cannot be set manually while the entity is driving.");
                CurrentNode = new Node(position: value);
            }
        }

        #endregion
        #region cvar

        private double vMax; // m/s
        private double actualVMax;
        private double acceleration; // m/s²
        private double accelerationTime; // s
        private double accelerationDistance; // m
        private double deceleration; // m/s²
        private double decelerationTime; // s
        private double decelerationDistance; // m

        private BinaryEventInstance<MobileEntity, ISimulationNode> nextArrivalInstance;

        #endregion
        #region prop

        #region simevents

        /// <summary>
        /// This event will be raised when the entity reaches the final node of a drive process
        /// </summary>
        public BinaryEvent<MobileEntity, ISimulationNode> ArrivalEvent { get; private set; }

        /// <summary>
        /// This event will be raised when the entity reaches the final node of a drive process
        /// </summary>
        public BinaryEvent<IMovable, Point> StoppedEvent { get; private set; }

        /// <summary>
        /// This event will be raised when the entity passes through a node during a drive process.
        /// The final node will not raise this event but the ArrivalEvent instead.
        /// </summary>
        public BinaryEvent<MobileEntity, ISimulationNode> PassThroughEvent { get; private set; }

        #endregion
        #region main

        /// <summary>
        /// please use a positive number in m/s²
        /// </summary>
        public double Acceleration
        {
            get { return acceleration; }
            set
            {
                acceleration = value;
                accelerationTime = vMax / acceleration; // time to vMax
                accelerationDistance = 0.5 * acceleration * Math.Pow(accelerationTime, 2); // distance to vMax
            }
        }

        /// <summary>
        /// please use a positive number in m/s²
        /// </summary>
        public double Deceleration
        {
            get { return deceleration; }
            set 
            {
                deceleration = value;
                decelerationTime = vMax / deceleration; // time from vMax to v0
                decelerationDistance = 0.5 * deceleration * Math.Pow(decelerationTime, 2); // distance from vMax to v0
            }
        }

        /// <summary>
        /// please use a positive number in m/s
        /// </summary>
        public double VMax
        {
            get { return vMax; }
            set 
            { 
                vMax = value;
                accelerationTime = vMax / acceleration; // time to vMax
                accelerationDistance = 0.5 * acceleration * Math.Pow(accelerationTime, 2); // distance to vMax
                decelerationTime = vMax / deceleration; // time from vMax to v0
                decelerationDistance = 0.5 * deceleration * Math.Pow(decelerationTime, 2); // distance from vMax to v0
            }
        }

        /// <summary>
        /// returns true if the drive unit is currently driving
        /// </summary>
        public bool IsDriving { get; private set; }

        /// <summary>
        /// The node on which the entity starts.
        /// </summary>
        public ISimulationNode InitialNode { get; set; }

        public bool IsDriveUnitInitialized { get; private set; }

        public Point LastKnownPosition { get; set; }

        public bool DeleteTemporaryNodesOnArrival { get; set; }

        public bool DeleteTemporaryConnectionsOnArrival { get; set; }

        public bool DeleteTemporaryNodesOnPassThrough { get; set; }

        public bool DeleteTemporaryConnectionsOnPassThrough { get; set; }

        #endregion
        #region network status

        public bool IsOnNode { get; private set; }

        public ISimulationNode PreviousNode { get; private set; }
        public ISimulationNode CurrentNode { get; set; }
        public ISimulationNode NextNode { get; private set; }

        public bool IsOnPath { get; private set; }

        public ISimulationPath CurrentPath { get; set; }
        public ISimulationConnection CurrentConnection { get { return GetCurrentConnection(); } }
        public ISimulationConnection PreviousConnection { get; set; }

        public bool IsOnConnection { get; private set; }

        #endregion
        #region temporary driving parameters

        /// <summary>
        /// the point in time at which the current driveing task was started
        /// the unit depends on the model
        /// </summary>
        public double DrivingStartTime { get; private set; }

        /// <summary>
        /// temporary, only valid while driving, seconds
        /// The value will not be updated during the drive.
        /// </summary>
        public double TimeToVMax { get; private set; }

        /// <summary>
        /// temporary, only valid while driving, meters
        /// The value will not be updated during the drive.
        /// </summary>
        public double DistanceToVMax { get; private set; }

        /// <summary>
        /// temporary, only valid while driving, seconds
        /// The value will not be updated during the drive.
        /// </summary>
        public double TimeToDeceleration { get; private set; }

        /// <summary>
        /// temporary, only valid while driving, meters
        /// The value will not be updated during the drive.
        /// </summary>
        public double DistanceToDeceleration { get; private set; }

        /// <summary>
        /// temporary, only valid while driving, seconds
        /// </summary>
        public double TimeAtVMax { get; private set; }

        /// <summary>
        /// temporary, only valid while driving, meters
        /// </summary>
        public double DistanceAtVMax { get; private set; }

        /// <summary>
        /// temporary, only valid while driving, seconds
        /// The value will not be updated during the drive.
        /// </summary>
        public double TimeToTarget { get; private set; }

        /// <summary>
        /// temporary, only valid while driving, meters
        /// The value will not be updated during the drive.
        /// </summary>
        public double DistanceToTarget { get; private set; }

        #endregion
        #region calculated driving params

        /// <summary>
        /// the time which has passed since the current driving task was started
        /// </summary>
        public double TimePassedDriving
        {
            get
            {
                if (IsDriving) return (Model.CurrentTime - DrivingStartTime).ToTimeSpan().TotalSeconds;
                else return double.NaN;
            }
        }

        /// <summary>
        /// the time which has passed since vmax was reached. 
        /// if the unit is currently still accelerating or not
        /// driving at all double.NaN will be returned.
        /// </summary>
        public double TimePassedSinceVMax
        {
            get
            {
                if (!IsDriving || IsAccelerating) return double.NaN;
                return TimePassedDriving - TimeToVMax;
            }
        }

        /// <summary>
        /// the time which has passed since deceleration started. 
        /// if the unit is not yet decelerating or not
        /// driving at all double.NaN will be returned.
        /// </summary>
        public double TimePassedSinceDeceleration
        {
            get
            {
                if (!IsDecelerating) return double.NaN;
                return TimePassedDriving - (TimeToVMax + TimeAtVMax);
            }
        }

        /// <summary>
        /// will only return true while the unit is 
        /// driving at vmax. as soon as the deceleration
        /// starts this will return false again.
        /// </summary>
        /// <returns></returns>
        public bool IsAtVMax
        {
            get
            {
                if (!IsDriving) return false;
                return TimePassedDriving >= TimeToVMax && TimePassedDriving < TimeToDeceleration;
            }
        }

        public bool IsAccelerating
        {
            get
            {
                if (!IsDriving) return false;
                return TimePassedDriving < TimeToVMax;
            }
        }

        public bool IsDecelerating
        {
            get
            {
                if (!IsDriving) return false;
                return TimePassedDriving >= TimeToDeceleration;
            }
        }

        #endregion

        #endregion
        #region ctor

        public MobileEntity() : base() { }

        public MobileEntity(IModel model,
                         string id = "",
                         string name = "",
                         ISimulationNode startNode = null,
                         double vMax = 1,
                         double acceleration = 1,
                         double deceleration = 1,
                         Action<Task> notifyTaskFinished = null,
                         Action<Task> notifyTaskStarted = null,
                         Action<Task> notifyTaskStartFailed = null,
                         List<string> states = null,
                         List<Tuple<string, string>> transitions = null,
                         string initialState = "",
                         Point initialPosition = null,
                         IResourceManager manager = null,
                         IEntity currentHolder = null)
            : base(model, id, name, notifyTaskFinished, notifyTaskStarted, notifyTaskStartFailed,
                   states, transitions, initialState, initialPosition, manager, currentHolder)
        {
            InitializeDriveUnit(startNode, vMax, acceleration, deceleration);
        }

        public MobileEntity(IModel model,
                         int seedID,
                         string id = "",
                         string name = "",
                         ISimulationNode startNode = null,
                         double vMax = 1,
                         double acceleration = 1,
                         double deceleration = 1,
                         Action<Task> notifyTaskFinished = null,
                         Action<Task> notifyTaskStarted = null,
                         Action<Task> notifyTaskStartFailed = null,
                         List<string> states = null,
                         List<Tuple<string, string>> transitions = null,
                         string initialState = "",
                         Point initialPosition = null,
                         IResourceManager manager = null,
                         IEntity currentHolder = null)
            : base(model, seedID, id, name, notifyTaskFinished, notifyTaskStarted, notifyTaskStartFailed,
                   states, transitions, initialState, initialPosition, manager, currentHolder)
        {
            InitializeDriveUnit(startNode, vMax, acceleration, deceleration);
        }

        #endregion
        #region init

        /// <summary>
        /// 
        /// </summary>
        /// <param name="startNode">will be the current node of this object until it is changed manually or by drive</param>
        /// <param name="vmax">m/s</param>
        /// <param name="acceleration">m/s² as positive number</param>
        /// <param name="deceleration">m/s² as positive number</param>
        public void InitializeDriveUnit(ISimulationNode startNode = null, double vMax = 1, double acceleration = 1, double deceleration = 1)
        {
            if (IsDriveUnitInitialized) throw new InitializationException();

            StoppedEvent = new BinaryEvent<IMovable, Point>(this.EntityName + ".Stopped");
            ArrivalEvent = new BinaryEvent<MobileEntity, ISimulationNode>(this.EntityName + ".Arrived");
            ArrivalEvent.AddHandler(InternalArrivalHandler, new Priority(type: Simulation.Enum.PriorityType.LowLevelBeforeOthers));

            PassThroughEvent = new BinaryEvent<MobileEntity, ISimulationNode>(this.EntityName + ".PassedThrough");

            PassThroughEvent.AddHandler(InternalBeforePassThroughHandler, new Priority(type: PriorityType.LowLevelBeforeOthers));
            PassThroughEvent.AddHandler(InternalAfterPassThroughHandler, new Priority(type: PriorityType.LowLevelAfterOthers));

            CurrentNode = startNode;
            IsOnNode = startNode != null;

            this.InitialNode = startNode;
            this.acceleration = acceleration;
            this.deceleration = deceleration;
            this.vMax = vMax;

            accelerationTime = vMax / acceleration; // time to vMax
            accelerationDistance = 0.5 * acceleration * Math.Pow(accelerationTime, 2); // distance to vMax
            decelerationTime = vMax / deceleration; // time from vMax to v0
            decelerationDistance = 0.5 * deceleration * Math.Pow(decelerationTime, 2); // distance from vMax to v0

            IsDriveUnitInitialized = true;
        }

        #endregion
        #region hand

        private void InternalBeforePassThroughHandler(MobileEntity sender, ISimulationNode node)
        {
            IsOnNode = true;
            PreviousNode = CurrentPath.GetNodeBefore(node);
            CurrentNode = node;
            NextNode = CurrentPath.GetNodeAfter(node);
            PreviousConnection = PreviousNode.GetSimulationConnection(CurrentNode);
        }

        private void InternalArrivalHandler(MobileEntity sender, ISimulationNode targetNode)
        {
            DrivingStartTime = double.NaN;
            TimeToVMax = double.NaN;
            DistanceToVMax = double.NaN;
            TimeToDeceleration = double.NaN;
            DistanceToDeceleration = double.NaN;
            TimeAtVMax = double.NaN;
            DistanceAtVMax = double.NaN;
            TimeToTarget = 0;
            DistanceToTarget = 0;

            IsOnNode = true;
            if (CurrentPath != null)
            {
                PreviousNode = CurrentPath.GetNodeBefore(targetNode);
                PreviousConnection = PreviousNode.GetSimulationConnection(targetNode);
            }
            CurrentNode = targetNode;
            NextNode = null;
            IsOnPath = false;
            CurrentPath = null;
            IsOnConnection = false;
            IsDriving = false;

            if (DeleteTemporaryConnectionsOnArrival && PreviousConnection is Connection && (PreviousConnection as Connection).IsTemporary) 
            {
                // TODO: remove from nodes, network(s) and path(s)
                PreviousConnection = null; // maybe not such a good idea as the reference will be overwritten anyways?
            }
            if (DeleteTemporaryNodesOnArrival && PreviousNode is Node && (PreviousNode as Node).IsTemporary) 
            {
                // TODO: remove connections
                PreviousNode = null; // maybe not such a good idea as the reference will be overwritten anyways?
            }
        }

        private void InternalAfterPassThroughHandler(MobileEntity sender, ISimulationNode node)
        {
            if (DeleteTemporaryConnectionsOnPassThrough && PreviousConnection is Connection && (PreviousConnection as Connection).IsTemporary) 
            {
                // TODO: remove from nodes, network(s) and path(s)
                PreviousConnection = null; // maybe not such a good idea as the reference will be overwritten anyways?
            }
            if (DeleteTemporaryNodesOnPassThrough && PreviousNode is Node && (PreviousNode as Node).IsTemporary) 
            {
                // TODO: remove connections
            }
            IsOnNode = false;
            PreviousNode = CurrentNode;
            CurrentNode = null;
        }

        #endregion
        #region impl

        #region drive

        /// <summary>
        /// CAUTION: this may invoke the network's FindPath method.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool DriveTo(ISimulationNode target)
        {
            ISimulationPath path = null;

            if (target.Network != null)
                path = target.Network.FindPath(CurrentNode, target) as ISimulationPath;
            
            if (path == null) 
                return DriveVia(CurrentNode.ConnectTo(target) as ISimulationConnection);
            else 
                return DriveVia(path);
        }

        /// <summary>
        /// drive over the given connection
        /// </summary>
        /// <param name="con"></param>
        /// <returns></returns>
        public bool DriveVia(ISimulationConnection con)
        {
            if (Position.Equals(con.Node1.Position)) return DriveVia(Path.Create(con.Node1, con.Node2));
            else return DriveVia(Path.Create(con.Node2, con.Node1));
        }

        /// <summary>
        /// Drive over the given path.
        /// <para />
        /// ^            a___b              <para />
        /// |           /     \             <para />
        /// |        /         \            <para />
        /// |     /             \           <para />
        /// |  /                 \          <para />
        /// 0---------------------c--->     <para />
        /// <para />
        ///  a = TimeToVMax, DistanceToVMax                     <para />
        ///  b = TimeToDeceleration, DistanceToDeceleration     <para />
        /// ab = TimeAtVMax, DistanceAtVMax                     <para />
        ///  c = TimeToTarget, DistanceToTarget                 <para />
        /// <para />
        /// in case of a triangular profile where vMax will not be reached: <para />
        /// <para />
        ///       ax + 0 = dx + dl          <para />
        ///      ax - dl = dx               <para />
        ///          -dl = (d-a)x           <para />
        ///  -dl / (d-a) = x                <para />
        /// 
        ///  e.g.: accel. = 2, deceler. = 3 (here -3, in code +3) <para />
        ///       x2 = 21 / (-3-2) = 4.2
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public virtual bool DriveVia(ISimulationPath path)
        {
            if (path != null && !path.IsEmpty && path.Length > 0 && path.FirstNode == CurrentNode)
            {
                double totalTime = 0;
                if (path.Length < accelerationDistance + decelerationDistance) // not enough space to reach vMax
                {
                    double distanceToVMax = (path.Length * deceleration) / (deceleration + acceleration); // correct
                    double distanceToStop = path.Length - distanceToVMax;
                    //actualVMax = vMax * (distanceToVMax / accelerationDistance);
                    actualVMax = acceleration * Math.Sqrt((2d * distanceToVMax) / acceleration);

                    double timeToVMax = actualVMax / acceleration;
                    double timeToT0 = actualVMax / deceleration;
                    totalTime = timeToVMax + timeToT0;

                    TimeToVMax = timeToVMax;
                    DistanceToVMax = distanceToVMax;
                    TimeToDeceleration = timeToVMax;
                    DistanceToDeceleration = distanceToVMax;
                    TimeAtVMax = 0;
                    DistanceAtVMax = 0;
                    TimeToTarget = totalTime;
                    DistanceToTarget = path.Length;
                }
                else // vMax will be reached
                {
                    double distanceAtVMax = path.Length - (accelerationDistance + decelerationDistance);
                    double timeAtVMax = distanceAtVMax / vMax;
                    actualVMax = vMax;
                    totalTime = accelerationTime + timeAtVMax + decelerationTime;

                    TimeToVMax = accelerationTime; // accelerationDistance * acceleration;
                    DistanceToVMax = accelerationDistance;
                    TimeToDeceleration = TimeToVMax + timeAtVMax;
                    DistanceToDeceleration = accelerationDistance + distanceAtVMax;
                    TimeAtVMax = timeAtVMax;
                    DistanceAtVMax = distanceAtVMax;
                    TimeToTarget = totalTime;
                    DistanceToTarget = path.Length;
                }

                DrivingStartTime = Model.CurrentTime;

                // TODO: reservation mechanism for paths or single connections?

                double arrivalTime = (TimeSpan.FromSeconds(totalTime)).ToDouble();
                nextArrivalInstance = ArrivalEvent.GetInstance(this, path.LastNode);
                Model.AddEvent(arrivalTime, nextArrivalInstance);
                Model.AddEvent(arrivalTime, StoppedEvent.GetInstance(this, path.LastNode.Position));

                IsOnConnection = true;
                IsOnNode = false;
                PreviousNode = CurrentNode;
                CurrentNode = null; // NextNode will be set in the following loop
                IsOnPath = true;
                CurrentPath = path;
                IsDriving = true;

                if (path.NodeCount > 2) // precalculate the pass-through time for each node on the path.
                {
                    double dist = 0;
                    ISimulationNode previousNode = null;
                    foreach (ISimulationNode n in path.SimulationNodes)
                    {
                        if (n == path.LastNode) break;              // ignore last node (has arrival event)
                        if (previousNode == null)
                        {
                            previousNode = n;                       // ignore first node (we are already there)
                        }
                        else                                        // we have a node in the middle of the path
                        {
                            if (NextNode == null) NextNode = n;

                            IConnection<double> con = previousNode.GetConnection(n);
                            dist += con.Length; // = path.GetDistanceTo(n);
                            double time = 0;

                            if (dist <= DistanceToVMax) // node will be reached during acceleration
                            { // t = sqrt(2s/a)
                                time = Math.Sqrt((2d * dist) / acceleration);
                            }
                            else if (dist <= DistanceToDeceleration) // node will be reached while at vmax
                            { // this is unreachable in a triangle profile because in that case DistanceToDeceleration == DistanceToVMax
                                time = TimeToVMax + (dist - DistanceToVMax) / vMax;
                            }
                            else // node will be reached during deceleration
                            { // t = t0 + sqrt(2s/a)
                                double actualDecelerationDistance = path.Length - (DistanceToVMax + DistanceAtVMax);
                                double deceleratingFractionToNode = dist - DistanceToDeceleration;
                                time = totalTime - Math.Sqrt((2d * (actualDecelerationDistance - deceleratingFractionToNode)) / deceleration);
                            }
                            
                            Model.AddEvent((TimeSpan.FromSeconds(time)).ToDouble(), PassThroughEvent.GetInstance(this, n));

                            previousNode = n;
                        }
                    }
                }

            }
            return false;
        }

        /// <summary>
        /// Drive over a clone of the given path with some nodes replaced by offset nodes.
        /// Use the OriginalNode / OriginalConnection properties to access the originals.
        /// </summary>
        /// <param name="offsets"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public virtual bool DriveVia(Dictionary<Node, Vector> offsets, Path path)
        {
            Path clonedPath = path.Clone() as Path;
            foreach (KeyValuePair<Node, Vector> kvp in offsets)
            {
                clonedPath.ReplaceNode(kvp.Key, kvp.Key.CreateOffsetNode(kvp.Value, true));
            }
            return DriveVia(clonedPath);
        }

        /// <summary>
        /// drive from fromNode to toNode
        /// this requires a connection between the nodes.
        /// </summary>
        /// <param name="fromNode"></param>
        /// <param name="toNode"></param>
        /// <returns></returns>
        public bool Drive(ISimulationNode fromNode, ISimulationNode toNode)
        {
            return DriveVia(Path.Create(fromNode, toNode));
        }

        /// <summary>
        /// drive from fromPosition to toPosition
        /// temporary nodes will be created for both 
        /// starting and ending positions
        /// </summary>
        /// <param name="fromNode"></param>
        /// <param name="toNode"></param>
        /// <returns></returns>
        public bool Drive(Point fromPosition, Point toPosition)
        {
            Node n1 = new Node(position: fromPosition);
            n1.IsTemporary = true;
            Node n2 = new Node(position: toPosition);
            n2.IsTemporary = true;
            Connection con = n1.ConnectTo(n2) as Connection;
            con.IsTemporary = true;
            return DriveVia(con);
        }

        #endregion
        #region calculations

        /// <summary>
        /// calculate the distance which has been covered since
        /// the current drive task has been started. In case of 
        /// asynchronity problems or if the entity is not driving,
        /// this will return double.NaN
        /// </summary>
        /// <returns></returns>
        public virtual double GetPassedDrivingDistance()
        {
            double result = 0;

            if (IsAccelerating)
            {
                result = 0.5 * acceleration * Math.Pow(TimePassedDriving, 2);
            }
            else if (IsAtVMax)
            {
                result = DistanceToVMax + TimePassedSinceVMax * actualVMax;
            }
            else if (IsDecelerating)
            {
                result = DistanceToTarget - (0.5 * deceleration * Math.Pow((TimeToTarget - TimeToDeceleration) - TimePassedSinceDeceleration, 2));
            }
            else
            {
                return double.NaN;
            }

            if (Double.IsNaN(result)) return result;
            return Math.Min(DistanceToTarget, result);
        }

        /// <summary>
        /// calculates the current speed
        /// </summary>
        /// <returns></returns>
        public double GetCurrentSpeed()
        {
            if (IsAtVMax) return vMax;
            else if (IsAccelerating) return TimePassedDriving * acceleration;
            else if (IsDecelerating) return vMax - (TimePassedSinceDeceleration * deceleration);
            else return 0;
        }

        /// <summary>
        /// calculates the connection on which the drive unit currently drives in O(n)
        /// </summary>
        /// <returns></returns>
        public ISimulationConnection GetCurrentConnection()
        {
            if (!IsDriving) return null;
            double sumLengths = 0;
            ISimulationConnection result = null;
            foreach (ISimulationConnection con in CurrentPath.SimulationConnections)
            {
                if (sumLengths + con.Length > GetPassedDrivingDistance())
                {
                    result = con;
                    break;
                }
                sumLengths += con.Length;
            }
            return result;
        }

        /// <summary>
        /// Calculates the absolute position of the drive unit in O(n) with n = number of nodes in the current path
        /// </summary>
        /// <returns></returns>
        public Point GetAbsolutePosition()
        {
            //the following three lines may produce contradictory results in asynchronous calls
            bool tmpIsDriving = IsDriving;
            ISimulationNode tmpCurrentNode = CurrentNode;
            ISimulationPath tmpCurrentPath = CurrentPath;
            
            // create an "emergency"-result for when we run in trouble due to asynchronous calls
            Point nonDrivingResult = tmpCurrentNode == null ? null : tmpCurrentNode.Position;

            // if we were not driving at the beginning of this method call we will return the nonDrivingResult
            if (!tmpIsDriving || tmpCurrentPath == null) return nonDrivingResult;

            // otherwise try to get the passed distance.
            double passedDist = GetPassedDrivingDistance();
            if (Double.IsNaN(passedDist)) return nonDrivingResult;

            // calculate current connection
            double sumLengths = 0;
            ISimulationNode tmpPreviousNode = null;
            ISimulationConnection tmpCurrentConnection = null;

            foreach (ISimulationNode n in tmpCurrentPath.SimulationNodes)
            {
                if (tmpPreviousNode == null) tmpPreviousNode = n;
                else
                {
                    ISimulationConnection con = tmpPreviousNode.GetConnection(n) as ISimulationConnection;
                    if (sumLengths + con.Length >= passedDist)
                    {
                        tmpCurrentConnection = con;
                        break;
                    }
                    sumLengths += con.Length;
                    tmpPreviousNode = n;
                }
            }

            if (tmpCurrentConnection == null) return nonDrivingResult;

            // calculate & return driving result
            double relPos = (passedDist - sumLengths) / tmpCurrentConnection.Length;
            if (tmpPreviousNode == tmpCurrentConnection.Node1)
                return tmpPreviousNode.Position + (Point)(tmpCurrentConnection.GetVector() * relPos);
            else
                return tmpPreviousNode.Position - (Point)(tmpCurrentConnection.GetVector() * relPos);
        }

        /// <summary>
        /// This is a wrapper for GetAbsolutePosition() to allow asynchronous access. In 
        /// case of a null value in GetAbsolutePosition, this will return LastKnownPosition.
        /// </summary>
        /// <returns></returns>
        public Point GetVisualPosition()
        {
            Point currentResult = GetAbsolutePosition();
            if (currentResult != null)
            {
                LastKnownPosition = currentResult;
                return currentResult;
            }
            return LastKnownPosition;
        }

        /// <summary>
        /// calculates the currently covered distance on the current connection in O(n)
        /// </summary>
        /// <returns></returns>
        public double GetAbsolutePositionOnConnection()
        {
            if (!IsDriving) return double.NaN;

            double sumLengths = 0;
            ISimulationConnection curCon = null;
            double passedDist = GetPassedDrivingDistance();

            foreach (ISimulationConnection con in CurrentPath.SimulationConnections)
            {
                if (sumLengths + con.Length > passedDist)
                {
                    curCon = con;
                    break;
                }
                sumLengths += con.Length;
            }

            return passedDist - sumLengths;
        }

        /// <summary>
        /// calculates the currently covered distance on the current connection in O(n)
        /// </summary>
        /// <returns></returns>
        public double GetRelativePositionOnConnection()
        {
            if (!IsDriving) return double.NaN;

            double sumLengths = 0;
            ISimulationConnection curCon = null;
            double passedDist = GetPassedDrivingDistance();

            foreach (ISimulationConnection con in CurrentPath.SimulationConnections)
            {
                if (sumLengths + con.Length > passedDist)
                {
                    curCon = con;
                    break;
                }
                sumLengths += con.Length;
            }

            return (passedDist - sumLengths) / curCon.Length;
        }

        #endregion
        #region IMovable

        public void MoveTo(Point position)
        {
            DriveTo(new Node(position: position));
        }

        public void StartMoving(Vector direction)
        {
            throw new NotImplementedException("A target has to be defined. Use MovableEntity for free driving.");
        }

        public void StopMoving()
        {
            throw new NotImplementedException("This entity can only handle predefined movement. Use MovableEntity for free driving.");
        }

        #endregion

        #endregion
    }
}