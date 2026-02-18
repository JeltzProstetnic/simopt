using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;

namespace MatthiasToolbox.SupplyChain.Database.ModelTables
{
    [Table(Name="tblShelfLives")]
    public class ShelfLife
    {
        #region ctor

        /// <summary>
        /// default ctor
        /// </summary>
        public ShelfLife() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="comment"></param>
        public ShelfLife(ShelfLifeZone zone, Product product, double shelfLife)
        {
            this.ZoneID = zone.ID;
            this.ProductID = product.ID;
            this.ShelfLifeValue = shelfLife;
        }

        #endregion
        #region data

        [Column(AutoSync = AutoSync.OnInsert,
                IsPrimaryKey = true,
                IsDbGenerated = true)]
        public int ID { get; private set; }

        [Column]
        public int ZoneID { get; set; }

        [Column]
        public int ProductID { get; set; }

        [Column]
        public double ShelfLifeValue { get; set; }
        
        #endregion
    }
}
