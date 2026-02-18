using System;
using System.Linq;
using System.Collections.Generic;
using System.Data.Linq.Mapping;

namespace MatthiasToolbox.Geography
{
	/// <summary>
    /// a SubRegion with Name, ID, ParentRegion(MacroRegion), MacroRegionID, Countries Dictionary, 
    /// coordinates(useless -> only there due to the Interface) and
    /// Image of SubRegionshape (path)
	/// </summary>
	[Table(Name = "tblSubRegion")]
    public class SubRegion : ILocation
    {
        #region over

        /// <summary>
        /// overriden: will return the name of the SubRegion
        /// </summary>
        public override string ToString()
        {
            return name;
        }

        #endregion
        #region prop

        /// <summary>
        /// the getter of this property will return the Parent MacroRegion of the SubRegion.
        /// the setter tries to set the macroRegionID of the SubRegion to the ID of the given MacroRegion; 
        /// if the given ID doesn't belong to a MacroRegion an ArgumentException will be thrown
        /// </summary>
        public ILocation Parent
        {
            get
            {
                return ParentRegion;
            }
            set
            {
                if (value is MacroRegion)
                {
                    macroRegionID = value.ID;
                }
                else
                {
                    throw new ArgumentException("The parent region of a subregion must be of the type macroregion");
                }
            }
        }

        /// <summary>
        /// Using the MacroRegionTable and a LING query, the getter returns the MacroRegion to which the SubRegion belongs.
        /// the setter sets the MacroRegionID of the SubRegion
        /// </summary>
		public MacroRegion ParentRegion {
			get { 
				return (from row in GeoDatabase.Instance.MacroRegionTable
				        where row.ID == macroRegionID
				        select row).ToList()[0];
			}
			set{ macroRegionID = value.ID; } 
		}

        /// <summary>
        /// the whole Name and path of the Image File of the SubRegionshape
        /// </summary>
        public String ShapeFile { get; set; }

        /// <summary>
        /// Dictionary containing the Countries of the SubRegion;
        /// with the country name as key and the country itself as value;
        /// uses the CountryTable in BaseDatabase and a LINQ query;
        /// dependent on the CountryKeySelector
        /// </summary>
		public Dictionary<string, Country> Countries {
			get {
                return (from row in GeoDatabase.Instance.CountryTable
				        where row.SubRegionID == ID
				        orderby row.Name 
				        select row).ToDictionary<Country, string>(CountryKeySelector);
			}
		}

        #endregion
        #region ctor

        /// <summary>
        /// empty ctor for SubRegion class
        /// </summary>
        public SubRegion() { }
		
        /// <summary>
        /// ctor for SubRegion class, requiring the name of the Subregion 
        /// and the MacroRegion to which it belongs
        /// </summary>
		public SubRegion(string name, MacroRegion parentRegion) : this()
		{
			this.name = name;
			this.macroRegionID = parentRegion.ID;
		}

        #endregion
		#region data
		
		#pragma warning disable 0649
		private int id;
		#pragma warning restore 0649
		private string name;
		private int macroRegionID;
		
        /// <summary>
        /// the SubRegion ID, which is the primary key of the SubRegionTable
        /// </summary>
		[Column(Storage="id",
		        AutoSync = AutoSync.OnInsert,
		        IsPrimaryKey = true,
		        IsDbGenerated = true)]
		public int ID {
			get { return id; }
		}
		
        /// <summary>
        /// the name of the SubRegion
        /// </summary>
		[Column]
		public string Name {
			get { return name; }
			set { name = value; }
		}
		
        /// <summary>
        /// the ID of the MacroRegion to which the SubRegion belongs
        /// </summary>
		[Column]
		public int MacroRegionID {
			get { return macroRegionID; }
			set { macroRegionID = value; }
		}
		
        /// <summary>
        /// Latitude of the coordinates of the SubRegion
        /// useless -> only there due to the Interface
        /// </summary>
		[Column]
		public double Latitude {get; set;}

        /// <summary>
        /// Longitude of the coordinates of the SubRegion
        /// useless -> only there due to the Interface
        /// </summary>
		[Column]
		public double Longitude {get; set;}
		#endregion
        #region util

        /// <summary>
        /// used by the Countries property to create a dictionary
        /// with the Country name as key and the Country itself as value
        /// </summary>
        private string CountryKeySelector(Country c)
        {
            return c.Name;
        }

        #endregion
    }
}
