using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;
using MatthiasToolbox.Logging;

namespace MatthiasToolbox.Money.Data
{
    [Table(Name = "tblPeriodicalPayments")]
    public class PeriodicalPayment
    {
        #region ctor

        public PeriodicalPayment() { DueDateTicks = DateTime.Now.Ticks; }

        public PeriodicalPayment(bool perYear, bool perMonth, DateTime dueDate, float amount, int userID = 1, int groupID = 1, int subGroupID = 1, int typeID = 1, int accountID = 1, string comment = "") 
        {
            if (perYear == perMonth)
            {
                this.Log<WARN>("Ony one of the two settings perYear and perMonth may be set to true!");
                this.perYear = !perMonth;
            }
            else this.perYear = perYear;

            this.perMonth = perMonth;
            GroupID = groupID;
            SubGroupID = subGroupID;
            TypeID = typeID;
            Amount = amount;
            PayedByUserID = userID;
            PayedFromAccountID = accountID;
            DueDateTicks = dueDate.Ticks;
            Comment = comment;
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

        /// <summary>
        /// 
        /// </summary>
        [Column]
        public int SubGroupID { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Column]
        public float Amount { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Column]
        public long DueDateTicks { get; set; }

        private bool perYear;
        /// <summary>
        /// 
        /// </summary>
        [Column(Storage="perYear")]
        public bool PerYear { get { return perYear; } set { perYear = value; perMonth = !value; } }

        private bool perMonth;
        /// <summary>
        /// 
        /// </summary>
        [Column(Storage="perMonth")]
        public bool PerMonth { get { return perMonth; } set { perMonth = value; perYear = !value; } }

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
        /// comment
        /// </summary>
        [Column]
        public string Comment { get; set; }

        #endregion
    }
}
