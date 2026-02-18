using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MatthiasToolbox.Presentation.Interfaces;
using MatthiasToolbox.Presentation.Utilities;

namespace MatthiasToolbox.GraphDesigner.DiagramDesigner
{
    internal class DesignerDragHelper
    {
        #region cvar

        private Point _origContentMouseDownPoint;
        private DesignerCanvas _designerCanvas;

        private readonly List<DependencyObject> _hitResultsList;
        private readonly Dictionary<DesignerItem, Point> _designerStartDragPositions;

        #endregion
        #region prop

        internal Point CanvasMouseDownPoint
        {
            get { return _origContentMouseDownPoint; }
            set { _origContentMouseDownPoint = value; }
        }

        #endregion
        #region ctor

        internal DesignerDragHelper()
        {
            _hitResultsList = new List<DependencyObject>();
            _designerStartDragPositions = new Dictionary<DesignerItem, Point>();
        }

        #endregion
        #region impl

        /// <summary>
        /// Initializes the Drag points and the moved designer items
        /// </summary>
        /// <param name="designerCanvas">The designer canvas.</param>
        /// <param name="designerCanvasMouseDown">The designer canvas mouse down.</param>
        internal void DragStart(DesignerCanvas designerCanvas, Point designerCanvasMouseDown)
        {
            this._designerCanvas = designerCanvas;
            this._origContentMouseDownPoint = designerCanvasMouseDown;
            this._designerStartDragPositions.Clear();

            // we only move DesignerItems, add start drag positions to a dictionary
            foreach (DesignerItem designerItem in _designerCanvas.SelectionService.CurrentSelection.OfType<DesignerItem>())
            {
                double left = Canvas.GetLeft(designerItem);
                double top = Canvas.GetTop(designerItem);

                if (double.IsNaN(left)) left = 0;
                if (double.IsNaN(top)) top = 0;

                _designerStartDragPositions.Add(designerItem, new Point(left, top));
            }
        }

        /// <summary>
        /// Does the shape drag move. Only the shape will be moved.
        /// </summary>
        /// <param name="e">The <see cref="System.Windows.Input.MouseEventArgs"/> instance containing the event data.</param>
        internal void DoDragMove(MouseEventArgs e)
        {
            double minLeft = double.MaxValue;
            double minTop = double.MaxValue;

            Point currentMousePointOnCanvas = e.GetPosition(_designerCanvas);

            //dragged distances
            double deltaHorizontal = Math.Max(-minLeft, currentMousePointOnCanvas.X - CanvasMouseDownPoint.X);
            double deltaVertical = Math.Max(-minTop, currentMousePointOnCanvas.Y - CanvasMouseDownPoint.Y);

            //set new positions of all dragged items
            foreach (DesignerItem item in this._designerStartDragPositions.Keys)
            {
                Point position = this._designerStartDragPositions[item];
                double left = position.X;
                double top = position.Y;

                Canvas.SetLeft(item, left + deltaHorizontal);
                Canvas.SetTop(item, top + deltaVertical);
            }

            _designerCanvas.InvalidateMeasure();
        }

        /// <summary>
        /// Called when dragging ends. Expands the Canvas if a dragged item is outside the canvas.
        /// </summary>
        /// <param name="e">The <see cref="System.Windows.Input.MouseEventArgs"/> instance containing the event data.</param>
        internal void DragEnd()
        {
            //get the minimum positions of the designer items
            double minLeft = double.MaxValue;
            double minTop = double.MaxValue;
            foreach (DesignerItem item in this._designerStartDragPositions.Keys)
            {
                double left = DesignerCanvas.GetLeft(item);
                double top = DesignerCanvas.GetTop(item);
                minLeft = Math.Min(minLeft, left);
                minTop = Math.Min(minTop, top);
            }

            //expand the canvas
            if (minLeft < DesignerCanvas.GAP_X || minTop < DesignerCanvas.GAP_Y)
            {
                minLeft = minLeft < DesignerCanvas.GAP_X ? Math.Abs(minLeft) + DesignerCanvas.GAP_X : 0;
                minTop = minTop < DesignerCanvas.GAP_Y ? Math.Abs(minTop) + DesignerCanvas.GAP_Y : 0;

                this._designerCanvas.ExpandCanvas(minLeft, minTop);
            }
        }

