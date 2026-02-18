using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using MatthiasToolbox.GraphDesigner.DiagramDesigner;
using MatthiasToolbox.GraphDesigner.Interfaces;
using MatthiasToolbox.GraphDesigner.Utilities;
using MatthiasToolbox.Presentation.Controls.DiagramDesigner;
using MatthiasToolbox.Basics.Datastructures.Graph;

namespace MatthiasToolbox.GraphDesigner.MouseHandlers
{
    public class ConnectionCursor : IMouseHandler
    {
        #region cvar
        
        private readonly GraphControl _graphControl;
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

        private readonly DesignerDragHelper _designerDragHelper;

        private DesignerItem _sourceDesignerItem;
        private IEdge<Point> _edge;
        private Point? _referenceConnectorPoint;
        private DesignerItem _hitDesignerItem;
        private NewConnectionAdorner _connectionAdorner;
        private Point? _relativePositionDesignerItemHit;

        #endregion
        #region ctor

        public ConnectionCursor()
        {
            this._designerDragHelper = new DesignerDragHelper();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionCursor"/> class.
        /// </summary>
        /// <param name="graphControl">The graph control.</param>
        /// <param name="edge">The selected edge.</param>
        public ConnectionCursor(GraphControl graphControl, IEdge<Point> edge)
            : this()
        {
            this._graphControl = graphControl;
            this._edge = edge;
        }

        #endregion
        #region Implementation of IGraphControlHandler

        /// <summary>
        /// Handles the <see cref="System.Windows.Input.Mouse.MouseDown"/> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs"/> that contains the event data.</param>
        public void MouseDown(object sender, MouseButtonEventArgs e)
        {
            this._mouseButtonDown = e.ChangedButton;
            this._origZoomAndPanControlMouseDownPoint = e.GetPosition(this._graphControl.Panel);
            this._designerCanvasMouseDown = e.GetPosition(this._graphControl.Designer);

            if (this._mouseButtonDown != MouseButton.Left)
                return;

            this._graphControl.Designer.Focus();
            Keyboard.Focus(_graphControl.Designer);

            //check if designer items were hit
            DesignerItem designerItemsHit;
            //designerItemsHit = this._designerDragHelper.GetDesignerItemsMouseHit(_designerCanvasMouseDown, _graphControl.Designer);
            Point? relativePosition;
            PathFinder.HitTesting(this._graphControl.Designer, this._designerCanvasMouseDown, null, out designerItemsHit, out relativePosition);
            this._relativePositionDesignerItemHit = relativePosition;

            if(designerItemsHit != null)
            {
                this._sourceDesignerItem = designerItemsHit;
                //Capture the mouse so that we eventually receive the mouse up event.
                //_graphControl.Panel.CaptureMouse();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handles the <see cref="System.Windows.Input.Mouse.MouseUp"/> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs"/> that contains the event data.</param>
        public void MouseUp(object sender, MouseButtonEventArgs e)
        {
            //if (this._sourceDesignerItem == null)
            //    return;

            //if (this._mouseButtonDown != MouseButton.Left)
            //    return;

            ////check if designer items were hit
            ////DesignerItem designerItemHit = this._designerDragHelper.GetDesignerItemsMouseHit(e.GetPosition(this._graphControl.Designer), _graphControl.Designer);

            //if (this._hitDesignerItem != null && this._sourceDesignerItem != this._hitDesignerItem)
            //{
            //    Connection connection = new Connection(this._sourceDesignerItem, this._hitDesignerItem);
            //    connection.Edge = new PartOfRelation() {RelationType = this._edge.RelationType};

            //    this._graphControl.Designer.Children.Add(connection);
            //}
            
            //this._graphControl.Panel.ReleaseMouseCapture();
            //e.Handled = true;
        }

        /// <summary>
        /// Handles the <see cref="System.Windows.Input.Mouse.MouseMove"/> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs"/> that contains the event data.</param>
        public void MouseMove(object sender, MouseEventArgs e)
        {
            if (this._sourceDesignerItem != null 
                && this._mouseButtonDown == MouseButton.Left) CreateConnectionAdorner();
            //{
            //    NewConnectionAdorner connectionAdorner = this.ConnectionAdornder;
            //}
            e.Handled = true;
            //Point currentPosition = Mouse.GetPosition(this._graphControl.Designer);
            //DesignerItem hitDesigner;
            //Point? referencePoint;
            //ConnectionHelper.HitTesting(this._graphControl.Designer, currentPosition, this._sourceDesignerItem, out hitDesigner, out referencePoint);
            //this._referenceConnectorPoint = referencePoint;
            //this._hitDesignerItem = hitDesigner;
            //this.pathGeometry = UpdatePathGeometry(currentPosition);
            //this.InvalidateVisual();
        }

        //protected NewConnectionAdorner ConnectionAdornder
        //{
        //    get
        //    {
        //        return CreateConnectionAdorner();
        //    }
        //}

        public void MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // nothing happens
        }

        #endregion
        #region impl

        private NewConnectionAdorner CreateConnectionAdorner()
        {
            if (this._connectionAdorner == null)
            {
                AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(this._graphControl.Designer);
                if (adornerLayer != null)
                {
                    this._connectionAdorner = new NewConnectionAdorner(this._graphControl.Designer, this._sourceDesignerItem, this._relativePositionDesignerItemHit, _edge);
                    adornerLayer.Add(this._connectionAdorner);
                }
                this._connectionAdorner.Visibility = Visibility.Visible;
            }
            return this._connectionAdorner;
        }

        #endregion
    }
}
