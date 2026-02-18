using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MatthiasToolbox.Basics.Interfaces;
using MatthiasToolbox.GraphDesigner.Utilities;
using MatthiasToolbox.Presentation.Events;
using MatthiasToolbox.Presentation.Utilities;
using System.Windows.Data;
using MatthiasToolbox.GraphDesigner.Interfaces;

namespace MatthiasToolbox.GraphDesigner.Controls
{
    /// <summary>
    /// Interaction logic for VertexToolbox.xaml
    /// </summary>
    public partial class VertexToolbox : UserControl, IPalette
    {
        #region evnt

        /// <summary>
        /// Item was selected for a single add.
        /// </summary>
        public static readonly RoutedEvent AddSingleItemEvent = EventManager.RegisterRoutedEvent("AddSingleItem",
            RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(VertexToolbox));

        /// <summary>
        /// Add event handler for single item selection events
        /// </summary>
        public event RoutedEventHandler AddSingleItem
        {
            add{AddHandler(AddSingleItemEvent, value);}
            remove { RemoveHandler(AddSingleItemEvent, value); }
        }

        public void RaiseAddSingleItemEvent(INamedElement element)
        {
            ToolboxEventArgs toolboxEventArgs = new ToolboxEventArgs(AddSingleItemEvent, this, element.GetType(), element);
            RaiseEvent(toolboxEventArgs);
        }

        /// <summary>
        /// Item was selected for multiple adds.
        /// </summary>
        public static readonly RoutedEvent AddMultipleItemStartEvent = EventManager.RegisterRoutedEvent("AddMultipleItemStart",
            RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(VertexToolbox));

        /// <summary>
        /// Add event handler for multiple item selection events
        /// </summary>
        public event RoutedEventHandler AddMultipleItemStart
        {
            add { AddHandler(AddMultipleItemStartEvent, value); }
            remove { RemoveHandler(AddMultipleItemStartEvent, value); }
        }

        /// <summary>
        /// Raises the add multiple item start event.
        /// </summary>
        /// <param name="element">The selected element.</param>
        public void RaiseAddMultipleItemStartEvent(INamedElement element)
        {
            ToolboxEventArgs toolboxEventArgs = new ToolboxEventArgs(AddMultipleItemStartEvent, this, element.GetType(), element);
            RaiseEvent(toolboxEventArgs);
        }

        /// <summary>
        /// Adding of multiple items ended.
        /// </summary>
        public static readonly RoutedEvent AddMultipleItemEndEvent = EventManager.RegisterRoutedEvent("AddMultipleItemEnd",
            RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(VertexToolbox));

        /// <summary>
        /// Adding of multiple items ended event handler.
        /// </summary>
        public event RoutedEventHandler AddMultipleItemEnd
        {
            add { AddHandler(AddMultipleItemEndEvent, value); }
            remove { RemoveHandler(AddMultipleItemEndEvent, value); }
        }

        /// <summary>
        /// Raises the add multiple item end event.
        /// </summary>
        /// <param name="element">The selected element.</param>
        public void RaiseAddMultipleItemEndEvent(INamedElement element)
        {
            ToolboxEventArgs toolboxEventArgs = new ToolboxEventArgs(AddMultipleItemEndEvent, this, element.GetType(), element);
            RaiseEvent(toolboxEventArgs);
        }

        #endregion
        #region cvar

        private Point _startPoint;

        #endregion
        #region prop


        public String Title { get; set; }

        /// <summary>
        /// Gets or sets the items source of the vertex container.
        /// </summary>
        /// <value>The items source.</value>
        public IEnumerable ItemsSource
        {
            set { SetValue(ItemsSourceProperty, value); }
            get { return GetValue(ItemsSourceProperty) as IEnumerable; }
        }

        /// <summary>
        /// ItemsSource of the vertex container
        /// </summary>
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.RegisterAttached("ItemsSource", typeof(IEnumerable), typeof(VertexToolbox),
            new FrameworkPropertyMetadata(null));
        
        private INamedElement SelectedItem { get; set; }

        #endregion
        #region ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="VertexToolbox"/> class.
        /// </summary>
        public VertexToolbox()
        {
            InitializeComponent();
        }

        #endregion
        #region impl

        /// <summary>
        /// Clears the selection.
        /// </summary>
        public void ClearSelection()
        {
            this.lbItems.SelectedItem = null;
        }

        public void UpdateItems(IEnumerable allItems)
        {
            ItemsSource = allItems;
            BindingOperations.GetBindingExpression(lbItems, ListBox.ItemsSourceProperty).UpdateTarget();
        }

        private INamedElement GetSelectedToolboxItem(object sender, MouseEventArgs e, out ListBoxItem item)
        {
            ListBox listbox = sender as ListBox;
            item = WPFTreeHelper.FindParentControl<ListBoxItem>((DependencyObject)e.OriginalSource);

            if (item == null)
                return null;

            // Find the data behind the ListViewItem
            return (INamedElement)listbox.ItemContainerGenerator.ItemFromContainer(item);
        }

        #region handlers

        /// <summary>
        /// Initialize the drag.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param>
        private void listBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //store the mouse position
            this._startPoint = e.GetPosition(null);
        }

        /// <summary>
        /// Drag the element
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseEventArgs"/> instance containing the event data.</param>
        private void listBox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            // Get the current mouse position and calculate the difference to starting point
            Point mousePos = e.GetPosition(null);
            Vector diff = this._startPoint - mousePos;

            if (e.LeftButton == MouseButtonState.Pressed &&
                Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                ListBoxItem item;
                INamedElement toolboxItem = GetSelectedToolboxItem(sender, e, out item);
                if (toolboxItem == null)
                    return;

                // Initialize the drag & drop operation
                DragObject dragObject = new DragObject { DataContext = toolboxItem };
                DragDrop.DoDragDrop(item, dragObject, DragDropEffects.Move);
            }
        }

        private void listBox_PreviewMouseUp(object sender, MouseEventArgs e)
        {
            // Get the dragged ListViewItem
            ListBoxItem item;
            INamedElement toolboxItem = GetSelectedToolboxItem(sender, e, out item);
            if (toolboxItem == null)
                return;

            RaiseAddSingleItemEvent(toolboxItem);
        }

        private void listBox_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Get the dragged ListViewItem
            ListBoxItem item;
            INamedElement toolboxItem = GetSelectedToolboxItem(sender, e, out item);
            if (toolboxItem == null)
                return;

            //set flag to start the event
            if (this.SelectedItem == null)
                RaiseAddMultipleItemStartEvent(toolboxItem);
            else
                RaiseAddMultipleItemEndEvent(toolboxItem);
        }

        #endregion
        #endregion
    }
}
