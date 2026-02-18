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

namespace Vr.WarehouseSimulator.Data.ProcessTables
{
    public class PDataWrapper { public IList GetNewBindingList() { return PData.GetNewBindingList(); } }

    // [Table(EntityName="PNODE")]
    public partial class PData
    {
        #region ctor

        public PData() { }

        public PData(int id, int pNodeID, decimal? fixedResource = 0, decimal? target = 0, decimal? delay = 0,
            decimal? targetTypeID = 0, decimal? targetInstanceID = 0, decimal? frTypeID = 0, decimal? frInstanceID = 0,
            string frClassName = "", string targetClassName = "", string frType = "", string targetType = "", decimal? itemTypeID = 0,
            bool? isCounterEnabled = false, decimal? counterID = 0, bool? isStrategyEnabled = false, string strategyName = "")
        {
            ID = id;
            PNODEID = pNodeID;
            FIXEDRESOURCE = fixedResource ?? 0;
            TARGET = target ?? 0;
            DELAY = delay ?? 0;
            TARGETTYPEID = targetTypeID ?? 0;
            TARGETINSTANCEID = targetInstanceID ?? 0;
            FRTYPEID = frTypeID ?? 0;
            FRINSTANCEID = frInstanceID ?? 0;
            FRCLASSNAME = frClassName;
            TARGETCLASSNAME = targetClassName;
            FRTYPE = frType;
            TARGETTYPE = targetType;
            ITEMTYPEID = itemTypeID ?? 0;
            ISCOUNTERENABLED = isCounterEnabled ?? false;
            COUNTERID = counterID ?? 0;
            ISSTRATEGYENABLED = isStrategyEnabled ?? false;
            STRATEGYNAME = strategyName;
        }

        #endregion
        #region data

        // [Column(CanBeNull = false, IsPrimaryKey = true, DbType = "NUMBER(11) NOT NULL")]
        public int ID { get; set; }
        public int PNODEID { get; set; }
        public decimal FIXEDRESOURCE { get; set; }
        public decimal TARGET { get; set; }
        public decimal DELAY { get; set; }
        public decimal TARGETTYPEID { get; set; }
        public decimal TARGETINSTANCEID { get; set; }
        public decimal FRTYPEID { get; set; }
        public decimal FRINSTANCEID { get; set; }
        public string FRCLASSNAME { get; set; }
        public string TARGETCLASSNAME { get; set; }
        public string FRTYPE { get; set; }
        public string TARGETTYPE { get; set; }
        public decimal ITEMTYPEID { get; set; }
        public bool ISCOUNTERENABLED { get; set; }
        public decimal COUNTERID { get; set; }
        public bool ISSTRATEGYENABLED { get; set; }
        public string STRATEGYNAME { get; set; }

        #endregion
        #region impl

