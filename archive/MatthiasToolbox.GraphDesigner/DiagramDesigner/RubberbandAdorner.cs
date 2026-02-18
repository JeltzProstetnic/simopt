using System;
using System.Windows.Documents;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using MatthiasToolbox.Presentation.Interfaces;

namespace MatthiasToolbox.GraphDesigner.DiagramDesigner
{
    /// <summary>
    /// DesignerItem selection AdornerLayer.
    /// </summary>
    public class RubberbandAdorner : Adorner
    {
        private readonly Point? _startPoint;
        private Point? _endPoint;
        private readonly Pen _rubberbandPen;

        private readonly GraphControl _graphControl;

        /// <summary>
        /// Initializes a new instance of the <see cref="RubberbandAdorner"/> class.
        /// Creates the AdornerLayer.
        /// </summary>
        /// <param name="graphControl">The graph control.</param>
        /// <param name="dragStartPoint">The drag start point .</param>
        public RubberbandAdorner(GraphControl graphControl, Point? dragStartPoint)
            : base(graphControl)
        {
            this._graphControl = graphControl;
            this._startPoint = dragStartPoint;
            _rubberbandPen = new Pen(Brushes.LightSlateGray, 1);
            _rubberbandPen.DashStyle = new DashStyle(new double[] { 2 }, 1);
        }

        /// <summary>
        /// If left mouse button is pressed, selection will be updated and the end point will be set.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs"/> that contains the event data.</param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (!this.IsMouseCaptured)
                    this.CaptureMouse();

                _endPoint = e.GetPosition(this);
                UpdateSelection();
                this.InvalidateVisual();
            }
            else
            {
                if (this.IsMouseCaptured) this.ReleaseMouseCapture();
            }

            e.Handled = true;
        }

        /// <summary>
        /// Invoked when an unhandled <see cref="E:System.Windows.Input.Mouse.MouseUp"/> routed event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// Removes the adorner layer of the drag thumb.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseButtonEventArgs"/> that contains the event data. The event data reports that the mouse button was released.</param>
        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            // release mouse capture
            if (this.IsMouseCaptured) this.ReleaseMouseCapture();

            // remove this adorner from adorner layer
            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(this._graphControl);
            if (adornerLayer != null)
                adornerLayer.Remove(this);

            e.Handled = true;
        }

        /// <summary>
        /// Draws the selection rectangle.
        /// </summary>
        /// <param name="dc">The drawing context.</param>
        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            // without a background the OnMouseMove event would not be fired!
            // Alternative: implement a Canvas as a child of this adorner, like
            // the ConnectionAdorner does.
            dc.DrawRectangle(Brushes.Transparent, null, new Rect(RenderSize));

            if (this._startPoint.HasValue && this._endPoint.HasValue)
                dc.DrawRectangle(Brushes.Transparent, _rubberbandPen, new Rect(this._startPoint.Value, this._endPoint.Value));
        }

        /// <summary>
        /// Updates the DesignerItem selection.
        /// </summary>
        private void UpdateSelection()
        {
            _graphControl.Designer.SelectionService.ClearSelection();

            Rect rubberBand = new Rect(_startPoint.Value, _endPoint.Value);
            foreach (UIElement item in _graphControl.Designer.Children)
            {
                Rect itemRect = VisualTreeHelper.GetDescendantBounds(item);
                Rect itemBounds = item.TransformToAncestor(_graphControl).TransformBounds(itemRect);

                if (rubberBand.Contains(itemBounds))
                {
                    if (item is Connection)
                        _graphControl.Designer.SelectionService.AddToSelection(item as ISelectable);
                    else
                    {
                        DesignerItem di = item as DesignerItem;
                        if (di != null && di.ParentID == Guid.Empty)
                            _graphControl.Designer.SelectionService.AddToSelection(di);
                    }
                }
            }
        }
    }
}