using System;
using System.Linq;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using MatthiasToolbox.Logging;

namespace MatthiasToolbox.Geography
{
	/// <summary>
	/// a Country with Name, CountryID, parent Subregion, Macroregion, Cities Dictionary, Locations Dictionary, 
    /// Capital, ClimateInformationID, Currency, Currency Subunit, Name Of Citizenship, Adjective Of Citizenship,
    /// Image of countryshape (path), language and bool variables that determine the best travel time;
    /// plus several comment fields;
	/// </summary>
	[Table(Name = "tblCountry")]
    public class Country : ILocation
    {
        #region over

        /// <summary>
        /// overriden: will return the name of the country
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return name;
        }

        #endregion
        #region prop

        /// <summary>
        /// the getter of this property will return the Parent SubRegion of the Country.
        /// the setter tries to set the countryID of the Country to the ID of the given SubRegion; 
        /// if the given ID doesn't belong to a SubRegion an ArgumentException will be thrown
        /// </summary>
        public ILocation Parent
        {
            get { return SubRegion; }
            set
            {
                if (value is SubRegion)
                {
                    subRegionID = value.ID;
                }
                else
                {
                    throw new ArgumentException("The parent region of a country must be of the type SubRegion");
                }
            }
        }

        /// <summary>
        /// the whole Name and path of the Image File of the Countryshape
        /// </summary>
        public String ShapeFile { get; set; }

        /// <summary>
        /// using the CityTable in BaseDatabase and a LINQ query
        /// this getter determines if there are cities associated to
        /// this country in the current database
        /// </summary>
        public bool HasCities {
            get
            {
                return (from row in GeoDatabase.Instance.CityTable
                        where row.CountryID == ID
                        select row).Any();
            }
        }

        /// <summary>
        /// Dictionary with the city name as key and the city itself as value;
        /// uses the CityTable in BaseDatabase and a LINQ query;
        /// dependent on the CityKeySelector
        /// </summary>
		public Dictionary<string, City> Cities {
			get {
                return (from row in GeoDatabase.Instance.CityTable
                        where row.CountryID == ID
                        select row).ToDictionary<City, string>(CityKeySelector);

                // equivalent to the above:
                //return (from row in BaseDatabase.OpenInstance.CityTable
                //        where row.CountryID == ID
                //        select row).ToDictionary<City, string>(c => c.Name);
			}
		}

        /// <summary>
        /// using the LocationTable in BaseDatabase and a LINQ query
        /// this getter determines if there are locations associated to
        /// this country in the current database
        /// </summary>
        public bool HasLocations
        {
            get
            {
                return (from row in GeoDatabase.Instance.LocationTable
                        where row.CountryID == ID
                        orderby row.Name
                        select row).Count() > 0;
            }
        }

        /// <summary>
        /// Dictionary with the location name as key and the location itself as value;
        /// uses the LocationTable in BaseDatabase and a LINQ query;
        /// dependent on the LocationKeySelector
        /// </summary>
		public Dictionary<string, Location> Locations {
			get {
				// return locations;
                return (from row in GeoDatabase.Instance.LocationTable
				        where row.CountryID == ID
				        orderby row.Name
				        select row).ToDictionary<Location, string>(LocationKeySelector);
			}
		}
		
		
		/// <summary>
        /// using the CityTable in BaseDatabase and a LINQ query,
        /// this getter returns the Capital City of the Country if one exists;
        /// otherwise it will return null
		/// </summary>
		public City Capital {
			get {
                List<City> capitals = (from row in GeoDatabase.Instance.CityTable
				                       where row.CountryID == ID && row.Capital == true
				                       select row).ToList<City>();
				if(capitals.Count != 0)
					return capitals[0];
				else
					return null;
			}
		}
		
        /// <summary>
        /// will return the MacroRegion to which the Country belongs
        /// </summary>
		public MacroRegion MacroRegion {
			get { return SubRegion.ParentRegion; }
		}
		
        /// <summary>
        /// the SubRegion to which the Country belongs
        /// </summary>
		public SubRegion SubRegion {
			get {
				// return city;
				return (from row in GeoDatabase.Instance.SubRegionTable
				        where row.ID == subRegionID
				        select row).ToList()[0];
			}
			set{ subRegionID = value.ID;}
			
		}

        public bool HasClimateInfo { get { return ClimateInformationID != 0 || Capital.ClimateInformationID != 0; } }
		
