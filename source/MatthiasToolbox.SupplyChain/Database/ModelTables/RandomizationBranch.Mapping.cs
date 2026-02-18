using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;

namespace MatthiasToolbox.SupplyChain.Database.ModelTables
{
    [Table(Name="tblRandomizationBranches")]
    public class RandomizationBranch
    {
        #region ctor

        /// <summary>
        /// default ctor
        /// </summary>
        public RandomizationBranch() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="comment"></param>
        public RandomizationBranch(string name)
        {
            this.Name = name;
        }

        #endregion
        #region data

        [Column(AutoSync = AutoSync.OnInsert,
                IsPrimaryKey = true,
                IsDbGenerated = true)]
        public int ID { get; private set; }

        [Column]
        public string Name { get; set; }

        #endregion
    }
}
