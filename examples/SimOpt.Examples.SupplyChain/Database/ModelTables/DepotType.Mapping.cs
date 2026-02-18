using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;

namespace MatthiasToolbox.SupplyChainSimulator.Database.ModelTables
{
    [Table(Name="tblDepotCategories")]
    public class DepotCategory
    {
        #region ctor

        /// <summary>
        /// default ctor
        /// </summary>
        public DepotCategory() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="comment"></param>
        public DepotCategory(string name)
        {
            this.Name = name;
        }

        #endregion
        #region data

        [Column(AutoSync = AutoSync.OnInsert,
                IsPrimaryKey = true,
                IsDbGenerated = true)]
        public int ID { get; private set; }

        [Column]
        public string Name { get; set; }

        #endregion
    }
}