        /// <summary>
        /// using the ClimateInformationTable in BaseDatabase and a LINQ query,
        /// this getter returns the Country's ClimateInformation;
        /// if there is no ClimateInformation it will the climate
        /// information for the country's capital city
        /// </summary>
		public ClimateInformation ClimateInfo {
			get {
                if (ClimateInformationID == 0 && Capital.ClimateInformationID != 0)
                {
                    return Capital.ClimateInfo;
                }
                else if (ClimateInformationID == 0)
                {
                    return new ClimateInformation();
                }
                return (from row in GeoDatabase.Instance.ClimateInformationTable
				        where row.ID == ClimateInformationID
				        select row).ToList()[0];
			}
		}

        #endregion
        #region ctor
        
        /// <summary>
        /// empty ctor for Country class
        /// </summary>
        public Country() { }
		

		// : this() calls the empty constructor (above) so that
		// the cities Dictionary will be created (instantiated)
		// saves on duplicate code - both constructors need to create the dictionary.
        /// <summary>
        /// ctor for Country class requiring the Name of the Country, the CountryCode 
        /// and the Subregion to which the Country belongs
        /// </summary>
		public Country(string name, string countryCode, SubRegion region) : this()
		{
			this.name = name;
			this.countryCode = countryCode;
			this.subRegionID = region.ID;
		}

        #endregion
		#region data
		
		#pragma warning disable 0649
		private int id;
		#pragma warning restore 0649
		private string name;
		private string countryCode;
		private int subRegionID;
		private int climateInformationID;
		private string longName;
		private string shortName;
		private string currency;
		private string currencyCode;
		private string currencySubunit;
		private string nameOfCitizenship;
		private string adjectiveOfCitizenship;
        private string language;
        private string commentClimate;
        private string commentCulture;
        private string commentActivity1;
        private string comment;
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
        private int takeClimateFromCityID;

        /// <summary>
        /// the Country ID, which is the primary key of the CountryTable
        /// </summary>
		[Column(Storage="id",
		        AutoSync = AutoSync.OnInsert,
		        IsPrimaryKey = true,
		        IsDbGenerated = true)]
		public int ID {
			get { return id; }
		}
		
        /// <summary>
        /// the name of the country
        /// </summary>
		[Column]
		public string Name {
			get { return name; }
			set { name = value; }
		}
		
        /// <summary>
        /// the CountryCode of the Country
        /// </summary>
		[Column]
		public string CountryCode {
			get { return countryCode; }
			set { countryCode = value; }
		}
		
        
		
        /// <summary>
        /// the ID of the Subregion to which the country belongs
        /// </summary>
		[Column]
		public int SubRegionID {
			get { return subRegionID; }
			set { subRegionID = value; }
		}
		
        /// <summary>
        /// Latitude of the coordinates of the country's capital
        /// </summary>
		[Column]
		public double Latitude {get; set;}

        /// <summary>
        /// Longitude of the coordinates of the country's capital
        /// </summary>
		[Column]
		public double Longitude {get; set;}
		

        /// <summary>
        /// the ID of the ClimateInformation for the country
        /// </summary>
		[Column]
		public int ClimateInformationID {
			get { return climateInformationID; }
			set { climateInformationID = value; }
		}
		
        /// <summary>
        /// the country's full Name
        /// </summary>
		[Column]
		public string LongName {
			get { return longName; }
			set { longName = value; }
		}
		
        /// <summary>
        /// the short version of the country's Name 
        /// </summary>
		[Column]
		public string ShortName {
			get { return shortName; }
			set { shortName = value; }
		}
		
        /// <summary>
        /// the currency of the country
        /// </summary>
		[Column]
		public string Currency {
			get { return currency; }
			set { currency = value; }
		}
		
        /// <summary>
        /// the code of the country's currency
        /// </summary>
		[Column]
		public string CurrencyCode {
			get { return currencyCode; }
			set { currencyCode = value; }
		}
		
        /// <summary>
        /// the subunit of the country's currency
        /// </summary>
		[Column]
		public string CurrencySubunit {
			get { return currencySubunit; }
			set { currencySubunit = value; }
		}
		
        /// <summary>
        /// the Name Of Citizenship in the country
        /// </summary>
		[Column]
		public string NameOfCitizenship {
			get { return nameOfCitizenship; }
			set { nameOfCitizenship = value; }
		}
		
        /// <summary>
        /// the Adjective Of Citizenship in the country
        /// </summary>
		[Column]
		public string AdjectiveOfCitizenship {
			get { return adjectiveOfCitizenship; }
			set { adjectiveOfCitizenship = value; }
		}

