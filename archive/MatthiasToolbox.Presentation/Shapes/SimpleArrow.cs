using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace MatthiasToolbox.Presentation.Shapes
{
    public sealed class SimpleArrow : Shape
    {
        #region over

        /// <summary>
        /// Geometry
        /// </summary>
        protected override Geometry DefiningGeometry
        {
            get
            {
                // Create a StreamGeometry for describing the shape
                StreamGeometry geometry = new StreamGeometry();
                geometry.FillRule = FillRule.EvenOdd;

                using (StreamGeometryContext context = geometry.Open())
                {
                    InternalDrawArrowGeometry(context);
                }

                // Freeze the geometry for performance benefits
                geometry.Freeze();

                return geometry;
            }
        }

        #endregion
        #region prop

        #region Dependency Properties

        /// <summary>
        /// X1
        /// </summary>
        public static readonly DependencyProperty X1Property = DependencyProperty.Register("X1", typeof(double), typeof(SimpleArrow), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));
        
        /// <summary>
        /// Y1
        /// </summary>
        public static readonly DependencyProperty Y1Property = DependencyProperty.Register("Y1", typeof(double), typeof(SimpleArrow), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));
        
        /// <summary>
        /// X2
        /// </summary>
        public static readonly DependencyProperty X2Property = DependencyProperty.Register("X2", typeof(double), typeof(SimpleArrow), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));
        
        /// <summary>
        /// Y2
        /// </summary>
        public static readonly DependencyProperty Y2Property = DependencyProperty.Register("Y2", typeof(double), typeof(SimpleArrow), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));
        
        /// <summary>
        /// Arrowhead
        /// </summary>
        public static readonly DependencyProperty HeadWidthProperty = DependencyProperty.Register("HeadWidth", typeof(double), typeof(SimpleArrow), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));
        
        /// <summary>
        /// Arrowhead
        /// </summary>
        public static readonly DependencyProperty HeadHeightProperty = DependencyProperty.Register("HeadHeight", typeof(double), typeof(SimpleArrow), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure)); 

		#endregion
		#region CLR Properties

        /// <summary>
        /// X1
        /// </summary>
		[TypeConverter(typeof(LengthConverter))]
		public double X1
		{
			get { return (double)base.GetValue(X1Property); }
			set { base.SetValue(X1Property, value); }
		}

        /// <summary>
        /// Y1
        /// </summary>
		[TypeConverter(typeof(LengthConverter))]
		public double Y1
		{
			get { return (double)base.GetValue(Y1Property); }
			set { base.SetValue(Y1Property, value); }
		}

        /// <summary>
        /// X2
        /// </summary>
		[TypeConverter(typeof(LengthConverter))]
		public double X2
		{
			get { return (double)base.GetValue(X2Property); }
			set { base.SetValue(X2Property, value); }
		}

        /// <summary>
        /// Y2
        /// </summary>
		[TypeConverter(typeof(LengthConverter))]
		public double Y2
		{
			get { return (double)base.GetValue(Y2Property); }
			set { base.SetValue(Y2Property, value); }
		}

        /// <summary>
        /// Arrowhead
        /// </summary>
		[TypeConverter(typeof(LengthConverter))]
		public double HeadWidth
		{
			get { return (double)base.GetValue(HeadWidthProperty); }
			set { base.SetValue(HeadWidthProperty, value); }
		}

        /// <summary>
        /// Arrowhead
        /// </summary>
		[TypeConverter(typeof(LengthConverter))]
		public double HeadHeight
		{
			get { return (double)base.GetValue(HeadHeightProperty); }
			set { base.SetValue(HeadHeightProperty, value); }
		}

		#endregion

        #endregion
        #region ctor
        
        public SimpleArrow() { }

        public SimpleArrow(Point source, Point target)
        {
            X1 = source.X;
            Y1 = source.Y;
            X2 = target.X;
            Y2 = target.Y;
        }

        #endregion
        #region impl

        private void InternalDrawArrowGeometry(StreamGeometryContext context)
		{
			double theta = Math.Atan2(Y1 - Y2, X1 - X2);
			double sint = Math.Sin(theta);
			double cost = Math.Cos(theta);

			Point pt1 = new Point(X1, this.Y1);
			Point pt2 = new Point(X2, this.Y2);

			Point pt3 = new Point(
				X2 + (HeadWidth * cost - HeadHeight * sint),
				Y2 + (HeadWidth * sint + HeadHeight * cost));

			Point pt4 = new Point(
				X2 + (HeadWidth * cost + HeadHeight * sint),
				Y2 - (HeadHeight * cost - HeadWidth * sint));

			context.BeginFigure(pt1, true, false);
			context.LineTo(pt2, true, true);
			context.LineTo(pt3, true, true);
			context.LineTo(pt2, true, true);
			context.LineTo(pt4, true, true);
		}
		
		#endregion
	}
}