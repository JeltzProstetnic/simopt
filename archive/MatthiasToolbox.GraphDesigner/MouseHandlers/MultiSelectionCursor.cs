using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using MatthiasToolbox.GraphDesigner.DiagramDesigner;
using MatthiasToolbox.GraphDesigner.Events;
using MatthiasToolbox.GraphDesigner.Interfaces;
using MatthiasToolbox.Presentation.Controls;
using MatthiasToolbox.Presentation.Controls.DiagramDesigner;

namespace MatthiasToolbox.GraphDesigner.MouseHandlers
{
    internal class MultiSelectionCursor : IMouseHandler
    {
        private readonly GraphControl _graphControl;
        private bool _isDragging = false;
        private readonly DesignerDragHelper _designerDragHelper;
        private Point? _designerCanvasMouseDown;
        private Point? _graphControlMouseDown;

        internal MultiSelectionCursor(GraphControl graphControl)
        {
            this._graphControl = graphControl;
            this._designerDragHelper = new DesignerDragHelper();
        }


        #region IGraphCursor

        public void MouseDown(object sender, MouseButtonEventArgs e)
        {
            // in case that this click is the start of a 
            // drag operation we cache the start point
            _designerCanvasMouseDown = e.GetPosition(_graphControl.Designer);
            _graphControlMouseDown = e.GetPosition(_graphControl);

            if (!(e.Source is DesignerCanvas) && !(e.Source is ZoomPanControl) && !(e.Source is GraphControl))
            {
                this._isDragging = true;
                this._designerDragHelper.DragStart(this._graphControl.Designer, (Point)this._designerCanvasMouseDown);
                this._graphControl.Designer.CaptureMouse();

                e.Handled = true;
                return;
            }
            this._isDragging = false;

            // if you click directly on the canvas all 
            // selected items are 'de-selected'
            this._graphControl.Designer.SelectionService.ClearSelection();
            this._graphControl.Designer.Focus();
            e.Handled = true;
        }

        /// <summary>
        /// Do Nothing
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs"/> that contains the event data.</param>
        public void MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (this._isDragging)
            {
                _graphControl.Designer.ReleaseMouseCapture();
                this._designerDragHelper.DragEnd();

                this._isDragging = false;

                foreach (DesignerItem designerItem in _graphControl.Designer.SelectionService.CurrentSelection.OfType<DesignerItem>())
                {
                    _graphControl.OnVertexChanged(new VertexEventArgs() {Vertex = designerItem.Vertex});
                }

                e.Handled = true;
            }
        }

        /// <summary>
        /// Handles the <see cref="System.Windows.Input.Mouse.MouseMove"/> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs"/> that contains the event data.</param>
        public void MouseMove(object sender, MouseEventArgs e)
        {
            if (this._isDragging)
            {//drag the selected items
                this._designerDragHelper.DoDragMove(e);
                
                e.Handled = true;
                return;
            }

            if (!(e.Source is DesignerCanvas) && !(e.Source is ZoomPanControl) && !(e.Source is GraphControl)) return;

            // if mouse button is not pressed we have no drag operation, ...
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                _graphControlMouseDown = null;
                _designerCanvasMouseDown = null;
            }

            // ... but if mouse button is pressed and start
            // point value is set we do have one
            if (_graphControlMouseDown.HasValue)
            {
                // create rubberband adorner
                AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(this._graphControl);
                if (adornerLayer != null)
                {
                    RubberbandAdorner adorner = new RubberbandAdorner(this._graphControl, _graphControlMouseDown);
                    adornerLayer.Add(adorner);
                }
            }
            e.Handled = true;
        }

        public void MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //TODO  GraphControl - Multiselection double click
        }

        #endregion
    }
}
