using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using MatthiasToolbox.Logging;

namespace MatthiasToolbox.Geography
{
	/// <summary>
    /// the GeoDatabase (for GeographyData) 
    /// with dictionaries sorting CountriesByCode and CitiesByStationID and
    /// dictionaries for all MacroRegionsByName, all SubRegions, all Countries, all Cities and all LocationTypesByName;
    /// provides a function to initialize /reset the Database
	/// </summary>
	public class GeoDatabase : DataContext
	{
		#region cvar

        private static GeoDatabase instance;

		#region static

        private static bool resetDB = false;
		private static bool initialized;
		
		#endregion
		#region tables
		
		/// <summary>
        /// <see cref="MacroRegion"/>
		/// </summary>
        public readonly Table<MacroRegion> MacroRegionTable;
        
        /// <summary>
        /// <see cref="SubRegion"/>
        /// </summary>
        public readonly Table<SubRegion> SubRegionTable;
        
        /// <summary>
        /// <see cref="Country"/>
        /// </summary>
        public readonly Table<Country> CountryTable;

        /// <summary>
        /// <see cref="City"/>
        /// </summary>
        public readonly Table<City> CityTable;

        /// <summary>
        /// <see cref="ClimateInformation"/>
        /// </summary>
        public readonly Table<ClimateInformation> ClimateInformationTable;

        /// <summary>
        /// <see cref="Location"/>
        /// </summary>
        public readonly Table<Location> LocationTable;

        /// <summary>
        /// <see cref="LocationType"/>
        /// </summary>
        public readonly Table<LocationType> LocationTypeTable;
		
		#endregion
		#region dictionaries
		
		private static Dictionary<string, MacroRegion> macroRegions =
			new Dictionary<string, MacroRegion>();

        private static Dictionary<string, SubRegion> subRegions =
            new Dictionary<string, SubRegion>();

        private static Dictionary<string, LocationType> locationTypes =
			new Dictionary<string, LocationType>();

        private static Dictionary<string, Country> countriesByCode =
			new Dictionary<string, Country>();

        private static Dictionary<int, Country> countriesByID =
            new Dictionary<int, Country>();
        
		#endregion
		
		#endregion
		#region prop

        /// <summary>
        /// the currently active instance
        /// </summary>
        public static GeoDatabase Instance { get { return instance; } }

        #region dictionaries

        /// <summary>
        /// Dictionary that sorts Countries By Code;
        /// with the country code (upper case!) as key,
        /// and the Country as value
		/// </summary>
        public static Dictionary<string, Country> CountriesByCode
        {
			get { return countriesByCode; }
		}

        /// <summary>
        /// Dictionary that sorts Countries By Code;
        /// with the country code (upper case!) as key,
        /// and the Country as value
        /// </summary>
        public static Dictionary<int, Country> CountriesByID
        {
            get { return countriesByID; }
        }

        /// <summary>
        /// Dictionary for MacroRegionsByName
        /// with the Name of the MacroRegion as key
        /// and the MacroRegion itself as value
        /// </summary>
        public static Dictionary<string, MacroRegion> MacroRegionsByName
        {
			get { return macroRegions; }
		}

        /// <summary>
        /// Dictionary for SubRegionsByName
        /// with the Name of the SubRegion as key
        /// and the SubRegion itself as value
        /// </summary>
        public static Dictionary<string, SubRegion> SubRegionsByName
        {
            get { return subRegions; }
        }

        /// <summary>
        /// Dictionary for LocationTypesByName
        /// with the Name of the LocationType as key
        /// and the LocationType itself as value
        /// </summary>
        public static Dictionary<string, LocationType> LocationTypesByName
        {
			get { return locationTypes; }
		}

        #endregion
        #region queries

        /// <summary>
        /// an IEnumerable containing all SubRegions in the BaseDatabase
        /// </summary>
		public IEnumerable<SubRegion> SubRegions 
        {
			get {
				return (from row in SubRegionTable
				        select row).AsEnumerable();
			}
		}
		
        /// <summary>
        /// an IEnumerable containing all Countries in the BaseDatabase
        /// </summary>
		public IEnumerable<Country> Countries {
			get {
				return (from row in CountryTable
				        orderby row.Name
				        select row).AsEnumerable();
			}
		}
		
        /// <summary>
        /// an IEnumerable containing all Cities in the BaseDatabase
        /// </summary>
		public IEnumerable<City> Cities {
			get {
				foreach(Country c in Countries){
					foreach(City ci in c.Cities.Values)
						yield return ci;
				}
			}
		}

        #endregion

        #endregion
        #region ctor

        /// <summary>
        /// constructor calls base (DataContext) passing on the connection string
        /// this will open the db connection.
        /// </summary>
        /// <param name="connection">a viable linq connection string</param>
		private GeoDatabase(string connection) : base(connection)
        {
            macroRegions = new Dictionary<string, MacroRegion>();
            countriesByCode = new Dictionary<string, Country>();
            countriesByID = new Dictionary<int, Country>();
        }
		
		#endregion
        #region init

        /// <summary>
        /// Creates an instance of GeoDatabase 
        /// If no database exists under the given 
        /// name an empty instance will be created.
        /// </summary>
        /// <param name="connection">connection or path and filename for the database</param>
        /// <returns>a success flag</returns>
        public static bool Initialize(string connection = "GeoDatabase.sdf")
        {
            if (initialized) return true;

            try
            {
                // create instance
                instance = new GeoDatabase(connection);

                // delete command?
                if (resetDB && instance.DatabaseExists())
                {
                    instance.DeleteDatabase();
                    instance.Log<INFO>("An existing version of the database was deleted.");
                }

                // create?
                if (!instance.DatabaseExists()) instance.CreateDatabase();
                
                // prepare for use
                FillDictionaries();

                // finish
                instance.Log<INFO>("GeoDatabase ready.");
                initialized = true;
                return true;
            }
            catch (Exception e)
            {
                Logger.Log<ERROR>("MatthiasToolbox.Geography.GeoDatabase", "Error opening DB connection or reading data: ", e);
                return false;
            }
        }

        private static void FillDictionaries()
        {
            foreach (MacroRegion mr in instance.MacroRegionTable) 
                MacroRegionsByName[mr.Name] = mr;

            foreach (SubRegion sr in instance.SubRegionTable)
                SubRegionsByName[sr.Name] = sr;

            foreach (Country c in GeoDatabase.Instance.CountryTable)
            {
                CountriesByCode[c.CountryCode] = c;
                CountriesByID[c.ID] = c;
            }
        }

        #endregion
		#region impl

        /// <summary>
        /// create a new BaseDatabase at the given location.
        /// </summary>
        /// <param name="file">a valid path and filename</param>
        /// <returns>success flag</returns>
        public static bool CreateNew(string file)
        {
            GeoDatabase.resetDB = true;
            return Initialize(file);
        }

        /// <summary>
        /// closes the connection
        /// </summary>
        public static void Close()
        {
            try
            {
                instance.Connection.Close();
            }
            catch { /* CANNOT RECOVER */ }
        }

        #region lookup

        /// <summary>
        /// may return null if no city with the given id is found
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public City FindCityByID(int id)
        {
            if (id < 1) return null;
            var x = from row in CityTable where row.ID == id select row;
            if (!x.Any()) return null;
            else return x.First() as City;
        }

        /// <summary>
        /// may return null if no location with the given id is found
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Location FindLocationByID(int id)
        {
            if (id < 1) return null;
            var x = from row in LocationTable where row.ID == id select row;
            if (!x.Any()) return null;
            else return x.First() as Location;
        }

        #endregion

        #endregion
    } // class
} // namespace