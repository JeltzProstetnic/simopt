using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;

namespace MatthiasToolbox.SupplyChain.Database.ModelTables
{
    [Table(Name="tblRandomizationBlocks")]
    public class RandomizationBlock
    {
        #region ctor

        /// <summary>
        /// default ctor
        /// </summary>
        public RandomizationBlock() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="comment"></param>
        public RandomizationBlock(string name, int split)
        {
            this.Name = name;
            this.Split = split;
        }

        #endregion
        #region data

        [Column(AutoSync = AutoSync.OnInsert,
                IsPrimaryKey = true,
                IsDbGenerated = true)]
        public int ID { get; private set; }

        [Column]
        public string Name { get; set; }

        [Column]
        public int Split { get; set; }

        #endregion
    }
}
