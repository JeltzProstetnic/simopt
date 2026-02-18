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
    [Table(Name = "tblPaymentGroups")]
    public class PaymentGroup
    {
        #region prop

        public Dictionary<string, PaymentSubGroup> SubGroupsByName 
        { 
            get 
            { 
                return (from row in Database.OpenInstance.PaymentSubGroupTable where row.GroupID == ID select row).ToDictionary(f => f.Name);
            } 
        }

        public Dictionary<int, PaymentSubGroup> SubGroupsByID
        {
            get
            {
                return (from row in Database.OpenInstance.PaymentSubGroupTable where row.GroupID == ID select row).ToDictionary(f => f.ID);
            }
        }

        public Dictionary<string, int> SubGroupIDsByName
        {
            get
            {
                return (from row in Database.OpenInstance.PaymentSubGroupTable where row.GroupID == ID select row).ToDictionary(f => f.Name, f => f.ID);
            }
        }

        #endregion
        #region ctor

        public PaymentGroup() { }

        public PaymentGroup(string name)
        {
            Name = name;
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
        public string Name { get; set; }

        #endregion
    }
}
