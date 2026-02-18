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
    /// <summary>
    /// The movable entity is one of the most complex base entities. It inherits TaskMachineEntity and provides support for movement including 
    /// acceleration and deceleration.
    /// 
    /// The movement of a movable entity is not approximated via sampling. Instead the movable object keeps track of the point in time at which 
    /// acceleration was started and calculates its current speed and location based on the speed profile only on request. This is a good
    /// example for a combination of discrete and continuous concepts. By abstaining from discretizing linear movements or other linear 
    /// processes a lot of computing resources can be saved, especially if the simulation is executed without animation. 
    /// 
    /// The movable entity has no built-in collision handling. Collisions can either be managed by using paths like resources and implementing 
    /// a reservation mechanism or by iterating over all movable units with collision potential whenever a drive task is started or changed.
    /// </summary>
    [Serializable]
    public class MovableEntity : TaskMachineEntity, IMovable
    {
        #region over

        public override void Reset()
        {
            base.Reset();
            startPosition = initialPosition;
            nextArrivalInstance = null;
            
            IsDriving = false;
            IsDecelerating = false;
            IsTargetKnown = false;
            decelerationStartTime = 0;

            DrivingStartTime = double.NaN;
            TimeToVMax = double.NaN;
            DistanceToVMax = double.NaN;
            TargetPosition = null;

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
                startPosition = value;
            }
        }

        #endregion
        #region cvar

        private double vMax; // m/s
        private double acceleration; // m/s²
        private double accelerationTime; // s
        private double accelerationDistance; // m
        private double deceleration; // m/s²
        private double decelerationTime; // s
        private double decelerationDistance; // m

        private double decelerationStartTime;

        private Vector direction;
        private Point startPosition;
        private Point initialPosition;

        private BinaryEventInstance<MovableEntity, Point> nextArrivalInstance;

        #endregion
        #region prop

        #region main

        /// <summary>
        /// This event will be raised when the entity reaches the final position of a drive process
        /// </summary>
        public BinaryEvent<MovableEntity, Point> ArrivalEvent { get; private set; }

        /// <summary>
        /// This event will be raised when the entity reaches the final position of a drive process
        /// </summary>
        public BinaryEvent<IMovable, Point> StoppedEvent { get; private set; }

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

        public bool IsDriveUnitInitialized { get; private set; }

        internal Point LastKnownPosition { get; set; }

        internal bool IsTargetKnown { get; set; }

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

        ///// <summary>
        ///// temporary, only valid while driving, seconds
        ///// The value will not be updated during the drive.
        ///// </summary>
        //public double TimeToTarget { get; private set; }

        /// <summary>
        /// The distance between the start position and the stopping position.
        /// Temporary value, only valid while driving.
        /// The value will not be updated during the drive.
        /// </summary>
        public double DistanceToTarget { get; private set; }

        /// <summary>
        /// If known the position where the entity will stop.
        /// </summary>
        public Point TargetPosition { get; private set; }

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
                return (Model.CurrentTime - decelerationStartTime).ToTimeSpan().TotalSeconds;
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
                if (!IsDriving || IsDecelerating) return false;
                return TimePassedDriving >= TimeToVMax;
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

        public bool IsDecelerating { get; set; }

        /// <summary>
        /// Returns double.NaN if the entity is not driving or the target position is not known yet.
        /// </summary>
        public double CurrentDistanceToTarget
        {
            get
            {
                if (TargetPosition == null || !IsDriving) return double.NaN;
                return (TargetPosition - this.Position).ToVector().Length();
            }
        }

        #endregion

        #endregion
        #region ctor

        public MovableEntity() : base() { }

        public MovableEntity(IModel model,
                         string id = "",
                         string name = "",
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
            InitializeDriveUnit(initialPosition, vMax, acceleration, deceleration);
        }

        public MovableEntity(IModel model,
                         int seedID,
                         string id = "",
                         string name = "",
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
            InitializeDriveUnit(initialPosition, vMax, acceleration, deceleration);
        }

        #endregion
        #region init

        public override void Initialize(IModel model, IEntityInitializationParams parameters)
        {
            IMovableEntityInitializationParams initParams = parameters as IMovableEntityInitializationParams;
            if (initParams == null) throw new ArgumentException("You must use an IMovableEntityInitializationParams instance to initialize a MovableEntity.");
            Initialize(model, initParams);
        }

        public void Initialize(IModel model, IMovableEntityInitializationParams parameters)
        {
            base.Initialize(model, parameters);
            InitializeDriveUnit(base.initialPosition, parameters.VMax, parameters.Acceleration, parameters.Deceleration);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="startNode">will be the current node of this object until it is changed manually or by drive</param>
        /// <param name="vmax">m/s</param>
        /// <param name="acceleration">m/s² as positive number</param>
        /// <param name="deceleration">m/s² as positive number</param>
        public void InitializeDriveUnit(Point initialPosition = null, double vMax = 1, double acceleration = 1, double deceleration = 1)
        {
            if (IsDriveUnitInitialized) throw new InitializationException();

            ArrivalEvent = new BinaryEvent<MovableEntity, Point>(this.EntityName + ".Arrived");
            StoppedEvent = new BinaryEvent<IMovable , Point>(this.EntityName + ".Stopped");
            ArrivalEvent.AddHandler(InternalArrivalHandler, new Priority(type: Simulation.Enum.PriorityType.LowLevelBeforeOthers));

            this.initialPosition = initialPosition == null ? new Point(0, 0, 0) : initialPosition;
            this.startPosition = initialPosition;
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

        private void InternalArrivalHandler(MovableEntity sender, Point target)
        {
            DrivingStartTime = double.NaN;
            TimeToVMax = double.NaN;
            DistanceToVMax = double.NaN;
            // TimeToTarget = 0;
            DistanceToTarget = 0;
            TargetPosition = null;

            startPosition = target;
            IsDriving = false;
            IsDecelerating = false;
        }

        #endregion
        #region impl

        #region drive

        /// <summary>
        /// Initiate deceleration of this entity.
        /// </summary>
        public virtual void Stop()
        {
            Point currentPosition = GetAbsolutePosition();
            IsDecelerating = true;
            decelerationStartTime = Model.CurrentTime;
            TargetPosition = currentPosition + (direction * decelerationDistance); // TODO: ████ incorrect, must use actual deceleration distance if vMax wasn't reached yet!
            DistanceToTarget = ((Vector)(TargetPosition - startPosition)).Length();
            double arrivalTime = (TimeSpan.FromSeconds(decelerationTime)).ToDouble(); // TODO: ████ incorrect, must use actual deceleration time if vMax wasn't reached yet!
            nextArrivalInstance = ArrivalEvent.GetInstance(this, TargetPosition);
            Model.AddEvent(arrivalTime, nextArrivalInstance);
            Model.AddEvent(arrivalTime, StoppedEvent.GetInstance(this, TargetPosition));
            IsTargetKnown = true;
        }

        /// <summary>
        /// Drive over the given path. The direction vector need not be normalized.
        /// <para />
        /// ^            a__(...)__b              <para />
        /// |           /           \             <para />
        /// |        /               \            <para />
        /// |     /                   \           <para />
        /// |  /                       \          <para />
        /// 0---------------------------c--->     <para />
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
        public virtual void Start(Vector direction)
        {
            if (direction != null && direction.Length() > 0)
            {
                
                this.direction = Vector.AsNormalized(direction);

                TimeToVMax = accelerationTime;          // only if acceleration is not interrupted!
                DistanceToVMax = accelerationDistance;  // only if acceleration is not interrupted!
                DrivingStartTime = Model.CurrentTime;

                IsDriving = true;
                IsTargetKnown = false;
            }
            else
            {
                throw new ArgumentException("The direction vector must not be null and it's length must be > 0.");
            }
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
                result = DistanceToVMax + TimePassedSinceVMax * vMax;
            }
            else if (IsDecelerating)
            {
                result = DistanceToTarget - (0.5 * deceleration * Math.Pow((decelerationTime) - TimePassedSinceDeceleration, 2));
            }
            else
            {
                return double.NaN;
            }

            return result;
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
        /// Calculates the absolute position of the drive unit in O(n) with n = number of nodes in the current path
        /// </summary>
        /// <returns></returns>
        public virtual Point GetAbsolutePosition()
        {
            if (!IsDriving) return startPosition;

            // otherwise try to get the passed distance.
            double passedDist = GetPassedDrivingDistance();
            if (Double.IsNaN(passedDist)) return startPosition;

            return startPosition + (direction * passedDist);
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

        #endregion
        #region IMovable

        public void MoveTo(Point position)
        {
            double timeToDeceleration;
            Vector dir = position - startPosition;

            if (dir.Length() < accelerationDistance + decelerationDistance) // not enough space to reach vMax
            {
                double distanceToVMax = (dir.Length() * deceleration) / (deceleration + acceleration); // correct
                double actualVMax = acceleration * Math.Sqrt((2d * distanceToVMax) / acceleration);
                timeToDeceleration = actualVMax / acceleration;
            }
            else // vMax will be reached
            {
                double distanceAtVMax = dir.Length() - (accelerationDistance + decelerationDistance);
                double timeAtVMax = distanceAtVMax / vMax;
                timeToDeceleration = accelerationTime + timeAtVMax;
            }

            StartMoving(dir);
            Model.Schedule((TimeSpan.FromSeconds(timeToDeceleration)).ToDouble(), () => Stop());
        }

        public void StartMoving(Vector direction)
        {
            Start(direction);
        }

        public void StopMoving()
        {
            Stop();
        }

        #endregion

        ///// <summary>
        ///// This method tries to predict the time necessary to pass the given distance.
        ///// CAUTION: may return double.PositiveInfinity
        ///// </summary>
        ///// <param name="distance"></param>
        ///// <returns>CAUTION: may return double.PositiveInfinity</returns>
        //public double PredictTimeForDistance(double distance)
        //{
        //    if (!IsDriving) return double.PositiveInfinity;
        //    if (IsDecelerating)
        //    {
        //        if (CurrentDistanceToTarget < distance) // the entity will be stopped before the distance is passed.
        //            return double.PositiveInfinity;
        //        else // the distance will be passed during the deceleration phase. 
        //        {
        //            // t = t0 + sqrt(2s/a)
        //            double actualDecelerationDistance = decelerationDistance; // TODO: ████ incorrect, must use actual deceleration distance if vMax wasn't reached yet!
        //            double distancePassedSinceDeceleration = actualDecelerationDistance - CurrentDistanceToTarget;
        //            double deceleratingFractionToNode = distancePassedSinceDeceleration + distance;
        //            double totalTime = 0; // TODO: ███ calculate this efficiently
        //            return totalTime - Math.Sqrt((2d * (actualDecelerationDistance - deceleratingFractionToNode)) / deceleration); // subtract current time
        //        }
        //    }
        //    else if (IsAccelerating)
        //    {
        //        // TODO: ███ crack this darn beginner's math problem!
        //        /*
        //         if (dist <= DistanceToVMax) // node will be reached during acceleration
        //                { // t = sqrt(2s/a)
        //                    time = Math.Sqrt((2d * dist) / acceleration);
        //                }
        //                else if (dist <= DistanceToDeceleration) // node will be reached while at vmax
        //                { // this is unreachable in a triangle profile because in that case DistanceToDeceleration == DistanceToVMax
        //                    time = TimeToVMax + (dist - DistanceToVMax) / vMax;
        //                }
        //                else // node will be reached during deceleration
        //                { 
        //                }
        //         */
        //        if (!IsTargetKnown)
        //        {
        //            // assuming vMax will be reached and kept indefinetely
        //            return 0;
        //        }
        //        else
        //        {
        //            return 0;
        //        }
        //    }
        //    else // currently driving at vMax
        //    {
        //        if (!IsTargetKnown)
        //        {
        //            // assuming vMax will be kept indefinetely
        //            double currentDistanceFromStart = 0;
        //            return TimeToVMax + (currentDistanceFromStart + distance) / vMax; // subtract current time
        //        }
        //        else
        //        {
        //            return 0;
        //        }
        //    }
        //}

        #endregion
    }
}