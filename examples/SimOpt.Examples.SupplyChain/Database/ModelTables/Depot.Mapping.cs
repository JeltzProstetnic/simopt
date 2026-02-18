using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;

namespace MatthiasToolbox.SupplyChain.Database.ModelTables
{
    [Table(Name="tblDepots")]
    public partial class Depot
    {
        #region ctor

        /// <summary>
        /// default ctor
        /// </summary>
        public Depot() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="comment"></param>
        public Depot(string name, DepotCategory category, DateTime start, TimeSpan transportTime, TimeSpan preparationTime, Area area, ShelfLifeZone zone)
        {
            this.Name = name;
            this.CategoryID = category.ID;
            this.StartDate = start.Ticks;
            this.TransportTime = transportTime.Ticks;
            this.PreparationTime = preparationTime.Ticks;
            this.AreaID = area.ID;
            this.ZoneID = zone.ID;
        }

        #endregion
        #region data

        [Column(AutoSync = AutoSync.OnInsert,
                IsPrimaryKey = true,
                IsDbGenerated = true)]
        public int ID { get; private set; }

        [Column]
        public int CategoryID { get; set; }

        [Column]
        public string Name { get; set; }

        [Column]
        public double StartDate { get; set; }

        [Column]
        public double TransportTime { get; set; }

        [Column]
        public double PreparationTime { get; set; }

        [Column]
        public int AreaID { get; set; }

        [Column]
        public int ZoneID { get; set; }

        #endregion
    }
}
