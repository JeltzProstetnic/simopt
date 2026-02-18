using System;
using System.Windows;
using System.Windows.Input;
using MatthiasToolbox.GraphDesigner.Interfaces;
using MatthiasToolbox.Presentation.Controls;
using MatthiasToolbox.GraphDesigner.Enumerations;
using MatthiasToolbox.Presentation.Interfaces;

namespace MatthiasToolbox.GraphDesigner.MouseHandlers
{
    internal class ZoomCursor : IMouseHandler
    {
        private readonly GraphControl _graphControl;
        private MouseButton _mouseButtonDown;
        private Point _designerCanvasMouseDown;
        private Point _origZoomAndPanControlMouseDownPoint;
        private MouseHandlingMode _mouseHandlingMode;

        internal ZoomCursor(GraphControl graphControl)
        {
            this._graphControl = graphControl;
        }

        #region IGraphCursor

        /// <summary>
        /// Handles the <see cref="System.Windows.Input.Mouse.MouseDown"/> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs"/> that contains the event data.</param>
        public void MouseDown(object sender, MouseButtonEventArgs e)
        {
            this._graphControl.Designer.Focus();
            Keyboard.Focus(_graphControl.Designer);

            this._mouseButtonDown = e.ChangedButton;
            this._origZoomAndPanControlMouseDownPoint = e.GetPosition(this._graphControl.Panel);
            this._designerCanvasMouseDown = e.GetPosition(this._graphControl.Designer);

            if ((e.ChangedButton == MouseButton.Left ||
                 e.ChangedButton == MouseButton.Right))
            {
                // Shift + left- or right-down initiates zooming mode.
                this._mouseHandlingMode = MouseHandlingMode.Zooming;
            }

            e.Handled = true;
        }

        /// <summary>
        /// Handles the <see cref="System.Windows.Input.Mouse.MouseUp"/> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs"/> that contains the event data.</param>
        public void MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (this._mouseHandlingMode == MouseHandlingMode.Zooming)
            {
                if (this._mouseButtonDown == MouseButton.Left)
                {
                    // Shift + left-click zooms in on the content.
                    this._graphControl.ZoomIn(this._designerCanvasMouseDown);
                }
                else if (this._mouseButtonDown == MouseButton.Right)
                {
                    // Shift + left-click zooms out from the content.
                    this._graphControl.ZoomOut(this._designerCanvasMouseDown);
                }
            }
            else if (this._mouseHandlingMode == MouseHandlingMode.DragZooming)
            {
                // When drag-zooming has finished we zoom in on the rectangle that was highlighted by the user.
                this._graphControl.ApplyDragZoomRect();
                this._graphControl.OnAfterRectangleZoom(e);
                //Cursor = Cursors.Arrow;
            }

            this._graphControl.zoomPanControl.ReleaseMouseCapture();
            this._mouseHandlingMode = MouseHandlingMode.None;
            e.Handled = true;
        }

        /// <summary>
        /// Handles the <see cref="System.Windows.Input.Mouse.MouseMove"/> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs"/> that contains the event data.</param>
        public void MouseMove(object sender, MouseEventArgs e)
        {
            if (_mouseHandlingMode == MouseHandlingMode.Zooming)
            {
                Point curZoomAndPanControlMousePoint = e.GetPosition(this._graphControl.zoomPanControl);
                Vector dragOffset = curZoomAndPanControlMousePoint - this._origZoomAndPanControlMouseDownPoint;
                if (this._mouseButtonDown == MouseButton.Left &&
                    (Math.Abs(dragOffset.X) > SystemParameters.MinimumHorizontalDragDistance ||
                     Math.Abs(dragOffset.Y) > SystemParameters.MinimumVerticalDragDistance))
                {
                    //
                    // When Shift + left-down zooming mode and the user drags beyond the drag threshold,
                    // initiate drag zooming mode where the user can drag out a rectangle to select the area
                    // to zoom in on.
                    //
                    this._mouseHandlingMode = MouseHandlingMode.DragZooming;
                    Point curContentMousePoint = e.GetPosition(this._graphControl.Designer);
                    this._graphControl.InitDragRect(this._designerCanvasMouseDown, curContentMousePoint);
                }

                e.Handled = true;
            }
            else if (this._mouseHandlingMode == MouseHandlingMode.DragZooming)
            {
                //
                // When in drag zooming mode continously update the position of the rectangle
                // that the user is dragging out.
                //
                Point curContentMousePoint = e.GetPosition(this._graphControl.Designer);
                this._graphControl.SetDragRect(this._designerCanvasMouseDown, curContentMousePoint);

                e.Handled = true;
            }
        }

        /// <summary>
        /// Mouses the double click.
        /// Pan to clicked location.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param>
        public void MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == 0)
            {
                Point doubleClickPoint = e.GetPosition(_graphControl.Designer);
                _graphControl.Panel.AnimatedSnapTo(doubleClickPoint);
            }
        }
        #endregion
    }
}
