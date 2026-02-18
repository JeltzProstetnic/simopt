using MatthiasToolbox.EmergencyDepartment.Interfaces;
using System;
using System.Linq;
using System.Collections.Generic;
using MatthiasToolbox.Simulation;
using MatthiasToolbox.Simulation.Engine;

namespace MatthiasToolbox.EmergencyDepartment.Model
{
	/// <summary>
	/// Description of Exit.
	/// </summary>
	public class Exit : TreatmentRoom
	{
		#region over
		
		public override void TakeItem(IPatient patient){
			//collect statistics...
			count++;
	
			sum[patient.Type] += Model.CurrentTime.ToDateTime().Subtract(patient.Birth).ToDouble();
			countt[patient.Type]++;
			values.Add(Model.CurrentTime.ToDateTime().Subtract(patient.Birth).ToDouble());
			
			if(count == Model.FindEntities<PatientSource>().First().Maxval){
				for(int i = 0; i < 4; i++ ){
					Console.WriteLine("Average Duration Type " + (i+1) + " (#: " + countt[i] + "): " + sum[i].ToTimeSpan().TotalMinutes/countt[i]);
					average += sum[i];
				}
				average = average/count;
				
				Console.WriteLine("Overall standard deviation: " + Std(values, average).ToTimeSpan().TotalMinutes);
				Console.WriteLine("Duration: " + Model.CurrentTime.ToDateTime().Subtract(Model.StartTime.ToDateTime()).TotalMinutes);
			}
		}
		
		public override void OnReset() {
			base.OnReset();
			sum = new double[4];
			count = 0;
			countt = new int[4];
			average = 0;
			values = new List<Double>();
		}
		
		#endregion
		#region cvar
		
		double[] sum = new double[4];
		int[] countt = new int[4];
		int count = 0;
		double average = 0;
		List<Double> values = new List<Double>();
		
		#endregion
		#region ctor
		
		public Exit(IModel model, String name) : base(model, name:name)
		{
		}

		#endregion
		#region impl
		
		public double Std(List<Double> values, double mean){
			
			double var = 0;
			
			foreach(double val in values){
				var += Math.Pow(mean - val, 2);
			}
			
			return Math.Sqrt(var/(values.Count - 1));
		}
		#endregion
	}
}
