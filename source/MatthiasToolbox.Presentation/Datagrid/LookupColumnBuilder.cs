using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Collections;
using System.Windows.Data;

namespace MatthiasToolbox.Presentation.Datagrid
{
    public class LookupColumnBuilder
    {
        /// <summary>
        /// Build a drop down column
        /// </summary>
        /// <param name="e"></param>
        /// <param name="itemsSource"></param>
        /// <param name="displayMemberPath"></param>
        /// <param name="selectedValuePath"></param>
        /// <param name="selectedValueBindingPath">if not given, the property name is assumed</param>
        /// <param name="header">if not given, the property name is assumed</param>
        /// <param name="sortMemberPath">if not given, the displayMemberPath is assumed</param>
        public static void CreateComboBoxColumn(DataGridAutoGeneratingColumnEventArgs e, 
            IEnumerable itemsSource, 
            string displayMemberPath,
            string header = "",
            string selectedValuePath = "ID", 
            string selectedValueBindingPath = "", 
            string sortMemberPath = "")
        {
            DataGridComboBoxColumn boundColumn = new DataGridComboBoxColumn();
            
            if (!string.IsNullOrEmpty(header)) boundColumn.Header = header;
            else boundColumn.Header = e.PropertyName;

            boundColumn.ItemsSource = itemsSource;
            boundColumn.DisplayMemberPath = displayMemberPath;
            boundColumn.SortMemberPath = sortMemberPath;
            boundColumn.SelectedValuePath = selectedValuePath;
            
            if(string.IsNullOrEmpty(selectedValueBindingPath)) 
                boundColumn.SelectedValueBinding = new Binding(e.PropertyName);
            else
                boundColumn.SelectedValueBinding = new Binding(selectedValueBindingPath);

            e.Column = boundColumn;
        }
    }
}
