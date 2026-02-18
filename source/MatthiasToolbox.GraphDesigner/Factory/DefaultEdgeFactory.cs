using System.Windows;
using MatthiasToolbox.Basics.Datastructures.Graph;
using MatthiasToolbox.GraphDesigner.DiagramDesigner;
using MatthiasToolbox.GraphDesigner.Interfaces;
using MatthiasToolbox.Basics.Interfaces;
using MatthiasToolbox.GraphDesigner.Enumerations;
using System.Windows.Media;

namespace MatthiasToolbox.GraphDesigner.Factory
{
    /// <summary>
    /// Edge UIElement factory, creates <see cref="Connection"/>
    /// </summary>
    public class DefaultEdgeFactory : IEdgeFactory
    {
        #region IEdgeFactory

        public virtual IEdge<Point> CreateEdge(IEdge<Point> template, IVertex<Point> source, IVertex<Point> target)
        {
            if (template is ITemplate<IEdge<Point>>)
                return (template as ITemplate<IEdge<Point>>).CreateDefaultInstance();
            else
                return template;
        }

        public virtual Connection CreateUIElement(IEdge<Point> edge, DesignerItem item1, DesignerItem item2, PathRouting routing = PathRouting.Direct)
        {
            Connection c = new Connection(item1, item2);
            UpdateConnectionFromEdge(c, edge, routing);

            return c;
        }

        #endregion
        #region impl

        protected virtual void UpdateConnectionFromEdge(Connection c, IEdge<Point> edge, PathRouting routing)
        {
            c.Edge = edge;
            c.Routing = routing;

            if (edge is IThickness<Thickness>)
                c.LineThickness = (edge as IThickness<Thickness>).Thickness.Top;
            else if (edge is IThickness)
                c.LineThickness = (edge as IThickness).Thickness;

            if (edge is ITooltip) c.ToolTip = (edge as ITooltip).TooltipText;
            //if (edge is ITitle) c.Title = (edge as ITitle).Title;

            if (edge is IColors<Color>)
            {
                c.Background = new SolidColorBrush((edge as IColors<Color>).BackgroundColor);
                c.Foreground = new SolidColorBrush((edge as IColors<Color>).ForegroundColor);
            }
            else if (edge is IColors<System.Drawing.Color>)
            {
                c.Background = new SolidColorBrush(Color.FromArgb(
                    (edge as IColors<System.Drawing.Color>).BackgroundColor.A,
                    (edge as IColors<System.Drawing.Color>).BackgroundColor.R,
                    (edge as IColors<System.Drawing.Color>).BackgroundColor.G,
                    (edge as IColors<System.Drawing.Color>).BackgroundColor.B
                                                       ));
                c.Foreground = new SolidColorBrush(Color.FromArgb(
                    (edge as IColors<System.Drawing.Color>).ForegroundColor.A,
                    (edge as IColors<System.Drawing.Color>).ForegroundColor.R,
                    (edge as IColors<System.Drawing.Color>).ForegroundColor.G,
                    (edge as IColors<System.Drawing.Color>).ForegroundColor.B
                                                       ));
            }
            else
            {
                // control needs a background for HitTest
                if (c.Background == null) c.Background = new SolidColorBrush(Colors.LightGray);
            }

        }
        #endregion
    }
}
