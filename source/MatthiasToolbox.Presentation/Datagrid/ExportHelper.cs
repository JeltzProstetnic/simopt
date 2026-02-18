using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Reflection;
using System.Windows.Data;
using System.Diagnostics;
using MatthiasToolbox.Logging;

namespace MatthiasToolbox.Presentation.Datagrid
{
    /// <summary>
    /// Attached properties and helper methods for the export of WPF <code>DataGrid</code> contents.
    /// TODO: ignore NewItemPlaceHolder
    /// </summary>
    public class ExportHelper
    {
        #region cvar

        #region attached properties

        /// <summary>
        /// Include the current column in export. Default is true.
        /// </summary>
        public static readonly DependencyProperty IsExportEnabledProperty = 
            DependencyProperty.RegisterAttached("IsExportEnabled", typeof(bool), typeof(DataGrid), new PropertyMetadata(true));

        /// <summary>
        /// Use a custom header for exporting
        /// </summary>
        public static readonly DependencyProperty ExportHeaderProperty = 
            DependencyProperty.RegisterAttached("ExportHeader", typeof(string), typeof(DataGrid), new PropertyMetadata(null));

        /// <summary>
        /// Use a custom path for exporting.
        /// </summary>
        public static readonly DependencyProperty ExportPathProperty = 
            DependencyProperty.RegisterAttached("ExportPath", typeof(string), typeof(DataGrid), new PropertyMetadata(null));

        /// <summary>
        /// Use a custom format for exporting.
        /// </summary>
        public static readonly DependencyProperty ExportFormatProperty = 
            DependencyProperty.RegisterAttached("ExportFormat", typeof(string), typeof(DataGrid), new PropertyMetadata(null));

        #endregion

        #endregion
        #region impl

        /// <summary>
        /// Get the content of the datagrid as two dimensional object array as defined
        /// through the attached properties <see cref="IsExportEnabled"/>, 
        /// <see cref="ExportHeader"/>, <see cref="ExportPath"/> 
        /// and <see cref="ExportFormat"/>.
        /// </summary>
        /// <param name="grid"></param>
        /// <returns></returns>
        public static object[,] GetExportData(DataGrid grid)
        {
            // Get only columns which have binding or have custom binding
            List<DataGridColumn> columns = 
                grid.Columns.Where(x => (GetIsExportEnabled(x) && 
                    ((x is DataGridBoundColumn) || 
                    (!string.IsNullOrEmpty(GetExportPath(x))) || 
                    (!string.IsNullOrEmpty(x.SortMemberPath))))).ToList();

            // Get list of items, bounded to grid
            List<object> list = grid.ItemsSource.Cast<object>().ToList();

            // Create data array (using array for data export optimization)
            object[,] data = new object[list.Count + 1, columns.Count];

            for (int columnIndex = 0; columnIndex < columns.Count; columnIndex++)
            {
                DataGridColumn gridColumn = columns[columnIndex];
                data[0, columnIndex] = GetHeader(gridColumn);
                string[] path = GetPath(gridColumn);
                string formatForExport = GetExportFormat(gridColumn);

                if (path != null)
                {
                    // Fill data with values
                    for (int rowIndex = 1; rowIndex <= list.Count; rowIndex++)
                    {
                        object source = list[rowIndex - 1];
                        data[rowIndex, columnIndex] = GetValue(path, source, formatForExport);
                    }
                }
            }

            return data;
        }

        /// <summary>
        /// Get the header for export.
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        private static string GetHeader(DataGridColumn column)
        {
            string headerForExport = GetExportHeader(column);
            if (headerForExport == null && column.Header != null)
                return column.Header.ToString();
            return headerForExport;
        }

        /// <summary>
        /// Get value of <paramref name="obj"/> by <paramref name="path"/>
        /// </summary>
        /// <param name="path"></param>
        /// <param name="obj"></param>
        /// <param name="formatForExport"></param>
        /// <returns></returns>
        private static object GetValue(string[] path, object obj, string formatForExport)
        {
            if (null == obj) return "";

            foreach (string pathStep in path)
            {
                Type type = obj.GetType();
                PropertyInfo property = type.GetProperty(pathStep);

                if (property == null)
                {
                    string message = string.Format("Couldn't find property '{0}' in type '{1}'", pathStep, type.Name);
#if DEBUG
                    Debug.WriteLine(message);
#endif
                    Logger.Log<WARN>("MatthiasToolbox.Presentation.Datagrid.ExportHelper.GetValue", message);
                    return null;
                }

                obj = property.GetValue(obj, null);
                if (null == obj) return "";
            }

            if (!string.IsNullOrEmpty(formatForExport))
                return string.Format("{0:" + formatForExport + "}", obj);

            return obj == null ? "" : obj.ToString();
        }

        /// <summary>
        /// Get path to get value. First try to get attached path value, then try to get path from binding
        /// </summary>
        /// <param name="gridColumn"></param>
        /// <returns></returns>
        private static string[] GetPath(DataGridColumn gridColumn)
        {
            string path = GetExportPath(gridColumn);

            if (string.IsNullOrEmpty(path))
            {
                if (gridColumn is DataGridBoundColumn)
                {
                    Binding binding = ((DataGridBoundColumn)gridColumn).Binding as Binding;
                    if (binding != null)
                    {
                        path = binding.Path.Path;
                    }
                }
                else
                {
                    path = gridColumn.SortMemberPath;
                }
            }

            return string.IsNullOrEmpty(path) ? null : path.Split('.');
        }

        #region attached property helper methods

        #region get

        public static Boolean GetIsExportEnabled(DataGridColumn element)
        {
            return (Boolean)element.GetValue(IsExportEnabledProperty);
        }

        public static string GetExportHeader(DataGridColumn element)
        {
            return (string)element.GetValue(ExportHeaderProperty);
        }

        public static string GetExportPath(DataGridColumn element)
        {
            return (string)element.GetValue(ExportPathProperty);
        }

        public static string GetExportFormat(DataGridColumn element)
        {
            return (string)element.GetValue(ExportFormatProperty);
        }

        #endregion
        #region set

        public static void SetIsExportEnabled(DataGridColumn element, Boolean value)
        {
            element.SetValue(IsExportEnabledProperty, value);
        }

        public static void SetExportHeader(DataGridColumn element, string value)
        {
            element.SetValue(ExportHeaderProperty, value);
        }

        public static void SetExportPath(DataGridColumn element, string value)
        {
            element.SetValue(ExportPathProperty, value);
        }

        public static void SetExportFormat(DataGridColumn element, string value)
        {
            element.SetValue(ExportFormatProperty, value);
        }

        #endregion

        #endregion
        
        #endregion
    }
}