using System;
using System.Data.Linq.Mapping;
using System.Linq;

using MatthiasToolbox.Utilities;

namespace MatthiasToolbox.Geography
{
	/// <summary>
	/// a City with Name, CityID, Parent Country, Coordinates, ClimateInformationID
    /// and a bool property that determines whether the City is the Capital of the Parent Country;
    /// plus bool variables that determine the best travel time and plus several comment fields;
	/// </summary>
	[Table(Name = "tblCity")]
	public class City : ILocation
    {
        #region over

        /// <summary>
        /// overriden: will return the name of the city
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return name;
        }

        #endregion
        #region prop

        /// <summary>
        /// the getter of this property will return the Parent Country of the City.
        /// the setter tries to set the countryID of the city to the ID of the given Country; 
        /// if the given ID doesn't belong to a Country an ArgumentException will be thrown
        /// </summary>
        public ILocation Parent
        {
            get { return Country; }
            set
            {
                if (value is Country)
                {
                    // Country = (Country)value;
                    countryID = value.ID;
                }
                else
                {
                    throw new ArgumentException("The parent region of a city must be of the type country");
                }
            }
        }

        /// <summary>
        /// using the CountryTable and a LING query, the getter returns the country to which the City belongs.
        /// the setter sets the countryID of the city
        /// </summary>
        public Country Country {
			get {
                return (from row in GeoDatabase.Instance.CountryTable
				        where row.ID == countryID
				        select row).ToList()[0];
			}
			set{
				countryID = value.ID;
			}
		}

        public bool HasClimateInfo { get { return ClimateInformationID != 0 || Country.HasClimateInfo; } }

        /// <summary>
        /// using the ClimateInformationTable and a LINQ query, this getter returns the Climate Information
        /// for a City if available. If the city has no climate information the getter will return the 
        /// Climate Information of the parent country
        /// </summary>
		public ClimateInformation ClimateInfo {
			get {
				if(ClimateInformationID == 0) return Country.ClimateInfo;
                return (from row in GeoDatabase.Instance.ClimateInformationTable
				        where row.ID == ClimateInformationID
				        select row).ToList()[0];
			}
		}

        /// <summary>
        /// returns the Name of the City without special chars
        /// </summary>
        public string NameASCII
        {
            get
            {
                return Name.ReplaceNonGermanSpecialChars();
            }
        }

        #endregion
        #region ctor

        /// <summary>
        /// empty ctor for City class
        /// </summary>
        public City(){ }
		
        /// <summary>
        /// ctor for City class, requiring the Name of the City and its Parent Country
        /// </summary>
        /// <param name="name"></param>
        /// <param name="country"></param>
		public City(string name, Country country)
		{
			this.name = name;
			this.countryID = country.ID;
		}

        #endregion		
		#region data
		
		#pragma warning disable 0649
		private int id;
		#pragma warning restore 0649
		private string name;
		private int countryID;
		private int climateInformationID;
		private bool capital;
        private bool bestTimeJan;
        private bool bestTimeFeb;
        private bool bestTimeMar;
        private bool bestTimeApr;
        private bool bestTimeMay;
        private bool bestTimeJun;
        private bool bestTimeJul;
        private bool bestTimeAug;
        private bool bestTimeSep;
        private bool bestTimeOct;
        private bool bestTimeNov;
        private bool bestTimeDec;
        private string commentClimate;
        private string commentActivity;
        private string commentGeneral;

        /// <summary>
        /// the City ID, which is the primary key of the CityTable
        /// </summary>
		[Column(Storage="id",
		        AutoSync = AutoSync.OnInsert,
		        IsPrimaryKey = true,
		        IsDbGenerated = true)]
		public int ID {
			get { return id; }
		}
		
        /// <summary>
        /// the ID of the Climate Station belonging to the City
        /// </summary>
		[Column]
		public string StationID { get; set; }
		
        /// <summary>
        /// the Name of the City
        /// </summary>
		[Column]
		public string Name {
			get { return name; }
			set { name = value; }
		}
		
