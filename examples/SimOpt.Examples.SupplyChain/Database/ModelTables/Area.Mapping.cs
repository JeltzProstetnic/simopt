using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;

namespace MatthiasToolbox.SupplyChain.Database.ModelTables
{
    [Table(Name="tblAreas")]
    public class Area
    {
        #region ctor

        /// <summary>
        /// default ctor
        /// </summary>
        public Area() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="comment"></param>
        public Area(string name, RandomizationBlock block)
        {
            this.Name = name;
            this.BlockID = block.ID;
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
        public int BlockID { get; set; }

        #endregion
    }
}
