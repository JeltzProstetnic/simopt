using System;
using System.Linq;
using System.Data.Linq.Mapping;

namespace MatthiasToolbox.Geography
{
	/// <summary>
    /// a Location that has a Name, ID, Type, coordinates, Country, ClimateInfoID, parentID and parentTypeName
	/// </summary>
	[Table(Name = "tblLocation")]
    public class Location : ILocation
    {
        #region over

        /// <summary>
        ///  overriden: will return the name of the location
        /// </summary>
        public override string ToString()
        {
            return name;
        }

        #endregion
        #region prop

        /// <summary>
        /// using the CountryTable in BaseDatabase and a LINQ query,
        /// this getter returns the country of a Location
        /// </summary>
		public Country Country {
			get {
				return (from row in GeoDatabase.Instance.CountryTable
				        where row.ID == countryID
				        select row).ToList()[0];
			}
		}
		
        /// <summary>
        /// using the ClimateInformationTable in BaseDatabase and a LINQ query,
        /// this getter returns the ClimateInformation for a Location;
        /// if none is available the ClimateInformation of the Country will be returned instead
        /// </summary>
		public ClimateInformation ClimateInfo {
			get {
				if(ClimateInformationID == 0) return Country.ClimateInfo;
                return (from row in GeoDatabase.Instance.ClimateInformationTable
				        where row.ID == ClimateInformationID
				        select row).ToList()[0];
			}
		}

        #endregion
        #region ctor

        /// <summary>
        /// empty ctor for Location class
        /// </summary>
        public Location()
		{
		}
		
        /// <summary>
        /// ctor for Location class, requiring the Name and the Type of the location
        /// and the Country to which it belongs
        /// </summary>
		public Location(string name, LocationType locationType, Country c) {
			this.name = name;
			locationTypeID = locationType.ID;
			countryID = c.ID;
			parentID = c.ID;
			parentTypeName = typeof(Country).Name;
		}

        #endregion
        #region data

        #pragma warning disable 0649
        private int id;
		#pragma warning restore 0649
		private string name;
		private int locationTypeID = 1;
		private int climateInformationID;
        private int countryID;
        private int parentID;
        private string parentTypeName;
		
        /// <summary>
        /// the Location ID, which is the primary key of the LocationTable
        /// </summary>
		[Column(Storage="id",
		        AutoSync = AutoSync.OnInsert,
		        IsPrimaryKey = true,
		        IsDbGenerated = true)]
		public int ID {
			get { return id; }
		}
		
        /// <summary>
        /// the name of the location
        /// </summary>
		[Column]
		public string Name {
			get { return name; }
			set { name = value; }
		}
		
        /// <summary>
        /// the ID of the location's type
        /// </summary>
		[Column]
		public int LocationTypeID {
			get { return locationTypeID; }
			set { locationTypeID = value; }
		}
		
        /// <summary>
        /// the Latitude of the location coordinates
        /// </summary>
		[Column]
		public double Latitude {get; set;}

        /// <summary>
        /// the Longitude of the location coordinates
        /// </summary>
		[Column]
		public double Longitude {get; set;}
		
        /// <summary>
        /// the ID of the ClimateInformation for the location
        /// </summary>
		[Column]
		public int ClimateInformationID {
			get { return climateInformationID; }
			set { climateInformationID = value; }
		}
		
        /// <summary>
        /// the ID of the Country to which the location belongs
        /// </summary>
		[Column]
		public int CountryID {
			get { return countryID; }
			set { countryID = value; }
		}
		
        /// <summary>
        /// the ID of the location's parent
        /// </summary>
		[Column]
		public int ParentID {
			get { return parentID; }
			set { parentID = value; }
		}
		
        /// <summary>
        /// the Name of the type of the location's parent
        /// </summary>
		[Column]
		public string ParentTypeName {
			get { return parentTypeName; }
			set { parentTypeName = value; }
		}
		#endregion
    }
}
