using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;

namespace MatthiasToolbox.Money.Data
{
    [Table(Name = "tblAccounts")]
    public class Account
    {
        #region prop

        // public static Account this[int id] { get { return (from row in Database.OpenInstance.AccountTable where row.ID == id select row).First(); } }

        #endregion
        #region ctor

        public Account() { }

        public Account(int forUserID, string name, string accountNumber = "")
        {
            OwnerUserID = forUserID;
            Name = name;
            AccountNumber = accountNumber;
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
        public int OwnerUserID { get; set; }

        [Column]
        public string Name { get; set; }

        [Column]
        public string AccountNumber { get; set; }

        #endregion
    }
}
