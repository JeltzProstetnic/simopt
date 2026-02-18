using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;
using MatthiasToolbox.Writer.Interfaces;
using System.Data.Linq;

namespace MatthiasToolbox.Writer.DataModel
{
    [Table(Name = "tblContainers")]
    public class Container : IDocumentTreeItem
    {
        #region cvar

        private EntityRef<Container> parent;

        #endregion
        #region ctor

        /// <summary>
        /// default ctor
        /// </summary>
        public Container()
        {
            SubContainers = new EntitySet<Container>();
        }

        /// <summary>
        /// full ctor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="comment"></param>
        public Container(Project project, string name, Container parentContainer = null, string text = "") : this()
        {
            this.ParentID = parentContainer == null ? 0 : parentContainer.ID;
            this.ProjectID = project.ID;
            this.Name = name;
            this.Text = text;
        }

        #endregion
        #region data

        [Column(AutoSync = AutoSync.OnInsert,
                IsPrimaryKey = true,
                IsDbGenerated = true)]
        public int ID { get; private set; }

        [Column]
        public int ProjectID { get; set; }

        [Column]
        public string Name { get; set; }

        [Column]
        public int ParentID { get; set; }

        [Association(Storage = "parent", ThisKey = "ParentID")]
        public Container Parent
        {
            get { return parent.Entity; }
            set { parent.Entity = value; }
        }

        [Association(OtherKey = "ParentID", DeleteRule = "CASCADE")]
        public EntitySet<Container> SubContainers { get; set; }

        [Column]
        public string Text { get; set; }
        
        #endregion
    }
}