        public static IList GetNewBindingList()
        {
            int n = 0;
            IList result = new List<PData>();
            PData tmp = new PData();
            string cmdQuery = "SELECT ID, PNODEID, FIXEDRESOURCE, TARGET, DELAY, TARGETTYPEID, TARGETINSTANCEID, " +
                "FRTYPEID, FRINSTANCEID, FRCLASSNAME, TARGETCLASSNAME, FRTYPE, TARGETTYPE, ITEMTYPEID, ISCOUNTERENABLED, " +
                "COUNTERID, ISSTRATEGYENABLED, STRATEGYNAME FROM PROCESSDATA";
            OracleCommand cmd = new OracleCommand(cmdQuery, Global.ProcessDatabase.Connection);
            cmd.CommandType = CommandType.Text;

            try
            {
                OracleDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    n += 1;
                    int i1 = reader.GetInt32(0);
                    int i2 = reader.GetInt32(1);
                    decimal? d3 = reader.GetValue(2) as decimal?;
                    decimal? d4 = reader.GetValue(3) as decimal?;
                    decimal? d5 = reader.GetValue(4) as decimal?;
                    decimal? d6 = reader.GetValue(5) as decimal?;
                    decimal? d7 = reader.GetValue(6) as decimal?;
                    decimal? d8 = reader.GetValue(7) as decimal?;
                    decimal? d9 = reader.GetValue(8) as decimal?;
                    string s10 = reader.GetValue(9) as string;
                    string s11 = reader.GetValue(10) as string;
                    string s12 = reader.GetValue(11) as string; // ??? is this in use?
                    string s13 = reader.GetValue(12) as string; // ??? is this in use?
                    decimal? d14 = reader.GetValue(13) as decimal?;
                    bool? b15 = reader.GetValue(14) as bool?; // ??? is this in use? false throughout the table
                    decimal? d16 = reader.GetValue(15) as decimal?;
                    bool? b17 = reader.GetValue(16) as bool?; // ??? is this in use? false throughout the table
                    string s18 = reader.GetValue(17) as string;

                    tmp = new PData(i1, i2, d3, d4, d5, d6, d7, d8, d9, s10, s11, s12, s13, d14, b15, d16, b17, s18);
                    result.Add(tmp);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                Logger.Log(tmp, "Error in row " + n.ToString() + ": ", ex);
            }
            finally
            {
                cmd.Dispose();
            }
            
            Logger.Log<INFO>(n.ToString() + " rows imported.");

            return result;
        }

        public static PData Get(int forProcessNode)
        {
            int n = 0;
            List<PData> result = new List<PData>();
            PData tmp = new PData();
            string cmdQuery = "SELECT ID, PNODEID, FIXEDRESOURCE, TARGET, DELAY, TARGETTYPEID, TARGETINSTANCEID, " +
                "FRTYPEID, FRINSTANCEID, FRCLASSNAME, TARGETCLASSNAME, FRTYPE, TARGETTYPE, ITEMTYPEID, ISCOUNTERENABLED, " +
                "COUNTERID, ISSTRATEGYENABLED, STRATEGYNAME FROM PROCESSDATA WHERE PNODEID=" + forProcessNode.ToString();
            OracleCommand cmd = new OracleCommand(cmdQuery, Global.ProcessDatabase.Connection);
            cmd.CommandType = CommandType.Text;

            try
            {
                OracleDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    n += 1;
                    int i1 = reader.GetInt32(0);
                    int i2 = reader.GetInt32(1);
                    decimal? d3 = reader.GetValue(2) as decimal?;
                    decimal? d4 = reader.GetValue(3) as decimal?;
                    decimal? d5 = reader.GetValue(4) as decimal?;
                    decimal? d6 = reader.GetValue(5) as decimal?;
                    decimal? d7 = reader.GetValue(6) as decimal?;
                    decimal? d8 = reader.GetValue(7) as decimal?;
                    decimal? d9 = reader.GetValue(8) as decimal?;
                    string s10 = reader.GetValue(9) as string;
                    string s11 = reader.GetValue(10) as string;
                    string s12 = reader.GetValue(11) as string; // ??? is this in use?
                    string s13 = reader.GetValue(12) as string; // ??? is this in use?
                    decimal? d14 = reader.GetValue(13) as decimal?;
                    bool? b15 = reader.GetValue(14) as bool?; // ??? is this in use? false throughout the table
                    decimal? d16 = reader.GetValue(15) as decimal?;
                    bool? b17 = reader.GetValue(16) as bool?; // ??? is this in use? false throughout the table
                    string s18 = reader.GetValue(17) as string;

                    tmp = new PData(i1, i2, d3, d4, d5, d6, d7, d8, d9, s10, s11, s12, s13, d14, b15, d16, b17, s18);
                    result.Add(tmp);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                Logger.Log(tmp, "Error in row " + n.ToString() + ": ", ex);
            }
            finally
            {
                cmd.Dispose();
            }

            if (result.Count > 1) throw new DataException("This should not have returned more than one item!");
            if (result.Count == 0) return null;
            return result[0];
        }

        #endregion
    }
}
