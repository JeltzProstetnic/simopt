using System;
using System.Linq;
using System.Collections.Generic;
using System.Data.Linq.Mapping;

namespace MatthiasToolbox.Geography
{
	/// <summary>
	/// a MacroRegion with Name, ID, coordinates (useless, only there due to Interface), SubRegions and
    /// Image of MacroRegionshape (path)
	/// </summary>
	[Table(Name = "tblMacroRegion")]
    public class MacroRegion : ILocation
    {
        #region over

        /// <summary>
        /// overriden: will return the name of the MacroRegion
        /// </summary>
        public override string ToString()
        {
            return name;
        }

        #endregion
        #region prop

        /// <summary>
        /// Dictionary with the SubRegion name as key and the SubRegion itself as value;
        /// uses the SubRegionTable in BaseDatabase and a LINQ query;
        /// dependent on the SubRegionKeySelector
        /// </summary>
        public Dictionary<string, SubRegion> SubRegions {
			get {
				return (from row in GeoDatabase.Instance.SubRegionTable
				        where row.MacroRegionID == ID
				        select row).ToDictionary<SubRegion, string>(SubRegionKeySelector);
			}
		}

        /// <summary>
        /// the whole Name and path of the Image File of the MacroRegionShape
        /// </summary>
        public String ShapeFile { get; set; }

		#endregion
		#region ctor
		
        /// <summary>
        /// empty ctor for MacroRegion class
        /// </summary>
		public MacroRegion() { }
		
        /// <summary>
        /// ctor for MacroRegion class requiring the Name of the MacroRegion
        /// </summary>
		public MacroRegion(string name) : this()
		{
			this.name = name;
		}
		
		#endregion
		#region data
		
		#pragma warning disable 0649
		private int id;
		#pragma warning restore 0649
		private string name;
		
        /// <summary>
        ///  the MacroRegion ID, which is the primary key of the MacroRegionTable
        /// </summary>
		[Column(Storage="id",
		        AutoSync = AutoSync.OnInsert,
		        IsPrimaryKey = true,
		        IsDbGenerated = true)]
		public int ID {
			get { return id; }
		}
		
        /// <summary>
        /// the name of the MacroRegion
        /// </summary>
		[Column]
		public string Name {
			get { return name; }
			set { name = value; }
		}
		
        /// <summary>
        /// the Latitude of the MacroRegion
        /// </summary>
		[Column]
		public double Latitude {get; set;}

        /// <summary>
        /// the Longitude of the MacroRegion
        /// </summary>
		[Column]
		public double Longitude {get; set;}

		#endregion
        #region util

        /// <summary>
        /// used by the SubRegions property to create a dictionary
        /// with the SubRegion name as key and the SubRegion itself as value
        /// </summary>
        private string SubRegionKeySelector(SubRegion sr)
        {
            return sr.Name;
        }

        #endregion
	}
}
