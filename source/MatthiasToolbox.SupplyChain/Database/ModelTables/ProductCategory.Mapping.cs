using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;

namespace MatthiasToolbox.SupplyChain.Database.ModelTables
{
    [Table(Name="tblProductCategories")]
    public class ProductCategory
    {
        #region ctor

        /// <summary>
        /// default ctor
        /// </summary>
        public ProductCategory() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="comment"></param>
        public ProductCategory(string name, int level, int pack, double shelfLife, double timeRange, string comment = "")
        {
            this.Name = name;
            this.Level = level;
            this.Pack = pack;
            this.ShelfLife = shelfLife;
            this.TimeRange = timeRange;
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
        public int Level { get; set; }

        [Column]
        public int Pack { get; set; }

        [Column]
        public double ShelfLife { get; set; }

        [Column]
        public double TimeRange { get; set; }

        [Column]
        public string Comment { get; set; }
        
        #endregion
    }
}