        /// <summary>
        /// the language of the country
        /// </summary>
        [Column]
        public string Language
        {
            get { return language; }
            set { language = value; }
        }

        /// <summary>
        /// string comment regarding the climate of the country
        /// </summary>
        [Column]
        public string CommentClimate
        {
            get { return commentClimate; }
            set { commentClimate = value; }
        }

        /// <summary>
        /// string comment regarding the Culture of the country
        /// </summary>
        [Column]
        public string CommentCulture
        {
            get { return commentCulture; }
            set { commentCulture = value; }
        }
       

        /// <summary>
        /// string comment regarding Activity1 of the country
        /// </summary>
        [Column]
        public string CommentActivity1
        {
            get { return commentActivity1; }
            set { commentActivity1 = value; }
        }


        /// <summary>
        /// string comment regarding the country in general
        /// </summary>
        [Column]
        public string Comment
        {
            get { return comment; }
            set { comment = value; }
        }


        /// <summary>
        /// determines whether January is the best travel time to visit this country
        /// </summary>
        [Column]
        public bool BestTimeJan
        {
            get { return bestTimeJan; }
            set { bestTimeJan = value; }
        }

        /// <summary>
        /// determines whether February is the best travel time to visit this country
        /// </summary>
        [Column]
        public bool BestTimeFeb
        {
            get { return bestTimeFeb; }
            set { bestTimeFeb = value; }
        }

        /// <summary>
        /// determines whether March is the best travel time to visit this country
        /// </summary>
        [Column]
        public bool BestTimeMar
        {
            get { return bestTimeMar; }
            set { bestTimeMar = value; }
        }

        /// <summary>
        /// determines whether April is the best travel time to visit this country
        /// </summary>
        [Column]
        public bool BestTimeApr
        {
            get { return bestTimeApr; }
            set { bestTimeApr = value; }
        }

        /// <summary>
        /// determines whether May is the best travel time to visit this country
        /// </summary>
        [Column]
        public bool BestTimeMay
        {
            get { return bestTimeMay; }
            set { bestTimeMay = value; }
        }

        /// <summary>
        /// determines whether June is the best travel time to visit this country
        /// </summary>
        [Column]
        public bool BestTimeJun
        {
            get { return bestTimeJun; }
            set { bestTimeJun = value; }
        }

        /// <summary>
        /// determines whether July is the best travel time to visit this country
        /// </summary>
        [Column]
        public bool BestTimeJul
        {
            get { return bestTimeJul; }
            set { bestTimeJul = value; }
        }

        /// <summary>
        /// determines whether August is the best travel time to visit this country
        /// </summary>
        [Column]
        public bool BestTimeAug
        {
            get { return bestTimeAug; }
            set { bestTimeAug = value; }
        }

        /// <summary>
        /// determines whether September is the best travel time to visit this country
        /// </summary>
        [Column]
        public bool BestTimeSep
        {
            get { return bestTimeSep; }
            set { bestTimeSep = value; }
        }

        /// <summary>
        /// determines whether October is the best travel time to visit this country
        /// </summary>
        [Column]
        public bool BestTimeOct
        {
            get { return bestTimeOct; }
            set { bestTimeOct = value; }
        }

        /// <summary>
        /// determines whether November is the best travel time to visit this country
        /// </summary>
        [Column]
        public bool BestTimeNov
        {
            get { return bestTimeNov; }
            set { bestTimeNov = value; }
        }

        /// <summary>
        /// determines whether December is the best travel time to visit this country
        /// </summary>
        [Column]
        public bool BestTimeDec
        {
            get { return bestTimeDec; }
            set { bestTimeDec = value; }
        }

        /// <summary>
        /// CityID from which the climate data for the climate tab in the country tab will be taken;
        /// default will be the country's capital
        /// </summary>
        [Column]
        public int TakeClimateFromCityID
        {
            get { return takeClimateFromCityID; }
            set { takeClimateFromCityID = value; }
        }


		#endregion
        #region util

        /// <summary>
        /// used by the Cities property to create a dictionary
        /// with the city name as key and the city itself as value
        /// </summary>
        private string CityKeySelector(City c)
        {
            return c.Name;
        }

        /// <summary>
        /// used by the Locations property to create a dictionary
        /// with the Location name as key and the Location itself as value
        /// </summary>
        private string LocationKeySelector(Location l)
        {
            return l.Name;
        }

        #endregion
    }
}