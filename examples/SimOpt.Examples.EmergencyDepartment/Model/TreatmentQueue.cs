using SimOpt.Examples.EmergencyDepartment.Interfaces;
using System;
using System.Collections.Generic;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Engine;

namespace SimOpt.Examples.EmergencyDepartment.Model
{
    /// <summary>
    /// Description of TreatmentQueue.
    /// </summary>
    public class TreatmentQueue : SimpleEntity, ITreatmentQueue<IPatient>
    {
        #region over

        public override void OnReset()
        {
            base.OnReset();
            Clear();
        }

        #endregion
        #region cvar

        private List<IPatient> internalQueue;

        #endregion
        #region prop

        public IEnumerable<IPatient> InternalQueue
        {
            get { return this.internalQueue; }
            set { throw new NotImplementedException(); }
        }

        #endregion
        #region ctor

        public TreatmentQueue(IModel model) : base(model, name: "InternalQueue")
        {
            this.internalQueue = new List<IPatient>();
        }

        #endregion
        #region impl

        public void Clear()
        {
            internalQueue.Clear();
        }

        public int Count()
        {
            return internalQueue.Count;
        }

        public void AddItem(IPatient patient)
        {
            internalQueue.Insert(0, patient);
        }

        public IPatient TakeItem()
        {
            if (Count() == 0) return null;
            IPatient result = internalQueue[internalQueue.Count - 1];
            internalQueue.Remove(result);
            return result;
        }

        public bool Empty
        {
            get { return (Count() == 0); }
        }

        public void Sort()
        {
            internalQueue.Sort();
        }

        #endregion
    }
}
