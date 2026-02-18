using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MatthiasToolbox.Presentation.Controls
{
    /// <summary>
    /// A zoom- and draggable image container.
    /// </summary>
    public partial class ImageBox : UserControl
    {
        #region cvar

        private Point origin;
        private Point start;

        private TransformGroup imageTransform;
        private MatrixTransform imageScale;
        private Matrix scaleMatrix = Matrix.Identity;
        private TranslateTransform imageTranslate;

        #endregion
        #region prop

        ///// <summary>
        ///// If set to true, causes the image to be resized to 
        ///// the container size if the image source is set or 
        ///// changed. Note: If the image source is set during your
        ///// window constructor or in the xaml code, this won't work
        ///// because the window's size will not be set yet. Use
        ///// AutoAdjustSize() to manually adjust the size.
        ///// Default setting is true
        ///// </summary>
        //public bool AutoSize { get; set; }

        /// <summary>
        /// Get or set a bitmap source for the contained image.
        /// </summary>
        public BitmapSource BitmapSource
        {
            get { return image1.Source as BitmapSource; }
            set
            {
                image1.Source = value;
            }
        }

        /// <summary>
        /// Get or set an image source for the contained image.
        /// </summary>
        public ImageSource ImageSource
        {
            get { return image1.Source; }
            set
            {
                image1.Source = value;
            }
        }

        /// <summary>
        /// Get or set the opacity of the contained image.
        /// </summary>
        public double ImageOpacity
        {
            get { return image1.Opacity; }
            set { image1.Opacity = value; }
        }

        #endregion
        #region ctor

        /// <summary>
        /// Default Constructor
        /// </summary>
        public ImageBox()
        {
            InitializeComponent();
            // AutoSize = true;
            RenderOptions.SetBitmapScalingMode(image1, BitmapScalingMode.HighQuality);

            imageTransform = new TransformGroup();
            imageScale = new MatrixTransform(scaleMatrix);
            imageTranslate = new TranslateTransform();

            image1.RenderTransformOrigin = new Point(0, 0);

            // order sensitive!
            imageTransform.Children.Add(imageScale);
            imageTransform.Children.Add(imageTranslate);

            image1.RenderTransform = imageTransform;

            image1.MouseWheel += MouseWheelHandler;
            image1.MouseLeftButtonDown += MouseLeftButtonDownHandler;
            image1.MouseLeftButtonUp += MouseLeftButtonUpHandler;
            image1.MouseMove += MouseMoveHandler;
        }

        #endregion
        #region hand

        private void MouseWheelHandler(object sender, MouseWheelEventArgs e)
        {
            Point mousePosition = e.GetPosition(image1);
            double zoomChange = e.Delta > 0 ? 1.2 : 0.8;
            
            scaleMatrix.ScaleAtPrepend(zoomChange, zoomChange, mousePosition.X, mousePosition.Y);
            imageScale.Matrix = scaleMatrix;
        }

        private void MouseMoveHandler(object sender, MouseEventArgs e)
        {
            if (!image1.IsMouseCaptured) return;

            Vector v = start - e.GetPosition(scroller1);
            imageTranslate.X = origin.X - v.X;
            imageTranslate.Y = origin.Y - v.Y;
        }

        private void MouseLeftButtonDownHandler(object sender, MouseButtonEventArgs e)
        {
            image1.CaptureMouse();
            start = e.GetPosition(scroller1);
            origin = new Point(imageTranslate.X, imageTranslate.Y);
        }

        private void MouseLeftButtonUpHandler(object sender, MouseButtonEventArgs e)
        {
            image1.ReleaseMouseCapture();
        }

        #endregion
        #region impl

        /// <summary>
        /// Load the image from the given URI. Example:
        /// "pack://application:,,,/MyRootNamespace;component/Resources/MyImage.jpg";
        /// </summary>
        /// <param name="uri">A valid unified resource identifier</param>
        /// <returns>A succes flag</returns>
        public bool LoadFromURI(string uri)
        {
            try
            {
                image1.Source = new ImageSourceConverter().ConvertFromString(uri) as ImageSource;
                return true;
            } 
            catch 
            {
                return false;
            }
        }

        #endregion
    }
}