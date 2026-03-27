using System;
using SimOpt.Examples.EmergencyDepartment.Interfaces;
using SimOpt.Simulation;
using SimOpt.Simulation.Engine;

namespace SimOpt.Examples.EmergencyDepartment.Model
{
    /// <summary>
    /// Description of CWTreatmentPoint.
    /// </summary>
    public class CWTreatmentPoint : TreatmentPoint
    {
        #region over

        public override void SetOldRoom(ITreatmentRoom<IPatient> oldRoom)
        {
            this.oldRoom = (CWTreatmentRoom)oldRoom;
        }

        public override void OnReset()
        {
            base.OnReset();
        }

        public override void StartTreatment()
        {
            if (CheckRoom.Queue.Count() >= 20 && !CheckRoom.Change)
            {
                CheckRoom.Change = true;
            }

            if (CheckRoom.Queue.Count() <= 5 && !CheckRoom.Rechange && CheckRoom.ChangeEnd)
            {
                CheckRoom.Rechange = true;
            }

            if (CheckRoom.Change && !Room.DoctorAdded)
            {
                CheckRoom.DoctorsChange.Add(this);
                Room.DoctorAdded = true;

                if (CheckRoom.DoctorsChange.Count == 2)
                {
                    foreach (CWTreatmentPoint doc in CheckRoom.DoctorsChange)
                    {
                        doc.room = doc.NewRoom;
                        if (doc.room != doc.CheckRoom)
                            doc.TimeExtend = true;
                        doc.StartTreatment(doc.room.Queue.TakeItem());
                    }
                    CheckRoom.ChangeEnd = true;
                }
            }
            else if (CheckRoom.Rechange && Room.DoctorAdded)
            {
                if (CheckRoom.DoctorsChange.Contains(this))
                {
                    CheckRoom.DoctorsReChange.Add(this);
                    CheckRoom.DoctorsChange.Remove(this);
                }

                if (CheckRoom.DoctorsReChange.Count == 2)
                {
                    foreach (CWTreatmentPoint doc in CheckRoom.DoctorsReChange)
                    {
                        doc.room = doc.OldRoom;
                        doc.TimeExtend = false;
                        doc.Room.DoctorAdded = false;
                        doc.StartTreatment(doc.room.Queue.TakeItem());
                    }
                    CheckRoom.DoctorsReChange.Clear();

                    CheckRoom.Change = false;
                    CheckRoom.Rechange = false;
                    CheckRoom.ChangeEnd = false;
                }
            }
            else
            {
                if (room.Queue.Empty)
                {
                    busy = false;
                }
                else
                {
                    StartTreatment(room.Queue.TakeItem());
                }
            }
        }

        public override void StartTreatment(IPatient patient)
        {
            busy = true;

            if (TimeExtend)
                Model.AddEventAt(Model.CurrentTime.ToDateTime().AddMinutes(1.2 * triangular.Next()).ToDouble(), TreatmentFinishedEvent.GetInstance(this, patient));
            else
                Model.AddEventAt(Model.CurrentTime.ToDateTime().AddMinutes(triangular.Next()).ToDouble(), TreatmentFinishedEvent.GetInstance(this, patient));
        }

        #endregion
        #region cvar

        private CWTreatmentRoom oldRoom;

        #endregion
        #region prop

        public bool TimeExtend { get; set; }

        public new CWTreatmentRoom Room
        {
            get { return (CWTreatmentRoom)room; }
        }

        public CWTreatmentRoom CheckRoom { get; set; }

        public CWTreatmentRoom NewRoom { get; set; }

        public CWTreatmentRoom OldRoom
        {
            get { return oldRoom; }
        }

        #endregion
        #region ctor

        public CWTreatmentPoint(IModel model, String name, bool busy, double min, double mode,
                                double max) : base(model, name, busy, min, mode, max)
        {
        }

        #endregion
    }
}
