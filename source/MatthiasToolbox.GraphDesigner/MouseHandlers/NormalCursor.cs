using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using MatthiasToolbox.GraphDesigner.DiagramDesigner;
using MatthiasToolbox.GraphDesigner.Events;
using MatthiasToolbox.GraphDesigner.Interfaces;
using MatthiasToolbox.Presentation.Controls.DiagramDesigner;
using MatthiasToolbox.GraphDesigner.Enumerations;

namespace MatthiasToolbox.GraphDesigner.MouseHandlers
{
    /// <summary>
    /// Normal Mouse Cursor handling
    /// </summary>
    internal class NormalCursor : IMouseHandler
    {
        private readonly GraphControl _graphControl;
        private readonly DesignerDragHelper _designerDragHelper;
        /// <summary>
        /// Records which mouse button clicked during mouse dragging.
        /// </summary>
        private MouseButton _mouseButtonDown;
        /// <summary>
        /// The point that was clicked relative to the ZoomAndPanControl.
        /// </summary>
        private Point _origZoomAndPanControlMouseDownPoint;
        /// <summary>
        /// The point that was clicked relative to the content that is contained within the ZoomAndPanControl.
        /// </summary>
        private Point _designerCanvasMouseDown;
        private bool _forceRectangleMode;
        /// <summary>
        /// Specifies the current state of the mouse handling logic.
        /// </summary>
        private MouseHandlingMode _mouseHandlingMode;

        #region ctor
        internal NormalCursor(GraphControl graphControl)
        {
            this._graphControl = graphControl;
            this._designerDragHelper = new DesignerDragHelper();
        }
        #endregion
        #region impl
        #region IGraphCursor

