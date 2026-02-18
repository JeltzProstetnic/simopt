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
    public class OrderPositionWrapper { public IList GetNewBindingList() { return OrderPosition.GetNewBindingList(); } }

    //  EntityName                    Type                Nullable
    //  ID	                    NUMBER(11,0)	    No
    //  MOCKUP_ID	            NUMBER(11,0)	    No
    //  ORDER_ID	            NUMBER(11,0)	    Yes
    //  POSITION_ID	            NUMBER(11,0)	    Yes
    //  FOREIGN_ID	            VARCHAR2(255 BYTE)	Yes
    //  FOREIGNARTICLE_ID       VARCHAR2(255 BYTE)	Yes
    //  ARTICLE_ID	            NUMBER(11,0)	    Yes
    //  ARTICLEAMOUNT	        NUMBER	            Yes
    //  ORDERTYPE	            NUMBER(11,0)	    Yes
    //  MODDATE	                DATE	            Yes
    //  MODUSER	                NUMBER(11,0)	    Yes
    //  ORDERDATE	            DATE	            Yes
    //  CUSTOMER_ID	            VARCHAR2(255 BYTE)  Yes
    //  DESTINATION_ID	        VARCHAR2(255 BYTE)	Yes
    //  ORDERTIME	            DATE	            Yes
    //  CONSIGNEE_ID	        VARCHAR2(255 BYTE)	Yes

    // [Table(EntityName="LGEOMETRY")]
    public partial class OrderPosition
    {
        #region ctor

        public OrderPosition() { }

        public OrderPosition(int id, int? articleID, int? articleAmount, DateTime orderDate, string customerID,
            string destinationID, string consigneeID) 
        {
            int aID = 0;
            int aAm = 0;

            if (articleID.HasValue) aID = articleID.Value;
            if (articleAmount.HasValue) aAm = articleAmount.Value;

            this.ID = id;
            this.ARTICLE_ID = aID;
            this.ARTICLEAMOUNT = aAm;
            this.ORDERDATE = orderDate;
            this.CUSTOMER_ID = customerID;
            this.DESTINATION_ID = destinationID;
            this.CONSIGNEE_ID = consigneeID;
        }

        #endregion
        #region data

        // [Column(CanBeNull = false, IsPrimaryKey = true, DbType = "NUMBER(11) NOT NULL")]
        public int ID { get; set; }

        // [Column(CanBeNull = true, DbType = "NUMBER")]
        public int ARTICLE_ID { get; set; }

        // [Column(CanBeNull = true, DbType = "NUMBER")]
        public int ARTICLEAMOUNT { get; set; }

        // [Column(CanBeNull = false, DbType = "NUMBER(11)")]
        public DateTime ORDERDATE { get; set; }
        
        // [Column(CanBeNull = true, DbType = "NUMBER")]
        public string CUSTOMER_ID { get; set; }

        // [Column(CanBeNull = true, DbType = "NUMBER")]
        public string DESTINATION_ID { get; set; }
        
        // [Column(CanBeNull = true, DbType = "NUMBER")]
        public string CONSIGNEE_ID { get; set; }

        #endregion
        #region impl

        public static IList GetNewBindingList()
        {
            int n = 0;
            IList result = new List<OrderPosition>();
            OrderPosition tmp = new OrderPosition();
            string cmdQuery = "SELECT ID, ARTICLE_ID, ARTICLEAMOUNT, ORDERDATE, CUSTOMER_ID, DESTINATION_ID, CONSIGNEE_ID FROM OORDERPOSITION";
            OracleCommand cmd = new OracleCommand(cmdQuery, Global.OrderDatabase.Connection);
            cmd.CommandType = CommandType.Text;

            try
            {
                OracleDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    n += 1;
                    tmp = new OrderPosition(reader.GetInt32(0), reader.GetValue(1) as int?, reader.GetValue(2) as int?, reader.GetDateTime(3),
                        reader.GetValue(3) as string, reader.GetValue(4) as string, reader.GetValue(5) as string);
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
