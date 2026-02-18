using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using MatthiasToolbox.Basics.Interfaces;

namespace MatthiasToolbox.Presentation.Events
{
    /// <summary>
    /// EventArgs for Toolbox selection events.
    /// </summary>
    internal class ToolboxEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ToolboxEventArgs"/> class.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="selectedElement">The selected element.</param>
        internal ToolboxEventArgs(RoutedEvent routedEvent, object source, Type objectType, INamedElement selectedElement)
            : base(routedEvent, source)
        {
            this.ObjectType = objectType;
            this.SelectedElement = selectedElement;
        }

        internal INamedElement SelectedElement { get; private set; }

        internal Type ObjectType { get; private set; }
    }
}