        /// <summary>
        /// Handles the <see cref="System.Windows.Input.Mouse.MouseDown"/> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs"/> that contains the event data.</param>
        public void MouseDown(object sender, MouseButtonEventArgs e)
        {
            _graphControl.Designer.Focus();
            Keyboard.Focus(_graphControl.Designer);

            _mouseButtonDown = e.ChangedButton;
            _origZoomAndPanControlMouseDownPoint = e.GetPosition(_graphControl.Panel);
            _designerCanvasMouseDown = e.GetPosition(_graphControl.Designer);

            if((Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                return;
            }

            if (((Keyboard.Modifiers & ModifierKeys.Shift) != 0) &&
                (e.ChangedButton == MouseButton.Left ||
                 e.ChangedButton == MouseButton.Right))
            {

                //set mouse handling mode
                // Shift + left- or right-down initiates zooming mode if no designeritem was hit
                _mouseHandlingMode = MouseHandlingMode.Zooming;
            }
            else if (_mouseButtonDown == MouseButton.Left)
            {
                //check if designer items were hit
                DesignerItem designerItemsHit = this._designerDragHelper.GetDesignerItemsMouseHit(_designerCanvasMouseDown, _graphControl.Designer);
                Connection connectionsHit = this._designerDragHelper.GetConnectionsMouseHit(_designerCanvasMouseDown, _graphControl.Designer);

                //set mouse handling mode
                if (designerItemsHit == null && connectionsHit == null)
                    _mouseHandlingMode = MouseHandlingMode.Panning; // Just a plain old left-down initiates panning mode.
                else
                {
                    _mouseHandlingMode = MouseHandlingMode.Dragging;
                    this._designerDragHelper.DragStart(_graphControl.Designer, _designerCanvasMouseDown);
                    _graphControl.Designer.CaptureMouse();
                    e.Handled = true;
                }
            }

            if (_mouseHandlingMode != MouseHandlingMode.None && _mouseHandlingMode != MouseHandlingMode.Dragging)
            {
                //Capture the mouse so that we eventually receive the mouse up event.
                _graphControl.Panel.CaptureMouse();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handles the <see cref="System.Windows.Input.Mouse.MouseUp"/> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs"/> that contains the event data.</param>
        public void MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (this._mouseHandlingMode == MouseHandlingMode.None)
            {
                return;
            }

            switch (this._mouseHandlingMode)
            {
                case MouseHandlingMode.Zooming:
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
                    break;
                case MouseHandlingMode.DragZooming: //drag a rectangle
                    this._graphControl.ApplyDragZoomRect();
                    this._graphControl.OnAfterRectangleZoom(e);
                    break;
                case MouseHandlingMode.Dragging: //drag the selected elements
                    this._graphControl.Designer.ReleaseMouseCapture();
                    this._designerDragHelper.DragEnd();
                    foreach (DesignerItem designerItem in _graphControl.Designer.SelectionService.CurrentSelection.OfType<DesignerItem>())
                    {
                        _graphControl.OnVertexChanged(new VertexEventArgs() { Vertex = designerItem.Vertex });
                    }
                    break;
                case MouseHandlingMode.Panning:
                    this._graphControl.Designer.SelectionService.ClearSelection();
                    this._graphControl.Designer.Focus();
                    break;
            }

            this._graphControl.Panel.ReleaseMouseCapture();
            this._mouseHandlingMode = MouseHandlingMode.None;
            e.Handled = true;
        }

        /// <summary>
        /// Handles the <see cref="System.Windows.Input.Mouse.MouseMove"/> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs"/> that contains the event data.</param>
        public void MouseMove(object sender, MouseEventArgs e)
        {
            if (_mouseHandlingMode == MouseHandlingMode.Panning)
            {
                DoPanning(e);

                e.Handled = true;
            }
            else if (_mouseHandlingMode == MouseHandlingMode.Zooming)
            {
                Point curZoomAndPanControlMousePoint = e.GetPosition(_graphControl.Panel);
                Vector dragOffset = curZoomAndPanControlMousePoint - _origZoomAndPanControlMouseDownPoint;
                if (_mouseButtonDown == MouseButton.Left &&
                    (Math.Abs(dragOffset.X) > SystemParameters.MinimumHorizontalDragDistance ||
                     Math.Abs(dragOffset.Y) > SystemParameters.MinimumVerticalDragDistance))
                {
                    //
                    // When Shift + left-down zooming mode and the user drags beyond the drag threshold,
                    // initiate drag zooming mode where the user can drag out a rectangle to select the area
                    // to zoom in on.
                    //
                    _mouseHandlingMode = MouseHandlingMode.DragZooming;
                    Point curContentMousePoint = e.GetPosition(_graphControl.Designer);
                    _graphControl.InitDragRect(_designerCanvasMouseDown, curContentMousePoint);
                }

                e.Handled = true;
            }
            else if (_mouseHandlingMode == MouseHandlingMode.DragZooming)
            {
                //
                // When in drag zooming mode continously update the position of the rectangle
                // that the user is dragging out.
                //
                Point curContentMousePoint = e.GetPosition(_graphControl.Designer);
                _graphControl.SetDragRect(_designerCanvasMouseDown, curContentMousePoint);

                e.Handled = true;
            }
            else if (_mouseHandlingMode == MouseHandlingMode.Dragging)
            {
                this._designerDragHelper.DoDragMove(e);
                e.Handled = true;
            }
        }




        /// <summary>
        /// Mouses the double click.
        /// 
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param>
        public void MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == 0)
            {
                Point doubleClickPoint = e.GetPosition(_graphControl.Designer);
                if(_designerDragHelper.GetControlsHit(doubleClickPoint, _graphControl.Designer).Count == 0)
                    _graphControl.Panel.AnimatedSnapTo(doubleClickPoint);
            }
        }

        #endregion


        #region Panning Methods

        /// <summary>
        /// The user is left-dragging the mouse.
        /// Pan the viewport by the appropriate amount.
        /// </summary>
        /// <param name="e">The <see cref="System.Windows.Input.MouseEventArgs"/> instance containing the event data.</param>
        private void DoPanning(MouseEventArgs e)
        {
            Point curContentMousePoint = e.GetPosition(_graphControl.Designer);
            Vector dragOffset = curContentMousePoint - _designerCanvasMouseDown;

            _graphControl.zoomPanControl.ContentOffsetX -= dragOffset.X;
            _graphControl.zoomPanControl.ContentOffsetY -= dragOffset.Y;
        }

        #endregion
        #endregion
    }
}
