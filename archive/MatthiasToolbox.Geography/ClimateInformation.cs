using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;

namespace MatthiasToolbox.Geography
{
	/// <summary>
	/// A set of climate data items. (one year)
	/// </summary>
	[Table(Name = "tblClimateInformation")]
	public class ClimateInformation
	{
		#region prop
		
        /// <summary>
        /// ClimateData for the month January 
        /// </summary>
		public ClimateData January {
			get { return ClimateData.FromString(JanuaryData); }
			set { JanuaryData = value.ToString(); }
		}

        /// <summary>
        /// ClimateData for the month February
        /// </summary>
		public ClimateData February {
			get { return ClimateData.FromString(FebruaryData); }
			set { FebruaryData = value.ToString(); }
		}
		
        /// <summary>
        /// ClimateData for the month March
        /// </summary>
		public ClimateData March {
			get { return ClimateData.FromString(MarchData); }
			set { MarchData = value.ToString(); }
		}

        /// <summary>
        /// ClimateData for the month April
        /// </summary>
		public ClimateData April {
			get { return ClimateData.FromString(AprilData); }
			set { AprilData = value.ToString(); }
		}

        /// <summary>
        /// ClimateData for the month May
        /// </summary>
		public ClimateData May {
			get { return ClimateData.FromString(MayData); }
			set { MayData = value.ToString(); }
		}

        /// <summary>
        /// ClimateData for the month June
        /// </summary>
		public ClimateData June {
			get { return ClimateData.FromString(JuneData); }
			set { JuneData = value.ToString(); }
		}

        /// <summary>
        /// ClimateData for the month July
        /// </summary>
		public ClimateData July {
			get { return ClimateData.FromString(JulyData); }
			set { JulyData = value.ToString(); }
		}

        /// <summary>
        /// ClimateData for the month August
        /// </summary>
		public ClimateData August {
			get { return ClimateData.FromString(AugustData); }
			set { AugustData = value.ToString(); }
		}

        /// <summary>
        /// ClimateData for the month September
        /// </summary>
		public ClimateData September {
			get { return ClimateData.FromString(SeptemberData); }
			set { SeptemberData = value.ToString(); }
		}
		
        /// <summary>
        /// ClimateData for the month October
        /// </summary>
		public ClimateData October {
			get { return ClimateData.FromString(OctoberData); }
			set { OctoberData = value.ToString(); }
		}

        /// <summary>
        /// ClimateData for the month Novmber
        /// </summary>
		public ClimateData November {
			get { return ClimateData.FromString(NovemberData); }
			set { NovemberData = value.ToString(); }
		}
		
        /// <summary>
        /// ClimateData for the month December
        /// </summary>
		public ClimateData December {
			get { return ClimateData.FromString(DecemberData); }
			set { DecemberData = value.ToString(); }
		}
		
