using System;
using MatthiasToolbox.EmergencyDepartment.Interfaces;
using MatthiasToolbox.Simulation;
using MatthiasToolbox.Simulation.Engine;

namespace MatthiasToolbox.EmergencyDepartment.Model
{
	/// <summary>
	/// Description of CWTreatmentPoint.
	/// </summary>
	public class CWTreatmentPoint : TreatmentPoint
	{
		#region over
		
		public override void SetOldRoom(ITreatmentRoom<IPatient> oldRoom){
			this.oldRoom = (CWTreatmentRoom)oldRoom;
		}

		public override void OnReset() {
			base.OnReset();
		}
		
		public override void StartTreatment(){
			//check if change is necessary
			if(CheckRoom.Queue.Count() >= 20 && !CheckRoom.Change){
				CheckRoom.Change = true;
			}
			
			//check if rechange is necessary
			if(CheckRoom.Queue.Count() <= 5 && !CheckRoom.Rechange && CheckRoom.ChangeEnd){
				CheckRoom.Rechange = true;
			}
			
			//check Queue length of check room (CW2)!!!
			if(CheckRoom.Change && !Room.DoctorAdded){
				//change assigned room

				//add doctor to changelist
				CheckRoom.DoctorsChange.Add(this);
				Room.DoctorAdded = true;

				//check if both doctors are available
				if(CheckRoom.DoctorsChange.Count == 2){
					//perform change of rooms and start further treatment
					foreach(CWTreatmentPoint doc in CheckRoom.DoctorsChange){
						doc.room = doc.NewRoom;
						if(doc.room != doc.CheckRoom)
							doc.TimeExtend = true;
						doc.StartTreatment(doc.room.Queue.TakeItem());
					}
					//change is finsihed
					CheckRoom.ChangeEnd = true;
				}
			}else if(CheckRoom.Rechange && Room.DoctorAdded){
				//re-change doctors
				if(CheckRoom.DoctorsChange.Contains(this)){
					CheckRoom.DoctorsReChange.Add(this);
					CheckRoom.DoctorsChange.Remove(this);
				}
				
				//check if both doctors are available
				if(CheckRoom.DoctorsReChange.Count == 2){
					//perform change
					foreach(CWTreatmentPoint doc in CheckRoom.DoctorsReChange){
						doc.room = doc.OldRoom;
						doc.TimeExtend = false;
						doc.Room.DoctorAdded = false;
						doc.StartTreatment(doc.room.Queue.TakeItem());
					}
					CheckRoom.DoctorsReChange.Clear();
					
					//finish rechange
					CheckRoom.Change = false;
					CheckRoom.Rechange = false;
					CheckRoom.ChangeEnd = false;
				}
			}else{
				if(room.Queue.Empty){
					busy = false;
				}else{
					StartTreatment(room.Queue.TakeItem());
				}
			}
		}
		
		public override void StartTreatment(IPatient patient){
			busy = true;
			
			if(TimeExtend)
				Model.AddEventAt(Model.CurrentTime.ToDateTime().AddMinutes(1.2 * triangular.Next()).ToDouble(), TreatmentFinishedEvent.GetInstance(this, patient));
			else
                Model.AddEventAt(Model.CurrentTime.ToDateTime().AddMinutes(triangular.Next()).ToDouble(), TreatmentFinishedEvent.GetInstance(this, patient));
		}
		
		#endregion
		#region cvar
		
		private CWTreatmentRoom oldRoom;
		
		#endregion
		#region prop
		
		public bool TimeExtend { get; set;	}

		public CWTreatmentRoom Room {
			get{
				return (CWTreatmentRoom)room;
			}
		}
		
		public CWTreatmentRoom CheckRoom { get; set; }
		
		public CWTreatmentRoom NewRoom { get; set; }

		public CWTreatmentRoom OldRoom {
			get{
				return oldRoom;
			}
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
