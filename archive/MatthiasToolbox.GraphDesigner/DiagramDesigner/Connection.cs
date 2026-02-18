using System;
using System.Windows.Documents;
using System.ComponentModel;
using MatthiasToolbox.GraphDesigner.Utilities;
using MatthiasToolbox.Presentation.Interfaces;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using MatthiasToolbox.GraphDesigner.Enumerations;
using System.Windows.Input;
using MatthiasToolbox.Basics.Datastructures.Graph;

namespace MatthiasToolbox.GraphDesigner.DiagramDesigner
{
    public class Connection : Control, ISelectable, INotifyPropertyChanged
    {
        #region cvar

        public Guid ID { get; set; }

        /// <summary>
        /// The ConnectionAdorner when the Connection is selected.
        /// </summary>
        private Adorner _connectionAdorner;

        /// <summary>
        /// The path the Connection uses.
        /// </summary>
        private PathGeometry _pathGeometry;
        private DoubleCollection _strokeDashArray;
        private Point _labelPosition;
        private bool _isSelected;

        #region anchors

        private Point _anchorPositionSource;
        private double _anchorAngleSource = 0;
        private Point _anchorPositionSink;
        private double _anchorAngleSink = 0;

        private ArrowSymbol sinkArrowSymbol = ArrowSymbol.None;

        #endregion
        #region line / path

        private double _lineThickness;
        private double _lineSelectionThickness;

        #endregion

        #endregion
        #region prop

        #region line

        /// <summary>
        /// Thickness of the normal unselected path.
        /// </summary>
        public double LineThickness
        {
            get
            {
                return _lineThickness;
            }
            set
            {
                _lineThickness = value;
                OnPropertyChanged("LineThickness");
            }
        }

        /// <summary>
        /// Thickness of the normal selected path.
        /// </summary>
        public double LineSelectionThickness
        {
            get
            {
                return _lineSelectionThickness;
            }
            set
            {
                _lineSelectionThickness = value;
                OnPropertyChanged("LineSelectionThickness");
            }
        }

        #endregion
        #region connection info

        /// <summary>
        /// Gets or sets the source ConnectionInfo. Use UpdateConnectionInfo to update the property.
        /// </summary>
        /// <value>The source info.</value>
        public ConnectionInfo SourceInfo { get; set; }
        /// <summary>
        /// Gets or sets the sink ConnectionInfo. Use UpdateConnectionInfo to update the property.
        /// </summary>
        /// <value>The sink info.</value>
        public ConnectionInfo SinkInfo { get; set; }

        #endregion
        #region relative position

