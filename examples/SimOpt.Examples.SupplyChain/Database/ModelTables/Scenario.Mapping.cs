using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;

namespace MatthiasToolbox.SupplyChain.Database.ModelTables
{
    [Table(Name="tblScenarios")]
    public class Scenario
    {
        #region ctor

        /// <summary>
        /// default ctor
        /// </summary>
        public Scenario() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="baseScenario"></param>
        /// <param name="comment"></param>
        public Scenario(string name, Scenario baseScenario = null, String comment = "")
        {
            this.Name = name;
            this.Comment = comment;
            if (baseScenario != null)
                this.BaseScenarioID = baseScenario.ID;
        }

        #endregion
        #region data

        [Column(AutoSync = AutoSync.OnInsert,
                IsPrimaryKey = true,
                IsDbGenerated = true)]
        public int ID { get; private set; }

        [Column]
        public int BaseScenarioID { get; set; }

        [Column]
        public string Name { get; set; }

        [Column]
        public string Comment { get; set; }
        
        #endregion
    }
}
