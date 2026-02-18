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

namespace Vr.WarehouseSimulator.Data.OrderTables
{
    public class ArticleWrapper { public IList GetNewBindingList() { return Article.GetNewBindingList(); } }

    // [Table(EntityName="LGEOMETRY")]
    public partial class Article
    {
        #region ctor

        public Article() { }

        public Article(int id, string name, int? foreignID, double? weight, int? amountPerPallet, int? amountPerCarton,
            string storageArea)  // recsts, moddate, moduser, height, abc
        {
            int fID = 0;
            double w = 0;
            int aPP = 0;
            int aPC = 0;

            if (foreignID.HasValue) fID = foreignID.Value;
            if (weight.HasValue) w = weight.Value;
            if (amountPerPallet.HasValue) aPP = amountPerPallet.Value;
            if (amountPerCarton.HasValue) aPC = amountPerCarton.Value;

            this.ID = id;
            this.NAME = name;
            this.FOREIGN_ID = fID;
            this.WEIGHT = w;
            this.AMOUNTPERPALLET = aPP;
            this.AMOUNTPERCARTON = aPC;
            this.STORAGEAREA = storageArea;
        }

        #endregion
        #region data

        // [Column(CanBeNull = false, IsPrimaryKey = true, DbType = "NUMBER(11) NOT NULL")]
        public int ID { get; set; }

        // [Column(CanBeNull = false, DbType = "NUMBER(11)")]
        public string NAME { get; set; }

        // [Column(CanBeNull = true, DbType = "NUMBER")]
        public int FOREIGN_ID { get; set; }

        // [Column(CanBeNull = true, DbType = "NUMBER")]
        public double WEIGHT { get; set; }

        // [Column(CanBeNull = true, DbType = "NUMBER")]
        public int AMOUNTPERPALLET { get; set; }

        // [Column(CanBeNull = true, DbType = "NUMBER")]
        public int AMOUNTPERCARTON { get; set; }

        // [Column(CanBeNull = true, DbType = "NUMBER")]
        public string STORAGEAREA { get; set; }

        #endregion
        #region impl

        public static IList GetNewBindingList()
        {
            int n = 0;
            IList result = new List<Article>();
            Article tmp = new Article();
            string cmdQuery = "SELECT ID, NAME, FOREIGN_ID, WEIGHT, AMOUNTPERPALLET, AMOUNTPERCARTON, STORAGEAREA FROM OARTICLE";
            OracleCommand cmd = new OracleCommand(cmdQuery, Global.OrderDatabase.Connection);
            cmd.CommandType = CommandType.Text;

            try
            {
                OracleDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    n += 1;
                    tmp = new Article(reader.GetInt32(0), reader.GetString(1), reader.GetValue(2) as int?, reader.GetDouble(3), reader.GetInt32(4),
                        reader.GetInt32(5), reader.GetValue(6) as string);
                    result.Add(tmp);
                }
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

        #endregion
    }
}
