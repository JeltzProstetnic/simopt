using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;
using MatthiasToolbox.SupplyChain.Interfaces;

namespace MatthiasToolbox.SupplyChain.Database.DataExtensions
{
    [Table(Name="tblIntegerDataExtensions")]
    public class IntegerDataExtension
    {
        #region ctor

        /// <summary>
        /// default ctor
        /// </summary>
        public IntegerDataExtension() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="extension"></param>
        /// <param name="item"></param>
        /// <param name="value"></param>
        public IntegerDataExtension(DataExtension extension, ITableMapping item, int value)
        {
            this.ExtensionID = extension.ID;
            this.ItemID = item.ID;
            this.Value = value;
        }

        #endregion
        #region data

        [Column(AutoSync = AutoSync.OnInsert,
                IsPrimaryKey = true,
                IsDbGenerated = true)]
        public int ID { get; private set; }

        [Column]
        public int ExtensionID { get; set; }

        [Column]
        public int ItemID { get; set; }
        
        [Column]
        public int Value { get; set; }
        
        #endregion
    }
}
