using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;

namespace MatthiasToolbox.SupplyChain.Database.ModelTables
{
    [Table(Name="tblShelfLifeZones")]
    public class ShelfLifeZone
    {
        #region ctor

        /// <summary>
        /// default ctor
        /// </summary>
        public ShelfLifeZone() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="comment"></param>
        public ShelfLifeZone(string name, double shelfLife, string comment = "")
        {
            this.Name = name;
            this.ShelfLife = shelfLife;
            this.Comment = comment;
        }

        #endregion
        #region data

        [Column(AutoSync = AutoSync.OnInsert,
                IsPrimaryKey = true,
                IsDbGenerated = true)]
        public int ID { get; private set; }

        [Column]
        public string Name { get; set; }

        [Column]
        public double ShelfLife { get; set; }

        [Column]
        public string Comment { get; set; }
        
        #endregion
    }
}
