using MatthiasToolbox.GraphDesigner.Controls;
using MatthiasToolbox.GraphDesigner.Interfaces;
using System.Windows;
using MatthiasToolbox.Basics.Interfaces;
using System.Windows.Media;
using MatthiasToolbox.Basics.Datastructures.Graph;

namespace MatthiasToolbox.GraphDesigner.Factory
{
    public class DefaultNodeFactory : INodeFactory
    {
        public virtual IVertex<Point> CreateVertex(IVertex<Point> template, Point position, Size? size)
        {
            IVertex<Point> vertex;

            if (template is ITemplate<IVertex<Point>>)
                vertex = (template as ITemplate<IVertex<Point>>).CreateDefaultInstance();
            else
                vertex = template;

            if (template is IColors<Color> && vertex is IColors<Color>)
            {
                ((IColors<Color>)vertex).BackgroundColor = (template as IColors<Color>).BackgroundColor;
                ((IColors<Color>)vertex).ForegroundColor = (template as IColors<Color>).ForegroundColor;
            }
            else if (template is IColors<System.Drawing.Color>)
            {
                ((IColors<Color>)vertex).BackgroundColor = Color.FromArgb(
                    (template as IColors<System.Drawing.Color>).BackgroundColor.A,
                    (template as IColors<System.Drawing.Color>).BackgroundColor.R,
                    (template as IColors<System.Drawing.Color>).BackgroundColor.G,
                    (template as IColors<System.Drawing.Color>).BackgroundColor.B
                    );
                ((IColors<Color>)vertex).ForegroundColor = Color.FromArgb(
                    (template as IColors<System.Drawing.Color>).ForegroundColor.A,
                    (template as IColors<System.Drawing.Color>).ForegroundColor.R,
                    (template as IColors<System.Drawing.Color>).ForegroundColor.G,
                    (template as IColors<System.Drawing.Color>).ForegroundColor.B
                    );
            }

            return vertex;
        }

        public virtual UIElement CreateUIElement(IVertex<Point> vertex, IVertex<Point> template) 
        {
            NodeControl c = new NodeControl(vertex);

            if (template is ITooltip) c.ToolTip = (template as ITooltip).TooltipText;
            //if (template is ITitle) c.Title = (template as ITitle).Title;
            //if (template is IText) c.Text = (template as IText).Text;

            

            return c;
        }
    }
}