using System;
using System.Windows;
using System.Windows.Input;
using MatthiasToolbox.Basics.Datastructures.Graph;
using MatthiasToolbox.Basics.Interfaces;
using MatthiasToolbox.GraphDesigner.DiagramDesigner;
using MatthiasToolbox.GraphDesigner.Interfaces;
using MatthiasToolbox.Presentation.Controls.DiagramDesigner;

namespace MatthiasToolbox.GraphDesigner.MouseHandlers
{
    /// <summary>
    /// The ShapeCursor drags a selected 
    /// </summary>
    internal class ShapeCursor : IMouseHandler
    {
        #region cvar

        private readonly GraphControl _graphControl;
        private Point _designerCanvasMouseDown;
        private bool _isMouseDragged = false;
        private MouseButton? _mouseButtonDown;
        private readonly IVertex<Point> _vertex;

        #endregion
        #region ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="ShapeCursor"/> class.
        /// </summary>
        /// <param name="graphControl">The graph control.</param>
        /// <param name="vertexToCopy">The vertex to copy into the designer.</param>
        internal ShapeCursor(GraphControl graphControl, IVertex<Point> vertexToCopy)
        {
            this._graphControl = graphControl;
            this._vertex = vertexToCopy;
        }

        #endregion
        #region Implementation of IGraphCursor

        /// <summary>
        /// Add the Vertex directly to the 
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs"/> that contains the event data.</param>
        public void MouseDown(object sender, MouseButtonEventArgs e)
        {
            _designerCanvasMouseDown = e.GetPosition(this._graphControl.Designer);
            _isMouseDragged = false;
            this._mouseButtonDown = e.ChangedButton;

            this._graphControl.Designer.CaptureMouse();
            
            e.Handled = true;
        }

        /// <summary>
        /// Handles the <see cref="System.Windows.Input.Mouse.MouseUp"/> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs"/> that contains the event data.</param>
        public void MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (this._mouseButtonDown != MouseButton.Left) return;

            //use the mouse down position to add the item
            double left = this._designerCanvasMouseDown.X;
            double top = this._designerCanvasMouseDown.Y;

            Point currentPosition = new Point();
            
            //if the mouse was moved the shape has a size
            if (this._isMouseDragged)
            {
                currentPosition = e.GetPosition(this._graphControl.Designer);

                //set shape position to upper left corner of the rectangle
                left = Math.Min(left, currentPosition.X);
                top = Math.Min(top, currentPosition.Y);
            }

            //use the mouse down position
            DesignerItem newItem;

            if (this._isMouseDragged)
            {
                Size desiredSize = new Size(Math.Abs(currentPosition.X - this._designerCanvasMouseDown.X),
                    Math.Abs(currentPosition.Y - this._designerCanvasMouseDown.Y));

                //expand the canvas
                if (left < DesignerCanvas.GAP_X || top < DesignerCanvas.GAP_Y)
                {
                    double minLeft = left < DesignerCanvas.GAP_X ? Math.Abs(left) + DesignerCanvas.GAP_X : 0;
                    double minTop = top < DesignerCanvas.GAP_Y ? Math.Abs(top) + DesignerCanvas.GAP_Y : 0;

                    this._graphControl.Designer.ExpandCanvas(minLeft, minTop);
                }

                this._graphControl.FadeOutDragRect();

                newItem = this._graphControl.Designer.CreateNode(this._vertex, new Point(Math.Max(0, left), Math.Max(0, top)), desiredSize);
                newItem.Width = desiredSize.Width;
                newItem.Height = desiredSize.Height;
            }
            else
            {
                Size? size = null;
                if (_vertex is ISize)
                    size = new Size(((ISize)_vertex).Width, ((ISize)_vertex).Height);
                else if (_vertex is ISize<Size>)
                    size = ((ISize<Size>) _vertex).Size;
                newItem = this._graphControl.Designer.CreateNode(this._vertex, new Point(Math.Max(0, left), Math.Max(0, top)), size);
            }
            
            DesignerCanvas.SetLeft(newItem, Math.Max(0, left));
            DesignerCanvas.SetTop(newItem, Math.Max(0, top));

            this._graphControl.Designer.AddDesignerItem(newItem);

            this._mouseButtonDown = null;

            this._graphControl.Designer.ReleaseMouseCapture();

            e.Handled = true;
        }

        /// <summary>
        /// Handles the <see cref="System.Windows.Input.Mouse.MouseMove"/> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs"/> that contains the event data.</param>
        public void MouseMove(object sender, MouseEventArgs e)
        {
            if (this._mouseButtonDown != MouseButton.Left)
                return;

            Point currentMousePosition = e.GetPosition(this._graphControl.Designer);
            Vector dragOffset = currentMousePosition - this._designerCanvasMouseDown;
            if (Math.Abs(dragOffset.X) > SystemParameters.MinimumHorizontalDragDistance ||
                     Math.Abs(dragOffset.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                if (this._isMouseDragged)
                {
                    // When already in dragging mode mode continously update the position of the rectangle
                    // that the user is dragging out.
                    Point curContentMousePoint = e.GetPosition(this._graphControl.Designer);
                    this._graphControl.SetDragRect(this._designerCanvasMouseDown, curContentMousePoint);
                }
                else
                {
                    this._isMouseDragged = true;
                    this._graphControl.InitDragRect(this._designerCanvasMouseDown, currentMousePosition);
                }
                e.Handled = true;

            }
        }

        public void MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
        }

        #endregion
    }
}