        /// <summary>
        /// Gets or sets the relative position (relative to left and top, point values between 0 and 1) on the source.
        /// </summary>
        /// <value>The relative position at the source.</value>
        public Point? RelativePositionSource
        {
            get { return (Point?)GetValue(RelativePositionSourceProperty); }
            set { SetValue(RelativePositionSourceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for RelativePositionSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RelativePositionSourceProperty =
            DependencyProperty.Register("RelativePositionSource", typeof(Point?), typeof(Connection), new UIPropertyMetadata(null, new PropertyChangedCallback(RelativePosition_PropertyChanged)));

        /// <summary>
        /// Gets or sets the relative position (relative to left and top, point values between 0 and 1) on the sink.
        /// </summary>
        /// <value>The relative position at the sink.</value>
        public Point? RelativePositionSink
        {
            get { return (Point?)GetValue(RelativePositionSinkProperty); }
            set { SetValue(RelativePositionSinkProperty, value); }
        }

        // Using a DependencyProperty as the backing store for RelativePositionSink.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RelativePositionSinkProperty =
            DependencyProperty.Register("RelativePositionSink", typeof(Point?), typeof(Connection), new UIPropertyMetadata(null, new PropertyChangedCallback(RelativePosition_PropertyChanged)));

        /// <summary>
        /// Update the path when a relative position is changed.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void RelativePosition_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Connection connection = d as Connection;
            connection.UpdatePathGeometry();
        }

        #endregion
        #region designer items

        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register("Source", typeof(DesignerItem), typeof(Connection),
            new UIPropertyMetadata(null, new PropertyChangedCallback(SourcePropertyChanged)));

        /// <summary>
        /// Removes the Connection from the old value and adds the Connection to the new DesignerItem.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void SourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Connection connection = d as Connection;

            //remove listeners and connections from the old source
            DesignerItem oldSource = e.OldValue as DesignerItem;
            if (oldSource != null)
            {
                oldSource.RemoveConnection(connection);

                //remove size and position events
                connection.RemovePositionListener(oldSource);
            }

            //add connection to new source and add listeners
            DesignerItem newSource = e.NewValue as DesignerItem;
            if (newSource != null)
            {
                newSource.AddConnection(connection);

                //add size and position events
                connection.AddPositionListener(newSource);
                if(connection.Edge != null)
                    connection.Edge.Vertex1 = newSource.Vertex;
            }
            else
            {
                //remove the attach point information
                connection.RelativePositionSource = null;
                if (connection.Edge != null)
                    connection.Edge.Vertex1 = null;
            }
                        
            connection.UpdatePathGeometry();
        }

        public DesignerItem Source
        {
            get { return (DesignerItem)GetValue(SourceProperty); }
            internal set { SetValue(SourceProperty, value); }
        }

        public static readonly DependencyProperty SinkProperty = DependencyProperty.Register("Sink", typeof(DesignerItem), typeof(Connection),
            new UIPropertyMetadata(null, new PropertyChangedCallback(SinkPropertychanged)));

        /// <summary>
        ///  Removes the Connection from the old value and adds the Connection to the new DesignerItem.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void SinkPropertychanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Connection connection = d as Connection;
            DesignerItem newSink = e.NewValue as DesignerItem;
            DesignerItem oldSink = e.OldValue as DesignerItem;

            if (oldSink != null)
            {
                oldSink.RemoveConnection(connection);

                //remove size and position events
                connection.RemovePositionListener(oldSink);
            }

            if (newSink != null)
            {
                newSink.AddConnection(connection);

                //add size and position events
                connection.AddPositionListener(newSink);
                if (connection.Edge != null)
                    connection.Edge.Vertex2 = newSink.Vertex;
            }
            else
            {
                //remove the attach point information
                connection.RelativePositionSink = null;
                if (connection.Edge != null)
                    connection.Edge.Vertex2 = null;
            }
            connection.UpdatePathGeometry();
        }

        public DesignerItem Sink
        {
            get { return (DesignerItem)GetValue(SinkProperty); }
            internal set { SetValue(SinkProperty, value); }
        }

        #endregion
        #region route

        public static readonly DependencyProperty RoutePointsProperty = DependencyProperty.Register("RoutePoints", typeof(Point[]), typeof(Connection),
            new UIPropertyMetadata(null));

        public Point[] RoutePoints
        {
            get { return (Point[])GetValue(RoutePointsProperty); }
            set { SetValue(RoutePointsProperty, value); }
        }

        /// <summary>
        /// connection path geometry
        /// </summary>
        public PathGeometry PathGeometry
        {
            get { return _pathGeometry; }
            set
            {
                if (_pathGeometry != value)
                {
                    _pathGeometry = value;
                    UpdateAnchorPosition();
                    OnPropertyChanged("PathGeometry");
                }
            }
        }

        /// <summary>
        /// The routing algorithm that was used to create the path.
        /// </summary>
        public PathRouting Routing
        {
            get { return (PathRouting)GetValue(RoutingProperty); }
            set { SetValue(RoutingProperty, value); }
        }

        /// <summary>
        /// The routing algorithm that was used to create the path.
        /// </summary>
        public static readonly DependencyProperty RoutingProperty =
            DependencyProperty.Register("Routing", typeof(PathRouting), typeof(Connection), new UIPropertyMetadata(PathRouting.Direct));

        #endregion
        #region edge

        /// <summary>
        /// Gets or sets the edge the connection is displaying.
        /// </summary>
        /// <value>The edge.</value>
        public IEdge<Point> Edge
        {
            get { return (IEdge<Point>)GetValue(EdgeProperty); }
            set
            {
                SetValue(EdgeProperty, value);
                OnPropertyChanged("Edge");
            }
        }

