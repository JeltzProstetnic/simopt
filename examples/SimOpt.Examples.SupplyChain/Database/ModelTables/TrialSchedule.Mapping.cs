using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;

namespace MatthiasToolbox.SupplyChain.Database.ModelTables
{
    [Table(Name="tblTrialSchedules")]
    public partial class TrialSchedule
    {
        #region ctor

        /// <summary>
        /// default ctor
        /// </summary>
        public TrialSchedule() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="comment"></param>
        public TrialSchedule(Site site, DateTime start, DateTime end, string comment = "")
        {
            this.SiteID = site.ID;
            this.StartDate = start.Ticks;
            this.EndDate = end.Ticks;
            this.Comment = comment;
        }

        #endregion
        #region data

        [Column(AutoSync = AutoSync.OnInsert,
                IsPrimaryKey = true,
                IsDbGenerated = true)]
        public int ID { get; private set; }

        [Column]
        public int SiteID { get; set; }

        [Column]
        public double StartDate { get; set; }

        [Column]
        public double EndDate { get; set; }

        [Column]
        public string Comment { get; set; }

        #endregion
    }
}
