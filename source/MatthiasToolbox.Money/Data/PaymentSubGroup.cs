using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;
using MatthiasToolbox.Money.Tools;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;

namespace MatthiasToolbox.Money.Data
{
    [Table(Name = "tblPaymentSubGroups")]
    public class PaymentSubGroup
    {
        #region prop

        public PaymentGroup Parent { get { return (from row in Database.OpenInstance.PaymentGroupTable where row.ID == GroupID select row).First(); } }
        public string FullName { get { return Parent.Name + " - " + Name; } }

        #endregion
        #region ctor

        public PaymentSubGroup() { }

        public PaymentSubGroup(string name, int groupID)
        {
            Name = name;
            GroupID = groupID;
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
        public string Name { get; set; }

        #endregion
    }
}
