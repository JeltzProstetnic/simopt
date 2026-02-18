using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;
using MatthiasToolbox.Logging;

namespace MatthiasToolbox.Money.Data
{
    [Table(Name = "tblBookings")]
    public class Booking
    {
        #region prop

        public Account Account { get { return (from row in Database.OpenInstance.AccountTable where row.ID == AccountID select row).First(); } }

        #endregion
        #region ctor

        public Booking() { }

        public Booking(int accountID,
            int hash,
            int accountStatementID,
            DateTime bookingDate,
            DateTime valutaDate,
            DateTime transferTime,
            string customerData,
            string currency,
            float amount,
            string bookingText,
            string transferText)
        {
            AccountID = accountID;
            Hash = hash;
            AccountStatementID = accountStatementID;
            BookingDateTicks = bookingDate.Ticks;
            ValutaDateTicks = valutaDate.Ticks;
            TransferTimeTicks = transferTime.Ticks;
            CustomerData = customerData;
            Currency = currency;
            Amount = amount;
            BookingText = bookingText;
            TransferText = transferText;
            AmountAccountedFor = 0;
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

        [Column]
        public long ValutaDateTicks { get; set; }

        [Column]
        public float Amount { get; set; }

        [Column]
        public float AmountAccountedFor { get; set; }

        [Column]
        public string BookingText { get; set; }

        [Column]
        public string TransferText { get; set; }

        [Column]
        public int AccountID { get; set; }

        [Column]
        public int AccountStatementID { get; set; }

        [Column]
        public long BookingDateTicks { get; set; }

        [Column]
        public long TransferTimeTicks { get; set; }

        [Column]
        public string CustomerData { get; set; }

        [Column]
        public string Currency { get; set; }

        [Column]
        public int Hash { get; set; }

        #endregion
    }
}