        /// <summary>
        /// Gets the designer item on top of the canvas hit by the mouse down event.
        /// </summary>
        /// <returns></returns>
        internal DesignerItem GetDesignerItemsMouseHit(Point mouseDownPoint, DesignerCanvas designerCanvas)
        {
            List<DesignerItem> designerItemsHit = new List<DesignerItem>();

            // Clear the contents of the list used for hit test results.
            _hitResultsList.Clear();

            // Set up a callback to receive the hit test result enumeration.
            VisualTreeHelper.HitTest(designerCanvas, null,
                                     new HitTestResultCallback(MyHitTestResult),
                                     new PointHitTestParameters(mouseDownPoint));

            // Perform actions on the hit test results list. check if a designer item was hit
            foreach (DependencyObject dependencyObject in _hitResultsList)
            {
                DesignerItem parentDesignerItem = WPFTreeHelper.FindParentControl<DesignerItem>(dependencyObject, designerCanvas);
                if (!designerItemsHit.Contains(parentDesignerItem))
                    designerItemsHit.Add(parentDesignerItem);
            }

            return designerItemsHit.Count > 0 ? designerItemsHit[0] : null;
        }

        /// <summary>
        /// Gets the designer item on top of the canvas hit by the mouse down event.
        /// </summary>
        /// <returns></returns>
        internal Connection GetConnectionsMouseHit(Point mouseDownPoint, DesignerCanvas designerCanvas)
        {
            List<Connection> connectionsHit = new List<Connection>();

            // Clear the contents of the list used for hit test results.
            _hitResultsList.Clear();

            // Set up a callback to receive the hit test result enumeration.
            VisualTreeHelper.HitTest(designerCanvas, null,
                                     new HitTestResultCallback(MyHitTestResult),
                                     new PointHitTestParameters(mouseDownPoint));

            // Perform actions on the hit test results list. check if a designer item was hit
            foreach (DependencyObject dependencyObject in _hitResultsList)
            {
                Connection parentConnection = WPFTreeHelper.FindParentControl<Connection>(dependencyObject, designerCanvas);
                if (!connectionsHit.Contains(parentConnection))
                    connectionsHit.Add(parentConnection);
            }

            return connectionsHit.Count > 0 ? connectionsHit[0] : null;
        }

        /// <summary>
        /// Gets the designer item on top of the canvas hit by the mouse down event.
        /// </summary>
        /// <returns></returns>
        internal List<ISelectable> GetControlsHit(Point mouseDownPoint, DesignerCanvas designerCanvas)
        {
            List<ISelectable> controls = new List<ISelectable>();

            // Clear the contents of the list used for hit test results.
            _hitResultsList.Clear();

            // Set up a callback to receive the hit test result enumeration.
            VisualTreeHelper.HitTest(designerCanvas, null,
                                     new HitTestResultCallback(MyHitTestResult),
                                     new PointHitTestParameters(mouseDownPoint));

            // Perform actions on the hit test results list. check if a designer item was hit
            foreach (DependencyObject dependencyObject in _hitResultsList)
            {
                ISelectable parentControl = (ISelectable) WPFTreeHelper.FindParentControlByInterface<ISelectable>(dependencyObject, designerCanvas);
                if (parentControl != null && !controls.Contains(parentControl))
                    controls.Add(parentControl);
            }

            return controls;
        }

        /// <summary>
        /// Return the result of the hit test to the callback.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <returns></returns>
        private HitTestResultBehavior MyHitTestResult(HitTestResult result)
        {
            // Add the hit test result to the list that will be processed after the enumeration.
            _hitResultsList.Add(result.VisualHit);

            // Set the behavior to return visuals at all z-order levels.
            return HitTestResultBehavior.Continue;

            // Set the behavior to stop enumerating visuals.
            //return HitTestResultBehavior.Stop;
        }

        #endregion
    }
}
