using System;
using System.Collections.Generic;
using SimOpt.Examples.EmergencyDepartment.Interfaces;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Engine;

namespace SimOpt.Examples.EmergencyDepartment.Model
{
    /// <summary>
    /// Description of Patient.
    /// </summary>
    public class Patient : SimpleEntity, IPatient
    {
        #region over

        public override void OnReset()
        {
            base.OnReset();
        }

        #endregion
        #region cvar

        private int idx;
        private int destidx = 0;
        private List<string> dest;
        private PatientSource source;

        #endregion
        #region prop

        public string Name { get; set; }

        public DateTime Birth { get; set; }

        public int Type { get; set; }

        public int Cw { get; set; }

        public int Idx
        {
            get { return this.idx; }
        }

        public List<string> Dest
        {
            set { this.dest = value; }
        }

        #endregion
        #region ctor

        public Patient(IModel model, PatientSource source) : this(model, source, "Patient " + source.Index.ToString())
        {
        }

        public Patient(IModel model, PatientSource source, String name)
            : base(model, name: name)
        {
            this.idx = source.IncreaseCounter();
            this.source = source;
        }

        #endregion
        #region impl

        public void SendToNextDestination()
        {
            if (destidx < dest.Count)
            {
                source.Destinations[dest[destidx]].TakeItem(this);
                this.destidx++;
            }
        }

        public int CompareTo(Object o)
        {
            return ((Patient)o).Idx.CompareTo(idx);
        }

        #endregion
    }
}
