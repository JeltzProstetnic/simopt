using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;

namespace MatthiasToolbox.Player.Data
{
    [Table(Name = "tblPlayListEntries")]
    public partial class PlayListEntry
    {
        public PlayListEntry() { }

        public PlayListEntry(PlayList pl, Clip c) 
        {
            this.PlayListID = pl.ID;
            this.ClipID = c.ID;
        }

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
        public int PlayListID { get; set; }

        [Column]
        public int ClipID { get; set; }

        [Column]
        public int OrderID { get; set; }

        #endregion
    }
}
