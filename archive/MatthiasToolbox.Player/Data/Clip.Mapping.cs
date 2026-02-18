using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;

namespace MatthiasToolbox.Player.Data
{
    [Table(Name = "tblClips")]
    public partial class Clip
    {
        public Clip() {}

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
        public string Label { get; set; }

        //[Column]
        //public int ArtistID { get; set; }

        //[Column]
        //public int AlbumID { get; set; }

        //[Column]
        //public int GenreID { get; set; }

        [Column]
        public string File { get; set; }

        [Column]
        public int Checksum { get; set; }

        [Column]
        public int SkipCount { get; set; }

        [Column]
        public int PlayCount { get; set; }

        #endregion
    }
}
