using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace MatthiasToolbox.Presentation.Datagrid
{
    /// <summary>
    /// A class to link a DataGrid Column with the bound property
    /// </summary>
    public class DataGridColumnInfo
    {
        #region cvar

        private DataGridColumn column;
        private string header;
        private string propertyName;

        #endregion
        #region prop

        /// <summary>
        /// the column on which this is based
        /// </summary>
        public DataGridColumn Column { get { return column; } }
        
        /// <summary>
        /// the header of the column
        /// </summary>
        public string Header { get { return header; } }
        
        /// <summary>
        /// the name of the bound property
        /// </summary>
        public string PropertyName { get { return propertyName; } }

        #endregion
        #region ctor

        /// <summary>
        /// create an instance
        /// </summary>
        /// <param name="column">the column</param>
        /// <param name="propertyName">the bound property name</param>
        public DataGridColumnInfo(DataGridColumn column, string propertyName)
        {
            this.column = column;
            this.propertyName = propertyName;
            this.header = column.Header.ToString();
        }

        #endregion
    }
}
