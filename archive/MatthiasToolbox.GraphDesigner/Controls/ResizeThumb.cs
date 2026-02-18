using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using MatthiasToolbox.GraphDesigner.DiagramDesigner;
using MatthiasToolbox.GraphDesigner.Utilities;
using MatthiasToolbox.Presentation.Controls.DiagramDesigner;
using MatthiasToolbox.Presentation.Utilities;

namespace MatthiasToolbox.GraphDesigner.Controls
{
    /// <summary>
    /// Handles the Resize Events of the DesignerItem
    /// </summary>
    public class ResizeThumb : Thumb
    {
        #region evnt
        public static readonly RoutedEvent ResizeCompletedEvent = EventManager.RegisterRoutedEvent("ResizeCompleted", RoutingStrategy.Bubble, 
            typeof(RoutedEventHandler), typeof(ResizeThumb));

        // Provide CLR accessors for the event
        public event RoutedEventHandler ResizeCompleted
        {
            add { AddHandler(ResizeCompletedEvent, value); }
            remove { RemoveHandler(ResizeCompletedEvent, value); }
        }

        #endregion
        #region ctor
        
        public ResizeThumb()
        {
            base.DragDelta += new DragDeltaEventHandler(ResizeThumb_DragDelta);
            base.DragCompleted += new DragCompletedEventHandler(ResizeThumb_DragCompleted);
        }

        #endregion
        #region impl
        #region evnt

        void ResizeThumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            //invoke resize completed event
            RoutedEventArgs newEventArgs = new RoutedEventArgs(ResizeCompletedEvent, this.DataContext);
            RaiseEvent(newEventArgs);
        }

        void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            DesignerItem designerItem = this.DataContext as DesignerItem;
            DesignerCanvas designer = VisualTreeHelper.GetParent(designerItem) as DesignerCanvas;

