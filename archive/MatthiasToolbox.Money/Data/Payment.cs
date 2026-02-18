using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;
using MatthiasToolbox.Logging;

namespace MatthiasToolbox.Money.Data
{
    [Table(Name = "tblPayments")]
    public class Payment
    {
        #region prop

        public PaymentSubGroup SubGroup { get { return (from row in Database.OpenInstance.PaymentSubGroupTable where row.ID == subGroupID select row).First(); } }

        #endregion
        #region ctor

        public Payment() 
        {
            TransactionDateTicks = DateTime.Now.Ticks;
        }

        public Payment(float amount, DateTime date, int userID = 1, int groupID = 1, int subGroupID = 1, int typeID = 1, int bookingID = 0, int accountID = 1, bool taxRefund = false, string comment = "")
        {
            GroupID = groupID;
            this.subGroupID = subGroupID;
            TypeID = typeID;
            Amount = amount;
            TaxRefundPossible = taxRefund;
            PayedByUserID = userID;
            PayedFromAccountID = accountID;
            Comment = comment;
            BookingID = bookingID;
            TransactionDateTicks = date.Ticks;
        }

        #endregion
        #region data

#pragma warning disable 0649
        private int id;
#pragma warning restore 0649

        [Column(Storage = "id",
                AutoSync = AutoSync.OnInsert,
                IsPrimaryKey = true,
                IsDbGenerated = true)]
        public int ID
        {
            get { return id; }
        }

        /// <summary>
        /// 
        /// </summary>
        [Column]
        public int GroupID { get; set; }

        private int subGroupID;
        /// <summary>
        /// 
        /// </summary>
        [Column(Storage = "subGroupID")]
        public int SubGroupID
        {
            get { return subGroupID; }
            set 
            {
                subGroupID = value;
                try
                {
                    GroupID = (from row in Database.OpenInstance.PaymentSubGroupTable where row.ID == subGroupID select row).First().Parent.ID;
                }
                catch (Exception e)
                {
                    this.Log(e);
                }
            }
        }
        
        /// <summary>
        /// euro
        /// </summary>
        [Column]
        public float Amount { get; set; }

        /// <summary>
        /// date of transaction
        /// </summary>
        [Column]
        public long TransactionDateTicks { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Column]
        public int PayedByUserID { get; set; }

        /// <summary>
        /// savings account, credit card, paypal account, whatever
        /// </summary>
        [Column]
        public int PayedFromAccountID { get; set; }

        /// <summary>
        /// type of payment
        /// </summary>
        [Column]
        public int TypeID { get; set; }

        /// <summary>
        /// the associated booking if applicable
        /// </summary>
        [Column]
        public int BookingID { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Column]
        public bool TaxRefundPossible { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Column]
        public string Comment { get; set; }

        #endregion
    }
}
