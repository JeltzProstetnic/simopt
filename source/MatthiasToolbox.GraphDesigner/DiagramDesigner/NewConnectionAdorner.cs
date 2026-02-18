using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using MatthiasToolbox.GraphDesigner.Enumerations;
using MatthiasToolbox.GraphDesigner.Utilities;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows;
using MatthiasToolbox.Basics.Datastructures.Graph;

namespace MatthiasToolbox.GraphDesigner.DiagramDesigner
{
    public class NewConnectionAdorner : Adorner
    {
        #region cvar
        #region visuals
        
        private readonly Pen _drawingPen;
        private readonly Canvas _adornerCanvas;
        private readonly VisualCollection _visualChildren;

        #endregion

        private PathGeometry _pathGeometry;
        private readonly DesignerCanvas _designerCanvas;
        
        private DesignerItem _hitDesignerItem;
        private DesignerItem _sourceDesignerItem;
        private Point? _relativePositionSource;
        private Point? _relativePositionSink;

        private IEdge<Point> _edgeTemplate;

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

        private DesignerItem SourceDesignerItem
        {
            get { return this._sourceDesignerItem; }
            set
            {
                if (_sourceDesignerItem != value)
                {
                    _sourceDesignerItem = value;
                }
            }
        }

        //#region override
        //protected override int VisualChildrenCount
        //{
        //    get
        //    {
        //        return this._visualChildren.Count;
        //    }
        //}

        //protected override Visual GetVisualChild(int index)
        //{
        //    return this._visualChildren[index];
        //}
        //#endregion
        #endregion
        #region ctor

        public NewConnectionAdorner(DesignerCanvas designer, DesignerItem sourceDesigner, Point? relativePositionSource, IEdge<Point> edgeTemplate)
            : base(designer)
        {
            this._edgeTemplate = edgeTemplate;
            this._designerCanvas = designer;
            this._sourceDesignerItem = sourceDesigner;
            this._relativePositionSource = relativePositionSource;

            //visuals
            ////canvas to draw
            //this._adornerCanvas = new Canvas();
            //this._visualChildren = new VisualCollection(this);
            //this._visualChildren.Add(_adornerCanvas);

            _drawingPen = new Pen(Brushes.LightSlateGray, 1);
            _drawingPen.LineJoin = PenLineJoin.Round;
            this.Cursor = Cursors.Cross;

            //base.Unloaded += new RoutedEventHandler(ConnectionAdorner_Unloaded);
        }

        #endregion
        #region impl
        #region mouse
        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (SourceDesignerItem != null && HitDesignerItem != null)
            {
                DesignerItem sourceDesignerItem = this.SourceDesignerItem;
                DesignerItem sinkDesignerItem = this.HitDesignerItem;

                //create connection
                IEdge<Point> edge = _designerCanvas.EdgeFactory.CreateEdge(_edgeTemplate, sourceDesignerItem.Vertex, sinkDesignerItem.Vertex);
                Connection con = _designerCanvas.EdgeFactory.CreateUIElement(edge, sourceDesignerItem, sinkDesignerItem);
                con.RelativePositionSource = this._relativePositionSource;
                con.RelativePositionSink = this._relativePositionSink;
                Canvas.SetZIndex(con, _designerCanvas.Children.Count);
                this._designerCanvas.AddConnection(con);
                
                //stop highlighting
                this.HitDesignerItem.IsDragConnectionOver = false;
            }

            if (this.IsMouseCaptured) this.ReleaseMouseCapture();

            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(this._designerCanvas as UIElement);
            if (adornerLayer != null)
            {
                adornerLayer.Remove(this);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (!this.IsMouseCaptured) this.CaptureMouse();
                DesignerItem hitDesignerItem;
                Point? relativePosition;
                PathFinder.HitTesting(this._designerCanvas, e.GetPosition(this._designerCanvas), this.SourceDesignerItem, out hitDesignerItem, out relativePosition);
                this.HitDesignerItem = hitDesignerItem;
                this._relativePositionSink = relativePosition;
                this._pathGeometry = GetPathGeometry(e.GetPosition(this));
                this.InvalidateVisual();
            }
            else
            {
                if (this.IsMouseCaptured) this.ReleaseMouseCapture();
            }
        }

        #endregion
        #region visual

        /// <summary>
        /// Draw the path when the adorner is rendered.
        /// </summary>
        /// <param name="dc"></param>
        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            dc.DrawGeometry(_drawingPen.Brush, _drawingPen, this._pathGeometry);

            // without a background the OnMouseMove event would not be fired
            // Alternative: implement a Canvas as a child of this adorner, like
            // the ConnectionAdorner does.
            dc.DrawRectangle(Brushes.Transparent, null, new Rect(RenderSize));
        }

        #endregion

        /// <summary>
        /// Get the path from the source to the current mouse position.
        /// </summary>
        /// <param name="targetPos">Target (current mouse) position</param>
        /// <returns></returns>
        private PathGeometry GetPathGeometry(Point targetPos)
        {
            PathGeometry result = new PathGeometry();

            //build source info
            ConnectionInfo sourceInfo = new ConnectionInfo(SourceDesignerItem);
            Point sourcePos = new Point(sourceInfo.DesignerItemLeft, sourceInfo.DesignerItemTop);
            sourceInfo.Position = this._relativePositionSource == null ? PathFinder.CalculateAttachPoint(sourceInfo.DesignerItemCenter, sourceInfo.DesignerItemSize, targetPos)
    : PathFinder.GetAbsolutePosition(sourceInfo.DesignerItemSize, sourcePos, (Point)_relativePositionSource);
            sourceInfo.Orientation = PathFinder.GetConnectorOrientation(sourcePos, sourceInfo.Position, sourceInfo.DesignerItemSize);

            //build sink info
            ConnectionInfo sinkInfo = new ConnectionInfo();
            sinkInfo.Cap = ArrowSymbol.None;
            sinkInfo.DesignerItemCenter = targetPos;
            sinkInfo.DesignerItemLeft = targetPos.X;
            sinkInfo.DesignerItemSize = new Size(0,0);
            sinkInfo.DesignerItemTop = targetPos.Y;
            sinkInfo.Position = targetPos;
            sinkInfo.Orientation = PathFinder.GetConnectorOrientation(sourceInfo.DesignerItemCenter, targetPos, sinkInfo.DesignerItemSize);

            result.Figures = PathFinder.GetConnectionPoints(sourceInfo, sinkInfo, PathRouting.Direct);

            return result;
        }
        
        #endregion
    }
}
