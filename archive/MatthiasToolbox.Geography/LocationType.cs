using System;
using System.Data.Linq.Mapping;

namespace MatthiasToolbox.Geography
{
	/// <summary>
	/// a locationtype with Name and ID
	/// </summary>
	[Table(Name = "tblLocationType")]
	public class LocationType
    {
        #region ctor

        /// <summary>
        /// empty ctor for LocationType class
        /// </summary>
        public LocationType()
        {
        }

        /// <summary>
        /// ctor for LocationType class, requiring the name of the LocationType
        /// </summary>
        public LocationType(string name)
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
        /// the LocationType ID, which is the primary key of the LocationTypeTable
        /// </summary>
		[Column(Storage="id",
		        AutoSync = AutoSync.OnInsert,
		        IsPrimaryKey = true,
		        IsDbGenerated = true)]
		public int ID {
			get { return id; }
		}
		
        /// <summary>
        /// the name of the LocationType
        /// </summary>
		[Column]
		public string Name {
			get { return name; }
			set { name = value; }
		}

        #endregion
	}
}
