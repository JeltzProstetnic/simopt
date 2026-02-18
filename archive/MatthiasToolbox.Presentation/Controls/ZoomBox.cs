using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using MatthiasToolbox.Presentation.Converters;

namespace MatthiasToolbox.Presentation.Controls
{
    public class ZoomBox : Control
    {
        #region cvar

        /// <summary>
        /// Rectangle to mark the current displayed part of the DesignerCanvas
        /// </summary>

        private Rectangle _borderRectangle;
        private bool isDragging;

        #region zoom

        private double _prevZoomScale;
        private Rect _prevZoomRect;
        private bool _prevZoomRectSet;

        #endregion

        #region PARTs

        private Thumb _zoomThumb;
        private Canvas _zoomCanvas;
        private Slider _zoomSlider;

        private Button PART_buttonZoomRectangle;
        private Button PART_buttonZoomOut;
        private Button PART_buttonZoomIn;
        private Button PART_button100;
        private Button PART_buttonFill;

        #endregion
        #region external controls

        private ZoomPanControl _zoomPanControl;

        #endregion
        #endregion cvar
        #region events

        public static RoutedEvent ZoomRectangleClickEvent =
            EventManager.RegisterRoutedEvent("ZoomRectangleClick", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(ZoomBox));

        public event RoutedEventHandler ZoomRectangleClick
        {
            add { AddHandler(ZoomRectangleClickEvent, value); }
            remove { RemoveHandler(ZoomRectangleClickEvent, value); }
        }

        #endregion
        #region prop
        public ScrollViewer ScrollViewer
        {
            get { return (ScrollViewer)GetValue(ScrollViewerProperty); }
            set { SetValue(ScrollViewerProperty, value); }
        }

        public static readonly DependencyProperty ScrollViewerProperty =
            DependencyProperty.Register("ScrollViewer", typeof(ScrollViewer), typeof(ZoomBox), new PropertyMetadata(OnScrollViewerPropertyChanged));



        public FrameworkElement VisualBrushControl
        {
            get { return (FrameworkElement)GetValue(VisualBrushControlProperty); }
            set { SetValue(VisualBrushControlProperty, value); }
        }

        // Using a DependencyProperty as the backing store for VisualBrushControl.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty VisualBrushControlProperty =
            DependencyProperty.Register("VisualBrushControl", typeof(FrameworkElement), typeof(ZoomBox), new UIPropertyMetadata(null));

        protected Rectangle BorderRectangle
        {
            get
            {
                if (this._borderRectangle == null)
                {
                    this._borderRectangle = new Rectangle();
                    this._borderRectangle.Stroke = new SolidColorBrush(Colors.Black);
                    this._zoomCanvas.Children.Add(this._borderRectangle);
                }
                return this._borderRectangle;
            }
            set
            {
                this._borderRectangle = value;
            }
        }

        #endregion prop
        #region ctor

