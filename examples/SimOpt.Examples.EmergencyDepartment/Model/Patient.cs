using System;
using System.Collections.Generic;
using MatthiasToolbox.EmergencyDepartment.Interfaces;
using MatthiasToolbox.Simulation.Entities;
using MatthiasToolbox.Simulation.Engine;

namespace MatthiasToolbox.EmergencyDepartment.Model
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

		public DateTime Birth {get; set;}
		
		public int Type {get; set;}
		
		public int Cw {get; set;}
		
		public int Idx {
			get{
				return this.idx;
			}
		}

		public List<string> Dest {
			set{
				this.dest = value;
			}
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

        //public Patient(IModel model, String name, int index, int seedIndex)
        //    : base(model, name: name, seedIndex)
        //{
        //}

		#endregion
		#region impl
		
		public void SendToNextDestination(){
			if(destidx < dest.Count){
				source.Destinations[dest[destidx]].TakeItem(this);
				//Console.Writeln(((IObject)destinations.get(destidx)).getName());
				this.destidx++;
			}
		}

		

		public int CompareTo(Object o) {
			return ((Patient)o).Idx.CompareTo(idx);
		}
		
		#endregion
	}
}

