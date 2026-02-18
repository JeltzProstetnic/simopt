using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;

namespace MatthiasToolbox.SupplyChain.Database.ModelTables
{
    [Table(Name="tblProducts")]
    public class Product
    {
        #region ctor

        /// <summary>
        /// default ctor
        /// </summary>
        public Product() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="comment"></param>
        public Product(string name, String comment = "")
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
