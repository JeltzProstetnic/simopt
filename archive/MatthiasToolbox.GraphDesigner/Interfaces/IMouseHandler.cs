using System.Windows.Input;

namespace MatthiasToolbox.GraphDesigner.Interfaces
{
    /// <summary>
    /// State Interface for the different Cursor types in the DesignerCanvas
    /// </summary>
    public interface IMouseHandler
    {
        /// <summary>
        /// Handles the <see cref="System.Windows.Input.Mouse.MouseDown"/> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs"/> that contains the event data.</param>
        void MouseDown(object sender, MouseButtonEventArgs e);

        /// <summary>
        /// Handles the <see cref="System.Windows.Input.Mouse.MouseUp"/> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs"/> that contains the event data.</param>
        void MouseUp(object sender, MouseButtonEventArgs e);

        /// <summary>
        /// Handles the <see cref="System.Windows.Input.Mouse.MouseMove"/> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs"/> that contains the event data.</param>
        void MouseMove(object sender, MouseEventArgs e);

        void MouseDoubleClick(object sender, MouseButtonEventArgs e);
    }
}
