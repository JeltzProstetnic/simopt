using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;

namespace MatthiasToolbox.SupplyChain.Database.ModelTables
{
    [Table(Name="tblSites")]
    public partial class Site
    {
        #region ctor

        /// <summary>
        /// default ctor
        /// </summary>
        public Site() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="depot"></param>
        /// <param name="category"></param>
        /// <param name="area"></param>
        /// <param name="reservation"></param>
        /// <param name="reservationPeriod"></param>
        /// <param name="initialStock"></param>
        /// <param name="minStock"></param>
        /// <param name="maxStock"></param>
        /// <param name="maxPatients"></param>
        /// <param name="transportTime"></param>
        public Site(string name, 
            Depot depot, 
            SiteCategory category, 
            Area area, 
            bool reservation, 
            int reservationPeriod, 
            int initialStock, 
            int minStock, 
            int maxStock, 
            int maxPatients, 
            TimeSpan transportTime)
        {
            this.Name = name;
            this.DepotID = depot.ID;
            this.CategoryID = category.ID;
            this.AreaID = area.ID;
            this.TransportTime = transportTime.Ticks;
            this.Reservation = reservation;
            this.ReservationPeriod = reservationPeriod;
            this.InitialStock = initialStock;
            this.MinimalStock = minStock;
            this.MaximalStock = maxStock;
            this.MaximalNumberOfPatients = maxPatients;
        }

        #endregion
        #region data

        [Column(AutoSync = AutoSync.OnInsert,
                IsPrimaryKey = true,
                IsDbGenerated = true)]
        public int ID { get; private set; }

        [Column]
        public int DepotID { get; set; }

        [Column]
        public int CategoryID { get; set; }

        [Column]
        public int AreaID { get; set; }

        [Column]
        public string Name { get; set; }

        [Column]
        public bool Reservation { get; set; }

        [Column]
        public int StartDelay { get; set; }

        [Column]
        public int ReservationPeriod { get; set; }

        [Column]
        public int InitialStock { get; set; }

        [Column]
        public int MinimalStock { get; set; }

        [Column]
        public int MaximalStock { get; set; }

        [Column]
        public int MaximalNumberOfPatients { get; set; }

        [Column]
        public double TransportTime { get; set; }

        [Column]
        public string Comment { get; set; }

        #endregion
    }
}
