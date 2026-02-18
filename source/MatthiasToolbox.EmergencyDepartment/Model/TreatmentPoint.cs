using MatthiasToolbox.EmergencyDepartment.Interfaces;
using System;
using MatthiasToolbox.Simulation.Entities;
using MatthiasToolbox.Simulation.Events;
using MatthiasToolbox.Mathematics.Stochastics.Distributions;
using MatthiasToolbox.Simulation.Engine;
using MatthiasToolbox.Simulation;

namespace MatthiasToolbox.EmergencyDepartment.Model
{
	/// <summary>
	/// Description of TreatmentPoint.
	/// </summary>
	public class TreatmentPoint : StochasticEntity, ITreatmentPoint<IPatient>
	{
		#region over

        public override void OnReset() {
			base.OnReset();
			busy = busyStart;
			room = roomStart;
		}

		#endregion
		#region cvar
		
		public BinaryEvent<ITreatmentPoint<IPatient>, IPatient> TreatmentFinishedEvent;
		protected bool busy = false;
		bool busyStart = false;
		protected ITreatmentRoom<IPatient> room;
		private ITreatmentRoom<IPatient> roomStart;
		
		protected Random<double> triangular;
		private double min;
		private double mode;
		private double max;
		
		#endregion
		#region prop

        public string Name { get; set; }

		public ITreatmentRoom<IPatient> Room {
			set{
				this.room = value;
				this.roomStart = value;
			}
		}
		
		public bool Busy {
			get{
				return busy;
			}
		}
		
		#endregion
		#region ctor
		
		public TreatmentPoint(IModel model, String name, bool busy, double min, double mode, double max)
			: this(model, name, min, mode, max)
		{
			this.busy = busy;
			this.busyStart = busy;
		}

        public TreatmentPoint(IModel model, String name, double min, double mode, double max)
            : base(model, name:name)
		{
			this.min = min;
			this.mode = mode;
			this.max = max;
			TreatmentFinishedEvent = new BinaryEvent<ITreatmentPoint<IPatient>, IPatient>(Name + ".TreatmentFinished");
			TreatmentFinishedEvent.Log = true;
			TreatmentFinishedEvent.AddHandler(PatientTreatmentHandler);
            Initialize();
		}
		
		#endregion
        #region init

        public void Initialize()
        {
            triangular = new Random<double>(this, new TriangularDistribution(min, mode, max));
        }

        #endregion
        #region impl

        public virtual void StartTreatment(IPatient patient){
			busy = true;
            Model.AddEventAt(Model.CurrentTime.ToDateTime().AddMinutes(triangular.Next()).ToDouble(), TreatmentFinishedEvent.GetInstance(this, patient));
		}
		
		public virtual void StartTreatment(){
			//try to get next patient for treatment
			if(room.Queue.Empty){
				busy = false;
			}else{
				StartTreatment(room.Queue.TakeItem());
			}
		}
		
		public void PatientTreatmentHandler(ITreatmentPoint<IPatient> sender, IPatient patient){
			StartTreatment();
			
			//send patient to the next destination
			patient.SendToNextDestination();
		}

		public virtual void SetOldRoom(ITreatmentRoom<IPatient> room) {
			throw new NotImplementedException("Not implemented in this task");
		}

		#endregion
	}
}