        static ZoomBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ZoomBox), new FrameworkPropertyMetadata(typeof(ZoomBox)));
        }

        #endregion
        #region impl
        #region initialize

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            Initialize();
        }

        private void Initialize()
        {
            if (this.ScrollViewer == null)
                return;

            ContentControl viewerControl = this.ScrollViewer;

            this._zoomPanControl = viewerControl.FindVisualChild<ZoomPanControl>();
            if (this._zoomPanControl == null)
                throw new Exception("ZoomPanControl must not be null!");

            this.VisualBrushControl = this._zoomPanControl.Content as FrameworkElement;
            if (this.VisualBrushControl == null)
                throw new Exception("The ZoomPanControl has no Content");

            this._zoomThumb = Template.FindName("PART_ZoomThumb", this) as Thumb;
            if (this._zoomThumb == null)
                throw new Exception("PART_ZoomThumb template is missing!");

            this._zoomCanvas = Template.FindName("PART_ZoomCanvas", this) as Canvas;
            if (this._zoomCanvas == null)
                throw new Exception("PART_ZoomCanvas template is missing!");

            //zoom slider
            this._zoomSlider = Template.FindName("PART_ZoomSlider", this) as Slider;
            if (this._zoomSlider == null)
                throw new Exception("PART_ZoomSlider template is missing!");

            //create slider binding
            Binding binding = new Binding("ContentScale");
            binding.Source = this._zoomPanControl;
            binding.Converter = new ScaleToPercentConverter();
            _zoomSlider.SetBinding(Slider.ValueProperty, binding);

            //listen to changes of the designer and zoom pan control to render the border and view-rectangle correctly
            //layoutupated is called too often
            //this._zoomPanControl.LayoutUpdated += new EventHandler(this.DesignerCanvas_LayoutUpdated);
            this._zoomPanControl.ContentScaleChanged += new EventHandler(ZoomBox_Rendered);
            this._zoomPanControl.ContentOffsetXChanged += new EventHandler(ZoomBox_Rendered);
            this._zoomPanControl.ContentOffsetYChanged += new EventHandler(ZoomBox_Rendered);
            this._zoomPanControl.SizeChanged += new SizeChangedEventHandler(ZoomBox_Rendered);
            VisualBrushControl.SizeChanged += new SizeChangedEventHandler(ZoomBox_Rendered);

            //listen to the sizechanged event, occuring after rendering the component
            this._zoomCanvas.SizeChanged += new SizeChangedEventHandler(ZoomCanvas_SizeChanged);

            //listen drag events
            this._zoomThumb.DragDelta += new DragDeltaEventHandler(this.Thumb_DragDelta);
            this._zoomThumb.DragStarted += new DragStartedEventHandler(_zoomThumb_DragStarted);
            this._zoomThumb.DragCompleted += new DragCompletedEventHandler(_zoomThumb_DragCompleted);

            //listen to zoom buttons
            PART_buttonFill = Template.FindName("PART_buttonFill", this) as Button;
            if (PART_buttonFill != null)
            {
                PART_buttonFill.Click += new RoutedEventHandler(PART_buttonFill_Click);
            }

            PART_button100 = Template.FindName("PART_button100", this) as Button;
            if (PART_button100 != null)
                PART_button100.Click += new RoutedEventHandler(PART_button100_Click);

            PART_buttonZoomIn = Template.FindName("PART_buttonZoomIn", this) as Button;
            if (PART_buttonZoomIn != null)
                PART_buttonZoomIn.Click += new RoutedEventHandler(PART_buttonZoomIn_Click);

            PART_buttonZoomOut = Template.FindName("PART_buttonZoomOut", this) as Button;
            if (PART_buttonZoomOut != null)
                PART_buttonZoomOut.Click += new RoutedEventHandler(PART_buttonZoomOut_Click);

            PART_buttonZoomRectangle = Template.FindName("PART_buttonZoomRectangle", this) as Button;
            if (PART_buttonZoomRectangle != null)
                PART_buttonZoomRectangle.Click += new RoutedEventHandler(PART_buttonZoomRectangle_Click);

            UpdateZoomBox();
        }


        #endregion
        #region zoom

        #region zoom evnt

        private static void OnScrollViewerPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ZoomBox zoomBox = d as ZoomBox;
            if (zoomBox != null) zoomBox.Initialize();
        }

        void PART_buttonZoomRectangle_Click(object sender, RoutedEventArgs e)
        {
            if (ZoomRectangleClickEvent == null)
                return;
            RoutedEventArgs args = new RoutedEventArgs(ZoomRectangleClickEvent, this);
            RaiseEvent(args);
        }

        void PART_buttonZoomOut_Click(object sender, RoutedEventArgs e)
        {
            ZoomOut(new Point(this._zoomPanControl.ContentZoomFocusX, this._zoomPanControl.ContentZoomFocusY));
        }

        void PART_buttonZoomIn_Click(object sender, RoutedEventArgs e)
        {
            ZoomIn(new Point(_zoomPanControl.ContentZoomFocusX, _zoomPanControl.ContentZoomFocusY));
        }

        private void PART_button100_Click(object sender, RoutedEventArgs e)
        {
            SavePrevZoomRect();
            _zoomPanControl.AnimatedZoomTo(1.0);
        }

        void PART_buttonFill_Click(object sender, RoutedEventArgs e)
        {
            SavePrevZoomRect();
            _zoomPanControl.AnimatedScaleToFit();
        }
        #endregion

        /// <summary>
        /// Record the previous zoom level, so that we can jump back to it when the backspace key is pressed.
        /// </summary>
        private void SavePrevZoomRect()
        {
            _prevZoomRect = new Rect(_zoomPanControl.ContentOffsetX, _zoomPanControl.ContentOffsetY, _zoomPanControl.ContentViewportWidth, _zoomPanControl.ContentViewportHeight);
            _prevZoomScale = _zoomPanControl.ContentScale;
            _prevZoomRectSet = true;
        }

        /// <summary>
        /// Jump back to the previous zoom level.
        /// </summary>
        private void JumpBackToPrevZoom()
        {
            _zoomPanControl.AnimatedZoomTo(_prevZoomScale, _prevZoomRect);
            ClearPrevZoomRect();
        }

        /// <summary>
        /// Clear the memory of the previous zoom level.
        /// </summary>
        private void ClearPrevZoomRect()
        {
            _prevZoomRectSet = false;
        }

        /// <summary>
        /// Zoom the viewport out, centering on the specified point (in content coordinates).
        /// </summary>
        internal void ZoomOut(Point contentZoomCenter)
        {
            _zoomPanControl.ZoomAboutPoint(_zoomPanControl.ContentScale - 0.1, contentZoomCenter);
        }

        /// <summary>
        /// Zoom the viewport in, centering on the specified point (in content coordinates).
        /// </summary>
        internal void ZoomIn(Point contentZoomCenter)
        {
            _zoomPanControl.ZoomAboutPoint(_zoomPanControl.ContentScale + 0.1, contentZoomCenter);
        }

        #endregion
        #region thumb

        /// <summary>
        /// Thumb dragging event. Updates the scrollviewer.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.Primitives.DragDeltaEventArgs"/> instance containing the event data.</param>
        private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double scale, xOffset, yOffset, scaledWidthDesigner, scaledHeightDesigner;
            this.InvalidateScale(out scale, out xOffset, out yOffset, out scaledWidthDesigner, out scaledHeightDesigner);

            //update scrollviewr ViewPort position
            this.ScrollViewer.ScrollToHorizontalOffset(this.ScrollViewer.HorizontalOffset + e.HorizontalChange / scale);
            this.ScrollViewer.ScrollToVerticalOffset(this.ScrollViewer.VerticalOffset + e.VerticalChange / scale);

            //update thumb position
            double left = Canvas.GetLeft(this._zoomThumb) + e.HorizontalChange;
            double top = Canvas.GetTop(this._zoomThumb) + e.VerticalChange;
            double right = left + this._zoomThumb.ActualWidth;
            double bottom = top + this._zoomThumb.ActualHeight;

            Rectangle borderRect = this.BorderRectangle;
            double leftBoundary = 0;
            double rightBoundary = this._zoomCanvas.ActualWidth;
            if (this._zoomThumb.Width < borderRect.ActualWidth)
            {
                leftBoundary = Canvas.GetLeft(borderRect);
                rightBoundary = leftBoundary + borderRect.ActualWidth;
            }
            double topBoundary = 0;
            double bottomBoundary = this._zoomCanvas.ActualHeight;
            if (_zoomThumb.ActualHeight < borderRect.ActualHeight)
            {
                topBoundary = Canvas.GetTop(borderRect);
                bottomBoundary = topBoundary + borderRect.ActualHeight;
            }


            if (left < leftBoundary)
                left = leftBoundary;
            if (right > rightBoundary)
                left = rightBoundary - this._zoomThumb.ActualWidth;
            if (top < topBoundary)
                top = topBoundary;
            if (bottom > bottomBoundary)
                top = bottomBoundary - this._zoomThumb.ActualHeight;

            Canvas.SetLeft(this._zoomThumb, left);
            Canvas.SetTop(this._zoomThumb, top);
        }

        private void _zoomThumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            this.isDragging = false;
        }

        private void _zoomThumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            this.isDragging = true;
        }

        #endregion
        #region designer update

        /// <summary>
        /// Handles the SizeChanged event of the ZoomCanvas control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="eventArgs">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        void ZoomCanvas_SizeChanged(object sender, EventArgs eventArgs)
        {
            UpdateZoomBox();
        }

        /// <summary>
        /// Update the thumb size and positions.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void ZoomBox_Rendered(object sender, EventArgs e)
        {
            if (isDragging)
                return;

            UpdateZoomBox();
        }

        /// <summary>
        /// Update the thumb size and positions. Draw the Border correctly.
        /// </summary>
        private void UpdateZoomBox()
        {
            double scale, xOffset, yOffset, scaledWidthDesigner, scaledHeightDesigner;
            this.InvalidateScale(out scale, out xOffset, out yOffset, out scaledWidthDesigner, out scaledHeightDesigner);

            DrawBorder(xOffset, yOffset, scaledWidthDesigner, scaledHeightDesigner);

            //calculate thumb position
            double left = xOffset + (this._zoomPanControl.HorizontalOffset * scale) / _zoomPanControl.ContentScale;
            double top = yOffset + (this._zoomPanControl.VerticalOffset * scale) / _zoomPanControl.ContentScale;

            //viewport size can be higher than the designer size
            //calculate thumb size
            double x, y, width, height;
            //if the view > the designer scaled size => adjust the offset and the thumb size
            if (this._zoomPanControl.ViewportWidth > this.VisualBrushControl.ActualWidth * _zoomPanControl.ContentScale)
            {
                x = this.VisualBrushControl.ActualWidth;
                width = (x * scale) / this._zoomPanControl.ContentScale;

                //center the thumb or adjust its size
                if (this._zoomCanvas.ActualWidth > width)
                {
                    left = (this._zoomCanvas.ActualWidth - width) / 2;
                }
                else
                {
                    left = 0;
                    width = this._zoomCanvas.ActualWidth;
                }
            }
            else
            {
                x = this._zoomPanControl.ViewportWidth;
                width = (x * scale) / this._zoomPanControl.ContentScale;
            }

            //if the view > the designer scaled size => adjust the offset and the thumb size
            if (this._zoomPanControl.ViewportHeight > this.VisualBrushControl.ActualHeight * _zoomPanControl.ContentScale)
            {
                y = this.VisualBrushControl.ActualHeight;
                height = (y * scale) / this._zoomPanControl.ContentScale;

                //center the thumb or adjust its size
                if (this._zoomCanvas.ActualHeight > height)
                {
                    top = (this._zoomCanvas.ActualHeight - height) / 2;
                }
                else
                {
                    top = 0;
                    height = this._zoomCanvas.ActualHeight;
                }
            }
            else
            {
                y = this._zoomPanControl.ViewportHeight;
                height = (y * scale) / this._zoomPanControl.ContentScale;
            }

            this._zoomThumb.Width = width;
            this._zoomThumb.Height = height;

            Canvas.SetLeft(this._zoomThumb, left);
            Canvas.SetTop(this._zoomThumb, top);
        }

        private void InvalidateScale(out double scale, out double xOffset, out double yOffset, out double scaledWidthDesigner, out double scaledHeightDesigner)
        {
            // designer canvas size
            double w = this.VisualBrushControl.ActualWidth;
            double h = this.VisualBrushControl.ActualHeight;

            // zoom canvas size
            double x = this._zoomCanvas.ActualWidth;
            double y = this._zoomCanvas.ActualHeight;

            double scaleX = x / w;
            double scaleY = y / h;
            scale = scaleX < scaleY ? scaleX : scaleY;

            scaledWidthDesigner = w * scale;
            scaledHeightDesigner = h * scale;

            xOffset = (x - scale * w) / 2;
            yOffset = (y - scale * h) / 2;
        }

        /// <summary>
        /// Draw a Border marking the corners of the designercanvas
        /// </summary>
        /// <param name="xOffset"></param>
        /// <param name="yOffset"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        private void DrawBorder(double xOffset, double yOffset, double width, double height)
        {
            Rectangle rect = this.BorderRectangle;
            rect.Width = width;
            rect.Height = height;
            rect.StrokeThickness = 2;
            Canvas.SetLeft(rect, xOffset);
            Canvas.SetTop(rect, yOffset);
        }
        #endregion
        #endregion impl
    }
}
