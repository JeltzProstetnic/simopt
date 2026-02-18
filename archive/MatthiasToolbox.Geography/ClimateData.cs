using System;

namespace MatthiasToolbox.Geography
{
	/// <summary>
	/// a structure to hold one set of climate indices (for one month)
	/// </summary>
	public class ClimateData // FINAL
	{
		#region over
		
        /// <summary>
        /// overriden: will return the ClimateData as string
        /// </summary>
		public override string ToString()
		{
			return absMax.ToString() + ";" +
				absMin.ToString() + ";" +
				avgMax.ToString() + ";" +
				avgMin.ToString() + ";" +
				relHumidity.ToString() + ";" +
				avgRainMM.ToString() + ";" +
				rainyDays.ToString() + ";" +
				sunshineHours.ToString() + ";" +
				waterTemp.ToString();
		}
		
		#endregion
		#region cvar
		
		private float absMax;
		private float absMin;
		private float avgMax;
		private float avgMin;
		private int relHumidity = -1;
		private float avgRainMM = -1;
		private float rainyDays;
		private float sunshineHours;
		private float waterTemp;
		
		#endregion
		#region prop
		
        /// <summary>
        /// float number for the absolute maximal Temperature (in°C) of a month
        /// </summary>
		public float AbsMax {
			get { return absMax; }
			set { absMax = value; }
		}
		
        /// <summary>
        /// float number for the absolute minimal Temperature (in°C) of a month
        /// </summary>
		public float AbsMin {
			get { return absMin; }
			set { absMin = value; }
		}
		
        /// <summary>
        /// float number for the average maximal Temperature (in°C) of a month
        /// </summary>
		public float AvgMax {
			get { return avgMax; }
			set { avgMax = value; }
		}
		
        /// <summary>
        /// float number for the average minimal Temperature (in°C) of a month
        /// </summary>
		public float AvgMin {
			get { return avgMin; }
			set { avgMin = value; }
		}
		
        /// <summary>
        /// the relative humidity in %
        /// </summary>
		public int RelHumidity {
			get { return relHumidity; }
			set { relHumidity = value; }
		}
		
        /// <summary>
        /// the average rainfall of a month in mm
        /// </summary>
		public float AvgRainMM {
			get { return avgRainMM; }
			set { avgRainMM = value; }
		}
		
        /// <summary>
        /// the number of rainy days (days with more than 1mm rainfall) of a month
        /// </summary>
		public float RainyDays {
			get { return rainyDays; }
			set { rainyDays = value; }
		}
		
        /// <summary>
        /// the average number of sunshine hours of a month
        /// </summary>
		public float SunshineHours {
			get { return sunshineHours; }
			set { sunshineHours = value; }
		}
		
        /// <summary>
        /// float number for the average water Temperature (in°C) of a month
        /// </summary>
		public float WaterTemp {
			get { return waterTemp; }
			set { waterTemp = value; }
		}
		
		#endregion
		#region ctor
		
		/// <summary>
		/// constructor for the ClimateData class
		/// </summary>
		/// <param name="absMax">absolute Maximum °C</param>
		/// <param name="absMin">absolute Minimum °C</param>
		/// <param name="avgMax">average Maximum °C</param>
		/// <param name="avgMin">average Minimum °C</param>
		/// <param name="relHumidity">relative Humidity in %</param>
		/// <param name="avgRainMM">average Rainfall in mm</param>
		/// <param name="rainyDays">number of days with more than 1mm rainfall</param>
		/// <param name="sunshineHours">average hours of sunshine per day</param>
        /// <param name="waterTemp">average water temperature</param>
		public ClimateData(float absMax, float absMin,
		                   float avgMax, float avgMin,
		                   int relHumidity, float avgRainMM,
		                   float rainyDays, float sunshineHours,
		                   float waterTemp)
		{
			this.absMax = absMax;
			this.absMin = absMin;
			this.avgMax = avgMax;
			this.avgMin = avgMin;
			this.relHumidity = relHumidity;
			this.avgRainMM = avgRainMM;
			this.rainyDays = rainyDays;
			this.sunshineHours = sunshineHours;
			this.waterTemp = waterTemp;
		}
		
        /// <summary>
        /// Constructor for climate data of Iten Import
        /// </summary>
        /// <param name="avgMax">average Maximum °C</param>
        /// <param name="avgMin">average Minimum °C</param>
        /// <param name="rainyDays">number of days with more than 1mm rainfall</param>
        /// <param name="sunshineHours">average hours of sunshine per day</param>
        /// <param name="waterTemp">float number for the average water Temperature (in°C) of a month</param>
		public ClimateData(float avgMax, float avgMin,
		                   float rainyDays, float sunshineHours,
		                   float waterTemp)
		{
			this.absMax = avgMax;
			this.absMin = avgMin;
			this.avgMax = avgMax;
			this.avgMin = avgMin;
			this.rainyDays = rainyDays;
			this.sunshineHours = sunshineHours;
			this.waterTemp = waterTemp;
		}
		#endregion
		#region impl
		
		/// <summary>
        /// parses and returns the ClimateData;
		/// the parameter string must be of the format
		/// "float;float;float;float;int;int;int;float"
		/// </summary>
		public static ClimateData FromString(string data) 
        {
            string[] tmp = data.Split(';');
            float waterTemp = float.NaN;
            float absMax = float.NaN;
            float absMin = float.NaN;

            try
            {
                if (tmp[0] != "n. def.") absMax = float.Parse(tmp[0]);
                if (tmp[1] != "n. def.") absMin = float.Parse(tmp[1]);

                if (tmp[8] != "n. def.") waterTemp = float.Parse(tmp[8]);
            }
            catch { }

			return new ClimateData(absMax,
			                       absMin,
			                       float.Parse(tmp[2]),
			                       float.Parse(tmp[3]),
			                       int.Parse(tmp[4]),
			                       float.Parse(tmp[5]),
			                       float.Parse(tmp[6]),
			                       float.Parse(tmp[7]),
			                       waterTemp);
		}
		
		#endregion
	}
}
