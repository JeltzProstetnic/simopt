using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;

namespace MatthiasToolbox.SupplyChain.Database.ModelTables
{
    [Table(Name="tblCustomerDistributions")]
    public class CustomerDistribution
    {
        #region ctor

        /// <summary>
        /// default ctor
        /// </summary>
        public CustomerDistribution() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="comment"></param>
        public CustomerDistribution(Area area, CustomerCategory category, double mu, double sigma, string comment = "")
        {
            this.AreaID = area.ID;
            this.CategoryID = category.ID;
            this.Mu = mu;
            this.Sigma = sigma;
            this.Comment = comment;
        }

        #endregion
        #region data

        [Column(AutoSync = AutoSync.OnInsert,
                IsPrimaryKey = true,
                IsDbGenerated = true)]
        public int ID { get; private set; }

        [Column]
        public int AreaID { get; set; }

        [Column]
        public int CategoryID { get; set; }

        [Column]
        public double Mu { get; set; }

        [Column]
        public double Sigma { get; set; }

        [Column]
        public string Comment { get; set; }

        #endregion
    }
}
