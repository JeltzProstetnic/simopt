using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;

namespace MatthiasToolbox.SupplyChain.Database.DataExtensions
{
    [Table(Name="tblDataExtensions")]
    public class DataExtension
    {
        #region ctor

        public DataExtension() { }

        /// <summary>
        /// default ctor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="tableName"></param>
        /// <param name="dataType"></param>
        public DataExtension(string name, string tableName, DataExtensionType dataType, bool hasMultipleValues = false)
        {
            this.Name = name;
            this.TableName = tableName;
            this.DataType = dataType.ID;
            this.HasMultipleValues = hasMultipleValues;
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
        public string TableName { get; set; }

        [Column]
        public int DataType { get; set; }

        [Column]
        public bool HasMultipleValues { get; set; }
        
        #endregion
        #region impl

        public static bool Exists(string tableName, string name)
        {
            return Global.ModelDatabase.DataExtensionsByTableName.ContainsKey(tableName) && Global.ModelDatabase.DataExtensionsByTableName[tableName].ContainsKey(name);
        }

        #endregion
    }
}
