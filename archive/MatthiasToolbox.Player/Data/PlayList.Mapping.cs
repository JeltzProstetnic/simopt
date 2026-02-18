using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MatthiasToolbox.Utilities;
using System.Data.Linq.Mapping;

namespace MatthiasToolbox.Player.Data
{
    [Table(Name = "tblPlayLists")]
    public partial class PlayList
    {
        private List<Clip> items;

        public PlayList()
        {
            items = new List<Clip>();
        }

        public PlayList(string name)
            : this()
        {
            this.Name = name;
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
        public string Name { get; set; }

        #endregion
    }
}
