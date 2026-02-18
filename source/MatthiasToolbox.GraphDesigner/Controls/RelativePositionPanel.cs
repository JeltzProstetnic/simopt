using System;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;

namespace MatthiasToolbox.GraphDesigner.Controls
{
    /// <summary>
    /// A Panel that allows it's children to be aligned using relative points (0.0, 0.0) to (1.0, 1.0).
    /// 
    /// This Panel is intended to be used to position small controls on a size changing parent.
    /// </summary>
    public class RelativePositionPanel : Panel
    {
        #region RelativePostion Extension
        
        public static readonly DependencyProperty RelativePositionProperty =
            DependencyProperty.RegisterAttached("RelativePosition", typeof(Point), typeof(RelativePositionPanel),
            new FrameworkPropertyMetadata(new Point(0, 0),
                                          new PropertyChangedCallback(RelativePositionPanel.OnRelativePositionChanged)));

        public static Point GetRelativePosition(UIElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (Point)element.GetValue(RelativePositionProperty);
        }

        public static void SetRelativePosition(UIElement element, Point value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(RelativePositionProperty, value);
        }

        private static void OnRelativePositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement reference = d as UIElement;
            if (reference != null)
            {
                RelativePositionPanel parent = VisualTreeHelper.GetParent(reference) as RelativePositionPanel;
                if (parent != null)
                {
                    parent.InvalidateArrange();
                }
            }
        }

        #endregion
        #region measure / arrange
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            foreach (UIElement element in base.InternalChildren)
            {
                if (element != null)
                {
                    Point relPosition = GetRelativePosition(element);
                    Point relativePosition = GetAbsolutePosition(arrangeSize, element.DesiredSize, relPosition);
                    
                    element.Arrange(new Rect(relativePosition, element.DesiredSize));
                }
            }
            return arrangeSize;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            Size size = new Size(double.PositiveInfinity, double.PositiveInfinity);

            // SDK docu says about InternalChildren Property: 'Classes that are derived from Panel 
            // should use this property, instead of the Children property, for internal overrides 
            // such as MeasureCore and ArrangeCore.

            foreach (UIElement element in this.InternalChildren)
            {
                if (element != null)
                    element.Measure(size);
            }

            return base.MeasureOverride(availableSize);
        }

        #endregion
        #region helpers

        /// <summary>
        /// Gets the absolute position inside the parent panel.
        /// </summary>
        /// <param name="parentSize">Size of the parent.</param>
        /// <param name="childSize">Size of the child.</param>
        /// <param name="relativePosition">The relative position.</param>
        /// <returns></returns>
        public static Point GetAbsolutePosition(Size parentSize, Size childSize, Point relativePosition)
        {
            
            double x = (parentSize.Width - childSize.Width) * relativePosition.X;
            double y = (parentSize.Height - childSize.Height) * relativePosition.Y;

            if (double.IsNaN(x)) x = 0;
            if (double.IsNaN(y)) y = 0;

            return new Point(x, y);
        }
        #endregion
    }
}
