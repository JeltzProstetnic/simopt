using System;
using System.Collections.Generic;
using System.Linq;

using SimOpt.Basics.Datastructures.Geometry;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Events;
using SimOpt.Simulation.Interfaces;
using SimOpt.Simulation.Tools;

namespace SimOpt.Simulation.Templates
{
    [Serializable]
    public class Conveyor<T> : StateMachineEntity, 
                                IContainer, 
                                IItemBuffer<T>,
                                IElectiveBuffer<T, int>
        where T : class, IAttachable
    {
        #region over

        public override void Reset()
        {
            base.Reset();

            tmpAdvanceCount = 0;
            tmpRepeatAdvance = 0;
            tmpRecedeCount = 0;
            tmpRepeatRecede = 0;
            offset.Clear();
            indices.Clear();
            items.Clear();
            detachedItems.Clear();
            IsRunning = false;
        }

        #endregion
        #region cvar

        private int tmpAdvanceCount = 0;
        private int tmpRepeatAdvance = 0;
        private int tmpRecedeCount = 0;
        private int tmpRepeatRecede = 0;
        private Vector orientation;
        private Dictionary<T, Vector> offset = new Dictionary<T, Vector>();
        private Dictionary<T, int> indices = new Dictionary<T, int>();
        private Dictionary<int, T> items = new Dictionary<int, T>();
        private IMovable mobileElement;
        private List<T> detachedItems = new List<T>();

        #endregion
        #region prop

        #region Main

        public Vector Orientation
        {
            get { return orientation; }
            set { orientation = Vector.AsNormalized(value); }
        }

        public double Length { get; set; }

        public int NumberOfSections { get; set; }

        public double SectionLength { get { return Length / (double)NumberOfSections; } }

        public double FirstSectionOffset { get; set; }

        public Vector AbsoluteOffset { get; set; }

        public Dictionary<T, Vector> Offsets
        {
            get { return offset; }
        }

        public override bool IsInitialized
        {
            get
            {
                return base.IsInitialized && mobileElement != null;
            }
        }

        public IMovable MobileElement
        {
            get { return mobileElement; }
            set
            {
                if (mobileElement != null) 
                    mobileElement.StoppedEvent.RemoveHandler(StoppedHandler);

                mobileElement = value;

                if (mobileElement != null)
                    mobileElement.StoppedEvent.AddHandler(StoppedHandler, new Priority(type: Enum.PriorityType.LowLevelBeforeOthers));
            }
        }

        public bool IsRunning { get; private set; }

        public bool LastMovingDirectionBackwards { get; set; }

        public bool LastMovingDirectionForwards { get { return !LastMovingDirectionBackwards; } }

        public bool HasItemAtFirstPosition
        {
            get { return items.ContainsKey(0); }
        }

        public bool HasItemAtLastPosition
        {
            get
            {
                int maxID = NumberOfSections;
                return items.ContainsKey(maxID);
            }
        }

        public double LastMovingDistance { get; private set; }

        public int LastMovingCount { get; private set; }

        public double PositioningTolerance { get; set; }

        #endregion
        #region SimEvents

        //public BinaryEvent<Conveyor<T>, T> EndReachedEvent { get; private set; }
        public BinaryEvent<IMovable, Point> StoppedEvent { get { return mobileElement == null ? null : mobileElement.StoppedEvent; } }

        #endregion
        #region IItemBuffer<T>

        public int Count { get { return offset.Count; } }

        public bool IsFull { get { return Count == NumberOfSections; } }

        #endregion
        #region IContainer

        public IEnumerable<IAttachable> ContainedObjects
        {
            get { return offset.Keys; }
        }

        public bool AutoDetach { get; set; }

        #endregion

        #endregion
        #region ctor

        public Conveyor()
            : base()
        {
            Orientation = new Vector(1, 0);
            AbsoluteOffset = new Vector(0, 0);
            //EndReachedEvent = new BinaryEvent<Conveyor<T>, T>(this.EntityName + ".EndReached");
        }

        public Conveyor(IModel model,
                        string id = "",
                        string name = "",
                        Vector orientation = null,
                        double length = 100,
                        int numberOfSections = 10,
                        double firstSectionOffset = 5,
                        Vector absoluteOffset = null,
                        double vMax = 1, 
                        double acceleration = 1, 
                        double deceleration = 1,
                        List<string> states = null,
                        List<Tuple<string, string>> transitions = null,
                        string initialState = null,
                        Point initialPosition = null,
                        IResourceManager manager = null,
                        IEntity currentHolder = null)
            : base(model, id, name, states, transitions, initialState, initialPosition, manager, currentHolder)
        {
            mobileElement = new MovableEntity(model, id + "_internal_mobile_entity",
                name + "_internal_mobile_entity", 
                vMax, 
                acceleration, 
                deceleration,
                initialPosition: Position);
            mobileElement.StoppedEvent.AddHandler(StoppedHandler, new Priority(type: Enum.PriorityType.LowLevelBeforeOthers));

            this.Length = length;
            this.NumberOfSections = numberOfSections;
            this.FirstSectionOffset = firstSectionOffset;

            Orientation = orientation == null ? new Vector(1, 0) : orientation;
            AbsoluteOffset = absoluteOffset == null ? new Vector(0, 0) : absoluteOffset;

            //EndReachedEvent = new BinaryEvent<Conveyor<T>, T>(this.EntityName + ".EndReached");
        }

        public Conveyor(IModel model,
                        int seedID,
                        string id = "",
                        string name = "",
                        Vector orientation = null,
                        double length = 100,
                        int numberOfSections = 10,
                        double firstSectionOffset = 5,
                        Vector absoluteOffset = null,
                        double vMax = 1,
                        double acceleration = 1,
                        double deceleration = 1,
                        List<string> states = null,
                        List<Tuple<string, string>> transitions = null,
                        string initialState = null,
                        Point initialPosition = null,
                        IResourceManager manager = null,
                        IEntity currentHolder = null)
            : base(model, seedID, id, name, states, transitions, initialState, initialPosition, manager, currentHolder)
        {
            mobileElement = new MovableEntity(model, id + "_internal_mobile_entity",
                name + "_internal_mobile_entity",
                vMax,
                acceleration,
                deceleration,
                initialPosition: Position);
            mobileElement.StoppedEvent.AddHandler(StoppedHandler, new Priority(type: Enum.PriorityType.LowLevelBeforeOthers));

            this.Length = length;
            this.NumberOfSections = numberOfSections;
            this.FirstSectionOffset = firstSectionOffset;

            Orientation = orientation == null ? new Vector(1, 0) : orientation;
            AbsoluteOffset = absoluteOffset == null ? new Vector(0, 0) : absoluteOffset;

            //EndReachedEvent = new BinaryEvent<Conveyor<T>, T>(this.EntityName + ".EndReached");
        }

        #endregion
        #region init

        public override void Initialize(IModel model, IEntityInitializationParams parameters)
        {
            IConveyorInitializationParams initParams = parameters as IConveyorInitializationParams;
            if (initParams == null) throw new ArgumentException("You must use an IConveyorInitializationParams instance to initialize a Conveyor.");
            Initialize(model, initParams);
        }

        public void Initialize(IModel model, IConveyorInitializationParams parameters)
        {
            base.Initialize(model, parameters);

            if (parameters.MobileElement == null)
                mobileElement = new MovableEntity(model, Identifier + "_internal_mobile_entity",
                EntityName + "_internal_mobile_entity",
                parameters.VMax,
                parameters.Acceleration,
                parameters.Deceleration,
                initialPosition: base.Position);
            else mobileElement = parameters.MobileElement;

            mobileElement.StoppedEvent.AddHandler(StoppedHandler, new Priority(type: Enum.PriorityType.LowLevelBeforeOthers));

            this.Length = parameters.Length;
            this.NumberOfSections = parameters.NumberOfSections;
            this.FirstSectionOffset = parameters.FirstSectionOffset;

            Orientation = parameters.Orientation == null ? new Vector(1, 0) : parameters.Orientation;
            AbsoluteOffset = parameters.AbsoluteOffset == null ? new Vector(0, 0) : parameters.AbsoluteOffset;

            PositioningTolerance = parameters.PositioningTolerance;
            //EndReachedEvent = new BinaryEvent<Conveyor<T>, T>(this.EntityName + ".EndReached");
        }

        #endregion
        #region hand

        private void StoppedHandler(IMovable sender, Point position)
        {
            detachedItems.Clear();

            IsRunning = false;
            // T lastItem = GetItemAtLastPosition();
            if (AutoDetach) detachedItems.AddRange(DetachItemsByPosition());
            if (tmpAdvanceCount == 0 && tmpRecedeCount == 0)
            {
                //foreach (T item in detachedItems)
                //    EndReachedEvent.Raise(this, item);
                return; // only in case of manual Advance(double) calls
            }

            items.Clear();
            foreach (T item in ContainedObjects)
            {
                if (tmpAdvanceCount != 0) indices[item] += tmpAdvanceCount;
                else indices[item] -= tmpRecedeCount;
                items[indices[item]] = item;
            }

            if (AutoDetach) detachedItems.AddRange(DetachItemsByIndex());
            
            tmpAdvanceCount = 0;
            tmpRecedeCount = 0;

            //foreach (T item in detachedItems)
            //    EndReachedEvent.Raise(this, item);

            if (tmpRepeatAdvance > 0)
            {
                tmpRepeatAdvance -= 1;
                if (tmpRepeatAdvance > 0) Advance(1);
            }
            else if (tmpRepeatRecede > 0)
            {
                tmpRepeatRecede -= 1;
                if (tmpRepeatRecede > 0) Recede(1);
            }
        }

        #endregion
        #region impl

        #region container

        public bool HasItemAt(int position)
        {
            return items.ContainsKey(position);
        }

        public T GetItemAt(int position)
        {
            if (!items.ContainsKey(position)) return null;
            return items[position];
        }

        public T GetFirstItem()
        {
            if (items.Count == 0) return null;
            int id = indices.Values.Min();
            return items[id];
        }

        public T GetLastItem()
        {
            if (items.Count == 0) return null;
            int id = indices.Values.Max();
            return items[id];
        }

        public T GetItemAtFirstPosition()
        {
            if (!items.ContainsKey(0)) return null;
            return items[0];
        }

        public T GetItemAtLastPosition()
        {
            int maxID = NumberOfSections;
            if (!items.ContainsKey(maxID)) return null;
            return items[maxID];
        }

        public T DetachItem(T item)
        {
            int index = indices.ContainsKey(item) ? indices[item] : -1;

            item.Position = AbsolutePositionOf(item);

            if (index != -1)
            {
                items.Remove(index);
                indices.Remove(item);
            }

            offset.Remove(item);

            item.IsAttached = false;
            item.Container = null;

            return item;
        }

        private IEnumerable<T> DetachItemsByPosition()
        {
            List<T> outsideItems = new List<T>();
            foreach (T item in ContainedObjects)
            {
                if (RelativePositionOf(item).ToVector().Length() > (Length + PositioningTolerance)
                    || RelativePositionOf(item).ToVector().Length() < (0 - PositioningTolerance))
                    outsideItems.Add(item);
            }
            foreach (T item in outsideItems)
            {
                yield return DetachItem(item);
            }
        }

        private IEnumerable<T> DetachItemsByIndex()
        {
            List<T> outsideItems = new List<T>();
            foreach (T item in ContainedObjects)
            {
                if (indices[item] > NumberOfSections || indices[item] < 0)
                    outsideItems.Add(item);
            }
            foreach (T item in outsideItems)
            {
                yield return DetachItem(item);
            }
        }

        #endregion
        #region ctrl

        #region manual

        /// <summary>
        /// CAUTION: using this will not increment the indices
        /// </summary>
        public void Start()
        {
            IsRunning = true;
            mobileElement.StartMoving(Orientation);
        }

        /// <summary>
        /// CAUTION: using this will not increment the indices
        /// </summary>
        public void Stop()
        {
            mobileElement.StopMoving();
            IsRunning = false;
        }

        /// <summary>
        /// CAUTION: this will ignore the indices
        /// </summary>
        public void Advance(double distance)
        {
            LastMovingDistance = distance;
            LastMovingDirectionBackwards = false;
            IsRunning = true; // will be set to false in the internal stopped event handler
            mobileElement.MoveTo(mobileElement.Position + distance * Orientation);
        }

        /// <summary>
        /// CAUTION: this will ignore the indices
        /// </summary>
        public void Recede(double distance)
        {
            LastMovingDistance = distance;
            LastMovingDirectionBackwards = true;
            IsRunning = true; // will be set to false in the internal stopped event handler
            mobileElement.MoveTo(mobileElement.Position + distance * -Orientation);
        }

        /// <summary>
        /// CAUTION: this will ignore the indices
        /// </summary>
        public void Put(T item, Point relativePosition)
        {
            item.Position = this.Position + AbsoluteOffset;
            offset[item] = (Vector)(-mobileElement.Position + relativePosition);
            item.Container = this;
            item.IsAttached = true;
        }

        #endregion
        #region indexed

        public void AdvanceToEnd(bool stopInEachSection = false) 
        {
            int numberOfFreePositionsAtTheEnd = 0;
            int lastKey = NumberOfSections ;
            if (items.ContainsKey(lastKey)) return;
            while (!items.ContainsKey(lastKey))
            {
                numberOfFreePositionsAtTheEnd += 1;
                lastKey -= 1;
                if (lastKey < 0) break;
            }
            if(stopInEachSection) AdvanceRepeatedly(numberOfFreePositionsAtTheEnd);
            else Advance(numberOfFreePositionsAtTheEnd);
        }

        public void AdvanceRepeatedly(int numberOfRepetitions)
        {
            if (numberOfRepetitions < 1) throw new ArgumentOutOfRangeException("The number of repetitions must be greater than zero!");
            tmpRepeatAdvance = numberOfRepetitions;
            Advance(1);
        }

        public void Advance(int numberOfSections = 1)
        {
            LastMovingCount = numberOfSections;
            if (numberOfSections < 1) throw new ArgumentOutOfRangeException("The number of sections to advance must be greater than zero!");
            tmpAdvanceCount = numberOfSections;
            Advance((double)numberOfSections * SectionLength);
        }

        public void RecedeToStart(bool stopInEachSection = false)
        {
            int numberOfFreePositionsAtTheStart = 0;
            int firstKey = 0;
            if (items.ContainsKey(firstKey)) return;
            while (!items.ContainsKey(firstKey))
            {
                numberOfFreePositionsAtTheStart += 1;
                firstKey += 1;
                if (firstKey == NumberOfSections) break;
            }
            if (stopInEachSection) RecedeRepeatedly(numberOfFreePositionsAtTheStart);
            else Recede(numberOfFreePositionsAtTheStart);
        }

        public void RecedeRepeatedly(int numberOfRepetitions)
        {
            if (numberOfRepetitions < 1) throw new ArgumentOutOfRangeException("The number of repetitions must be greater than zero!");
            tmpRepeatRecede = numberOfRepetitions;
            Recede(1);
        }

        public void Recede(int numberOfSections = 1)
        {
            LastMovingCount = numberOfSections;
            if (numberOfSections < 1) throw new ArgumentOutOfRangeException("The number of sections to advance must be greater than zero!");
            tmpRecedeCount = numberOfSections;
            Recede((double)numberOfSections * SectionLength);
        }

        #endregion

        #endregion
        #region IItemSink<T>

        public bool ConnectTo(IItemSource<T> source)
        {
            throw new NotImplementedException();
        }

        public void ConnectTo(params IItemSource<T>[] sources)
        {
            throw new NotImplementedException();
        }

        public void ConnectTo(IEnumerable<IItemSource<T>> sources)
        {
            throw new NotImplementedException();
        }

        public bool IsConnectionAllowed(IItemSource<T> fromSource)
        {
            throw new NotImplementedException();
        }

        #endregion
        #region IItemBuffer<T>

        public bool Put(T item)
        {
            return Put(item, 0);
        }

        public T Get()
        {
            return Get(NumberOfSections - 1);
        }

        public T Preview()
        {
            return Preview(NumberOfSections - 1);
        }

        #endregion
        #region IElectiveBuffer<T, int>

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="index">zero based</param>
        public bool Put(T item, int index)
        {
            if (index > NumberOfSections) throw new ArgumentOutOfRangeException("index", "The zero based index must be less than the number of sections.");
            if (items.ContainsKey(index)) return false;

            items[index] = item;
            indices[item] = index;
            Put(item, (SectionLength * index + FirstSectionOffset) * orientation);
            return true;
        }

        public T Get(int id)
        {
            if (!items.ContainsKey(id)) throw new ArgumentOutOfRangeException("No item was found at the given index.");
            
            T result = items[id];
            DetachItem(result);
            
            return result;
        }

        public T Preview(int id)
        {
            if (!items.ContainsKey(id)) throw new ArgumentOutOfRangeException("No item was found at the given index.");
            return items[id];
        }

        #endregion
        #region IContainer

        public Point RelativePositionOf(IAttachable containedObject)
        {
            T o = containedObject as T;
            if (o == null || !offset.ContainsKey(o)) return null;
            return mobileElement.Position + offset[o];
        }

        public Point AbsolutePositionOf(IAttachable containedObject)
        {
            T o = containedObject as T;
            if (o == null || !offset.ContainsKey(o)) return null;
            containedObject.IsAttached = false;
            Point pos = containedObject.Position;
            containedObject.IsAttached = true;
            return pos + mobileElement.Position + offset[o];
        }

        #endregion
        #region sensors

        private Dictionary<string, Sensor<T>> activeSensors;
        private Dictionary<Sensor<T>, double> sensorPositions;

        public Sensor<T> AddSensor(string sensorID, double relativePosition)
        {
            Sensor<T> result = new Sensor<T>(Model, this, sensorID, sensorID);
            activeSensors[sensorID] = result;
            sensorPositions[result] = relativePosition;

            if (IsRunning)
            {
                foreach (T item in items.Values.Where(i => RelativePositionOf(i).ToVector().Length() < relativePosition))
                {
                    // TODO: ███ implement this and update events for new items / changed status
                    // mobileElement.PredictTimeForDistance(relativePosition);
                }
            }

            return result;
        }

        public void RemoveSensor(string sensorID)
        {
            RemoveSensor(activeSensors[sensorID]);
        }

        public void RemoveSensor(Sensor<T> sensor)
        {
            try
            {
                sensor.Deactivate();
            }
            catch (Exception ex)
            {
                this.Log<SIM_ERROR>("There was a problem deactivating the sensor. Check if a SensorEvent instance is currently scheduled.", ex, Model);
            }
            activeSensors.Remove(sensor.Identifier);
            sensorPositions.Remove(sensor);
        }

        #endregion

        #endregion
    }
}