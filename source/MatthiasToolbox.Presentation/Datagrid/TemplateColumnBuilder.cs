using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows;
using MatthiasToolbox.Presentation.Converters;
using System.Windows.Media;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace MatthiasToolbox.Presentation.Datagrid
{
    /// <summary>
    /// Helper class to create Template Columns for a WPF Datagrid
    /// </summary>
    public class TemplateColumnBuilder
    {
        /// <summary>
        /// create a DatePicker Column from a field containing ticks as long or double value
        /// </summary>
        /// <param name="e"></param>
        public static void CreateDateCellTemplate(DataGridAutoGeneratingColumnEventArgs e)
        {
            Binding ticksToDate = new Binding(e.PropertyName);
            ticksToDate.Converter = new TicksConverter();
            ticksToDate.StringFormat = "{0:dd. MM. yyyy}";

            Binding datePickerToTicks = new Binding(e.PropertyName);
            datePickerToTicks.StringFormat = "{0:dd. MM. yyyy}";
            datePickerToTicks.Converter = new TicksConverter();

            DataGridTemplateColumn templateColumn = new DataGridTemplateColumn();
            templateColumn.Header = e.PropertyName.Replace("Ticks", "");
            templateColumn.SortMemberPath = e.PropertyName;

            FrameworkElementFactory editorFactory = new FrameworkElementFactory(typeof(DatePicker));
            FrameworkElementFactory viewerFactory = new FrameworkElementFactory(typeof(TextBlock));

            editorFactory.SetValue(DatePicker.SelectedDateProperty, datePickerToTicks);
            editorFactory.SetValue(DatePicker.NameProperty, "SomeDatePicker");
            viewerFactory.SetValue(TextBlock.TextProperty, ticksToDate);
            viewerFactory.SetValue(TextBlock.NameProperty, "SomeTextBlock");

            DataTemplate cellEditingTemplate = new DataTemplate();
            DataTemplate cellTemplate = new DataTemplate();
            cellEditingTemplate.VisualTree = editorFactory;
            cellTemplate.VisualTree = viewerFactory;

            templateColumn.CellEditingTemplate = cellEditingTemplate;
            templateColumn.CellTemplate = cellTemplate;

            e.Column = templateColumn;
        }

        /// <summary>
        /// create a DateTime Column (including milliseconds) from a field containing ticks as long or double value
        /// </summary>
        /// <param name="e"></param>
        public static void CreateExactDateTimeCellTemplate(DataGridAutoGeneratingColumnEventArgs e)
        {
            Binding b = new Binding(e.PropertyName);
            b.Converter = new TicksConverter();
            ((DataGridTextColumn)e.Column).Binding = b;
            ((DataGridTextColumn)e.Column).Binding.StringFormat = "{0:dd. MM. yyyy hh:mm:ss:fff}";
        }

        /// <summary>
        /// create a DateTime Column (dd. MM. yyyy hh:mm:ss) from a field containing ticks as long or double value
        /// </summary>
        /// <param name="e"></param>
        public static void CreateDateTimeCellTemplate(DataGridAutoGeneratingColumnEventArgs e)
        {
            Binding b = new Binding(e.PropertyName);
            b.Converter = new TicksConverter();
            ((DataGridTextColumn)e.Column).Binding = b;
            ((DataGridTextColumn)e.Column).Binding.StringFormat = "{0:dd. MM. yyyy hh:mm:ss}";
        }

        /// <summary>
        /// create a TimeSpan Column (dd. hh:mm:ss) from a field containing ticks as long or double value
        /// </summary>
        /// <param name="e"></param>
        public static void CreateLongTimeSpanCellTemplate(DataGridAutoGeneratingColumnEventArgs e)
        {
            Binding b = new Binding(e.PropertyName);
            b.Converter = new TicksConverter();
            ((DataGridTextColumn)e.Column).Binding = b;
            ((DataGridTextColumn)e.Column).Binding.StringFormat = "{0:d\\d hh:mm:ss}";
        }

        /// <summary>
        /// create a TimeSpan Column (hh:mm:ss) from a field containing ticks as long or double value
        /// </summary>
        /// <param name="e"></param>
        public static void CreateShortTimeSpanCellTemplate(DataGridAutoGeneratingColumnEventArgs e)
        {
            Binding b = new Binding(e.PropertyName);
            b.Converter = new TicksConverter();
            ((DataGridTextColumn)e.Column).Binding = b;
            ((DataGridTextColumn)e.Column).Binding.StringFormat = "{0:hh:mm:ss}";
        }

        /// <summary>
        /// create a custom date time string format column from a field containing ticks as long or double value
        /// </summary>
        /// <param name="e"></param>
        /// <param name="format">A valid formatting string for a DateTime or TimeSpan value.</param>
        public static void CreateCustomDateTimeCellTemplate(DataGridAutoGeneratingColumnEventArgs e, string format = "{0:dd. MM. yyyy hh:mm:ss:fff}")
        {
            Binding b = new Binding(e.PropertyName);
            b.Converter = new TicksConverter();
            ((DataGridTextColumn)e.Column).Binding = b;
            ((DataGridTextColumn)e.Column).Binding.StringFormat = format;
        }

        /// <summary>
        /// Creates a Currency Column from a field containing an amount of money as float.
        /// Negative amounts will be in red.
        /// </summary>
        /// <param name="e"></param>
        public static void CreateCurrencyCellTemplate(DataGridAutoGeneratingColumnEventArgs e)
        {
            CreateCurrencyCellTemplate(e, Colors.Red, Colors.Black);
        }

        /// <summary>
        /// create a Currency Column from a field containing an amount of money as float
        /// </summary>
        /// <param name="e"></param>
        /// <param name="negativeColor"></param>
        /// <param name="positiveColor"></param>
        public static void CreateCurrencyCellTemplate(DataGridAutoGeneratingColumnEventArgs e, Color negativeColor, Color positiveColor)
        {
            Binding numberToCurrency = new Binding(e.PropertyName);
            numberToCurrency.Converter = new CurrencyStringConverter();
            numberToCurrency.TargetNullValue = "-"; // does this work? default(float) is not null

            Binding numberToColor = new Binding(e.PropertyName);
            numberToColor.Converter = new CurrencyColorConverter(negativeColor, positiveColor);

            DataGridTemplateColumn templateColumn = new DataGridTemplateColumn();
            templateColumn.SortMemberPath = e.PropertyName;

            FrameworkElementFactory viewerFactory = new FrameworkElementFactory(typeof(TextBlock));
            FrameworkElementFactory editorFactory = new FrameworkElementFactory(typeof(TextBox));

            viewerFactory.SetValue(TextBlock.TextProperty, numberToCurrency);
            viewerFactory.SetValue(TextBlock.ForegroundProperty, numberToColor);
            viewerFactory.SetValue(TextBlock.NameProperty, "SomeCurrencyString");

            editorFactory.SetValue(TextBox.TextProperty, new Binding(e.PropertyName));
            editorFactory.SetValue(TextBox.NameProperty, "SomeCurrencyField");

            DataTemplate cellViewerTemplate = new DataTemplate();
            cellViewerTemplate.VisualTree = viewerFactory;

            DataTemplate cellEditorTemplate = new DataTemplate();
            cellEditorTemplate.VisualTree = editorFactory;

            templateColumn.CellTemplate = cellViewerTemplate;
            templateColumn.CellEditingTemplate = cellEditorTemplate;

            templateColumn.Header = e.Column.Header;
            e.Column = templateColumn;
        }

        /// <summary>
        /// Prepends "file:///" and replaces backslashes with slashes to create a valid URI
        /// </summary>
        /// <param name="navigationHandler"></param>
        /// <param name="linkTextProperty">if omitted the actual property will be used</param>
        /// <param name="e"></param>
        public static void CreateFileLinkCellTemplate(DataGridAutoGeneratingColumnEventArgs e, RequestNavigateEventHandler navigationHandler, string linkTextProperty = null) 
        {
            string lt = linkTextProperty == null ? e.PropertyName : linkTextProperty;
            Binding linkTextBinding = new Binding(lt);
            Binding linkURIBinding = new Binding(e.PropertyName);
            linkURIBinding.Converter = new FileLinkConverter();

            DataGridTemplateColumn templateColumn = new DataGridTemplateColumn();
            templateColumn.SortMemberPath = e.PropertyName;

            FrameworkElementFactory viewerFactoryBase = new FrameworkElementFactory(typeof(TextBlock));
            FrameworkElementFactory viewerFactory = new FrameworkElementFactory(typeof(Hyperlink));
            FrameworkElementFactory viewerSubFactory = new FrameworkElementFactory(typeof(TextBlock));
            FrameworkElementFactory editorFactory = new FrameworkElementFactory(typeof(TextBox));

            viewerFactoryBase.AppendChild(viewerFactory);
            viewerFactory.SetValue(Hyperlink.NavigateUriProperty, linkURIBinding);
            viewerFactory.AddHandler(Hyperlink.RequestNavigateEvent, navigationHandler);
            viewerFactory.AppendChild(viewerSubFactory);
            viewerSubFactory.SetValue(TextBlock.TextProperty, linkTextBinding);
            viewerFactory.SetValue(TextBlock.NameProperty, "SomeLink");

            editorFactory.SetValue(TextBox.TextProperty, new Binding(e.PropertyName));
            editorFactory.SetValue(TextBox.NameProperty, "SomeLink");

            DataTemplate cellViewerTemplate = new DataTemplate();
            cellViewerTemplate.VisualTree = viewerFactoryBase;

            DataTemplate cellEditorTemplate = new DataTemplate();
            cellEditorTemplate.VisualTree = editorFactory;

            templateColumn.CellTemplate = cellViewerTemplate;
            templateColumn.CellEditingTemplate = cellEditorTemplate;

            templateColumn.Header = e.Column.Header;
            e.Column = templateColumn;
        }
        
        /// <summary>
        /// Prepends "mailto:" to create a valid URI
        /// </summary>
        /// <param name="navigationHandler"></param>
        /// <param name="e"></param>
        public static void CreateMailLinkCellTemplate(DataGridAutoGeneratingColumnEventArgs e, RequestNavigateEventHandler navigationHandler, string linkTextProperty = null) 
        {
            string lt = linkTextProperty == null ? e.PropertyName : linkTextProperty;
            Binding linkText = new Binding(lt);
            Binding linkURI = new Binding(e.PropertyName);
            linkURI.Converter = new MailLinkConverter();

            DataGridTemplateColumn templateColumn = new DataGridTemplateColumn();
            templateColumn.SortMemberPath = e.PropertyName;

            FrameworkElementFactory viewerFactoryBase = new FrameworkElementFactory(typeof(TextBlock));
            FrameworkElementFactory viewerFactory = new FrameworkElementFactory(typeof(Hyperlink));
            FrameworkElementFactory viewerSubFactory = new FrameworkElementFactory(typeof(TextBlock));
            FrameworkElementFactory editorFactory = new FrameworkElementFactory(typeof(TextBox));

            viewerFactoryBase.AppendChild(viewerFactory);
            viewerFactory.SetValue(Hyperlink.NavigateUriProperty, linkURI);
            viewerFactory.AddHandler(Hyperlink.RequestNavigateEvent, navigationHandler);
            viewerFactory.AppendChild(viewerSubFactory);
            viewerSubFactory.SetValue(TextBlock.TextProperty, linkText);
            viewerFactory.SetValue(TextBlock.NameProperty, "SomeLink");

            editorFactory.SetValue(TextBox.TextProperty, new Binding(e.PropertyName));
            editorFactory.SetValue(TextBox.NameProperty, "SomeLink");

            DataTemplate cellViewerTemplate = new DataTemplate();
            cellViewerTemplate.VisualTree = viewerFactoryBase;

            DataTemplate cellEditorTemplate = new DataTemplate();
            cellEditorTemplate.VisualTree = editorFactory;

            templateColumn.CellTemplate = cellViewerTemplate;
            templateColumn.CellEditingTemplate = cellEditorTemplate;

            templateColumn.Header = e.Column.Header;
            e.Column = templateColumn;
        }

        /// <summary>
        /// assumes that the content can be used to create a valid URI
        /// </summary>
        /// <param name="navigationHandler"></param>
        /// <param name="e"></param>
        public static void CreateHyperLinkCellTemplate(DataGridAutoGeneratingColumnEventArgs e, RequestNavigateEventHandler navigationHandler, string linkTextProperty = null)
        {
            string lt = linkTextProperty == null ? e.PropertyName : linkTextProperty;
            Binding linkText = new Binding(lt);
            Binding linkURI = new Binding(e.PropertyName);
            linkURI.Converter = new HyperLinkConverter();

            DataGridTemplateColumn templateColumn = new DataGridTemplateColumn();
            templateColumn.SortMemberPath = e.PropertyName;

            FrameworkElementFactory viewerFactoryBase = new FrameworkElementFactory(typeof(TextBlock));
            FrameworkElementFactory viewerFactory = new FrameworkElementFactory(typeof(Hyperlink));
            FrameworkElementFactory viewerSubFactory = new FrameworkElementFactory(typeof(TextBlock));
            FrameworkElementFactory editorFactory = new FrameworkElementFactory(typeof(TextBox));

            viewerFactoryBase.AppendChild(viewerFactory);
            viewerFactory.SetValue(Hyperlink.NavigateUriProperty, linkURI);
            viewerFactory.AddHandler(Hyperlink.RequestNavigateEvent, navigationHandler);
            viewerFactory.AppendChild(viewerSubFactory);
            viewerSubFactory.SetValue(TextBlock.TextProperty, linkText);
            viewerFactory.SetValue(TextBlock.NameProperty, "SomeLink");

            editorFactory.SetValue(TextBox.TextProperty, new Binding(e.PropertyName));
            editorFactory.SetValue(TextBox.NameProperty, "SomeLink");

            DataTemplate cellViewerTemplate = new DataTemplate();
            cellViewerTemplate.VisualTree = viewerFactoryBase;

            DataTemplate cellEditorTemplate = new DataTemplate();
            cellEditorTemplate.VisualTree = editorFactory;

            templateColumn.CellTemplate = cellViewerTemplate;
            templateColumn.CellEditingTemplate = cellEditorTemplate;

            templateColumn.Header = e.Column.Header;
            e.Column = templateColumn;
        }
    }
}
