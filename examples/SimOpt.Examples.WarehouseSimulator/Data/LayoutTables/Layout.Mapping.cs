using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;
using System.Data.OracleClient;
using System.Data;
using MatthiasToolbox.Logging;
using System.Collections;

#pragma warning disable 0618 // OracleClient is obsolet - switch to devart dotConnect before migrating to .NET 5.0

namespace Vr.WarehouseSimulator.Data.LayoutTables
{
    public class LayoutWrapper { public IList GetNewBindingList() { return Layout.GetNewBindingList(); } }

    // [Table(EntityName="LLAYOUT")]
    public partial class Layout
    {
        #region ctor

        public Layout() { }

        public Layout(int? id, string name, int? propertyBagID) 
        {
            int bagID = 0;
            this.ID = id.HasValue ? id.Value : 0;
            this.NAME = name;
            if (propertyBagID.HasValue) bagID = propertyBagID.Value;
            this.PROPERTYBAG_ID = bagID;
        }

        #endregion
        #region data

        // [Column(CanBeNull = false, IsPrimaryKey = true, DbType = "NUMBER(11) NOT NULL")]
        public int ID { get; set; }

        // [Column(CanBeNull = true, DbType = "VARCHAR2(255)")]
        public string NAME { get; set; }

        // [Column(CanBeNull = true, DbType = "NUMBER(11)")]
        public int PROPERTYBAG_ID { get; set; }
        
        #endregion
        #region impl

        public static IList GetNewBindingList()
        {
            IList result = new List<Layout>();
            string cmdQuery = "SELECT ID, NAME, PROPERTYBAG_ID FROM LLAYOUT";
            OracleCommand cmd = new OracleCommand(cmdQuery, Global.LayoutDatabase.Connection);
            cmd.CommandType = CommandType.Text;
            int n = 0;

            try
            {
                OracleDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    n += 1;
                    result.Add(new Layout(reader.GetValue(0) as int?, reader.GetValue(1) as string, reader.GetValue(2) as int?));
                }
            }
            catch (Exception ex)
            {
                Logger.Log(new Layout(), "Error in row " + n.ToString() + ": ", ex);
            }
            finally
            {
                cmd.Dispose();
            }

            Logger.Log<INFO>(n.ToString() + " rows imported.");

            return result;
        }

        #endregion
    }
}
