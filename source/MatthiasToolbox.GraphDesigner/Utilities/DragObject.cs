using System;
using System.Windows;

namespace MatthiasToolbox.GraphDesigner.Utilities
{
    /// <summary>
    /// Wraps info of the dragged object into a class
    /// </summary>
    public class DragObject
    {
        /// <summary>
        /// Defines width and height of the DesignerItem
        /// when this DragObject is dropped on the DesignerCanvas
        /// </summary>
        public Size? DesiredSize { get; set; }

        /// <summary>
        /// Xaml string that represents the serialized content
        /// </summary>
        public String Xaml { get; set; }

        /// <summary>
        /// Gets or sets the data context of the dragged shape.
        /// </summary>
        /// <value>The data context.</value>
        public object DataContext { get; set; }
    }
}
