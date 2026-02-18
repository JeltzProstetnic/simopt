using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;

namespace MatthiasToolbox.SupplyChain.Database.ModelTables
{
    [Table(Name="tblPackageSizes")]
    public class PackageSize
    {
        #region ctor

        /// <summary>
        /// default ctor
        /// </summary>
        public PackageSize() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="comment"></param>
        public PackageSize(Product product, int size, string comment = "")
        {
            this.ProductID = product.ID;
            this.Size = size;
            this.Comment = comment;
        }

        #endregion
        #region data

        [Column(AutoSync = AutoSync.OnInsert,
                IsPrimaryKey = true,
                IsDbGenerated = true)]
        public int ID { get; private set; }

        [Column]
        public int ProductID { get; set; }

        [Column]
        public int Size { get; set; }

        [Column]
        public string Comment { get; set; }
        
        #endregion
    }
}
