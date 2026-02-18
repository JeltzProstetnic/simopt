using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;

namespace MatthiasToolbox.Money.Data
{
    [Table(Name = "tblPaymentTypes")]
    public class PaymentType
    {
        #region ctor

        public PaymentType() { }

        public PaymentType(string name) 
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
