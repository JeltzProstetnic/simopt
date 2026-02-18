using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;
using System.Data.Linq;

namespace MatthiasToolbox.Writer.DataModel
{
    [Table(Name = "tblDocuments")]
    public class Document
    {
        #region cvar

        private EntityRef<Project> project;

        #endregion
        #region ctor

        /// <summary>
        /// default ctor
        /// </summary>
        public Document() { }

        /// <summary>
        /// full ctor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="comment"></param>
        public Document(string name, Project project)
        {
            this.Name = name;
            this.ProjectID = project.ID;
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
        public int ProjectID { get; set; }

        [Association(Storage = "project", ThisKey = "ProjectID")]
        public Project Project
        {
            get { return project.Entity; }
            set { project.Entity = value; }
        }
        
        #endregion
    }
}