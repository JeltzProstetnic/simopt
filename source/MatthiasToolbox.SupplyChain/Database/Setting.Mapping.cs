using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;

namespace MatthiasToolbox.SupplyChain.Database
{
    [Table(Name = "tblSettings")]
    public partial class Setting
    {
        #region ctor

        /// <summary>
        /// empty ctor for Settings class
        /// </summary>
        public Setting() { }

        public Setting(string name, Type type, string value)
        {
            Name = name;
            DataClass = type.FullName;
            SettingData = value;
            User = UserDatabase.SettingsDatabase.CurrentUser;
        }

        #endregion
        #region data

#pragma warning disable 0649
        private int id;
#pragma warning restore 0649

        /// <summary>
        /// the Settings ID, which is the primary key of the SettingsTable
        /// </summary>
        [Column(Storage = "id",
                AutoSync = AutoSync.OnInsert,
                IsPrimaryKey = true,
                IsDbGenerated = true)]
        public int ID
        {
            get { return id; }
        }

        [Column]
        public string User { get; set; }

        [Column]
        public string Name { get; set; }

        [Column]
        public string SettingData { get; set; }

        [Column]
        public string DataClass { get; set; }

        #endregion
    }
}