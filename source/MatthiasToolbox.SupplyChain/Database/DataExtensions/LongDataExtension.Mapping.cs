using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;
using MatthiasToolbox.SupplyChain.Interfaces;

namespace MatthiasToolbox.SupplyChain.Database.DataExtensions
{
    [Table(Name="tblLongDataExtensions")]
    public class LongDataExtension
    {
        #region ctor

        /// <summary>
        /// default ctor
        /// </summary>
        public LongDataExtension() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="extension"></param>
        /// <param name="item"></param>
        /// <param name="value"></param>
        public LongDataExtension(DataExtension extension, ITableMapping item, long value)
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
        public long Value { get; set; }
        
        #endregion
    }
}