        public static readonly DependencyProperty EdgeProperty =
            DependencyProperty.Register("Edge", typeof(IEdge<Point>), typeof(Connection), new PropertyMetadata(null));

        #endregion
        #region Anchors

        /// <summary>
        /// between source connector position and the beginning 
        /// of the path geometry we leave some space for visual reasons; 
        /// so the anchor position source really marks the beginning 
        /// of the path geometry on the source side
        /// </summary>
        public Point AnchorPositionSource
        {
            get { return _anchorPositionSource; }
            set
            {
                if (_anchorPositionSource != value)
                {
                    _anchorPositionSource = value;
                    OnPropertyChanged("AnchorPositionSource");
                }
            }
        }

        /// <summary>
        /// slope of the path at the anchor position
        /// needed for the rotation angle of the arrow
        /// </summary>
        public double AnchorAngleSource
        {
            get { return _anchorAngleSource; }
            set
            {
                if (_anchorAngleSource != value)
                {
                    _anchorAngleSource = value;
                    OnPropertyChanged("AnchorAngleSource");
                }
            }
        }


        /// <summary>
        /// analogue to source side
        /// </summary>
        public Point AnchorPositionSink
        {
            get { return _anchorPositionSink; }
            set
            {
                if (_anchorPositionSink != value)
                {
                    _anchorPositionSink = value;
                    OnPropertyChanged("AnchorPositionSink");
                }
            }
        }


        /// <summary>
        /// analogue to source side
        /// </summary>
        public double AnchorAngleSink
        {
            get { return _anchorAngleSink; }
            set
            {
                if (_anchorAngleSink != value)
                {
                    _anchorAngleSink = value;
                    OnPropertyChanged("AnchorAngleSink");
                }
            }
        }

        private ArrowSymbol _sourceArrowSymbol = ArrowSymbol.None;
        public ArrowSymbol SourceArrowSymbol
        {
            get { return _sourceArrowSymbol; }
            set
            {
                if (_sourceArrowSymbol != value)
                {
                    _sourceArrowSymbol = value;
                    OnPropertyChanged("SourceArrowSymbol");
                }
            }
        }

        ///<summary>
        ///</summary>
        public ArrowSymbol SinkArrowSymbol
        {
            get { return sinkArrowSymbol; }
            set
            {
                if (sinkArrowSymbol != value)
                {
                    sinkArrowSymbol = value;
                    OnPropertyChanged("SinkArrowSymbol");
                }
            }
        }

        #endregion

        /// <summary>
        /// specifies a point at half path length
        /// </summary>
        public Point LabelPosition
        {
            get { return _labelPosition; }
            set
            {
                if (_labelPosition != value)
                {
                    _labelPosition = value;
                    OnPropertyChanged("LabelPosition");
                }
            }
        }

        /// <summary>
        /// pattern of dashes and gaps that is used to outline the connection path
        /// </summary>
        public DoubleCollection StrokeDashArray
        {
            get
            {
                return _strokeDashArray;
            }
            set
            {
                if (_strokeDashArray != value)
                {
                    _strokeDashArray = value;
                    OnPropertyChanged("StrokeDashArray");
                }
            }
        }

