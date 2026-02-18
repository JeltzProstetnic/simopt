using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Simulation.Engine;
using MatthiasToolbox.Simulation.Events;
using MatthiasToolbox.Simulation.Interfaces;
using MatthiasToolbox.Simulation.Enum;
using MatthiasToolbox.Simulation.Entities;

namespace MatthiasToolbox.Simulation.Tools
{
    public class Sensor<T> : SimpleEntity
    {
        #region over

        public override void Reset()
        {
            activeEventInstances = new List<BinaryEventInstance<Sensor<T>, T>>();
        }

        #endregion
        #region cvar

        private List<BinaryEventInstance<Sensor<T>, T>> activeEventInstances;

        #endregion
        #region prop

        public BinaryEvent<Sensor<T>, T> SensorEvent { get; private set; }

        #endregion
        #region ctor

        public Sensor(IModel model, IContainer parent, string id, string name)
            : base(model, id, name)
        {
            Container = parent;
            SensorEvent = new BinaryEvent<Sensor<T>, T>(EntityName + ".SensorActivated");
            activeEventInstances = new List<BinaryEventInstance<Sensor<T>, T>>();
            SensorEvent.AddHandler(InternalSensorEventHandler, new Priority(type: PriorityType.LowLevelBeforeOthers));
        }

        #endregion
        #region hand

        private void InternalSensorEventHandler(Sensor<T> sensor, T item)
        {
            BinaryEventInstance<Sensor<T>, T> currentEventInstance = Model.CurrentEvent as BinaryEventInstance<Sensor<T>, T>;
            if (activeEventInstances.Contains(currentEventInstance)) activeEventInstances.Remove(currentEventInstance);
        }

        #endregion
        #region impl

        public void Deactivate()
        {
            foreach (BinaryEventInstance<Sensor<T>, T> eventInstance in activeEventInstances)
                Model.RemoveEvent(eventInstance);
            activeEventInstances.Clear();
        }

        public void ScheduleEvent(double delay, T item)
        {
            ScheduleEventAt(Model.CurrentTime + delay, item);
        }

        public void ScheduleEventAt(double dateTime, T item)
        {
            BinaryEventInstance<Sensor<T>, T> eventInstance = SensorEvent.GetInstance(this, item);
            Model.AddEventAt(dateTime, eventInstance);
            activeEventInstances.Add(eventInstance);
        }

        #endregion
    }
}