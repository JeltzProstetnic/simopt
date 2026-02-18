using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MatthiasToolbox.Basics.Datastructures.Graph;
using MatthiasToolbox.GraphDesigner.Controls;
using MatthiasToolbox.Utilities.Serialization.GraphML;

using MatthiasToolbox.GraphDesigner.Enumerations;
using MatthiasToolbox.GraphDesigner.DiagramDesigner;
using MatthiasToolbox.GraphDesigner.MouseHandlers;
using MatthiasToolbox.GraphDesigner.Interfaces;
using MatthiasToolbox.GraphDesigner.Utilities;
using MatthiasToolbox.GraphDesigner.Events;

using MatthiasToolbox.Presentation.Controls;
using MatthiasToolbox.Presentation.Events;
using MatthiasToolbox.Presentation.Utilities;

namespace MatthiasToolbox.GraphDesigner
{
    /// <summary>
    /// Interaktionslogik für GraphControl.xaml
    /// </summary>
    public partial class GraphControl : UserControl
    {
        #region cvar

        private readonly Dictionary<IVertex<Point>, DesignerItem> _vertices = new Dictionary<IVertex<Point>, DesignerItem>();
        private readonly Dictionary<IEdge<Point>, Connection> _edges = new Dictionary<IEdge<Point>, Connection>();

        #region commands

        public static RoutedCommand SetDefaultMouseMode = new RoutedCommand();
        public static RoutedCommand SetRectangleSelectionMode = new RoutedCommand();

        public static RoutedCommand ShowOptions = new RoutedCommand();

        #endregion
        #region viewport

        private double _width = 1000;
        private double _height = 1000;

        #endregion
        #region mouse

        private readonly MouseManager _mouseManager;
        private DateTime _mouseDownTime; // TODO  GraphDesigner - move to mouse manager?

        #endregion
        #region zoom rectangle

        /// <summary>
        /// Saves the previous zoom rectangle, pressing the backspace key jumps back to this zoom rectangle.
        /// </summary>
        private Rect _prevZoomRect;

        /// <summary>
        /// Save the previous content scale, pressing the backspace key jumps back to this scale.
        /// </summary>
        private double _prevZoomScale;

        /// <summary>
        /// Set to 'true' when the previous zoom rect is saved.
        /// </summary>
        private bool _prevZoomRectSet = false;

        #endregion
        #region constants

        private const double BORDER_X = 50;
        private const double BORDER_Y = 50;

        #endregion

        #endregion
        #region evnt

        public delegate void ShowOptionsHandler(object sender, OptionsDialogEventArgs e);

        public delegate void PaletteOptionsHandler(object sender, PaletteOptionsEventArgs options);

        public event ShowOptionsHandler OptionsDialogExecuted;

        /// <summary>
        /// A custom event
        /// </summary>
        public event MouseEventHandler RectangleZoomApplied;

        /// <summary>
        /// A custom click event
        /// </summary>
        public event MouseEventHandler Click;

        #region vertex

        public delegate void VertexCreationHandler(object sender, VertexEventArgs e);
        public event VertexCreationHandler VertexCreated;

        public delegate void VertexRemovedHandler(object sender, VertexEventArgs e);
        public event VertexRemovedHandler VertexRemoved;

        public delegate void VertexChangedHandler(object sender, VertexEventArgs e);
        public event VertexChangedHandler VertexChanged;

        #endregion
        #region edge

        public delegate void EdgeCreationHandler(object sender, EdgeEventArgs e);
        public event EdgeCreationHandler EdgeCreated;

        public delegate void EdgeRemovedHandler(object sender, EdgeEventArgs e);
        public event EdgeRemovedHandler EdgeRemoved;

        public delegate void EdgeChangedHandler(object sender, EdgeEventArgs e);
        public event EdgeChangedHandler EdgeChanged;
        
        #endregion

        #region shadowed

        // TODO  GraphDesigner - check with daniel if this is necessary
        // overriding mouse events
        public new event MouseButtonEventHandler MouseUp;
        public new event MouseButtonEventHandler MouseDown;
        public new event MouseEventHandler MouseMove;

        #endregion
        
