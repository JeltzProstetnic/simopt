using MatthiasToolbox.Basics.Datastructures.Graph;
using MatthiasToolbox.GraphDesigner.DiagramDesigner;
using MatthiasToolbox.Presentation.Controls.DiagramDesigner;
using System.Windows;
using MatthiasToolbox.GraphDesigner.Enumerations;

namespace MatthiasToolbox.GraphDesigner.Interfaces
{
    ///<summary>
    /// Edge UIElements factory.
    ///</summary>
    public interface IEdgeFactory
    {
        Connection CreateUIElement(IEdge<Point> edge, DesignerItem item1, DesignerItem item2, PathRouting routing = PathRouting.Direct);
        IEdge<Point> CreateEdge(IEdge<Point> template, IVertex<Point> source, IVertex<Point> target);
    }
}