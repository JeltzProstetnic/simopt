using System;
using System.Collections.Generic;
using MatthiasToolbox.EmergencyDepartment.Interfaces;
using MatthiasToolbox.Simulation.Entities;
using MatthiasToolbox.Simulation.Events;
using MatthiasToolbox.Mathematics.Stochastics.Distributions;
using MatthiasToolbox.Simulation.Engine;
using MatthiasToolbox.Simulation;

namespace MatthiasToolbox.EmergencyDepartment.Model
{
	/// <summary>
	/// Description of PatientSource.
	/// </summary>
	public class PatientSource : StochasticEntity
	{
		#region init
		
		public void Initialize() {
            NegExponentialDistribution negexp = new NegExponentialDistribution();
			negexp.ConfigureMean(exppar);
			arrival = new Random<double>(this, negexp);
			rnd = new Random<double>(this, new UniformDoubleDistribution(0d, 1d));
			Model.AddEventAt(Model.CurrentTime, PatientAppearsEvent.GetInstance(this, construct.Invoke(this)));
		}
		
		public override void OnReset() {
			base.OnReset();
			counter = 1;
		}

		#endregion
		#region cvar
		
		private int counter = 1;
		private int maxval;
		private double exppar;
		private Random<double> arrival;
		private Random<double> rnd;
		private Func<PatientSource, IPatient> construct;

        public BinaryEvent<PatientSource, IPatient> PatientAppearsEvent;

		#endregion
		#region prop
		
		public Dictionary<string, ITreatmentRoom<IPatient>> Destinations {get; set;}
		
		public int Maxval {
			get{
				return this.maxval;
			}
		}
		
		public int Index {
			get{
				return counter;
			}
		}
		
		#endregion
		#region ctor
		
		public PatientSource(IModel model, Func<PatientSource, IPatient> construct) : this(model, construct, "PatientSource", 250, 0.3)
		{
		}

        public PatientSource(IModel model, Func<PatientSource, IPatient> construct, int maxval)
            : this(model, construct, "PatientSource", maxval, 0.3)
		{
		}

        public PatientSource(IModel model, Func<PatientSource, IPatient> construct, String name, int maxval, double exppar)
            : base(model, name, name)
		{
			this.maxval = maxval;
			this.exppar = exppar;
			this.construct = construct;
			PatientAppearsEvent = new BinaryEvent<PatientSource, IPatient>(name + ".PatientAppeared");
			PatientAppearsEvent.Log = true;
			PatientAppearsEvent.AddHandler(PatientAppearedHandler);
            Initialize();
		}
		
		#endregion
		#region impl
		
		public void PatientAppearedHandler(PatientSource sender, IPatient patient) {
			//System.out.println(patient.Name + " has appeared at " + sender.getName());
			
			//set birth date
			patient.Birth = Model.CurrentTime.ToDateTime();
			
			//add to internal queue
			if(counter <= maxval){
				//check if number of desired patients is reached and schedule new arrival is necessary
				Model.AddEventAt(Model.CurrentTime.ToDateTime().AddMinutes(arrival.Next()).ToDouble(), sender.PatientAppearsEvent.GetInstance(sender, construct.Invoke(this)));
			}
			
			//set Patient parameter
			SetPatientParams(patient);
			
			//send Patient to first destiantion
			patient.SendToNextDestination();
		}

		private void SetPatientParams(IPatient patient){
			
			double rndval = rnd.Next();
			
			//set casuality ward
			if(rndval <= 0.60){
				patient.Cw =0;
			}else{
				patient.Cw = 1;
			}

			//set treatment group for patients
			if(rndval <= 0.35){
				patient.Type = 0;
			}else if(rndval <= 0.55){
				patient.Type = 1;
			}else if(rndval <= 0.6){
				patient.Type = 2;
			}else{
				patient.Type = 3;
			}

			//set correct destinationlist
			switch(patient.Cw){
				case 0:
					switch(patient.Type){
						case 0:
							patient.Dest = new List<string>{"registration","cw1","xray","cw1","exit"};
							break;
						case 1:
							patient.Dest = new List<string>{"registration","cw1","plaster","exit"};
							break;
						case 2:
							patient.Dest = new List<string>{"registration","cw1","xray","plaster","xray","cw1","exit"};
							break;
						case 3:
							patient.Dest = new List<string>{"registration","cw1","exit"};
							break;
					}
					break;
				case 1:
					switch(patient.Type){
						case 0:
							patient.Dest = new List<string>{"registration","cw2","xray","cw2","exit"};
							break;
						case 1:
							patient.Dest = new List<string>{"registration","cw2","plaster","exit"};
							break;
						case 2:
							patient.Dest = new List<string>{"registration","cw2","xray","plaster","xray","cw2","exit"};
							break;
						case 3:
							patient.Dest = new List<string>{"registration","cw2","exit"};
							break;
					}
					break;
			}
		}
		
		public int IncreaseCounter(){
			return counter++;
		}
		
		#endregion
	}
}

