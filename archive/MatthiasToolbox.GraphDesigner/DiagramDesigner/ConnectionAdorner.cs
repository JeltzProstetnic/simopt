using System.Windows.Documents;
using System.Windows.Controls;
using MatthiasToolbox.GraphDesigner.Events;
using MatthiasToolbox.GraphDesigner.Utilities;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using MatthiasToolbox.Presentation.Utilities;

namespace MatthiasToolbox.GraphDesigner.DiagramDesigner
{
    public class ConnectionAdorner : Adorner
    {
        #region cvar

        private readonly DesignerCanvas _designerCanvas;
        private readonly Canvas _adornerCanvas;
        private readonly Connection _connection;
        private PathGeometry _pathGeometry;
        private Thumb _sourceDragThumb, _sinkDragThumb;
        private readonly Pen _drawingPen;

        private DesignerItem _hitDesignerItem, _fixDesigneritem;

        private readonly VisualCollection _visualChildren;
        private Point? _fixDIRelativePosition, _hitDIRelativePosition;
        private GraphControl _graphControl;

        #endregion
        #region prop

        private DesignerItem HitDesignerItem
        {
            get { return _hitDesignerItem; }
            set
            {
                if (_hitDesignerItem != value)
                {
                    if (_hitDesignerItem != null)
                        _hitDesignerItem.IsDragConnectionOver = false;

                    _hitDesignerItem = value;

                    if (_hitDesignerItem != null)
                        _hitDesignerItem.IsDragConnectionOver = true;
                }
            }
        }

        #region override
        protected override int VisualChildrenCount
        {
            get
            {
                return this._visualChildren.Count;
            }
        }

        protected override Visual GetVisualChild(int index)
        {
            return this._visualChildren[index];
        }
        #endregion
        #endregion
        #region ctor

        public ConnectionAdorner(DesignerCanvas designer, Connection connection)
            : base(designer)
        {
            this._designerCanvas = designer;
            this._graphControl = WPFTreeHelper.FindParentControl<GraphControl>(designer);

            this._adornerCanvas = new Canvas();

            this._visualChildren = new VisualCollection(this);
            this._visualChildren.Add(_adornerCanvas);

            this._connection = connection;
            this._connection.PropertyChanged += new PropertyChangedEventHandler(AnchorPositionChanged);

            InitializeDragThumbs();

            _drawingPen = new Pen(Brushes.LightSlateGray, 1);
            _drawingPen.LineJoin = PenLineJoin.Round;

            base.Unloaded += new RoutedEventHandler(ConnectionAdorner_Unloaded);
        }

        #endregion
        #region impl

        #region events

        void AnchorPositionChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("AnchorPositionSource"))
            {
                Canvas.SetLeft(_sourceDragThumb, _connection.AnchorPositionSource.X);
                Canvas.SetTop(_sourceDragThumb, _connection.AnchorPositionSource.Y);
            }