        #endregion
        #region prop

        public IGraph<Point> Graph { get; set; }

        /// <summary>
        /// All DesignerItems on the DesignerCanvas
        /// </summary>
        public IEnumerable<DesignerItem> DesignerItems { get { return Designer.Children.OfType<DesignerItem>(); } }

        public IEnumerable<Connection> Connections { get { return Designer.Children.OfType<Connection>(); } }

        #region wrapped controls

        /// <summary>
        /// ZoomPanControl support for zooming and paning. Parent Panel of the DesignerCanvas.
        /// </summary>
        public ZoomPanControl Panel { get { return zoomPanControl; } }

        /// <summary>
        /// The canvas on which the actual rendering happens
        /// </summary>
        public DesignerCanvas Designer { get { return canvasModel; } }

        public ScrollViewer Scroller { get { return scroller; } }

        #endregion
        #region factories

        public INodeFactory NodeFactory
        {
            get
            {
                return Designer.NodeFactory;
            }
            set
            {
                Designer.NodeFactory = value;
            }
        }

        public IEdgeFactory EdgeFactory
        {
            get
            {
                return Designer.EdgeFactory;
            }
            set
            {
                Designer.EdgeFactory = value;
            }
        }

        #endregion

        #endregion
        #region ctor

        /// <summary>
        /// Default constructor assigns all mouse events and handlers.
        /// </summary>
        public GraphControl()
        {
            // init custom mouse handling
            this._mouseManager = new MouseManager(this, new NormalCursor(this));
            this._mouseManager.HandlerChanged += new MouseManager.MouseManagerChangedHandler(MouseHandlerChanged);
            
            this.Loaded += new RoutedEventHandler(GraphControl_Loaded);

            InitializeComponent();

            //add event handlers
            MouseDown += new MouseButtonEventHandler(GraphControl_MouseDown);
            MouseMove += new MouseEventHandler(GraphControl_MouseMove);
            MouseUp += new MouseButtonEventHandler(GraphControl_MouseUp);
            RectangleZoomApplied += new MouseEventHandler(GraphControl_RectangleZoomApplied);
            AddHandler(ResizeThumb.ResizeCompletedEvent, (RoutedEventHandler)DesignerItem_ResizeCompleted);

            this.CommandBindings.Add(new CommandBinding(GraphControl.SetDefaultMouseMode, SetDefaultMouseMode_Executed));
            this.CommandBindings.Add(new CommandBinding(GraphControl.SetRectangleSelectionMode, SetRectangleSelectionMode_Executed));
            this.CommandBindings.Add(new CommandBinding(GraphControl.ShowOptions, ShowOptions_Executed, ShowOptions_Enabled));
        }

        private void DesignerItem_ResizeCompleted(object sender, RoutedEventArgs e)
        {
            DesignerItem item = e.OriginalSource as DesignerItem;
            if (item == null)
                return;
            OnVertexChanged(new VertexEventArgs() { Vertex = item.Vertex });
            e.Handled = true;
        }

        #endregion
        #region hand

        #region ZoomPanControl

        /// <summary>
        /// Event raised when the user has double clicked in the zoom and pan control.
        /// </summary>
        private void zoomPanControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            this._mouseManager.MouseDoubleClick(sender, e);
        }

