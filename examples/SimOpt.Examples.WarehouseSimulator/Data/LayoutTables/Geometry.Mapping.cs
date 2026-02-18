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
    public class GeometryWrapper { public IList GetNewBindingList() { return Geometry.GetNewBindingList(); } }

    // [Table(EntityName="LGEOMETRY")]
    public partial class Geometry
    {
        #region ctor

        public Geometry() { }

        public Geometry(int id, int layoutID, double? valueX, double? valueY, double? valueZ,
            double? anchorX, double? anchorY, double? anchorZ, double? angle) 
        {
            double x = 0;
            double y = 0;
            double z = 0;
            double aX = 0;
            double aY = 0;
            double aZ = 0;
            double ang = 0;
            
            if (valueX != null) x = (double)valueX;
            if (valueY != null) y = (double)valueY;
            if (valueZ != null) z = (double)valueZ;

            if (anchorX != null) aX = (double)anchorX;
            if (anchorY != null) aY = (double)anchorY;
            if (anchorY != null) aZ = (double)anchorZ;

            if (angle != null) ang = (double)angle;

            this.ID = id;

            this.VALUE_X = x;
            this.VALUE_Y = y;
            this.VALUE_Z = z;

            this.ANCHOR_X = x;
            this.ANCHOR_Y = y;
            this.ANCHOR_Z = z;

            this.ANGLE = ang;
        }

        #endregion
        #region data

        // [Column(CanBeNull = false, IsPrimaryKey = true, DbType = "NUMBER(11) NOT NULL")]
        public int ID { get; set; }

        // [Column(CanBeNull = false, DbType = "NUMBER(11)")]
        public int LAYOUT_ID { get; set; }

        // [Column(CanBeNull = true, DbType = "NUMBER")]
        public double VALUE_X { get; set; }

        // [Column(CanBeNull = true, DbType = "NUMBER")]
        public double VALUE_Y { get; set; }

        // [Column(CanBeNull = true, DbType = "NUMBER")]
        public double VALUE_Z { get; set; }

        // [Column(CanBeNull = true, DbType = "NUMBER")]
        public double ANCHOR_X { get; set; }

        // [Column(CanBeNull = true, DbType = "NUMBER")]
        public double ANCHOR_Y { get; set; }

        // [Column(CanBeNull = true, DbType = "NUMBER")]
        public double ANCHOR_Z { get; set; }

        // [Column(CanBeNull = true, DbType = "NUMBER")]
        public double ANGLE { get; set; }

        #endregion
        #region impl

        public static IList GetNewBindingList()
        {
            int n = 0;
            IList result = new List<Geometry>();
            string cmdQuery = "SELECT ID, LAYOUT_ID, VALUE_X, VALUE_Y, VALUE_Z, ANCHOR_X, ANCHOR_Y, ANCHOR_Z, ANGLE FROM LGEOMETRY";
            OracleCommand cmd = new OracleCommand(cmdQuery, Global.LayoutDatabase.Connection);
            cmd.CommandType = CommandType.Text;

            try
            {
                OracleDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    n += 1;
                    result.Add(new Geometry(reader.GetInt32(0), reader.GetInt32(0), reader.GetDouble(1), reader.GetDouble(2), 
                        reader.GetDouble(3), reader.GetDouble(4), reader.GetDouble(5), reader.GetDouble(6), reader.GetDouble(7)));
                }
            }
            catch (Exception ex)
            {
                Logger.Log(new Geometry(), "Error in row " + n.ToString() + ": ", ex);
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