        /// <summary>
        /// the ID of the City's Parent Country
        /// </summary>
		[Column]
		public int CountryID {
			get { return countryID; }
			set { countryID = value; }
		}
		
        /// <summary>
        /// the Latitude of the City coordinates
        /// </summary>
		[Column]
		public double Latitude {get; set;}

        /// <summary>
        /// the Longitude of the City coordinates
        /// </summary>
		[Column]
		public double Longitude {get; set;}
		
        /// <summary>
        /// the ID of the City's ClimateInformation
        /// </summary>
		[Column]
		public int ClimateInformationID {
			get { return climateInformationID; }
			set { climateInformationID = value; }
		}
		
        /// <summary>
        /// determines whether the City is the capital of the parent country
        /// </summary>
		[Column]
		public bool Capital {
			get { return capital; }
			set { capital = value; }
		}

        /// <summary>
        /// determines whether January is the best travel time to visit this city
        /// </summary>
        [Column]
        public bool BestTimeJan
        {
            get { return bestTimeJan; }
            set { bestTimeJan = value; }
        }

        /// <summary>
        /// determines whether February is the best travel time to visit this city
        /// </summary>
        [Column]
        public bool BestTimeFeb
        {
            get { return bestTimeFeb; }
            set { bestTimeFeb = value; }
        }

        /// <summary>
        /// determines whether March is the best travel time to visit this city
        /// </summary>
        [Column]
        public bool BestTimeMar
        {
            get { return bestTimeMar; }
            set { bestTimeMar = value; }
        }

        /// <summary>
        /// determines whether April is the best travel time to visit this city
        /// </summary>
        [Column]
        public bool BestTimeApr
        {
            get { return bestTimeApr; }
            set { bestTimeApr = value; }
        }

        /// <summary>
        /// determines whether May is the best travel time to visit this city
        /// </summary>
        [Column]
        public bool BestTimeMay
        {
            get { return bestTimeMay; }
            set { bestTimeMay = value; }
        }

        /// <summary>
        /// determines whether June is the best travel time to visit this city
        /// </summary>
        [Column]
        public bool BestTimeJun
        {
            get { return bestTimeJun; }
            set { bestTimeJun = value; }
        }

        /// <summary>
        /// determines whether July is the best travel time to visit this city
        /// </summary>
        [Column]
        public bool BestTimeJul
        {
            get { return bestTimeJul; }
            set { bestTimeJul = value; }
        }

        /// <summary>
        /// determines whether August is the best travel time to visit this city
        /// </summary>
        [Column]
        public bool BestTimeAug
        {
            get { return bestTimeAug; }
            set { bestTimeAug = value; }
        }

        /// <summary>
        /// determines whether September is the best travel time to visit this city
        /// </summary>
        [Column]
        public bool BestTimeSep
        {
            get { return bestTimeSep; }
            set { bestTimeSep = value; }
        }

        /// <summary>
        /// determines whether October is the best travel time to visit this city
        /// </summary>
        [Column]
        public bool BestTimeOct
        {
            get { return bestTimeOct; }
            set { bestTimeOct = value; }
        }

        /// <summary>
        /// determines whether November is the best travel time to visit this city
        /// </summary>
        [Column]
        public bool BestTimeNov
        {
            get { return bestTimeNov; }
            set { bestTimeNov = value; }
        }

        /// <summary>
        /// determines whether December is the best travel time to visit this city
        /// </summary>
        [Column]
        public bool BestTimeDec
        {
            get { return bestTimeDec; }
            set { bestTimeDec = value; }
        }

        /// <summary>
        /// string comment regarding the climate of the city
        /// </summary>
        [Column]
        public string CommentClimate
        {
            get { return commentClimate; }
            set { commentClimate = value; }
        }

        /// <summary>
        /// string comment regarding Activity1 of the city
        /// </summary>
        [Column]
        public string CommentActivity
        {
            get { return commentActivity; }
            set { commentActivity = value; }
        }


        /// <summary>
        /// string comment regarding the city in general
        /// </summary>
        [Column]
        public string CommentGeneral
        {
            get { return commentGeneral; }
            set { commentGeneral = value; }
        }

		#endregion
	}
}
