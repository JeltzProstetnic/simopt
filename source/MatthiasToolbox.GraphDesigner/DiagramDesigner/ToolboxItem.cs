using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using MatthiasToolbox.GraphDesigner.Utilities;

namespace MatthiasToolbox.GraphDesigner.DiagramDesigner
{
    /// <summary>
    /// Represents a selectable item in the Toolbox.
    /// </summary>
    public class ToolboxItem : ContentControl
    {
        private const double SCALE = 1.3;

        /// <summary>
        /// caches the start point of the drag operation
        /// </summary>
        private Point? _dragStartPoint = null;

        /// <summary>
        /// Initializes the <see cref="ToolboxItem"/> class.
        /// Set the key to reference the style for this control.
        /// </summary>
        static ToolboxItem()
        {
            // set the key to reference the style for this control
            FrameworkElement.DefaultStyleKeyProperty.OverrideMetadata(
                typeof(ToolboxItem), new FrameworkPropertyMetadata(typeof(ToolboxItem)));
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
            this._dragStartPoint = new Point?(e.GetPosition(this));
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (e.LeftButton != MouseButtonState.Pressed)
                this._dragStartPoint = null;

            if (!this._dragStartPoint.HasValue) return;

            DragObject dataObject = GetDataObject();
            DragDrop.DoDragDrop(this, dataObject, DragDropEffects.Copy);
            e.Handled = true;
        }

        /// <summary>
        /// Gets the DragObject.
        /// </summary>
        /// <returns></returns>
        private DragObject GetDataObject()
        {
            DragObject dataObject;

            if (this.Content is UserControl)
            {
                UserControl control = this.Content as UserControl;
                dataObject = new DragObject { DataContext = control.DataContext };
            }
            else
            {
                // XamlWriter.Save() has limitations in exactly what is serialized,
                // see SDK documentation; short term solution only;
                string xamlString = XamlWriter.Save(this.Content);
                dataObject = new DragObject { Xaml = xamlString };
            }

            dataObject.DesiredSize = GetParentSize();

            return dataObject;
        }

        /// <summary>
        /// Gets the size of the parent WrapPanel.
        /// </summary>
        /// <returns></returns>
        private Size? GetParentSize()
        {
            WrapPanel panel = VisualTreeHelper.GetParent(this) as WrapPanel;

            if (panel != null)
            {
                // desired size for DesignerCanvas is the stretched Toolbox item size
                return new Size(panel.ItemWidth * SCALE, panel.ItemHeight * SCALE);
            }

            return null;
        }
    }
}