		/// <summary>
		/// returns all climate data in order of the months
		/// </summary>
		public IEnumerable<ClimateData> Year {
			get {
				yield return January;
				yield return February;
				yield return March;
				yield return April;
				yield return May;
				yield return June;
				yield return July;
				yield return August;
				yield return September;
				yield return October;
				yield return November;
				yield return December;
			}
		}

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<float> WaterTemperatures
        {
            get
            {
                for (int i = 1; i <= 12; i++)
                {
                    yield return GetInfo(i).WaterTemp;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<float> AbsoluteMaxTemperatures
        {
            get
            {
                for (int i = 1; i <= 12; i++)
                {
                    yield return GetInfo(i).AbsMax;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<float> AbsoluteMinTemperatures
        {
            get
            {
                for (int i = 1; i <= 12; i++)
                {
                    yield return GetInfo(i).AbsMin;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<float> AverageMaxTemperatures
        {
            get
            {
                for (int i = 1; i <= 12; i++)
                {
                    yield return GetInfo(i).AvgMax;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<float> AverageMinTemperatures
        {
            get
            {
                for (int i = 1; i <= 12; i++)
                {
                    yield return GetInfo(i).AvgMin;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<float> AverageRainInMM
        {
            get
            {
                for (int i = 1; i <= 12; i++)
                {
                    yield return GetInfo(i).AvgRainMM;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<float> RainyDaysPerMonth
        {
            get
            {
                for (int i = 1; i <= 12; i++)
                {
                    yield return GetInfo(i).RainyDays;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<float> SunshineHoursPerDay
        {
            get
            {
                for (int i = 1; i <= 12; i++)
                {
                    yield return GetInfo(i).SunshineHours;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<int> RelativeHumidity
        {
            get
            {
                for (int i = 1; i <= 12; i++)
                {
                    yield return GetInfo(i).RelHumidity;
                }
            }
        }


		#endregion
        #region ctor

        /// <summary>
        /// default ctor, does nothing yet
        /// </summary>
        public ClimateInformation() { }

        #endregion
        #region data

        #pragma warning disable 0649
        private int id;
		#pragma warning restore 0649
		
        /// <summary>
        /// the ClimateInformation ID, which is the primary key of the ClimateInformationTable
        /// </summary>
		[Column(Storage="id",
		        AutoSync = AutoSync.OnInsert,
		        IsPrimaryKey = true,
		        IsDbGenerated = true)]
		public int ID {
			get { return id; }
		}
		
        /// <summary>
        /// a comment regarding the Climate Information
        /// </summary>
		[Column]
		public string Comment { get; set; }
		
        /// <summary>
        /// ClimateData (string) for the month January 
        /// </summary>
		[Column]
		public string JanuaryData { get; set; }

        /// <summary>
        /// ClimateData (string) for the month February 
        /// </summary>
		[Column]
		public string FebruaryData { get; set; }

        /// <summary>
        /// ClimateData (string) for the month March 
        /// </summary>
		[Column]
		public string MarchData { get; set; }

        /// <summary>
        /// ClimateData (string) for the month April 
        /// </summary>
		[Column]
		public string AprilData { get; set; }

        /// <summary>
        /// ClimateData (string) for the month May 
        /// </summary>
		[Column]
		public string MayData { get; set; }

        /// <summary>
        /// ClimateData (string) for the month June 
        /// </summary>
		[Column]
		public string JuneData { get; set; }

        /// <summary>
        /// ClimateData (string) for the month July 
        /// </summary>
		[Column]
		public string JulyData { get; set; }

        /// <summary>
        /// ClimateData (string) for the month August 
        /// </summary>
		[Column]
		public string AugustData { get; set; }

        /// <summary>
        /// ClimateData (string) for the month September 
        /// </summary>
		[Column]
		public string SeptemberData { get; set; }

        /// <summary>
        /// ClimateData (string) for the month October 
        /// </summary>
		[Column]
		public string OctoberData { get; set; }

        /// <summary>
        /// ClimateData (string) for the month November 
        /// </summary>
		[Column]
		public string NovemberData { get; set; }

        /// <summary>
        /// ClimateData (string) for the month December 
        /// </summary>
		[Column]
		public string DecemberData { get; set; }
		
		#endregion
        #region impl

        /// <summary>
        /// get the climate data for the given month
        /// </summary>
        /// <param name="forMonth">1..12</param>
        /// <returns></returns>
        public ClimateData GetInfo(int forMonth)
        {
            switch (forMonth)
            {
                case 1:
                    return January;
                case 2:
                    return February;
                case 3:
                    return March;
                case 4:
                    return April;
                case 5:
                    return May;
                case 6:
                    return June;
                case 7:
                    return July;
                case 8:
                    return August;
                case 9:
                    return September;
                case 10:
                    return October;
                case 11:
                    return November;
                case 12:
                    return December;

                default:
                    throw new ApplicationException("month must be between 1 and 12!");
            }
        }

        #endregion
    }
}
