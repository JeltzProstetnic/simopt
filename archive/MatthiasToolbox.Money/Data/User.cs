using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;
using MatthiasToolbox.Money.Tools;

namespace MatthiasToolbox.Money.Data
{
    [Table(Name = "tblUsers")]
    public class User : IIdentifiable
    {
        #region prop

        public IEnumerable<Account> Accounts { get { return (from row in Database.OpenInstance.AccountTable where row.OwnerUserID == ID select row); } }

        #endregion
        #region ctor

        public User() { }

        public User(string name) { Name = name; }

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
        public string Name { get; set; }

        #endregion
    }
}
