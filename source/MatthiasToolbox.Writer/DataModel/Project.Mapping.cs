using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;
using System.Data.Linq;

namespace MatthiasToolbox.Writer.DataModel
{
    [Table(Name = "tblProjects")]
    public partial class Project
    {
        #region ctor

        /// <summary>
        /// default ctor
        /// </summary>
        public Project()
        {
            Documents = new EntitySet<Document>();
            Containers = new EntitySet<Container>();
            Fragments = new EntitySet<Fragment>();
        }

        /// <summary>
        /// full ctor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="comment"></param>
        public Project(string name, string dataDirectory) : this()
        {
            this.Name = name;
            this.DataDirectory = dataDirectory;
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
        public string DataDirectory { get; set; }

        [Association(OtherKey = "ProjectID", DeleteRule="CASCADE")]
        public EntitySet<Document> Documents { get; set; }

        [Association(OtherKey = "ProjectID", DeleteRule = "CASCADE")]
        public EntitySet<Container> Containers { get; set; }

        [Association(OtherKey = "ProjectID", DeleteRule = "CASCADE")]
        public EntitySet<Fragment> Fragments { get; set; }

        #endregion
    }
}