            if (e.PropertyName.Equals("AnchorPositionSink"))
            {
                Canvas.SetLeft(_sinkDragThumb, _connection.AnchorPositionSink.X);
                Canvas.SetTop(_sinkDragThumb, _connection.AnchorPositionSink.Y);
            }
        }

        void thumbDragThumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            if (HitDesignerItem != null)
            {
                if (_connection != null)
                {
                    if (_connection.Source == _fixDesigneritem)
                    {
                        _connection.Sink = this.HitDesignerItem;
                        _connection.RelativePositionSink = this._hitDIRelativePosition;
                    }
                    else
                    {
                        _connection.Source = this.HitDesignerItem;
                        _connection.RelativePositionSource = this._hitDIRelativePosition;
                    }
                }
            }

            this.HitDesignerItem = null;
            this._hitDIRelativePosition = null;
            this._fixDIRelativePosition = null;
            this._pathGeometry = null;
            this._connection.StrokeDashArray = null;
            this.InvalidateVisual();

            this._graphControl.OnEdgeChanged(new EdgeEventArgs() { Edge = this._connection.Edge });
        }

        void thumbDragThumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            this.HitDesignerItem = null;
            this._pathGeometry = null;
            this.Cursor = Cursors.Cross;
            this._connection.StrokeDashArray = new DoubleCollection(new double[] { 1, 2 });

            if (sender == _sourceDragThumb)
            {
                _fixDesigneritem = _connection.Sink;
                _fixDIRelativePosition = _connection.RelativePositionSink;
            }
            else if (sender == _sinkDragThumb)
            {
                _fixDesigneritem = _connection.Source;
                _fixDIRelativePosition = _connection.RelativePositionSource;
            }
        }

        void thumbDragThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            Point currentPosition = Mouse.GetPosition(this);
            DesignerItem hitDesigner;
            Point? referencePoint;
            PathFinder.HitTesting(this._designerCanvas, currentPosition, _fixDesigneritem, out hitDesigner, out referencePoint);
            this._hitDIRelativePosition = referencePoint;
            this.HitDesignerItem = hitDesigner;
            this._pathGeometry = UpdatePathGeometry(currentPosition);
            this.InvalidateVisual();
        }
        #endregion

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            dc.DrawGeometry(null, _drawingPen, this._pathGeometry); //fill, stroke, geometry to draw
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _adornerCanvas.Arrange(new Rect(0, 0, this._designerCanvas.ActualWidth, this._designerCanvas.ActualHeight));
            return finalSize;
        }

        private void ConnectionAdorner_Unloaded(object sender, RoutedEventArgs e)
        {
            _sourceDragThumb.DragDelta -= new DragDeltaEventHandler(thumbDragThumb_DragDelta);
            _sourceDragThumb.DragStarted -= new DragStartedEventHandler(thumbDragThumb_DragStarted);
            _sourceDragThumb.DragCompleted -= new DragCompletedEventHandler(thumbDragThumb_DragCompleted);

            _sinkDragThumb.DragDelta -= new DragDeltaEventHandler(thumbDragThumb_DragDelta);
            _sinkDragThumb.DragStarted -= new DragStartedEventHandler(thumbDragThumb_DragStarted);
            _sinkDragThumb.DragCompleted -= new DragCompletedEventHandler(thumbDragThumb_DragCompleted);
        }

        private void InitializeDragThumbs()
        {
            Style dragThumbStyle = _connection.FindResource("ConnectionAdornerThumbStyle") as Style;
            this._hitDIRelativePosition = null;
            this._fixDIRelativePosition = null;

            //source drag thumb
            _sourceDragThumb = new Thumb();
            Canvas.SetLeft(_sourceDragThumb, _connection.AnchorPositionSource.X);
            Canvas.SetTop(_sourceDragThumb, _connection.AnchorPositionSource.Y);
            this._adornerCanvas.Children.Add(_sourceDragThumb);
            if (dragThumbStyle != null)
                _sourceDragThumb.Style = dragThumbStyle;

            _sourceDragThumb.DragDelta += new DragDeltaEventHandler(thumbDragThumb_DragDelta);
            _sourceDragThumb.DragStarted += new DragStartedEventHandler(thumbDragThumb_DragStarted);
            _sourceDragThumb.DragCompleted += new DragCompletedEventHandler(thumbDragThumb_DragCompleted);

            // sink drag thumb
            _sinkDragThumb = new Thumb();
            Canvas.SetLeft(_sinkDragThumb, _connection.AnchorPositionSink.X);
            Canvas.SetTop(_sinkDragThumb, _connection.AnchorPositionSink.Y);
            this._adornerCanvas.Children.Add(_sinkDragThumb);
            if (dragThumbStyle != null)
                _sinkDragThumb.Style = dragThumbStyle;

            _sinkDragThumb.DragDelta += new DragDeltaEventHandler(thumbDragThumb_DragDelta);
            _sinkDragThumb.DragStarted += new DragStartedEventHandler(thumbDragThumb_DragStarted);
            _sinkDragThumb.DragCompleted += new DragCompletedEventHandler(thumbDragThumb_DragCompleted);
        }

        /// <summary>
        /// Updates the path geometry. Calculates the path for the different routing algorithm to the connector or designer item.
        /// Prefers the Connectors.
        /// </summary>
        /// <param name="targetPos">The target position.</param>
        /// <returns></returns>
        private PathGeometry UpdatePathGeometry(Point targetPos)
        {
            if ((this._connection.Source == null || this._connection.Sink == null))
                return null;

            PathGeometry geometry = new PathGeometry();

            //build source info
            ConnectionInfo sourceInfo = new ConnectionInfo(this._fixDesigneritem);
            Point parentPos = new Point(sourceInfo.DesignerItemLeft, sourceInfo.DesignerItemTop);
            if (this._fixDIRelativePosition == null)
                sourceInfo.Position = PathFinder.CalculateAttachPoint(sourceInfo.DesignerItemCenter, sourceInfo.DesignerItemSize, targetPos);
            else
                sourceInfo.Position = PathFinder.GetAbsolutePosition(sourceInfo.DesignerItemSize, parentPos, (Point) this._fixDIRelativePosition);
            sourceInfo.Orientation = PathFinder.GetConnectorOrientation(parentPos, sourceInfo.Position, sourceInfo.DesignerItemSize);

            //build sink info
            ConnectionInfo sinkInfo = new ConnectionInfo();
            sinkInfo.Cap = this._connection.SinkInfo.Cap;
            sinkInfo.DesignerItemCenter = targetPos;
            sinkInfo.DesignerItemLeft = targetPos.X;
            sinkInfo.DesignerItemSize = new Size(0, 0);
            sinkInfo.DesignerItemTop = targetPos.Y;
            sinkInfo.Position = targetPos;
            sinkInfo.Orientation = PathFinder.GetConnectorOrientation(sourceInfo.DesignerItemCenter, targetPos, sinkInfo.DesignerItemSize);

            this._connection.UpdateConnectionInfo();
            geometry.Figures = PathFinder.GetConnectionPoints(sourceInfo, sinkInfo, this._connection.Routing);
            
            return geometry;
        }

        #endregion
    }
}
