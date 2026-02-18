using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;

namespace MatthiasToolbox.SupplyChain.Database.ModelTables
{
    [Table(Name="tblPackageCategories")]
    public class PackageCategory
    {
        #region ctor

        /// <summary>
        /// default ctor
        /// </summary>
        public PackageCategory() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="comment"></param>
        public PackageCategory(string name, string comment = "")
        {
            this.Name = name;
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
        public string Comment { get; set; }
        
        #endregion
    }
}