        /// <summary>
        /// if connected, the ConnectionAdorner becomes visible
        /// </summary>
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged("IsSelected");
                    if (_isSelected)
                        ShowAdorner();
                    else
                        HideAdorner();
                }
            }
        }

        #endregion
        #region ctor

        public Connection()
        {
            this.ID = Guid.NewGuid();
            base.Unloaded += new RoutedEventHandler(Connection_Unloaded);
        }

        public Connection(DesignerItem source, DesignerItem sink)
            : this()
        {
            this.Source = source;
            this.Sink = sink;

            UpdatePathGeometry();
        }

        #endregion
        #region impl

        #region DesignerItem Changed Events

        /// <summary>
        /// Add position and size changed event listeners on the designer item.
        /// </summary>
        /// <param name="designerItem">The designer item.</param>
        private void AddPositionListener(DesignerItem designerItem)
        {
            DependencyPropertyDescriptor topProperty = DependencyPropertyDescriptor.FromProperty(Canvas.TopProperty, typeof(DesignerItem));
            topProperty.AddValueChanged(designerItem, new EventHandler(this.OnDesignerChanged));

            DependencyPropertyDescriptor leftProperty = DependencyPropertyDescriptor.FromProperty(Canvas.LeftProperty, typeof(DesignerItem));
            leftProperty.AddValueChanged(designerItem, new EventHandler(this.OnDesignerChanged));

            designerItem.SizeChanged += new SizeChangedEventHandler(this.OnDesignerChanged);
        }

        /// <summary>
        /// Removes position and size changed event listeners on the designer item.
        /// </summary>
        /// <param name="designerItem">The designer item.</param>
        private void RemovePositionListener(DesignerItem designerItem)
        {
            DependencyPropertyDescriptor topProperty = DependencyPropertyDescriptor.FromProperty(Canvas.TopProperty, typeof(DesignerItem));
            topProperty.RemoveValueChanged(designerItem, new EventHandler(this.OnDesignerChanged));

            DependencyPropertyDescriptor leftProperty = DependencyPropertyDescriptor.FromProperty(Canvas.LeftProperty, typeof(DesignerItem));
            leftProperty.RemoveValueChanged(designerItem, new EventHandler(this.OnDesignerChanged));

            designerItem.SizeChanged -= new SizeChangedEventHandler(this.OnDesignerChanged);
        }

        private void OnDesignerChanged(object sender, EventArgs e)
        {
            this.UpdatePathGeometry();
        }

        #endregion
        #region mouse events
        protected override void OnMouseDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            // usual selection business
            DesignerCanvas designer = VisualTreeHelper.GetParent(this) as DesignerCanvas;
            if (designer != null)
            {
                if ((Keyboard.Modifiers & (ModifierKeys.Shift | ModifierKeys.Control)) != ModifierKeys.None)
                    if (this.IsSelected)
                    {
                        designer.SelectionService.RemoveFromSelection(this);
                    }
                    else
                    {
                        designer.SelectionService.AddToSelection(this);
                    }
                else if (!this.IsSelected)
                {
                    designer.SelectionService.SelectItem(this);
                }

                Focus();
            }
            e.Handled = false;
        }

        #endregion
        #region PathGeometry update

        /// <summary>
        /// Updates the path geometry. Calculates the path for the different routing algorithm to the designer item.
        /// </summary>
        public void UpdatePathGeometry()
        {
            if ((Source == null || Sink == null))
                return;

            UpdateConnectionInfo();

            PathGeometry geometry = new PathGeometry();
            geometry.Figures = PathFinder.GetConnectionPoints(SourceInfo, SinkInfo, Routing);
            
            this.PathGeometry = geometry;
        }

        private void UpdateAnchorPosition()
        {
            Point pathStartPoint, pathTangentAtStartPoint;
            Point pathEndPoint, pathTangentAtEndPoint;
            Point pathMidPoint, pathTangentAtMidPoint;

            if (this.PathGeometry.Bounds.Width == 0 && this.PathGeometry.Bounds.Height == 0)
                return;

            // the PathGeometry.GetPointAtFractionLength method gets the point and a tangent vector 
            // on PathGeometry at the specified fraction of its length
            this.PathGeometry.GetPointAtFractionLength(0, out pathStartPoint, out pathTangentAtStartPoint);
            this.PathGeometry.GetPointAtFractionLength(1, out pathEndPoint, out pathTangentAtEndPoint);
            this.PathGeometry.GetPointAtFractionLength(0.5, out pathMidPoint, out pathTangentAtMidPoint);

            // get angle from tangent vector
            this.AnchorAngleSource = Math.Atan2(-pathTangentAtStartPoint.Y, -pathTangentAtStartPoint.X) * (180 / Math.PI);
            this.AnchorAngleSink = Math.Atan2(pathTangentAtEndPoint.Y, pathTangentAtEndPoint.X) * (180 / Math.PI);


            // add some margin on source and sink side for visual reasons only
            pathStartPoint.Offset(-pathTangentAtStartPoint.X * 5, -pathTangentAtStartPoint.Y * 5);
            pathEndPoint.Offset(pathTangentAtEndPoint.X * 5, pathTangentAtEndPoint.Y * 5);

            this.AnchorPositionSource = pathStartPoint;
            this.AnchorPositionSink = pathEndPoint;
            this.LabelPosition = pathMidPoint;
        }

        #endregion
        #region ConnectionInfo

        /// <summary>
        /// Update the SourceInfo and SinkInfo properties. Updating the connection info requires both source and sink, to calculate the attach point if the 
        /// relative position is not set.
        /// </summary>
        public virtual void UpdateConnectionInfo()
        {
            ConnectionInfo ciSource = new ConnectionInfo(Source);
            ConnectionInfo ciSink = new ConnectionInfo(Sink);

            ciSource.Position = RelativePositionSource == null ? PathFinder.CalculateAttachPoint(ciSource.DesignerItemCenter, ciSource.DesignerItemSize, ciSink.DesignerItemCenter)
                : PathFinder.GetAbsolutePosition(ciSource.DesignerItemSize, new Point(ciSource.DesignerItemLeft, ciSource.DesignerItemTop), (Point)RelativePositionSource);
            ciSource.Orientation = PathFinder.GetConnectorOrientation(new Point(ciSource.DesignerItemLeft, ciSource.DesignerItemTop), ciSource.Position ,ciSource.DesignerItemSize);
            ciSource.Cap = SourceArrowSymbol;

            ciSink.Position = RelativePositionSink == null ? PathFinder.CalculateAttachPoint(ciSink.DesignerItemCenter, ciSink.DesignerItemSize, ciSource.DesignerItemCenter)
                : PathFinder.GetAbsolutePosition(ciSink.DesignerItemSize, new Point(ciSink.DesignerItemLeft, ciSink.DesignerItemTop), (Point)RelativePositionSink);
            ciSink.Orientation = PathFinder.GetConnectorOrientation(new Point(ciSink.DesignerItemLeft, ciSink.DesignerItemTop), ciSink.Position, ciSink.DesignerItemSize);
            ciSink.Cap = SinkArrowSymbol;

            //after setting all data update the the properties
            this.SourceInfo = ciSource;
            this.SinkInfo = ciSink;
        }

        #endregion
        #region adorner

        private void ShowAdorner()
        {
            // the ConnectionAdorner is created once for each Connection
            if (this._connectionAdorner == null)
            {
                DesignerCanvas designer = VisualTreeHelper.GetParent(this) as DesignerCanvas;

                AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(this);
                if (adornerLayer != null)
                {
                    this._connectionAdorner = new ConnectionAdorner(designer, this);
                    adornerLayer.Add(this._connectionAdorner);
                }
            }
            this._connectionAdorner.Visibility = Visibility.Visible;
        }

        internal void HideAdorner()
        {
            if (this._connectionAdorner != null)
                this._connectionAdorner.Visibility = Visibility.Collapsed;
        }

        #endregion
        #region destructor

        /// <summary>
        /// Do some housekeeping when Connection is unloaded.
        /// Remove event handlers and adorner.
        /// </summary>
        /// <param name="sender">Can be null.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/>Can be null.</param>
        private void Connection_Unloaded(object sender, RoutedEventArgs e)
        {
            // remove event handler
            this.RelativePositionSource = null;
            this.RelativePositionSink = null;

            this.Source = null;
            this.Sink = null;

            // remove adorner);
            if (this._connectionAdorner != null)
            {
                //DesignerCanvas designer = VisualTreeHelper.GetParent(this) as DesignerCanvas;

                AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(this);
                if (adornerLayer != null)
                {
                    adornerLayer.Remove(this._connectionAdorner);
                    this._connectionAdorner = null;
                }
            }
        }

        #endregion

        #endregion
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        #endregion
    }
}