            if (designerItem != null && designer != null && designerItem.IsSelected)
            {
                double minLeft, minTop, minDeltaHorizontal, minDeltaVertical;
                double dragDeltaVertical, dragDeltaHorizontal, scale;

                IEnumerable<DesignerItem> selectedDesignerItems = designer.SelectionService.CurrentSelection.OfType<DesignerItem>();

                CalculateDragLimits(selectedDesignerItems, out minLeft, out minTop,
                                    out minDeltaHorizontal, out minDeltaVertical);

                foreach (DesignerItem item in selectedDesignerItems)
                {
                    if (item != null && item.ParentID == Guid.Empty)
                    {
                        switch (base.VerticalAlignment)
                        {
                            case VerticalAlignment.Bottom:
                                dragDeltaVertical = Math.Min(-e.VerticalChange, minDeltaVertical);
                                scale = (item.ActualHeight - dragDeltaVertical) / item.ActualHeight;
                                DragBottom(scale, item, designer.SelectionService);
                                break;
                            case VerticalAlignment.Top:
                                double top = Canvas.GetTop(item);
                                dragDeltaVertical = Math.Min(Math.Max(-minTop, e.VerticalChange), minDeltaVertical);
                                scale = (item.ActualHeight - dragDeltaVertical) / item.ActualHeight;
                                DragTop(scale, item, designer.SelectionService);
                                break;
                            default:
                                break;
                        }

                        switch (base.HorizontalAlignment)
                        {
                            case HorizontalAlignment.Left:
                                double left = Canvas.GetLeft(item);
                                dragDeltaHorizontal = Math.Min(Math.Max(-minLeft, e.HorizontalChange), minDeltaHorizontal);
                                scale = (item.ActualWidth - dragDeltaHorizontal) / item.ActualWidth;
                                DragLeft(scale, item, designer.SelectionService);
                                break;
                            case HorizontalAlignment.Right:
                                dragDeltaHorizontal = Math.Min(-e.HorizontalChange, minDeltaHorizontal);
                                scale = (item.ActualWidth - dragDeltaHorizontal) / item.ActualWidth;
                                DragRight(scale, item, designer.SelectionService);
                                break;
                            default:
                                break;
                        }
                    }
                }
                e.Handled = true;
            }
        }
        #endregion
        #region Helper methods

        private void DragLeft(double scale, DesignerItem item, SelectionService selectionService)
        {
            IEnumerable<DesignerItem> groupItems = selectionService.GetGroupMembers(item).Cast<DesignerItem>();

            //dwi 9.12.2010
            //double groupLeft = Canvas.GetLeft(item) + item.Width;
            double groupLeft = Canvas.GetLeft(item) + item.ActualWidth;
            foreach (DesignerItem groupItem in groupItems)
            {
                double groupItemLeft = Canvas.GetLeft(groupItem);
                double delta = (groupLeft - groupItemLeft) * (scale - 1);
                Canvas.SetLeft(groupItem, groupItemLeft - delta);
                groupItem.Width = groupItem.ActualWidth * scale;
            }
        }

        private void DragTop(double scale, DesignerItem item, SelectionService selectionService)
        {
            IEnumerable<DesignerItem> groupItems = selectionService.GetGroupMembers(item).Cast<DesignerItem>();

            //dwi 9.12.2010
            //double groupBottom = Canvas.GetTop(item) + item.Height;
            double groupBottom = Canvas.GetTop(item) + item.ActualHeight;
            foreach (DesignerItem groupItem in groupItems)
            {
                double groupItemTop = Canvas.GetTop(groupItem);
                double delta = (groupBottom - groupItemTop) * (scale - 1);
                Canvas.SetTop(groupItem, groupItemTop - delta);
                groupItem.Height = groupItem.ActualHeight * scale;
            }
        }

        private void DragRight(double scale, DesignerItem item, SelectionService selectionService)
        {
            IEnumerable<DesignerItem> groupItems = selectionService.GetGroupMembers(item).Cast<DesignerItem>();

            double groupLeft = Canvas.GetLeft(item);
            foreach (DesignerItem groupItem in groupItems)
            {
                double groupItemLeft = Canvas.GetLeft(groupItem);
                double delta = (groupItemLeft - groupLeft) * (scale - 1);

                Canvas.SetLeft(groupItem, groupItemLeft + delta);
                groupItem.Width = groupItem.ActualWidth * scale;
            }
        }

        private void DragBottom(double scale, DesignerItem item, SelectionService selectionService)
        {
            IEnumerable<DesignerItem> groupItems = selectionService.GetGroupMembers(item).Cast<DesignerItem>();
            double groupTop = Canvas.GetTop(item);
            foreach (DesignerItem groupItem in groupItems)
            {
                double groupItemTop = Canvas.GetTop(groupItem);
                double delta = (groupItemTop - groupTop) * (scale - 1);

                Canvas.SetTop(groupItem, groupItemTop + delta);
                groupItem.Height = groupItem.ActualHeight * scale;
            }
        }

        private void CalculateDragLimits(IEnumerable<DesignerItem> selectedItems, out double minLeft, out double minTop, out double minDeltaHorizontal, out double minDeltaVertical)
        {
            minLeft = double.MaxValue;
            minTop = double.MaxValue;
            minDeltaHorizontal = double.MaxValue;
            minDeltaVertical = double.MaxValue;

            // drag limits are set by these parameters: canvas top, canvas left, minHeight, minWidth
            // calculate min value for each parameter for each item
            foreach (DesignerItem item in selectedItems)
            {
                double left = Canvas.GetLeft(item);
                double top = Canvas.GetTop(item);

                minLeft = double.IsNaN(left) ? 0 : Math.Min(left, minLeft);
                minTop = double.IsNaN(top) ? 0 : Math.Min(top, minTop);

                minDeltaVertical = Math.Min(minDeltaVertical, item.ActualHeight - item.MinHeight);
                minDeltaHorizontal = Math.Min(minDeltaHorizontal, item.ActualWidth - item.MinWidth);
            }
        }

        #endregion
        #endregion
    }
}
