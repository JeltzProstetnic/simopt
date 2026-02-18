using System;
using System.Collections.Generic;
using MatthiasToolbox.EmergencyDepartment.Interfaces;
using MatthiasToolbox.Simulation.Engine;

namespace MatthiasToolbox.EmergencyDepartment.Model
{
	/// <summary>
	/// Description of CWTreatmentRoom.
	/// </summary>
	public class CWTreatmentRoom : TreatmentRoom
	{
		#region over
		
		public override void OnReset() {
			base.OnReset();
			DoctorsChange = new List<CWTreatmentPoint>();
			DoctorsReChange = new List<CWTreatmentPoint>();
			Change = false;
			Rechange = false;
		}

		#endregion
		#region prop
		
		public List<CWTreatmentPoint> DoctorsChange {get; set;}

		public List<CWTreatmentPoint> DoctorsReChange {get;set; }

		public bool DoctorAdded {get; set;}
		
		public bool Change { get; set;}

		public bool Rechange {get; set;}
		
		public bool ChangeEnd { get; set;}
		
		#endregion
		#region ctor
		
		public CWTreatmentRoom(IModel model, String name, List<ITreatmentPoint<IPatient>> treatments) 
			: base(model, name, new TreatmentQueue(model), treatments, false)
		{
			DoctorsChange = new List<CWTreatmentPoint>();
			DoctorsReChange = new List<CWTreatmentPoint>();
			foreach(ITreatmentPoint<IPatient> doc in treatments){
				doc.SetOldRoom(this);
			}
		}
		
		#endregion
	}
}
