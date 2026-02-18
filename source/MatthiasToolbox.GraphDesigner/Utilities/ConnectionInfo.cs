using System.Windows;
using System.Windows.Controls;
using MatthiasToolbox.GraphDesigner.DiagramDesigner;
using MatthiasToolbox.GraphDesigner.Enumerations;

namespace MatthiasToolbox.GraphDesigner.Utilities
{
    /// <summary>
    /// Provides compact info about the end of a connection; used for the routing algorithm
    /// </summary>
    public struct ConnectionInfo
    {
        public DesignerItem Item { get; set; }
        public double DesignerItemLeft { get; set; }
        public double DesignerItemTop { get; set; }
        public Size DesignerItemSize { get; set; }

        public Point DesignerItemCenter { get; set; }

        /// <summary>
        /// Relative Position of the connection start point
        /// </summary>
        public Point Position { get; set; }
        public ConnectorOrientation Orientation { get; set; }
        public ArrowSymbol Cap { get; set; }

        public ConnectionInfo(DesignerItem item) : this()
        {
            Item = item;
            DesignerItemSize = new Size(item.ActualWidth, item.ActualHeight);
            DesignerItemLeft = Canvas.GetLeft(item);
            DesignerItemTop = Canvas.GetTop(item);
            DesignerItemCenter = new Point(DesignerItemLeft + DesignerItemSize.Width / 2, DesignerItemTop + DesignerItemSize.Height / 2);
            Cap = ArrowSymbol.None;
            Position = new Point(0,0);
            Orientation = ConnectorOrientation.None;
        }
    }
}