        /// <summary>
        /// Event raised on mouse down in the ZoomAndPanControl.
        /// </summary>
        private void zoomPanControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _mouseDownTime = DateTime.Now;
            if (MouseDown != null) MouseDown.Invoke(this, e);
        }

        /// <summary>
        /// Event raised on mouse up in the ZoomAndPanControl.
        /// </summary>
        private void zoomPanControl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (MouseUp != null) MouseUp.Invoke(this, e);
            if (DateTime.Now.Subtract(_mouseDownTime) < new TimeSpan(0, 0, 0, 0, 500))
                if (Click != null) Click.Invoke(this, new MouseEventArgs(e.MouseDevice, e.Timestamp));
        }

        /// <summary>
        /// Event raised on mouse move in the ZoomAndPanControl.
        /// </summary>
        private void zoomPanControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (MouseMove != null) MouseMove.Invoke(this, e);
        }

        /// <summary>
        /// Event raised by rotating the mouse wheel
        /// </summary>
        private void zoomPanControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;

            if (e.Delta > 0)
            {
                Point curContentMousePoint = e.GetPosition(Designer);
                ZoomIn(curContentMousePoint);
            }
            else if (e.Delta < 0)
            {
                Point curContentMousePoint = e.GetPosition(Designer);
                ZoomOut(curContentMousePoint);
            }
        }

        #endregion
        #region Model Canvas

        private void canvasModel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _mouseDownTime = DateTime.Now;
            if (MouseDown != null) MouseDown.Invoke(this, e);
        }

        private void canvasModel_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (MouseUp != null) MouseUp.Invoke(this, e);
            if (DateTime.Now.Subtract(_mouseDownTime) < new TimeSpan(0, 0, 0, 0, 500))
                if (Click != null) Click.Invoke(this, new MouseEventArgs(e.MouseDevice, e.Timestamp));
        }

        private void canvasModel_MouseMove(object sender, MouseEventArgs e)
        {
            if (MouseMove != null) MouseMove.Invoke(this, e);
        }

        private void canvasModel_DesignerItemCreated(object sender, DesignerItemEventArgs e)
        {
            if (e.Item.Vertex != null)
            {
                OnVertexCreated(new VertexEventArgs() { Vertex = e.Item.Vertex });
            }
        }

        private void canvasModel_EdgeCreated(object sender, EdgeEventArgs e)
        {
            if (e.Edge != null)
            {
                if (Graph != null)
                {
                    Graph.AddEdge(e.Edge);
                }
                OnEdgeCreated(e);
            }
        }

        private void canvasModel_EdgeRemoved(object sender, EdgeEventArgs e)
        {
            if(e.Edge != null)
            {
                Graph.RemoveEdge(e.Edge);
            }
            OnEdgeRemoved(e);
        }

        private void canvasModel_DesignerItemRemoved(object sender, DesignerItemEventArgs e)
        {
            if(e.Item.Vertex != null)
            {
                Graph.RemoveVertex(e.Item.Vertex);
            }
            OnVertexRemoved(new VertexEventArgs() { Vertex = e.Item.Vertex });
        }

        #endregion
        #region commands

        private void SetDefaultMouseMode_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SetNormalState();
        }

        private void SetRectangleSelectionMode_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SetGraphState(MouseUsageStates.SelectionRectangle, false);
        }

        #endregion
        #region this

        private void GraphControl_RectangleZoomApplied(object sender, MouseEventArgs e)
        {
            SetNormalState();
        }

        #endregion

        #endregion
        #region impl

        void GraphControl_Loaded(object sender, RoutedEventArgs e)
        {
            double width = zoomPanControl.ViewportWidth;
            double height = zoomPanControl.ViewportHeight;
            double x = (Designer.ActualWidth - width) / 2;
            double y = (Designer.ActualHeight - height) / 2;
           zoomPanControl.ZoomTo(new Rect(x,y, width, height));
        }

        #region event invokers

        protected internal virtual void OnAfterRectangleZoom(MouseEventArgs e)
        {
            if (RectangleZoomApplied != null) RectangleZoomApplied.Invoke(this, e);
        }

        #region edge and vertex

        protected void OnVertexCreated(VertexEventArgs e)
        {
            if (this.VertexCreated != null) this.VertexCreated.Invoke(this, e);
        }

        protected void OnVertexRemoved(VertexEventArgs e)
        {
            if (this.VertexRemoved != null) this.VertexRemoved.Invoke(this, e);
        }

        internal void OnVertexChanged(VertexEventArgs e)
        {
            if (this.VertexChanged != null) this.VertexChanged.Invoke(this, e);
        }

        protected void OnEdgeCreated(EdgeEventArgs e)
        {
            if (this.EdgeCreated != null) this.EdgeCreated.Invoke(this, e);
        }

        protected void OnEdgeRemoved(EdgeEventArgs e)
        {
            if (this.EdgeRemoved != null) this.EdgeRemoved.Invoke(this, e);
        }

        internal void OnEdgeChanged(EdgeEventArgs e)
        {
            if(this.EdgeChanged != null) this.EdgeChanged.Invoke(this, e);
        }

        #endregion
        #endregion
        #region zoom

        #region public

        public void ZoomFill()
        {
            SavePrevZoomRect();
            zoomPanControl.AnimatedScaleToFit();
        }

        public void ResetZoom()
        {
            SavePrevZoomRect();
            zoomPanControl.AnimatedZoomTo(1.0);
        }

        public void ZoomIn()
        {
            ZoomIn(new Point(zoomPanControl.ContentZoomFocusX, zoomPanControl.ContentZoomFocusY));
        }

        public void ZoomOut()
        {
            ZoomOut(new Point(zoomPanControl.ContentZoomFocusX, zoomPanControl.ContentZoomFocusY));
        }

        #endregion
        #region internal

        /// <summary>
        /// Zoom the viewport out, centering on the specified point (in content coordinates).
        /// </summary>
        internal void ZoomOut(Point contentZoomCenter)
        {
            zoomPanControl.ZoomAboutPoint(zoomPanControl.ContentScale - 0.1, contentZoomCenter);
        }

        /// <summary>
        /// Zoom the viewport in, centering on the specified point (in content coordinates).
        /// </summary>
        internal void ZoomIn(Point contentZoomCenter)
        {
            zoomPanControl.ZoomAboutPoint(zoomPanControl.ContentScale + 0.1, contentZoomCenter);
        }

        /// <summary>
        /// When the user has finished dragging out the rectangle the zoom operation is applied.
        /// </summary>
        internal void ApplyDragZoomRect()
        {
            // Record the previous zoom level, so that we can jump back to it when the backspace key is pressed.
            SavePrevZoomRect();

            // Retreive the rectangle that the user draggged out and zoom in on it.
            double contentX = Canvas.GetLeft(dragZoomBorder);
            double contentY = Canvas.GetTop(dragZoomBorder);
            double contentWidth = dragZoomBorder.Width;
            double contentHeight = dragZoomBorder.Height;
            zoomPanControl.AnimatedZoomTo(new Rect(contentX, contentY, contentWidth, contentHeight));

            FadeOutDragRect();
        }

        #endregion
        #region private

        /// <summary>
        /// The 'ZoomIn' command (bound to the plus key) was executed.
        /// </summary>
        private void ZoomIn_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ZoomIn();
        }

        /// <summary>
        /// The 'ZoomOut' command (bound to the minus key) was executed.
        /// </summary>
        private void ZoomOut_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ZoomOut();
        }

        /// <summary>
        /// The 'JumpBackToPrevZoom' command was executed.
        /// </summary>
        private void JumpBackToPrevZoom_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            JumpBackToPrevZoom();
        }

        /// <summary>
        /// Determines whether the 'JumpBackToPrevZoom' command can be executed.
        /// </summary>
        private void JumpBackToPrevZoom_CanExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _prevZoomRectSet;
        }

        /// <summary>
        /// The 'Fill' command was executed.
        /// </summary>
        private void Fill_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ZoomFill();
        }

        /// <summary>
        /// The 'OneHundredPercent' command was executed.
        /// </summary>
        private void OneHundredPercent_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ResetZoom();
        }

        /// <summary>
        /// Clear the memory of the previous zoom level.
        /// </summary>
        private void ClearPrevZoomRect()
        {
            _prevZoomRectSet = false;
        }

        /// <summary>
        /// Record the previous zoom level, so that we can jump back to it when the backspace key is pressed.
        /// </summary>
        private void SavePrevZoomRect()
        {
            _prevZoomRect = new Rect(zoomPanControl.ContentOffsetX, zoomPanControl.ContentOffsetY, zoomPanControl.ContentViewportWidth, zoomPanControl.ContentViewportHeight);
            _prevZoomScale = zoomPanControl.ContentScale;
            _prevZoomRectSet = true;
        }

        /// <summary>
        /// Jump back to the previous zoom level.
        /// </summary>
        private void JumpBackToPrevZoom()
        {
            zoomPanControl.AnimatedZoomTo(_prevZoomScale, _prevZoomRect);
            ClearPrevZoomRect();
        }

        #endregion

        #endregion
        #region mouse commands

        void GraphControl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            this._mouseManager.MouseUp(sender, e);
        }

        void GraphControl_MouseMove(object sender, MouseEventArgs e)
        {
            this._mouseManager.MouseMove(sender, e);
        }

        void GraphControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this._mouseManager.MouseDown(sender, e);
        }

        #endregion

        #region load graphML / clear

        public void Load()
        {
            _vertices.Clear();
            _edges.Clear();
            Designer.Children.Clear();

            foreach (IVertex<Point> vertex in Graph.Vertices)
            {
                DesignerItem item = Designer.Add(NodeFactory.CreateUIElement(vertex, vertex), vertex);
                _vertices[vertex] = item;
            }

            //IMPORTANT! Displays the Nodes to add the connections on the right position
            //update Connectors only possible after the canvas was measured!!!!
            InvalidateVisual();
            Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            foreach (IEdge<Point> edge in Graph.Edges)
            {
                if (_vertices.ContainsKey(edge.Vertex1) && _vertices.ContainsKey(edge.Vertex2))
                {
                    _edges[edge] = Designer.AddConnection(
                        EdgeFactory.CreateUIElement(edge, _vertices[edge.Vertex1], _vertices[edge.Vertex2]),
                        doThrowEvent: false);
                }
            }
        }

        public void LoadFromGraphML(string file)
        {
            MatthiasToolbox.Utilities.Serialization.GraphML.Container net = Serializer.LoadFromFile(file);
            this.Graph = net.Graph;
            Load();
        }

        public void Clear()
        {
            Designer.Children.Clear();

            _width = 1000;
            _height = 1000;
            Designer.Width = _width + 2 * BORDER_X;
            Designer.Height = _height + 2 * BORDER_Y;
        }

        #endregion
        #region mouse manager

        /// <summary>
        /// If the previous event was a shape toolbox event, the selected toolbox item has to be deselected.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="MatthiasToolbox.Presentation.Events.MouseManagerEventArgs"/> instance containing the event data.</param>
        void MouseHandlerChanged(object sender, MouseManagerEventArgs e)
        {
            if(e.OldHandler is ShapeCursor && !(e.NewHandler is ShapeCursor))
            {
                // TODO  OntoEdit - clear selection in all toolboxes?
                // this.TermToolbox.ClearSelection();
            }
        }

        /// <summary>
        /// Handles the AddMultipleItemStart event of the Toolbox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="MatthiasToolbox.Presentation.Events.ToolboxEventArgs"/> instance containing the event data.</param>
        public void Toolbox_AddMultipleItemStart(object sender, RoutedEventArgs e)
        {
            ToolboxEventArgs toolboxEvent = (ToolboxEventArgs) e;
            if (toolboxEvent.SelectedElement is IVertex<Point>)
                this._mouseManager.SetEventHandler(new ShapeCursor(this, toolboxEvent.SelectedElement as IVertex<Point>), false);
            else if (toolboxEvent.SelectedElement is IEdge<Point>)
                this._mouseManager.SetEventHandler(new ConnectionCursor(this, toolboxEvent.SelectedElement as IEdge<Point>), true);
        }

        /// <summary>
        /// Handles the AddMultipleItemEnd event of the Toolbox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        public void Toolbox_AddMultipleItemEnd(object sender, RoutedEventArgs e)
        {
            this._mouseManager.SetEventHandler(new NormalCursor(this), false);
        }

        /// <summary>
        /// Handles the AddSingleItem event of the Toolbox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="toolboxEvent">The <see cref="MatthiasToolbox.Presentation.Events.ToolboxEventArgs"/> instance containing the event data.</param>
        public void Toolbox_AddSingleItem(object sender, RoutedEventArgs e)
        {
            ToolboxEventArgs toolboxEvent = (ToolboxEventArgs)e;
            if (toolboxEvent.SelectedElement is IVertex<Point>)
                this._mouseManager.SetEventHandler(new ShapeCursor(this, toolboxEvent.SelectedElement as IVertex<Point>), true);
            else if (toolboxEvent.SelectedElement is IEdge<Point>)
                this._mouseManager.SetEventHandler(new ConnectionCursor(this, toolboxEvent.SelectedElement as IEdge<Point>), true);
        }

        /// <summary>
        /// Sets the state of the graph.
        /// </summary>
        /// <param name="graphControlStates">The graph control states.</param>
        /// <param name="isSingleEvent">if set to <c>true</c> just the next mouse down, drag and up events will will be catched,
        /// instead of handling all events with the same handlers.</param>
        public void SetGraphState(MouseUsageStates graphControlStates, bool isSingleEvent)
        {
            IMouseHandler graphCursor;
            switch (graphControlStates)
            {
                case MouseUsageStates.Normal:
                    this.Cursor = Cursors.Arrow;
                    graphCursor = new NormalCursor(this);
                    break;
                case MouseUsageStates.SelectionRectangle:
                    this.Cursor = Cursors.Cross;
                    graphCursor = new MultiSelectionCursor(this);
                    break;
                case MouseUsageStates.ZoomRectangle:
                    this.Cursor = Cursors.Cross;
                    graphCursor = new ZoomCursor(this);
                    break;
                default:
                    this.Cursor = Cursors.Arrow;
                    graphCursor = new NormalCursor(this);
                    break;
            }

            //set the event handler
            this._mouseManager.SetEventHandler(graphCursor, isSingleEvent);
        }

        #endregion
        #region drag rectangle

        /// <summary>
        /// Inits the drag rectangle displayed on top of the designer.
        /// </summary>
        /// <param name="pt1">The PT1.</param>
        /// <param name="pt2">The PT2.</param>
        public void InitDragRect(Point pt1, Point pt2)
        {
            SetDragRect(pt1, pt2);

            dragZoomCanvas.Visibility = Visibility.Visible;
            dragZoomBorder.Opacity = 0.5;
        }

        /// <summary>
        /// Fade out the drag zoom rectangle.
        /// </summary>
        internal void FadeOutDragRect()
        {
            AnimationHelper.StartAnimation(dragZoomBorder, Border.OpacityProperty, 0.0, 0.1,
                delegate(object sender, EventArgs e)
                {
                    dragZoomCanvas.Visibility = Visibility.Collapsed;
                });
        }

        /// <summary>
        /// Update the position and size of the rectangle that user is dragging out.
        /// </summary>
        internal void SetDragRect(Point pt1, Point pt2)
        {
            double x, y, width, height;

            // Determine x, y, width and height of the rect and invert the points if necessary.
            if (pt2.X < pt1.X)
            {
                x = pt2.X;
                width = pt1.X - pt2.X;
            }
            else
            {
                x = pt1.X;
                width = pt2.X - pt1.X;
            }

            if (pt2.Y < pt1.Y)
            {
                y = pt2.Y;
                height = pt1.Y - pt2.Y;
            }
            else
            {
                y = pt1.Y;
                height = pt2.Y - pt1.Y;
            }

            // Update the coordinates of the rectangle that is being dragged out by the user.
            // Then offset and rescale to convert from content coordinates.
            Canvas.SetLeft(dragZoomBorder, x);
            Canvas.SetTop(dragZoomBorder, y);
            dragZoomBorder.Width = width;
            dragZoomBorder.Height = height;
        }

        #endregion
        #region options menu item

        private void ShowOptions_Enabled(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
            e.Handled = true;
        }

        private void ShowOptions_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            OptionsDialogEventArgs ex = new OptionsDialogEventArgs(this.Designer.SelectionService.CurrentSelection.ToArray());
            if (this.OptionsDialogExecuted != null) this.OptionsDialogExecuted.Invoke(this, ex);
        }

        #endregion

        #endregion
        #region util

        private void SetNormalState()
        {
            Cursor = Cursors.Arrow;
            SetGraphState(MouseUsageStates.Normal, false);
        }

        /// <summary>
        /// Update the Bindings
        /// </summary>
        public void RefreshData()
        {
            foreach (var child in this.Designer.Children.OfType<DependencyObject>())
            {
                child.UpdateAllTargets();
            }
        }

        #endregion
    }
}