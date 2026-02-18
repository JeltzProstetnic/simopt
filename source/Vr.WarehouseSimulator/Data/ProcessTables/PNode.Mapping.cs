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
    public class PNodeWrapper { public IList GetNewBindingList() { return PNode.GetNewBindingList(); } }

    // [Table(EntityName="PNODE")]
    public partial class PNode
    {
        #region ctor

        public PNode() { }

        public PNode(int id, string name, decimal? parentID, decimal? typeID, decimal? inboundCount,
            decimal? outboundCount)
        {
            decimal pID = 0;
            decimal tID = 0;
            decimal iC = 0;
            decimal oC = 0;

            if (parentID.HasValue) pID = parentID.Value;
            if (typeID.HasValue) tID = typeID.Value;
            if (inboundCount.HasValue) iC = inboundCount.Value;
            if (outboundCount.HasValue) oC = outboundCount.Value;

            this.ID = id;
            this.NAME = name;
            this.PARENTNODEID = pID;
            this.PNODETYPEID = tID;
            this.NRCONNECTORSIN = iC;
            this.NRCONNECTORSOUT = oC;
        }

        #endregion
        #region data

        // [Column(CanBeNull = false, IsPrimaryKey = true, DbType = "NUMBER(11) NOT NULL")]
        public int ID { get; set; }

        // [Column(CanBeNull = false, DbType = "NUMBER(11)")]
        public string NAME { get; set; }

        // [Column(CanBeNull = true, DbType = "NUMBER")]
        public decimal PARENTNODEID { get; set; }

        // [Column(CanBeNull = true, DbType = "NUMBER")]
        public decimal PNODETYPEID { get; set; }

        // [Column(CanBeNull = true, DbType = "NUMBER")]
        public decimal NRCONNECTORSIN { get; set; }

        // [Column(CanBeNull = true, DbType = "NUMBER")]
        public decimal NRCONNECTORSOUT { get; set; }

        #endregion
        #region impl

        public static IList GetNewBindingList()
        {
            int n = 0;
            IList result = new List<PNode>();
            PNode tmp = new PNode();
            string cmdQuery = "SELECT ID, NAME, PARENTNODEID, PNODETYPEID, NRCONNECTORSIN, NRCONNECTORSOUT FROM PNODE";
            OracleCommand cmd = new OracleCommand(cmdQuery, Global.ProcessDatabase.Connection);
            cmd.CommandType = CommandType.Text;

            try
            {
                OracleDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    n += 1;
                    tmp = new PNode(reader.GetInt32(0), reader.GetString(1), reader.GetValue(2) as decimal?, reader.GetValue(3) as decimal?,
                        reader.GetValue(4) as decimal?, reader.GetValue(5) as decimal?);
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

        public static List<PNode> GetPNodes(int parentID)
        {
            int n = 0;
            List<PNode> result = new List<PNode>();
            PNode tmp = new PNode();
            string cmdQuery = "SELECT ID, NAME, PARENTNODEID, PNODETYPEID, NRCONNECTORSIN, NRCONNECTORSOUT FROM PNODE WHERE PARENTNODEID=" + parentID.ToString();
            OracleCommand cmd = new OracleCommand(cmdQuery, Global.ProcessDatabase.Connection);
            cmd.CommandType = CommandType.Text;

            try
            {
                OracleDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    n += 1;
                    tmp = new PNode(reader.GetInt32(0), reader.GetString(1), reader.GetValue(2) as decimal?, reader.GetValue(3) as decimal?,
                        reader.GetValue(4) as decimal?, reader.GetValue(5) as decimal?);
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

        public List<PNode> GetChildren()
        {
            int n = 0;
            List<PNode> result = new List<PNode>();
            PNode tmp = new PNode();
            string cmdQuery = "SELECT ID, NAME, PARENTNODEID, PNODETYPEID, NRCONNECTORSIN, NRCONNECTORSOUT FROM PNODE WHERE PARENTNODEID=" + ID.ToString();
            OracleCommand cmd = new OracleCommand(cmdQuery, Global.ProcessDatabase.Connection);
            cmd.CommandType = CommandType.Text;

            try
            {
                OracleDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    n += 1;
                    tmp = new PNode(reader.GetInt32(0), reader.GetString(1), reader.GetValue(2) as decimal?, reader.GetValue(3) as decimal?,
                        reader.GetValue(4) as decimal?, reader.GetValue(5) as decimal?);
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

            // Logger.Log<INFO>(n.ToString() + " rows imported.");

            return result;
        }

        public bool HasChildren()
        {
            string cmdQuery = "SELECT COUNT(ID) FROM PNODE WHERE PARENTNODEID=" + ID.ToString();
            OracleCommand cmd = new OracleCommand(cmdQuery, Global.ProcessDatabase.Connection);
            cmd.CommandType = CommandType.Text;

            try
            {
                OracleDataReader reader = cmd.ExecuteReader();
                reader.Read();
                decimal? d = reader.GetValue(0) as decimal?;
                reader.Close();
                if (d.HasValue && d.Value > 0) return true;
                return false;
            }
            catch (Exception ex)
            {
                Logger.Log("Error in query " + cmdQuery + ": ", ex);
            }
            finally
            {
                cmd.Dispose();
            }

            return false;
        }

        #endregion
    }
}
