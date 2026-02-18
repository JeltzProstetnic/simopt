using MatthiasToolbox.EmergencyDepartment.Interfaces;
using System;
using System.Collections.Generic;
using MatthiasToolbox.Simulation.Entities;
using MatthiasToolbox.Simulation.Engine;

namespace MatthiasToolbox.EmergencyDepartment.Model
{
	/// <summary>
	/// Description of TreatmentRoom.
	/// </summary>
	public class TreatmentRoom : SimpleEntity, ITreatmentRoom<IPatient>
	{
		#region over
		
		public override void OnReset() {
			base.OnReset();
			if(queue != null)
				queue.OnReset();
		}

		#endregion
		#region cvar
		
		private ITreatmentQueue<IPatient> queue;
		private List<ITreatmentPoint<IPatient>> treatments;
		private bool sort = false;
		
		#endregion
		#region prop
		
		public ITreatmentQueue<IPatient> Queue {
			get{
				return queue;
			}
		}
		
		#endregion
		#region ctor
		
		public TreatmentRoom(IModel model, String name) : base(model, name: name)
		{
		}

        public TreatmentRoom(IModel model, String name, List<ITreatmentPoint<IPatient>> treatments)
			: this(model, name, new TreatmentQueue(model), treatments, false)
		{
		}

        public TreatmentRoom(IModel model, String name, List<ITreatmentPoint<IPatient>> treatments, bool sort)
            : this(model, name, new TreatmentQueue(model), treatments, sort)
		{
		}

        public TreatmentRoom(IModel model, String name, ITreatmentQueue<IPatient> queue, List<ITreatmentPoint<IPatient>> treatments, bool sort)
            : base(model, name: name)
		{
			this.queue = queue;
			this.treatments = treatments;
			this.sort = sort;
			foreach(ITreatmentPoint<IPatient> treat in treatments){
				treat.Room = this;
			}
		}
		
		#endregion
		#region impl
		
		public virtual void TakeItem(IPatient patient){
			//add item to queue
			queue.AddItem(patient);
			if(sort)
				queue.Sort();
			
			//check if each treatmentpoint is busy --> start working
			foreach(ITreatmentPoint<IPatient> treat in treatments){
				if(!treat.Busy){
					treat.StartTreatment(queue.TakeItem());
					break;
				}
			}
			
		}
		
		#endregion
	}
}
